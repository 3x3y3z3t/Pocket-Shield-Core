// ;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreServer
    {
        private void Sync_SyncDataToPlayers()
        {
            foreach (IMyPlayer player in m_Players)
            {
                ++m_SyncSaved;

                if (m_ForceSyncPlayers.Contains(player.SteamUserId))
                {
                    m_ForceSyncPlayers.Remove(player.SteamUserId);
                    m_Logger.WriteLine("Request Sync due to: Force Sync <" + player.SteamUserId + ">", 3);
                    Sync_SendSyncDataToPlayer(player);
                }
                else if (player.Character != null &&
                         m_ShieldEmitters.ContainsKey(player.Character.EntityId) && 
                         m_ShieldEmitters[player.Character.EntityId].RequireSync)
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
                packet.OtherShieldData = new List<OtherCharacterShieldData>();

            foreach (var value in m_ShieldDamageEffects.Values)
            {
                double distance = Vector3D.Distance(m_CachedPlayersPosition[_player.SteamUserId], value.Entity.WorldVolume.Center);
                if (distance < Constants.HIT_EFFECT_SYNC_DISTANCE)
                {
                    int ticks = m_Ticks - value.Ticks;
                    if (ticks > Constants.HIT_EFFECT_LIVE_TICKS)
                        value.Ticks = Constants.HIT_EFFECT_LIVE_TICKS;
                    else if (ticks < 0)
                        continue;
                    else
                        value.Ticks = Constants.HIT_EFFECT_LIVE_TICKS - ticks;

                    packet.OtherShieldData.Add(value);
                }
            }

            packet.PlayerSteamUserId = _player.SteamUserId;
            if (m_ShieldEmitters.ContainsKey(_player.Character.EntityId))
            {
                ShieldEmitter emitter = m_ShieldEmitters[_player.Character.EntityId];
                if (emitter != null)
                {
                    packet.MyShieldData = new MyShieldData();
                    packet.MyShieldData.SubtypeId = emitter.SubtypeId;
                    packet.MyShieldData.Energy = emitter.Energy;
                    packet.MyShieldData.MaxEnergy = emitter.MaxEnergy;
                    packet.MyShieldData.DefResList = emitter.DefResList;
                    packet.MyShieldData.OverchargeRemainingPercent = emitter.OverchargeRemainingPercent;

                    emitter.RequireSync = false;
                }
                else
                {
                    m_Logger.WriteLine("> Warning < Emitter is null. This should not happens.");
                    m_Logger.WriteLine("  More info: Player Steam UID = " + _player.SteamUserId);
                }
            }

            var data = MyAPIGateway.Utilities.SerializeToBinary(packet);
            m_Logger.WriteLine("Sending sync data to player " + _player.SteamUserId, 5);
            MyAPIGateway.Multiplayer.SendMessageTo(Constants.MSG_HANDLER_ID_SYNC, data, _player.SteamUserId);

            --m_SyncSaved;
            m_ShieldDamageEffects.Clear();
        }

    }
}
