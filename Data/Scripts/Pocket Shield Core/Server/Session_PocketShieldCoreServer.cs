// ;
using Sandbox.Game;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using ExShared;
using VRageMath;
using VRage.Utils;
using System;
using VRage.Game.Entity;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.ObjectBuilders;
using SpaceEngineers.Game.World;

namespace PocketShieldCore
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public partial class Session_PocketShieldCoreServer : MySessionComponentBase
    {
        public bool IsServer { get; private set; } = false;
        public bool IsDedicated { get; private set; } = false;

        private int m_Ticks = 0;
        private bool m_IsFirstTick = true;
        private bool m_IsSetupDone = false;

        private Logger m_Logger = null;
        private ServerConfig m_Config = null;
        private SaveDataManager m_SaveData = null;
        private ShieldManager m_ShieldManager = null;

        private List<IMyPlayer> m_CachedPlayers = new List<IMyPlayer>();
        private Dictionary<ulong, Vector3D> m_CachedPlayersPosition = new Dictionary<ulong, Vector3D>();

        private Dictionary<long, OtherCharacterShieldData> m_ShieldDamageEffects = new Dictionary<long, OtherCharacterShieldData>();

        private List<ulong> m_ForceSyncPlayers = new List<ulong>();

        private Dictionary<string, IMyCharacter> m_HookedEntities = null;

        //private List<long> m_IdToRemove = new List<long>();

        //public Session_PocketShieldCoreServer() : base()
        //{
        //    Priority = 2;
        //}

        public override void LoadData()
        {
            IsServer = MyAPIGateway.Multiplayer.IsServer;
            IsDedicated = IsServer && MyAPIGateway.Utilities.IsDedicated;

            m_Logger = new Logger("server");
            m_Logger.WriteLine("  IsServer = " + IsServer);
            m_Logger.WriteLine("  IsDedicated = " + IsDedicated);

            if (!IsServer)
                return;

            m_Config = new ServerConfig("server_config.ini", m_Logger);

            m_Logger.LogLevel = m_Config.LogLevel;

            m_Logger.WriteLine("Setting up..");

            ShieldManager.PluginModifiers = new Dictionary<MyStringHash, Dictionary<MyStringHash, float>>(MyStringHash.Comparer);
            m_ShieldManager = new ShieldManager(m_Logger);

            m_ApiBackend_RegisteredMod = new List<string>();
            m_ApiBackend_ExposedMethods = new List<Delegate>()
            {
                (Action<long, bool>)m_ShieldManager.ActivateManualShield,
                (Action<long, bool, bool>)m_ShieldManager.TurnShieldOnOff,
                (Func<List<object>, bool>)m_ShieldManager.RegisterEmitter
            };

            m_HookedEntities = new Dictionary<string, IMyCharacter>();

            m_Inventory_Plugins = new List<MyStringHash>(12); // By design, a Manual Emitter supports 4 and an Auto Emitter supports 8 Plugins;


            MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += Entities_OnEntityRemove;

            MyAPIGateway.Utilities.MessageEntered += ChatCommand_MessageEnteredHandle;
            MyAPIGateway.Utilities.RegisterMessageHandler(PocketShieldAPIV2.MOD_ID, ApiBackend_ModMessageHandle);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Constants.SYNC_ID_TO_SERVER, Sync_ReceiveDataFromClient);

            //MyAPIGateway.Projectiles.AddOnHitInterceptor(50, Projectiles_OnHitInterceptor);

            
        }

        protected override void UnloadData()
        {
            if (IsServer)
            {
                m_Logger.WriteLine("Shutting down..");
                
                foreach (string mod in m_ApiBackend_RegisteredMod)
                    m_Logger.WriteLine("  > Warning < Mod " + mod + " has not called DeInit() yet");

                m_SaveData.UnloadData();
                
                ulong syncSaved = m_Sync_SyncCalled - m_Sync_SyncPerformed;
                ulong pluginOpSaved = ShieldEmitter.PluginChangedCalled - ShieldEmitter.PluginChangedPerformed;
                m_Logger.WriteLine(string.Format(">> {0:0}/{1:0} sync operations saved ({2:0.##}%), {3:0}/{4:0} plugin processing operation saved ({5:0.##}%) <<",
                    syncSaved, m_Sync_SyncCalled, syncSaved * 100.0f / m_Sync_SyncCalled,
                    pluginOpSaved, ShieldEmitter.PluginChangedCalled, pluginOpSaved * 100.0f / ShieldEmitter.PluginChangedCalled));

                m_Logger.WriteLine("Released Hooks: " + released + "/" + hooked);

                m_HookedEntities.Clear();

                m_ShieldManager.Close();
               
                MyAPIGateway.Utilities.MessageEntered -= ChatCommand_MessageEnteredHandle;
                MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
                MyAPIGateway.Entities.OnEntityRemove -= Entities_OnEntityRemove;

                MyAPIGateway.Utilities.UnregisterMessageHandler(PocketShieldAPIV2.MOD_ID, ApiBackend_ModMessageHandle);
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Constants.SYNC_ID_TO_SERVER, Sync_ReceiveDataFromClient);

                MyAPIGateway.Projectiles.RemoveOnHitInterceptor(Projectiles_OnHitInterceptor);
                
                MyAPIGateway.Players.ItemConsumed -= Players_ItemConsumed;

                m_Logger.WriteLine("Shutdown Done");
            }

            ShieldManager.PluginModifiers = null;

            //MyAPIGateway.Entities.RemoveEntity(PSProjectileDetector.DummyEntity);
            PSProjectileDetector.DummyEntity = null;

            m_Logger.Close();
        }

        public override void BeforeStart()
        {
            if (!IsServer)
                return;

            m_SaveData = new SaveDataManager(m_ShieldManager.CharacterInfos, m_Logger);

            MyAPIGateway.Players.ItemConsumed += Players_ItemConsumed;
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(50, BeforeDamageHandler);
            
            m_Logger.WriteLine("Setup Done");

            m_IsSetupDone = true;
        }

        public override void UpdateBeforeSimulation()
        {
            if (!m_IsSetupDone)
                return;

            ++m_Ticks;
            // clear ticks count;
            if (m_Ticks >= 2000000000)
                m_Ticks -= 2000000000;

            if (m_Ticks % m_Config.ServerUpdateInterval == 0)
            {
                if (m_IsFirstTick)
                {
                    m_Logger.WriteLine("  Updating all Characters' inventory once before sim..", 1);
                    Inventory_UpdatePlayerCharacterInventoryOnceBeforeSim();

                    m_Logger.WriteLine("  Updating all Emitters once before sim..", 1);
                    m_SaveData.ApplySavedataOnceBeforeSim();

                    m_IsFirstTick = false;
                }

                UpdatePlayersCacheLists();
                Sync_SyncDataToPlayers();

                m_SaveData.Update();


            }

            if (m_Ticks % m_Config.ShieldUpdateInterval == 0)
            {
                foreach (CharacterShieldInfo charInfo in m_ShieldManager.CharacterInfos.Values)
                {
                    charInfo.Update(m_Config.ShieldUpdateInterval);
                }
            }

            if (m_Ticks % 60000 == 0)
            {
                Blueprints_UpdateBlueprintData();
                ulong syncSaved = m_Sync_SyncCalled - m_Sync_SyncPerformed;
                ulong pluginOpSaved = ShieldEmitter.PluginChangedCalled - ShieldEmitter.PluginChangedPerformed;
                m_Logger.WriteLine(string.Format(">> {0:0}/{1:0} sync operations saved ({2:0.##}%), {3:0}/{4:0} plugin processing operation saved ({5:0.##}%) <<",
                    syncSaved, m_Sync_SyncCalled, syncSaved * 100.0f / m_Sync_SyncCalled,
                    pluginOpSaved, ShieldEmitter.PluginChangedCalled, pluginOpSaved * 100.0f / ShieldEmitter.PluginChangedCalled));
            }




            // end;
        }

        public override void SaveData()
        {
            if (!IsServer)
                return;

            m_SaveData.SaveData();
        }

        static int hooked = 0;
        static int released = 0;
        private void Entities_OnEntityAdd(VRage.ModAPI.IMyEntity _entity)
        {
            m_Logger.WriteLine("New Entity added..", 5);
            IMyCharacter character = _entity as IMyCharacter;
            if (character == null)
                return;

            if (m_HookedEntities.ContainsKey(character.Name))
            {
                m_Logger.WriteLine("  Added back Character [" + Utils.GetCharacterName(character) + "] (EntitId = " + character.EntityId + ") (no new hooks)", 2);
                return;
            }

            m_Logger.WriteLine("  Added Character [" + Utils.GetCharacterName(character) + "] (EntitId = " + character.EntityId + ")", 2);
            HookEntity(character);
        }

        private void Entities_OnEntityRemove(VRage.ModAPI.IMyEntity _entity)
        {
            m_Logger.WriteLine("Removing Entity..", 5);
            IMyCharacter character = _entity as IMyCharacter;
            if (character == null)
                return;

            if (!m_HookedEntities.ContainsKey(character.Name))
            {
                m_Logger.WriteLine("  Character [" + Utils.GetCharacterName(character) + "] (EntitId = " + character.EntityId + ") haven't hooked anything (this should not happen)", 1);
            }

            if (MyAPIGateway.Entities.EntityExists(character.Name))
            {
                m_Logger.WriteLine("  Character [" + Utils.GetCharacterName(character) + "] (EntitId = " + character.EntityId + ") still exists, no unhook", 2);
                return;
            }

            m_Logger.WriteLine("  Removing Character [" + Utils.GetCharacterName(character) + "] (EntitId = " + character.EntityId + ")", 2);
            UnHookEntity(character);
        }

        private void Character_CharacterDied(IMyCharacter _character)
        {
            if (_character == null)
                return;

            m_ShieldManager.DropEmitter(_character, true);
            m_ShieldManager.DropEmitter(_character, false);

            m_ShieldManager.CharacterInfos.Remove(_character.EntityId);

            ulong playerUid = GetPlayerSteamUid(_character);
            if (playerUid == 0U)
            {
                m_Logger.WriteLine("Character [" + Utils.GetCharacterName(_character) + "] died and their ShieldEmitter has been removed", 2);

                if (m_Config.NpcInventoryOperationOnDeath == NpcInventoryOperation.NoTouch)
                    return;

                MyInventory inventory = _character.GetInventory() as MyInventory;
                if (inventory == null)
                    return;

                Inventory_ConvertDeadCharacterInventory(inventory);
                m_Logger.WriteLine("  Also all their Shield Emitters and/or Plugins have been removed", 2);
            }
            else
            {
                m_ForceSyncPlayers.Add(playerUid);
                m_Logger.WriteLine("Character [" + Utils.GetCharacterName(_character) + "] (Player <" + playerUid + ">) died and their ShieldEmitter has been removed", 2);

                // NOTE: Debug;
                //if (ConfigManager.ServerConfig.NpcInventoryOperationOnDeath == NpcInventoryOperation.NoTouch)
                //    return;
                //MyInventory inventory = _character.GetInventory() as MyInventory;
                //if (inventory == null)
                //    return;
                //ManipulateDeadCharacterInventory(inventory);
                //m_Logger.WriteLine("  Also all their Shield Emitters and/or Plugins have been removed", 2);
            }
        }


        private void Players_ItemConsumed(IMyCharacter _character, MyDefinitionId _itemDefId)
        {
            m_Logger.WriteLine("> Event triggered: " + _itemDefId.SubtypeId.String + " (name: " + _itemDefId.SubtypeName + ")", 5);
            IMyInventory inventory = _character.GetInventory();
            if (inventory == null)
                return; // this should not happens;

            foreach (PocketShieldAPIV2.ShieldEmitterProperties prop in m_ShieldManager.EmitterProperties.Values)
            {
                if (!prop.IsManual && _itemDefId.SubtypeId == prop.SubtypeId)
                {
                    m_Logger.WriteLine("  Match found: " + _itemDefId.SubtypeId, 4);

                    CharacterShieldInfo charInfo = m_ShieldManager.CharacterInfos[_character.EntityId];
                    float energy = charInfo.LastAutoEnergy;
                    bool onoff = !charInfo.LastAutoTurnedOn;
                    int itemInd = charInfo.AutoEmitterIndex;
                    m_Logger.WriteLine("  Toggled Index = " + itemInd, 4);

                    //// HACK: force update savedata;
                    //m_SaveData.SetSaveAutoShieldEnergy(_character.EntityId, energy);
                    //m_SaveData.SetSaveAutoShieldTurnedOn(_character.EntityId, onoff);

                    inventory.AddItems(1, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(_itemDefId), itemInd);
                    m_Logger.WriteLine("  Item added back at index " + itemInd, 4);

                    charInfo.AutoEmitter.Energy = energy;
                    charInfo.AutoEmitter.IsTurnedOn = onoff;

                    //m_Logger.WriteLine("  Last energy = " + m_ShieldManager_CharacterShieldManagers[_character.EntityId].LastAutoEnergy, 5);
                    //m_Logger.WriteLine("  Current energy = " + m_ShieldManager_CharacterShieldManagers[_character.EntityId].AutoEmitter?.Energy, 5);
                    return;
                }
            }
        }

        private MyProjectileInfo? pjInfo = null;

        private void Projectiles_OnHitInterceptor(ref MyProjectileInfo _projectile, ref MyProjectileHitInfo _hitInfo)
        {

#if false
            m_Logger.WriteLine("Projectile captured");
            m_Logger.WriteLine("  _projectile.Origin = " + _projectile.Origin);
            m_Logger.WriteLine("  _projectile.Position = " + _projectile.Position);
            m_Logger.WriteLine("  _projectile.MaxTrajectory = " + _projectile.MaxTrajectory);
            m_Logger.WriteLine("  _projectile.Velocity = " + _projectile.Velocity);
            m_Logger.WriteLine("  _projectile.OwnerEntity.EntityId = " + _projectile.OwnerEntity?.EntityId);

            m_Logger.WriteLine("  _hitInfo.Damage = " + _hitInfo.Damage);
            m_Logger.WriteLine("  _hitInfo.Velocity = " + _hitInfo.Velocity);
            m_Logger.WriteLine("  _hitInfo.HitEntity.EntityId = " + _hitInfo.HitEntity?.EntityId);

            m_Logger.WriteLine("  DummyEntity.EntityId = " + PSProjectileDetector.DummyEntity?.EntityId);

            if (_hitInfo.HitEntity?.EntityId == PSProjectileDetector.DummyEntity.EntityId)
            {
                pjInfo = _projectile;



            }

            MyAPIGateway.Projectiles.Add(
                _projectile.WeaponDefinition,
                _projectile.ProjectileAmmoDefinition,
                _projectile.Position,
                _projectile.Velocity,
                Vector3D.Normalize(_projectile.Position - _projectile.Origin),
                (MyEntity)_projectile.OwnerEntity,
                (MyEntity)_projectile.OwnerEntityAbsolute,
                (MyEntity)_projectile.OwnerEntity,
                new MyEntity[] { PSProjectileDetector.DummyEntity },
                false,
                _projectile.OwningPlayer);
#endif

            // TODO: implement this;
        }

        public void BeforeDamageHandler(object _target, ref MyDamageInformation _damageInfo)
        {
            //m_Logger.WriteLine("> BeforeDamageHandler is triggered <");
            m_Logger.WriteLine("  Damage Info: " + _damageInfo.Amount + " " + _damageInfo.Type.String + " dmg from [" + _damageInfo.AttackerId + "]", 4);

            IMyCharacter character = _target as IMyCharacter;
            if (character == null)
                return;

            if (_damageInfo.IsDeformation)
                return;

            //if (_damageInfo.Type != MyDamageType.Bullet &&
            //    _damageInfo.Type != MyDamageType.Explosion &&
            //    _damageInfo.Type != MyDamageType.Wolf &&
            //    _damageInfo.Type != MyDamageType.Spider)
            //{
            //    m_Logger.WriteLine("  Damage type: " + _damageInfo.Type.String + ", amount = " + _damageInfo.Amount);
            //}

            if (GetPlayerSteamUid(character) == 0U)
            {
                m_Logger.WriteLine("  Damage captured: " + _damageInfo.Amount + " " + _damageInfo.Type.String + " damage" +
                    " - Character [" + Utils.GetCharacterName(character) + "] (EntityId = " + character.EntityId + ")", 4);
            }
            else
            {
                m_Logger.WriteLine("  Damage captured: " + _damageInfo.Amount + " " + _damageInfo.Type.String + " damage" +
                    " - Character [" + Utils.GetCharacterName(character) + "] (EntityId = " + character.EntityId + ") (Player <" + GetPlayerSteamUid(character) + ">)", 4);
            }

            if (!m_ShieldManager.CharacterInfos.ContainsKey(character.EntityId) || 
                !m_ShieldManager.CharacterInfos[character.EntityId].HasAnyEmitter)
                return;

            CharacterShieldInfo charInfo = m_ShieldManager.CharacterInfos[character.EntityId];
            //float beforeDamageHealth = MyVisualScriptLogicProvider.GetPlayersHealth(character.ControllerInfo.ControllingIdentityId);

            // damage will try passing through Manual Shield first;
            if (charInfo.ManualEmitter != null)
            {
                if (charInfo.ManualEmitter.TakeDamage(ref _damageInfo))
                {
                    // update effect list;
                }

                
                if (_damageInfo.Amount <= 0)
                    return;
            }

            // the rest of damage will pass through Auto Shield;
            if (charInfo.AutoEmitter != null)
            {

                if (charInfo.AutoEmitter.TakeDamage(ref _damageInfo))
                {
                    // update effect list;



                    if (m_ShieldDamageEffects.ContainsKey(character.EntityId))
                    {
                        m_ShieldDamageEffects[character.EntityId].Ticks = m_Ticks;
                        m_ShieldDamageEffects[character.EntityId].ShieldAmountPercent = charInfo.AutoEmitter.ShieldEnergyPercent;
                    }
                    else
                    {
                        m_ShieldDamageEffects[character.EntityId] = new OtherCharacterShieldData()
                        {
                            Entity = character,
                            EntityId = character.EntityId,
                            Ticks = m_Ticks,
                            ShieldAmountPercent = charInfo.AutoEmitter.ShieldEnergyPercent
                        };
                    }


                }
                
                if (_damageInfo.Amount <= 0)
                    return;
            }

            
        }

        private void HookEntity(IMyCharacter _character)
        {
            m_Logger.WriteLine("  Character [" + Utils.GetCharacterName(_character) + "]: Hooking character.CharacterDied..", 5);
            _character.CharacterDied += Character_CharacterDied;

            MyInventory inventory = _character.GetInventory() as MyInventory;
            if (inventory == null)
            {
                m_Logger.WriteLine("  Character [" + _character.DisplayName + "] doesn't have inventory", 5);
                return;
            }

            m_Logger.WriteLine("  Character [" + Utils.GetCharacterName(_character) + "]: Hooking inventory.ContentChanged..", 5);
            inventory.ContentsChanged += Inventory_ContentsChanged;
            inventory.BeforeContentsChanged += Inventory_BeforeContentsChanged;

            ++hooked;
            m_HookedEntities.Add(_character.Name, _character);

            m_ShieldManager.CharacterInfos[_character.EntityId] = new CharacterShieldInfo();
            m_Logger.WriteLine("Added character " + _character.EntityId);
        }

        private void UnHookEntity(IMyCharacter _character)
        {
            m_Logger.WriteLine("  Character [" + Utils.GetCharacterName(_character) + "]: UnHooking character.CharacterDied..", 5);
            _character.CharacterDied -= Character_CharacterDied;

            MyInventory inventory = _character.GetInventory() as MyInventory;
            if (inventory == null)
                return;

            m_Logger.WriteLine("  Character [" + Utils.GetCharacterName(_character) + "]: UnHooking inventory.ContentChanged..", 5);
            inventory.ContentsChanged -= Inventory_ContentsChanged;
            inventory.BeforeContentsChanged -= Inventory_BeforeContentsChanged;

            ++released;
            m_HookedEntities.Remove(_character.Name);
        }

        private void UpdatePlayersCacheLists()
        {
            m_CachedPlayers.Clear();
            m_CachedPlayersPosition.Clear();

            MyAPIGateway.Players.GetPlayers(m_CachedPlayers);
            for (int i = 0; i < m_CachedPlayers.Count; ++i)
            {
                var player = m_CachedPlayers[i];
                if (player.IsBot)
                {
                    m_CachedPlayers.RemoveAt(i);
                    --i;
                }
                else
                {
                    if (player.Character != null)
                        m_CachedPlayersPosition[player.SteamUserId] = player.Character.WorldVolume.Center;
                }
            }
        }

        private IMyPlayer GetPlayer(ulong _steamUID)
        {
            if (_steamUID == 0UL)
                return null;

            if (m_CachedPlayers.Count == 0)
                UpdatePlayersCacheLists();

            foreach (IMyPlayer player in m_CachedPlayers)
            {
                if (player.SteamUserId == _steamUID)
                    return player;
            }

            return null;
        }

        private IMyPlayer GetPlayer(IMyCharacter _character)
        {
            if (_character == null)
                return null;

            if (m_CachedPlayers.Count == 0)
                UpdatePlayersCacheLists();

            foreach (IMyPlayer player in m_CachedPlayers)
            {
                if (player.Character != null && player.Character.EntityId == _character.EntityId)
                    return player;
            }

            return null;
        }

        private ulong GetPlayerSteamUid(IMyCharacter _character)
        {
            IMyPlayer player = GetPlayer(_character);
            if (player == null)
                return 0U;

            return player.SteamUserId;
        }


    }
}
