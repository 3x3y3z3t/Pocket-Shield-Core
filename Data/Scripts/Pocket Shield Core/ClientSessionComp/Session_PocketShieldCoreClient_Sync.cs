// ;
using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Utils;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreClient
    {
        public void Sync_HandleSyncShieldData(ushort _handlerId, byte[] _package, ulong _senderPlayerId, bool _sentMsg)
        {
            if (MyAPIGateway.Session == null)
                return;

            m_Logger.WriteLine("Starting HandleSyncShieldData()", 5);

            try
            {
                string decodedPackage = Encoding.Unicode.GetString(_package);
                //m_Logger.WriteLine("  _handlerId = " + _handlerId + ", _senderPlayerId = " + _senderPlayerId + ", _sentMsg = " + _sentMsg, 5);
                m_Logger.WriteLine("  Recieved message from <" + _senderPlayerId + ">", 5);

                Packet_ShieldData packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet_ShieldData>(_package);

                if (packet.PlayerSteamUserId != MyAPIGateway.Session.Player.SteamUserId)
                {
                    m_Logger.WriteLine("  Data is for player <" + packet.PlayerSteamUserId + ">, not me", 4);
                    return;
                }

                if (packet.OtherShieldData != null)
                {
                    foreach (var data in packet.OtherShieldData)
                    {
                        Sync_AddOrUpdateData(data);
                    }
                }

                if (packet.MyShieldData != null && packet.MyShieldData.HasShield)
                {
                    Sync_CopyShieldDataFrom(packet.MyShieldData);
                }
                else
                {
                    Sync_ClearShieldData();
                }
                m_Logger.WriteLine("  Shield Data updated", 4);

                if (m_ShieldHudPanel != null)
                    m_ShieldHudPanel.RequireUpdate = true;
            }
            catch (Exception _e)
            {
                m_Logger.WriteLine("  > Exception < Error during parsing sync data from <" + _senderPlayerId + ">: " + _e.Message, 0);
            }
        }

        public void Sync_AddOrUpdateData(OtherCharacterShieldData _data)
        {
            foreach (var data in m_DrawList)
            {
                if (data.EntityId == _data.EntityId)
                {
                    data.Ticks = _data.Ticks;
                    data.ShieldAmountPercent = _data.ShieldAmountPercent;
                    data.ShouldPlaySound = _data.ShouldPlaySound;
                    return;
                }
            }

            m_DrawList.Add(_data);
        }

        private void Sync_ClearShieldData()
        {
            m_ShieldData.SubtypeId = MyStringHash.NullOrEmpty;
            m_ShieldData.Energy = 0.0f;
            m_ShieldData.MaxEnergy = 0.0f;
            m_ShieldData.OverchargeRemainingPercent = 0.0f;
            m_ShieldData.DefResList.Clear();
        }

        private void Sync_CopyShieldDataFrom(MyShieldData _other)
        {
            m_ShieldData.SubtypeId = _other.SubtypeId;
            m_ShieldData.Energy = _other.Energy;
            m_ShieldData.MaxEnergy = _other.MaxEnergy;
            m_ShieldData.OverchargeRemainingPercent = _other.OverchargeRemainingPercent;
            if (_other.DefResList == null)
                m_ShieldData.DefResList.Clear();
            else
                m_ShieldData.DefResList = _other.DefResList;
        }
    }
}
