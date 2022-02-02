// ;
using ProtoBuf;
using System.Collections.Generic;
using VRage.ModAPI;
using VRage.Utils;

namespace PocketShieldCore
{
    [ProtoInclude(1000, typeof(Packet_ShieldData))]
    [ProtoContract]
    public abstract class PacketBase
    {
        protected PacketBase()
        { }
    }

    [ProtoContract]
    public class Packet_ShieldData : PacketBase
    {
        [ProtoMember(1)] public ulong PlayerSteamUserId;

        [ProtoMember(2)] public MyShieldData MyShieldData;
        [ProtoMember(3)] public List<OtherCharacterShieldData> OtherShieldData;
    }

    [ProtoContract]
    public class MyShieldData
    {
        public bool HasShield { get { return SubtypeId != MyStringHash.NullOrEmpty; } }
        public float EnergyRemainingPercent { get { if (MaxEnergy != 0.0f) return Energy / MaxEnergy; return 0.0f; } }

        [ProtoMember(1)] public MyStringHash SubtypeId;
        [ProtoMember(2)] public float Energy;
        [ProtoMember(3)] public float MaxEnergy;
        [ProtoMember(4)] public float OverchargeRemainingPercent;
        [ProtoMember(5)] public Dictionary<MyStringHash, DefResPair> DefResList;
    }

    [ProtoContract]
    public class OtherCharacterShieldData
    {
        public IMyEntity Entity;
        public bool ShouldPlaySound = true;

        [ProtoMember(1)] public long EntityId;
        [ProtoMember(2)] public float ShieldAmountPercent;
        [ProtoMember(3)] public int Ticks;
    }
}
