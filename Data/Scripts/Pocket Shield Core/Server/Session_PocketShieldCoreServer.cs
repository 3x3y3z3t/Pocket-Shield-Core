// ;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRage.Game;
using ExShared;
using VRage.Game.Entity;
using VRageMath;

namespace PocketShieldCore
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public partial class Session_PocketShieldCoreServer : MySessionComponentBase
    {
        public bool IsServer { get; private set; }
        public bool IsDedicated { get; private set; }
        public bool IsSetupDone { get; private set; }

        private int m_Ticks = 0;
        private bool m_IsFirstTick = true;

        private Logger m_Logger = null;
        private ServerConfig m_Config = null;
        private SaveDataManager m_SaveData = null;

        /// <summary> [Debug] </summary>
        private ulong m_SyncSaved = 0;

        private List<IMyPlayer> m_Players = new List<IMyPlayer>();
        private Dictionary<ulong, Vector3D> m_CachedPlayersPosition = new Dictionary<ulong, Vector3D>();

        private Dictionary<long, ShieldEmitter> m_ShieldEmitters = new Dictionary<long, ShieldEmitter>();

        private Dictionary<long, OtherCharacterShieldData> m_ShieldDamageEffects = new Dictionary<long, OtherCharacterShieldData>();

        private List<ulong> m_ForceSyncPlayers = new List<ulong>();
        private List<int> m_DamageQueue = new List<int>();
        private List<long> m_IdToRemove = new List<long>();

        private Dictionary<ulong, Logger> m_ShieldLoggers = new Dictionary<ulong, Logger>();

        public override void LoadData()
        {
            m_Logger = new Logger("server");
            m_Config = new ServerConfig("server_config.ini", m_Logger);

            m_Logger.LogLevel = m_Config.LogLevel;

            m_Logger.WriteLine("Setting up..");

            MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += Entities_OnEntityRemove;

            MyAPIGateway.Utilities.MessageEntered += ChatCommand_MessageEnteredHandle;
            MyAPIGateway.Utilities.RegisterMessageHandler(PocketShieldAPI.MOD_ID, ApiBackend_ModMessageHandle);
        }

        protected override void UnloadData()
        {
            m_Logger.WriteLine("Shutting down..");

            if (m_ApiBackend_EmitterPropCallbacks.Count > 0)
            {
                string callbackLeakLog = "";
                callbackLeakLog = "  > Warning < Some mods has not unregistered their " + m_ApiBackend_EmitterPropCallbacks.Count + " callbacks yet";
                if (m_ApiBackend_RegisteredMod.Count == 0)
                    callbackLeakLog += " (this should not happens)";
                m_Logger.WriteLine(callbackLeakLog);
            }
            m_ApiBackend_EmitterPropCallbacks.Clear();

            foreach (string mod in m_ApiBackend_RegisteredMod)
                m_Logger.WriteLine("  > Warning < Mod " + mod + " has not called DeInit() yet");

            m_SaveData.UnloadData();

            m_Logger.WriteLine("  > " + m_SyncSaved + " < sync operations saved");

            MyAPIGateway.Utilities.MessageEntered -= ChatCommand_MessageEnteredHandle;
            //MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
            //MyAPIGateway.Entities.OnEntityRemove -= Entities_OnEntityRemove;

            MyAPIGateway.Utilities.UnregisterMessageHandler(PocketShieldAPI.MOD_ID, ApiBackend_ModMessageHandle);

            ShieldEmitter.s_PluginBonusModifiers = null;

            m_Logger.WriteLine("Shutdown Done");

            foreach (Logger logger in m_ShieldLoggers.Values)
            {
                logger.Close();
            }
            m_Logger.Close();

            m_ShieldLoggers = null;
        }

        public override void BeforeStart()
        {
            IsServer = (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer);
            IsDedicated = IsServer && MyAPIGateway.Utilities.IsDedicated;
            if (!IsServer)
                return;

            m_SaveData = new SaveDataManager(m_ShieldEmitters, m_Logger);

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(50, BeforeDamageHandler);

            m_Logger.WriteLine("  IsServer = " + IsServer);
            m_Logger.WriteLine("  IsDedicated = " + IsDedicated);

            m_Logger.WriteLine("Setup Done");
            IsSetupDone = true;
        }

        public override void UpdateBeforeSimulation()
        {
            ++m_Ticks;
            // clear ticks count;
            if (m_Ticks >= 2000000000)
                m_Ticks -= 2000000000;

            if (m_Ticks % m_Config.ServerUpdateInterval == 0)
            {
                if (!IsSetupDone)
                    return;

                if (m_IsFirstTick)
                {
                    Inventory_UpdatePlayerCharacterInventoryOnceBeforeSim();

                    m_Logger.WriteLine("  Updating all Emitters once before sim..", 1);
                    m_SaveData.ApplySavedata();

                    m_IsFirstTick = false;
                }

                UpdatePlayersList();
                CachePlayersPosition();
                Sync_SyncDataToPlayers();

                m_SaveData.Update();

                m_ApiBackend_CallbacksCount = m_ApiBackend_EmitterPropCallbacks.Count;
                //m_Logger.WriteLine("Callback count = " + m_ApiBackend_CallbacksCount);
            }

            if (m_Ticks % m_Config.ShieldUpdateInterval == 0)
            {
                foreach (ShieldEmitter emitter in m_ShieldEmitters.Values)
                    emitter.Update(m_Config.ShieldUpdateInterval);
            }

            if (m_Ticks % 60000 == 0)
            {
                Blueprints_UpdateBlueprintData();
                m_Logger.WriteLine("> " + m_SyncSaved + " < sync operations saved");
            }

            // end of method;
            return;
        }

        public override void SaveData()
        {
            m_SaveData.SaveData();
        }

        private void Entities_OnEntityAdd(VRage.ModAPI.IMyEntity _entity)
        {
            // m_Logger may be null here during LoadData();

            m_Logger.WriteLine("New Entity added..", 5);
            IMyCharacter character = _entity as IMyCharacter;
            if (character == null)
                return;

            m_Logger.WriteLine("  Added Character [" + Utils.GetCharacterName(character) + "] (EntitId = " + character.EntityId + ")", 1);
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

            ShieldFactory_ReplaceEmitter(_character, null);

            ulong playerUid = GetPlayerSteamUid(_character);
            if (playerUid == 0U)
            {
                m_Logger.WriteLine("Character [" + Utils.GetCharacterName(_character) + "] died and their ShieldEmitter has been removed", 2);

                if (m_Config.NpcInventoryOperationOnDeath == NpcInventoryOperation.NoTouch)
                    return;

                MyInventory inventory = _character.GetInventory() as MyInventory;
                if (inventory == null)
                    return;

                Inventory_ManipulateDeadCharacterInventory(inventory);
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

        public void BeforeDamageHandler(object _target, ref MyDamageInformation _damageInfo)
        {
            if (m_DamageQueue.Contains(_damageInfo.GetHashCode()))
                return;

            IMyCharacter character = _target as IMyCharacter;
            if (character == null)
                return;

            if (_damageInfo.IsDeformation)
                return;

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

            if (!m_ShieldEmitters.ContainsKey(character.EntityId))
                return;

            m_Logger.WriteLine("  Trying passing damage through Shield Emitter..", 4);
            ShieldEmitter emitter = m_ShieldEmitters[character.EntityId];
            float beforeDamageHealth = MyVisualScriptLogicProvider.GetPlayersHealth(character.ControllerInfo.ControllingIdentityId);
            if (emitter.TakeDamage(ref _damageInfo))
            {
                if (m_ShieldDamageEffects.ContainsKey(character.EntityId))
                {
                    m_ShieldDamageEffects[character.EntityId].Ticks = m_Ticks;
                    m_ShieldDamageEffects[character.EntityId].ShieldAmountPercent = emitter.ShieldEnergyPercent;
                }
                else
                {
                    m_ShieldDamageEffects[character.EntityId] = new OtherCharacterShieldData()
                    {
                        Entity = character,
                        EntityId = character.EntityId,
                        Ticks = m_Ticks,
                        ShieldAmountPercent = emitter.ShieldEnergyPercent
                    };
                }
            }

            m_DamageQueue.Add(_damageInfo.GetHashCode());
            bool shouldSync = _damageInfo.Amount > 0.0f;
            character.DoDamage(_damageInfo.Amount, _damageInfo.Type, shouldSync, attackerId: _damageInfo.AttackerId);
            _damageInfo.Amount = 0;
            m_DamageQueue.Remove(_damageInfo.GetHashCode());
        }

        private void UpdatePlayersList()
        {
            m_Players.Clear();

            MyAPIGateway.Players.GetPlayers(m_Players);
            for (int i = 0; i < m_Players.Count; ++i)
            {
                if (m_Players[i].IsBot)
                {
                    m_Players.RemoveAt(i);
                    --i;
                }
            }
        }

        private void CachePlayersPosition()
        {
            m_CachedPlayersPosition.Clear();
            foreach (var player in m_Players)
            {
                if (player.Character != null)
                    m_CachedPlayersPosition[player.SteamUserId] = player.Character.WorldVolume.Center;
            }
        }

        private IMyPlayer GetPlayer(IMyCharacter _character)
        {
            if (_character == null)
                return null;

            if (m_Players.Count == 0)
                UpdatePlayersList();

            foreach (IMyPlayer player in m_Players)
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
