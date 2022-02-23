// ;
using ProtoBuf;
using System.Collections.Generic;
using VRage.ModAPI;
using VRage.Utils;

namespace PocketShieldCore
{
    [ProtoInclude(1000, typeof(Packet_ShieldData))]
    [ProtoInclude(1001, typeof(Packet_ToggleShieldData))]
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

        [ProtoMember(2)] public MyShieldData MyManualShieldData;
        [ProtoMember(3)] public MyShieldData MyAutoShieldData;
        [ProtoMember(4)] public List<OtherCharacterShieldData> OtherManualShieldData;
        [ProtoMember(5)] public List<OtherCharacterShieldData> OtherAutoShieldData;
    }

    [ProtoContract]
    public class MyShieldData
    {
        public bool HasShield { get { return SubtypeId != MyStringHash.NullOrEmpty; } }
        public float EnergyRemainingPercent { get { if (MaxEnergy != 0.0f) return Energy / MaxEnergy; return 0.0f; } }

        [ProtoMember(1)] public MyStringHash SubtypeId;
        [ProtoMember(2)] public bool IsActive;
        [ProtoMember(3)] public bool IsTurnedOn;
        [ProtoMember(4)] public float Energy;
        [ProtoMember(5)] public float MaxEnergy;
        [ProtoMember(6)] public float OverchargeRemainingPercent;
        [ProtoMember(7)] public Dictionary<MyStringHash, DefResPair> DefResList;
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

    public class Packet_ToggleShieldData : PacketBase
    {
        [ProtoMember(1)] public byte Key;
    }
}
