// ;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreServer
    {
        /// <summary> [Debug] </summary>
        private ulong m_Sync_SyncCalled = 0UL;
        private ulong m_Sync_SyncPerformed = 0UL;

        private void Sync_SyncDataToPlayers()
        {
            foreach (IMyPlayer player in m_CachedPlayers)
            {
                ++m_Sync_SyncCalled;

                if (m_ForceSyncPlayers.Contains(player.SteamUserId))
                {
                    m_ForceSyncPlayers.Remove(player.SteamUserId);
                    m_Logger.WriteLine("Request Sync due to: Force Sync <" + player.SteamUserId + ">", 3);
                    Sync_SendSyncDataToPlayer(player);
                }
                else if (player.Character != null &&
                         m_ShieldManager.CharacterInfos.ContainsKey(player.Character.EntityId) &&
                         m_ShieldManager.CharacterInfos[player.Character.EntityId].RequireSync)
                {
                    m_Logger.WriteLine("Request Sync due to: Shield Updated <" + player.SteamUserId + ">", 3);
                    Sync_SendSyncDataToPlayer(player);
                }
                else if (m_ShieldDamageEffects.Count > 0)
                {
                    m_Logger.WriteLine("Request Sync due to: Shield Effect Updated <" + player.SteamUserId + ">", 3);
                    Sync_SendSyncDataToPlayer(player);
                }
            }
        }

        /// <summary>
        /// Defined in Session_PocketShieldServer_Sync.cs.
        /// </summary>
        /// <param name="_player"></param>
        private void Sync_SendSyncDataToPlayer(IMyPlayer _player)
        {
            Packet_ShieldData packet = new Packet_ShieldData();

            if (m_ShieldDamageEffects.Count > 0)
                packet.OtherAutoShieldData = new List<OtherCharacterShieldData>();

            foreach (var value in m_ShieldDamageEffects.Values)
            {
                double distance = Vector3D.Distance(m_CachedPlayersPosition[_player.SteamUserId], value.Entity.WorldVolume.Center);
                m_Logger.WriteLine("EntityId = " + value.EntityId);
                if (distance < Constants.HIT_EFFECT_SYNC_DISTANCE)
                {
                    int ticks = m_Ticks - value.Ticks;
                    if (ticks > Constants.HIT_EFFECT_LIVE_TICKS)
                        value.Ticks = Constants.HIT_EFFECT_LIVE_TICKS;
                    else if (ticks < 0)
                        continue;
                    else
                        value.Ticks = Constants.HIT_EFFECT_LIVE_TICKS - ticks;

                    packet.OtherAutoShieldData.Add(value);
                }
            }

            packet.PlayerSteamUserId = _player.SteamUserId;
            packet.MyManualShieldData = new MyShieldData() { SubtypeId = MyStringHash.NullOrEmpty };
            packet.MyAutoShieldData = new MyShieldData() { SubtypeId = MyStringHash.NullOrEmpty };

            if (m_ShieldManager.CharacterInfos.ContainsKey(_player.Character.EntityId))
            {
                ShieldEmitter emitter = null;

                // manual emitter;
                emitter = m_ShieldManager.CharacterInfos[_player.Character.EntityId].ManualEmitter;
                if (emitter != null)
                {
                    packet.MyManualShieldData = new MyShieldData
                    {
                        SubtypeId = emitter.SubtypeId,
                        IsActive = emitter.IsActive,
                        IsTurnedOn = emitter.IsTurnedOn,
                        Energy = emitter.Energy,
                        MaxEnergy = emitter.MaxEnergy,
                        DefResList = emitter.DefResList,
                        OverchargeRemainingPercent = emitter.OverchargeRemainingPercent
                    };
                    emitter.RequireSync = false;
                }

                // auto emitter;
                emitter = m_ShieldManager.CharacterInfos[_player.Character.EntityId].AutoEmitter;
                if (emitter != null)
                {
                    packet.MyAutoShieldData = new MyShieldData
                    {
                        SubtypeId = emitter.SubtypeId,
                        IsTurnedOn = emitter.IsTurnedOn,
                        Energy = emitter.Energy,
                        MaxEnergy = emitter.MaxEnergy,
                        DefResList = emitter.DefResList,
                        OverchargeRemainingPercent = emitter.OverchargeRemainingPercent
                    };
                    emitter.RequireSync = false;
                    m_Logger.WriteLine("Overcharge Percent = " + emitter.OverchargeRemainingPercent);
                }
            }

            var data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            m_Logger.WriteLine("Sending sync data to player " + _player.SteamUserId, 5);
            MyAPIGateway.Multiplayer.SendMessageTo(Constants.SYNC_ID_TO_CLIENT, data, _player.SteamUserId);

            ++m_Sync_SyncPerformed;
            m_ShieldDamageEffects.Clear();
        }

        public void Sync_ReceiveDataFromClient(ushort _handlerId, byte[] _package, ulong _senderPlayerId, bool _sentMsg)
        {
            if (MyAPIGateway.Session == null)
                return;

            m_Logger.WriteLine("Starting HandleSyncShieldData()", 5);

            m_Logger.WriteLine("  Recieved message from <" + _senderPlayerId + ">", 5);
            if (_sentMsg)
            {
                m_Logger.WriteLine("  Message came from server and will be ignored", 4);
                return;
            }

            try
            {
                Packet_ToggleShieldData data = MyAPIGateway.Utilities.SerializeFromBinary<Packet_ToggleShieldData>(_package);
                if (data.Key != Constants.TOGGLE_SHIELD_KEY)
                {
                    m_Logger.WriteLine("  Key did not match (key=" + data.Key + ")", 4);
                    return;
                }

                IMyPlayer player = GetPlayer(_senderPlayerId);
                if (player == null)
                {
                    m_Logger.WriteLine("  Player <" + _senderPlayerId + "> does not exist (this should not happen)", 4);
                    return;
                }

                if (player.Character == null)
                {
                    m_Logger.WriteLine("  Player <" + _senderPlayerId + "> does not have a Character (this should not happen)", 4);
                    return;
                }

                if (!m_ShieldManager.CharacterInfos.ContainsKey(player.Character.EntityId))
                {
                    m_Logger.WriteLine("  Player <" + _senderPlayerId + "> character entity is not added yet (this should not happen)", 4);
                    return;
                }

                CharacterShieldInfo charInfo = m_ShieldManager.CharacterInfos[player.Character.EntityId];
                if (charInfo.ManualEmitter != null)
                    charInfo.ManualEmitter.IsTurnedOn = !charInfo.ManualEmitter.IsTurnedOn;
                if (charInfo.AutoEmitter != null)
                    charInfo.AutoEmitter.IsTurnedOn = !charInfo.AutoEmitter.IsTurnedOn;

                m_Logger.WriteLine("  Player <" + _senderPlayerId + "> toggled their shield(s)", 4);
            }
            catch (Exception _e)
            {
                m_Logger.WriteLine("  > Exception < Error during parsing sync data from <" + _senderPlayerId + ">: " + _e.Message, 0);
            }
        }

    }
}
