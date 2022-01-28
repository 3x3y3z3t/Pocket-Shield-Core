// ;
using ExShared;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;

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
                //m_Logger.WriteLine("  _handlerId = " + _handlerId + ", _package = " + decodedPackage + ", _senderPlayerId = " + _senderPlayerId + ", _sentMsg = " + _sentMsg, 5);
                m_Logger.WriteLine("  Recieved message from <" + _senderPlayerId + ">: " + decodedPackage, 5);

                SyncObject obj = MyAPIGateway.Utilities.SerializeFromXML<SyncObject>(decodedPackage);

                if (obj.m_OthersShieldData != null && obj.m_OthersShieldData.Count > 0)
                {
                    foreach (var data in obj.m_OthersShieldData)
                    {
                        data.ShouldPlaySound = true;
                        Sync_AddOrUpdateData(data);
                    }
                }
                
                if (obj.m_MyShieldData != null)
                {
                    if (obj.m_MyShieldData.PlayerSteamUserId == MyAPIGateway.Session.Player.SteamUserId)
                    {
                        if (obj.HasShield)
                        {
                            //m_ShieldData = obj.m_MyShieldData;
                            m_ShieldData.CopyFrom(obj.m_MyShieldData);
                        }
                        else
                        {
                            m_ShieldData.Clear(true);
                        }
                        if (m_ShieldHudPanel != null)
                            m_ShieldHudPanel.RequireUpdate = true;
                        
                        m_Logger.WriteLine("  Shield Data updated", 4);
                    }
                    else
                    {
                        m_Logger.WriteLine("  Data is for player <" + m_ShieldData.PlayerSteamUserId + ">, not me", 4);
                    }
                }
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


    }
}
