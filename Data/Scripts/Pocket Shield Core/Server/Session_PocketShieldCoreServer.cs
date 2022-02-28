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

        /// <summary> [Debug] </summary>
        private ulong m_SyncSaved = 0;

        private List<IMyPlayer> m_CachedPlayers = new List<IMyPlayer>();
        private Dictionary<ulong, Vector3D> m_CachedPlayersPosition = new Dictionary<ulong, Vector3D>();

        private Dictionary<long, OtherCharacterShieldData> m_ShieldDamageEffects = new Dictionary<long, OtherCharacterShieldData>();

        private List<ulong> m_ForceSyncPlayers = new List<ulong>();
        private List<int> m_DamageQueue = new List<int>();
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

                m_ApiBackend_RegisteredMod = new List<string>();
                m_ApiBackend_ExposedMethods = new List<Delegate>()
                {
                    (Action<long, bool>)ShieldManager_ActivateManualShield,
                    (Action<long, bool, bool>)ShieldManager_TurnShieldOnOff
                };

                m_ShieldManager_CharacterShieldManagers = new Dictionary<long, CharacterShieldManager>();
                m_ShieldManager_EmitterConstructionData = new Dictionary<MyStringHash, List<object>>(MyStringHash.Comparer);
                m_ShieldManager_ShieldLoggers = new Dictionary<long, VRage.MyTuple<Logger, Logger>>();




                m_CachedProps = new PocketShieldAPIV2.ShieldEmitterProperties(null);

                m_Config = new ServerConfig("server_config.ini", m_Logger);

                m_Logger.LogLevel = m_Config.LogLevel;

                m_Logger.WriteLine("Setting up..");

                MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
                MyAPIGateway.Entities.OnEntityRemove += Entities_OnEntityRemove;

                MyAPIGateway.Utilities.MessageEntered += ChatCommand_MessageEnteredHandle;
                MyAPIGateway.Utilities.RegisterMessageHandler(PocketShieldAPIV2.MOD_ID, ApiBackend_ModMessageHandle);
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Constants.SYNC_ID_TO_SERVER, Sync_ReceiveDataFromClient);

                MyAPIGateway.Projectiles.AddOnHitInterceptor(50, Projectiles_OnHitInterceptor);
                

            

        }

        protected override void UnloadData()
        {
            if (IsServer)
            {
                m_Logger.WriteLine("Shutting down..");
                
                foreach (string mod in m_ApiBackend_RegisteredMod)
                    m_Logger.WriteLine("  > Warning < Mod " + mod + " has not called DeInit() yet");

                m_SaveData.UnloadData();

                m_Logger.WriteLine("  > " + m_SyncSaved + " < sync operations saved");

                foreach (var loggers in m_ShieldManager_ShieldLoggers.Values)
                {
                    if (loggers.Item1 != null)
                        loggers.Item1.Close();
                    if (loggers.Item2 != null)
                        loggers.Item2.Close();
                }
                m_ShieldManager_ShieldLoggers.Clear();

                MyAPIGateway.Utilities.MessageEntered -= ChatCommand_MessageEnteredHandle;
                MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
                MyAPIGateway.Entities.OnEntityRemove -= Entities_OnEntityRemove;

                MyAPIGateway.Utilities.UnregisterMessageHandler(PocketShieldAPIV2.MOD_ID, ApiBackend_ModMessageHandle);
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Constants.SYNC_ID_TO_SERVER, Sync_ReceiveDataFromClient);

                MyAPIGateway.Projectiles.RemoveOnHitInterceptor(Projectiles_OnHitInterceptor);
                
                MyAPIGateway.Players.ItemConsumed -= Players_ItemConsumed;

                m_Logger.WriteLine("Shutdown Done");
            }

            ShieldEmitter.s_PluginBonusModifiers = null;

            //MyAPIGateway.Entities.RemoveEntity(PSProjectileDetector.DummyEntity);
            PSProjectileDetector.DummyEntity = null;

            m_Logger.Close();
        }

        public override void BeforeStart()
        {
            if (!IsServer)
                return;

            m_SaveData = new SaveDataManager(m_ShieldManager_CharacterShieldManagers, m_Logger);

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
                foreach (CharacterShieldManager character in m_ShieldManager_CharacterShieldManagers.Values)
                {
                    character.Update(m_Config.ShieldUpdateInterval);
                }
            }

            if (m_Ticks % 60000 == 0)
            {
                Blueprints_UpdateBlueprintData();
                m_Logger.WriteLine("> " + m_SyncSaved + " < sync operations saved");
            }




            // end;
        }

        public override void SaveData()
        {
            if (!IsServer)
                return;

            m_SaveData.SaveData();
        }

        private void Entities_OnEntityAdd(VRage.ModAPI.IMyEntity _entity)
        {
            m_Logger.WriteLine("New Entity added..", 5);
            IMyCharacter character = _entity as IMyCharacter;
            if (character == null)
                return;

            m_Logger.WriteLine("  Added Character [" + Utils.GetCharacterName(character) + "] (EntitId = " + character.EntityId + ")", 2);
            m_Logger.WriteLine("  Character [" + Utils.GetCharacterName(character) + "]: Hooking character.CharacterDied..", 5);
            character.CharacterDied += Character_CharacterDied;

            MyInventory inventory = character.GetInventory() as MyInventory;
            if (inventory == null)
            {
                m_Logger.WriteLine("  Character [" + character.DisplayName + "] doesn't have inventory", 5);
                return;
            }

            m_Logger.WriteLine("  Character [" + Utils.GetCharacterName(character) + "]: Hooking inventory.ContentChanged..", 5);
            inventory.ContentsChanged += Inventory_ContentsChangedHandle;
        }

        private void Entities_OnEntityRemove(VRage.ModAPI.IMyEntity _entity)
        {
            m_Logger.WriteLine("Removing Entity..", 5);
            IMyCharacter character = _entity as IMyCharacter;
            if (character == null)
                return;

            m_Logger.WriteLine("  Entity is Character [" + Utils.GetCharacterName(character) + "]", 5);
            m_Logger.WriteLine("  Character [" + Utils.GetCharacterName(character) + "]: UnHooking character.CharacterDied..", 5);
            character.CharacterDied -= Character_CharacterDied;

            MyInventory inventory = character.GetInventory() as MyInventory;
            if (inventory == null)
                return;

            m_Logger.WriteLine("  Character [" + Utils.GetCharacterName(character) + "]: UnHooking inventory.ContentChanged..", 5);
            inventory.ContentsChanged -= Inventory_ContentsChangedHandle;

        }

        private void Character_CharacterDied(IMyCharacter _character)
        {
            if (_character == null)
                return;

            ShieldManager_DropEmitter(_character, true);
            ShieldManager_DropEmitter(_character, false);

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

            PocketShieldAPIV2.ShieldEmitterProperties prop = null;
            foreach (var emitterData in m_ShieldManager_EmitterConstructionData.Values)
            {
                prop = new PocketShieldAPIV2.ShieldEmitterProperties(emitterData);
                if (!prop.IsManual && _itemDefId.SubtypeId == prop.SubtypeId)
                {
                    m_Logger.WriteLine("  Match found: " + _itemDefId.SubtypeId, 4);

                    if (!m_ShieldManager_CharacterShieldManagers.ContainsKey(_character.EntityId))
                        return;

                    CharacterShieldManager manager = m_ShieldManager_CharacterShieldManagers[_character.EntityId];
                    float energy = manager.LastAutoEnergy;
                    bool onoff = !manager.LastAutoTurnedOn;

                    int itemInd = manager.AutoEmitterIndex;
                    m_Logger.WriteLine("  Toggled Index = " + itemInd, 4);
                    
                    // HACK: force update savedata;
                    m_SaveData.SetSaveAutoShieldEnergy(_character.EntityId, energy);
                    m_SaveData.SetSaveAutoShieldTurnedOn(_character.EntityId, onoff);
                    

                    inventory.AddItems(1, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(_itemDefId), itemInd);
                    m_Logger.WriteLine("  Item added back at index " + itemInd, 4);

                    manager.AutoEmitter.Energy = energy;
                    manager.AutoEmitter.IsTurnedOn = onoff;

                    // TODO: turn on-off shield;
                    //m_Logger.WriteLine("  Last energy = " + m_ShieldManager_CharacterShieldManagers[_character.EntityId].LastAutoEnergy, 5);
                    //m_Logger.WriteLine("  Current energy = " + m_ShieldManager_CharacterShieldManagers[_character.EntityId].AutoEmitter?.Energy, 5);




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
            if (m_DamageQueue.Contains(_damageInfo.GetHashCode()))
                return;

            //m_Logger.WriteLine("> BeforeDamageHandler is triggered <");
            m_Logger.WriteLine("  Damage Info: " + _damageInfo.Amount + " " + _damageInfo.Type.String + " dmg from [" + _damageInfo.AttackerId + "]", 4);

            IMyCharacter character = _target as IMyCharacter;
            if (character == null)
                return;

            if (_damageInfo.IsDeformation)
                return;

            if (_damageInfo.Type != MyDamageType.Bullet &&
                _damageInfo.Type != MyDamageType.Explosion &&
                _damageInfo.Type != MyDamageType.Wolf &&
                _damageInfo.Type != MyDamageType.Spider)
            {
                m_Logger.WriteLine("Damage type: " + _damageInfo.Type.String);
            }

            if (GetPlayerSteamUid(character) == 0U)
            {
                m_Logger.WriteLine("Damage captured: " + _damageInfo.Amount + " " + _damageInfo.Type.String + " damage" +
                    " - Character [" + Utils.GetCharacterName(character) + "] (EntityId = " + character.EntityId + ")", 4);
            }
            else
            {
                m_Logger.WriteLine("Damage captured: " + _damageInfo.Amount + " " + _damageInfo.Type.String + " damage" +
                    " - Character [" + Utils.GetCharacterName(character) + "] (EntityId = " + character.EntityId + ") (Player <" + GetPlayerSteamUid(character) + ">)", 4);
            }

            if (!m_ShieldManager_CharacterShieldManagers.ContainsKey(character.EntityId) || 
                !m_ShieldManager_CharacterShieldManagers[character.EntityId].HasAnyEmitter)
                return;

            CharacterShieldManager manager = m_ShieldManager_CharacterShieldManagers[character.EntityId];
            //float beforeDamageHealth = MyVisualScriptLogicProvider.GetPlayersHealth(character.ControllerInfo.ControllingIdentityId);

            // damage will try passing through Manual Shield first;
            if (manager.ManualEmitter != null)
            {
                if (manager.ManualEmitter.TakeDamage(ref _damageInfo))
                {
                    // update effect list;
                }



                if (_damageInfo.Amount <= 0)
                    return;
            }

            // the rest of damage will pass through Auto Shield;
            if (manager.AutoEmitter != null)
            {

                if (manager.AutoEmitter.TakeDamage(ref _damageInfo))
                {
                    // update effect list;



                    if (m_ShieldDamageEffects.ContainsKey(character.EntityId))
                    {
                        m_ShieldDamageEffects[character.EntityId].Ticks = m_Ticks;
                        m_ShieldDamageEffects[character.EntityId].ShieldAmountPercent = manager.AutoEmitter.ShieldEnergyPercent;
                    }
                    else
                    {
                        m_ShieldDamageEffects[character.EntityId] = new OtherCharacterShieldData()
                        {
                            Entity = character,
                            EntityId = character.EntityId,
                            Ticks = m_Ticks,
                            ShieldAmountPercent = manager.AutoEmitter.ShieldEnergyPercent
                        };
                    }


                }





                if (_damageInfo.Amount <= 0)
                    return;
            }



            //m_Logger.WriteLine("  Trying passing damage through Shield Emitter..", 4);
            //ShieldEmitter emitter = m_ShieldEmitters[character.EntityId];
            //float beforeDamageHealth = MyVisualScriptLogicProvider.GetPlayersHealth(character.ControllerInfo.ControllingIdentityId);
            //if (emitter.TakeDamage(ref _damageInfo))
            //{
            //    if (m_ShieldDamageEffects.ContainsKey(character.EntityId))
            //    {
            //        m_ShieldDamageEffects[character.EntityId].Ticks = m_Ticks;
            //        m_ShieldDamageEffects[character.EntityId].ShieldAmountPercent = emitter.ShieldEnergyPercent;
            //    }
            //    else
            //    {
            //        m_ShieldDamageEffects[character.EntityId] = new OtherCharacterShieldData()
            //        {
            //            Entity = character,
            //            EntityId = character.EntityId,
            //            Ticks = m_Ticks,
            //            ShieldAmountPercent = emitter.ShieldEnergyPercent
            //        };
            //    }
            //}

            //m_DamageQueue.Add(_damageInfo.GetHashCode());
            //bool shouldSync = _damageInfo.Amount > 0.0f;
            //character.DoDamage(_damageInfo.Amount, _damageInfo.Type, shouldSync, attackerId: _damageInfo.AttackerId);
            //_damageInfo.Amount = 0;
            //m_DamageQueue.Remove(_damageInfo.GetHashCode());
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
