// ;
using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Utils;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreClient
    {
        public void Sync_ReceiveDataFromServer(ushort _handlerId, byte[] _package, ulong _senderPlayerId, bool _sentMsg)
        {
            if (MyAPIGateway.Session == null)
                return;

            m_Logger.WriteLine("Starting HandleSyncShieldData()", 5);

            m_Logger.WriteLine("  Recieved message from <" + _senderPlayerId + ">", 5);
            if (!_sentMsg)
            {
                m_Logger.WriteLine("  Message did not come from server and will be ignored", 4);
                return;
            }

            try
            {
                Packet_ShieldData packet = MyAPIGateway.Utilities.SerializeFromBinary<Packet_ShieldData>(_package);

                if (packet.PlayerSteamUserId != MyAPIGateway.Session?.Player?.SteamUserId)
                {
                    m_Logger.WriteLine("  Data is for player <" + packet.PlayerSteamUserId + ">, not me", 4);
                    return;
                }

                if (packet.OtherAutoShieldData != null)
                {
                    foreach (var data in packet.OtherAutoShieldData)
                    {
                        Sync_AddOrUpdateData(data);
                    }
                }

                if (packet.MyManualShieldData != null)
                    Sync_CopyManualShieldData(packet.MyManualShieldData);

                if (packet.MyAutoShieldData != null)
                    Sync_CopyAutoShieldData(packet.MyAutoShieldData);

                m_Logger.WriteLine("  Shield Data updated", 4);

                m_ShieldHudPanel?.UpdatePanel();
                m_ShieldHudPanel?.UpdatePanelVisibility();
            }
            catch (Exception _e)
            {
                m_Logger.WriteLine("  > Exception < Error during parsing sync data from <" + _senderPlayerId + ">: " + _e.Message, 0);
            }
        }

        private void Sync_CopyManualShieldData(MyShieldData _data)
        {
            if (_data.HasShield != m_ManualShieldData.HasShield && m_ShieldHudPanel != null)
                m_ShieldHudPanel.RequireConfigUpdate = true;

            if (_data.HasShield)
            {
                m_ManualShieldData.SubtypeId = _data.SubtypeId;
                m_ManualShieldData.IsActive = _data.IsActive;
                m_ManualShieldData.IsTurnedOn = _data.IsTurnedOn;
                m_ManualShieldData.Energy = _data.Energy;
                m_ManualShieldData.MaxEnergy = _data.MaxEnergy;
                m_ManualShieldData.OverchargeRemainingPercent = _data.OverchargeRemainingPercent;

            }
            else
            {
                m_ManualShieldData.SubtypeId = MyStringHash.NullOrEmpty;
                m_ManualShieldData.IsActive = false;
                m_ManualShieldData.IsTurnedOn = false;
                m_ManualShieldData.Energy = 0.0f;
                m_ManualShieldData.MaxEnergy = 0.0f;
                m_ManualShieldData.OverchargeRemainingPercent = 0.0f;
            }
        }

        private void Sync_CopyAutoShieldData(MyShieldData _data)
        {
            if (_data.HasShield != m_ManualShieldData.HasShield && m_ShieldHudPanel != null)
                m_ShieldHudPanel.RequireConfigUpdate = true;

            if (_data.HasShield)
            {
                m_AutoShieldData.SubtypeId = _data.SubtypeId;
                m_AutoShieldData.IsActive = _data.IsActive;
                m_AutoShieldData.IsTurnedOn = _data.IsTurnedOn;
                m_AutoShieldData.Energy = _data.Energy;
                m_AutoShieldData.MaxEnergy = _data.MaxEnergy;
                m_AutoShieldData.OverchargeRemainingPercent = _data.OverchargeRemainingPercent;
                m_AutoShieldData.DefResList.Clear();
                if (_data.DefResList != null)
                {
                    foreach (var pair in _data.DefResList)
                        m_AutoShieldData.DefResList[pair.Key] = pair.Value;
                }
            }
            else
            {
                m_AutoShieldData.SubtypeId = MyStringHash.NullOrEmpty;
                m_AutoShieldData.IsActive = false;
                m_AutoShieldData.IsTurnedOn = false;
                m_AutoShieldData.Energy = 0.0f;
                m_AutoShieldData.MaxEnergy = 0.0f;
                m_AutoShieldData.OverchargeRemainingPercent = 0.0f;
                m_AutoShieldData.DefResList.Clear();
            }
        }

        public void Sync_AddOrUpdateData(OtherCharacterShieldData _data)
        {
            foreach (var data in m_DrawList)
            {
                m_Logger.WriteLine("Add Data");
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


    }
}
