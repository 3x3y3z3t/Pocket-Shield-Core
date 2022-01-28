// ;

using ExShared;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using VRage;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreServer
    {
        private SyncObject m_SyncObject = new SyncObject();
        
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
                else if (m_PlayerShieldEmitters.ContainsKey((long)player.SteamUserId) && m_PlayerShieldEmitters[(long)player.SteamUserId].RequireSync)
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
            // TODO: add others' data;
            foreach (var value in m_ShieldDamageEffects.Values)
            {
                double distance = Vector3D.Distance(_player.GetPosition(), value.Entity.GetPosition());
                if (distance < Constants.HIT_EFFECT_SYNC_DISTANCE)
                {
                    int ticks = m_Ticks - value.Ticks;
                    if (ticks > Constants.HIT_EFFECT_LIVE_TICKS)
                        value.Ticks = Constants.HIT_EFFECT_LIVE_TICKS;
                    else if (ticks < 0)
                        continue;
                    else
                        value.Ticks = Constants.HIT_EFFECT_LIVE_TICKS - ticks;
                    
                    m_SyncObject.m_OthersShieldData.Add(value);
                }
            }

            m_SyncObject.m_MyShieldData.PlayerSteamUserId = _player.SteamUserId;
            if (m_PlayerShieldEmitters.ContainsKey((long)_player.SteamUserId))
            {
                ShieldEmitter emitter = m_PlayerShieldEmitters[(long)_player.SteamUserId];
                if (emitter != null)
                {
                    m_SyncObject.m_MyShieldData.Energy = emitter.Energy;
                    m_SyncObject.m_MyShieldData.MaxEnergy = emitter.MaxEnergy;
                    m_SyncObject.m_MyShieldData.Def = emitter.DefList;
                    m_SyncObject.m_MyShieldData.Res = emitter.ResList;
                    m_SyncObject.m_MyShieldData.SubtypeId = emitter.SubtypeId;
                    m_SyncObject.m_MyShieldData.OverchargeRemainingPercent = emitter.OverchargeRemainingPercent;
                    m_SyncObject.HasShield = true;

                    emitter.RequireSync = false;
                }
            }
            
            string data = MyAPIGateway.Utilities.SerializeToXML(m_SyncObject);
            m_Logger.WriteLine("Sending sync data to player " + _player.SteamUserId, 5);
            MyAPIGateway.Multiplayer.SendMessageTo(Constants.MSG_HANDLER_ID_SYNC, Encoding.Unicode.GetBytes(data), _player.SteamUserId);

            --m_SyncSaved;
            m_SyncObject.Clear();
            m_ShieldDamageEffects.Clear();
        }

    }

    public class OtherCharacterShieldData
    {
        [XmlIgnore]
        public IMyEntity Entity = null;
        [XmlIgnore]
        public bool ShouldPlaySound = false;

        public long EntityId = 0L;
        public float ShieldAmountPercent = 0.0f;
        public int Ticks = 0;
    }

    public class MyShieldData
    {
        [XmlIgnore]
        public float EnergyRemainingPercent { get { return Energy / MaxEnergy; } }
        //[XmlIgnore]
        //public int MaxDefOrResCount { get { return Math.Max(Def.Count, Res.Count); } }

        public ulong PlayerSteamUserId = 0UL; // redundancy?;

        public float Energy = 0.0f;
        
        public float MaxEnergy = 0.0f;

        public List<MyTuple<MyStringHash, float>> Def = new List<MyTuple<MyStringHash, float>>();
        public List<MyTuple<MyStringHash, float>> Res = new List<MyTuple<MyStringHash, float>>();

        public MyStringHash SubtypeId = MyStringHash.GetOrCompute("");

        public float OverchargeRemainingPercent = 0.0f;

        public void Clear(bool _force = false)
        {
            PlayerSteamUserId = 0UL;
            Energy = 0.0f;
            MaxEnergy = 0.0f;
            SubtypeId = MyStringHash.NullOrEmpty;
            OverchargeRemainingPercent = 0.0f;

            if (_force)
            {
                Def.Clear();
                Res.Clear();
            }
        }

        public void CopyFrom(MyShieldData _other)
        {
            PlayerSteamUserId = _other.PlayerSteamUserId;
            Energy = _other.Energy;
            MaxEnergy = _other.MaxEnergy;
            Def = _other.Def;
            Res = _other.Res;
            SubtypeId = _other.SubtypeId;
            OverchargeRemainingPercent = _other.OverchargeRemainingPercent;
        }
    }

    public class SyncObject
    {
        public bool HasShield = false;
        public List<OtherCharacterShieldData> m_OthersShieldData = new List<OtherCharacterShieldData>();
        public MyShieldData m_MyShieldData = new MyShieldData();
        
        public void Clear()
        {
            HasShield = false;
            m_OthersShieldData.Clear();
            m_MyShieldData.Clear();
        }
    }

}
