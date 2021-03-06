// ;
using ExShared;
using Sandbox.Game;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

using PluginModType = PocketShieldCore.PocketShieldAPIV2.PluginModType;
using Modifiers = System.Collections.Generic.Dictionary<VRage.Utils.MyStringHash, float>;

namespace PocketShieldCore
{
    [ProtoBuf.ProtoContract]
    public struct DefResPair
    {
        public bool IsZero { get { return Def == 0.0f && Res == 0.0f; } }

        [ProtoBuf.ProtoMember(1)] public float Def;
        [ProtoBuf.ProtoMember(2)] public float Res;
    }

    public class ShieldEmitter
    {
        public const float PLUGIN_POWER_MOD = 1.1f;

        public static ulong PluginChangedCalled { get; private set; } = 0;
        public static ulong PluginChangedPerformed { get; private set; } = 0;

        public float ShieldEnergyPercent { get { return Energy / MaxEnergy; } }
        public float OverchargeRemainingPercent { get { if (OverchargeDuration == 0.0f) return 0.0f; return m_OverchargeRemainingTicks / (OverchargeDuration * 60.0f); } }

        public bool IsManual { get; private set; } = false;
        public MyStringHash SubtypeId { get; private set; } = MyStringHash.NullOrEmpty;
        public IMyCharacter Character { get; private set; } = null;

        public bool IsTurnedOn { get { return m_IsTurnedOn; } set { m_IsTurnedOn = value; RequireSync = true; } }
        public bool IsActive { get { return m_IsActive; } set { m_IsActive = value; RequireSync = true; } }
        public float Energy { get; set; } = 0.0f;
        public float MaxEnergyMod { get; private set; } = 0.0f;
        public int PluginsCount { get; private set; } = 0;
        public bool IsOverchargeActive { get; private set; } = false;
        public bool RequireSync { get; set; } = true;

        public float MaxEnergy { get; private set; } = 0.0f;
        public float ChargeRate { get; private set; } = 0.0f;
        public float ChargeDelay { get; private set; } = 0.0f;
        public float OverchargeDuration { get; private set; } = 0.0f;
        public float OverchargeDefBonus { get; private set; } = 0.0f;
        public float OverchargeResBonus { get; private set; } = 0.0f;
        public double PowerConsumption { get; private set; } = 0.0;
        public int MaxPluginsCount { get { return m_BaseMaxPluginsCount; } }
        public Dictionary<MyStringHash, DefResPair> DefResList { get; private set; } = new Dictionary<MyStringHash, DefResPair>();

        //public List<object> Stats { get; private set; } = new List<object>()
        //{
        //    MyStringHash.NullOrEmpty,   /* SubtypeId */
        //    0.0f,                       /* Energy */
        //    false,                      /* IsOverchargeActive */
        //    0.0f,                       /* MaxEnergy */
        //    0.0f,                       /* ChargeRate */
        //    0.0f,                       /* ChargeDelay */
        //    0.0f,                       /* OverchargeDuration */
        //    0.0f,                       /* OverchargeDefResBonus */
        //    0.0,                        /* PowerConsumption */
        //    new Dictionary<MyStringHash, float>(MyStringHash.Comparer),     /* Def */
        //    new Dictionary<MyStringHash, float>(MyStringHash.Comparer)      /* Res */
        //};





        #region Base Stats
        private float m_BaseMaxEnergy = 0.0f;
        private float m_BaseChargeRate = 0.0f;
        private float m_BaseChargeDelay = 0.0f;
        private float m_BaseOverchargeDuration = 0.0f;
        private float m_BaseOverchargeDefBonus = 0.0f;
        private float m_BaseOverchargeResBonus = 0.0f;
        private double m_BasePowerConsumption = 0.0;
        private int m_BaseMaxPluginsCount = 0;
        private Dictionary<MyStringHash, float> m_BaseDef = new Dictionary<MyStringHash, float>(MyStringHash.Comparer);
        private Dictionary<MyStringHash, float> m_BaseRes = new Dictionary<MyStringHash, float>(MyStringHash.Comparer);
        #endregion

        private bool m_IsTurnedOn = true;
        private bool m_IsActive = true;
        private int m_ChargeDelayRemainingTicks = 0;
        private int m_OverchargeRemainingTicks = 0;
        private Dictionary<MyStringHash, float> m_Def = new Dictionary<MyStringHash, float>(MyStringHash.Comparer);
        private Dictionary<MyStringHash, float> m_Res = new Dictionary<MyStringHash, float>(MyStringHash.Comparer);
        private Dictionary<MyStringHash, uint> m_Plugins = new Dictionary<MyStringHash, uint>(MyStringHash.Comparer);
        private Dictionary<MyStringHash, uint> m_NewPlugins = new Dictionary<MyStringHash, uint>(MyStringHash.Comparer);

        private int m_Ticks = 0;
        private Logger m_Logger = null;
        
        private static VRage.Game.MyDefinitionId s_PoweKitDefinitionID = default(VRage.Game.MyDefinitionId);


        public ShieldEmitter(PocketShieldAPIV2.ShieldEmitterProperties _properties, IMyCharacter _character, Logger _logger)
        {
            Character = _character;
            m_Logger = _logger;

            string logString = ">> Character [" + Utils.GetCharacterName(_character) + "] <" + _character.EntityId + ">";
            if (_character.IsBot)
                logString += " (Npc)";
            m_Logger.WriteLine(logString);

            SubtypeId = _properties.SubtypeId;
            IsManual = _properties.IsManual;

            m_BaseMaxEnergy = _properties.BaseMaxEnergy;
            m_BaseChargeRate = _properties.BaseChargeRate;
            m_BaseChargeDelay = _properties.BaseChargeDelay;
            m_BaseOverchargeDuration = _properties.BaseOverchargeDuration;
            m_BaseOverchargeDefBonus = _properties.BaseOverchargeDefBonus;
            m_BaseOverchargeResBonus = _properties.BaseOverchargeResBonus;
            m_BasePowerConsumption = _properties.BasePowerConsumption;
            m_BaseMaxPluginsCount = _properties.MaxPluginsCount;

            m_Logger.WriteLine("Def Count = " + _properties.BaseDef.Count);

            foreach (var key in _properties.BaseDef.Keys)
            {
                m_BaseDef[key] = _properties.BaseDef[key];
            }
            foreach (var key in _properties.BaseRes.Keys)
            {
                m_BaseRes[key] = _properties.BaseRes[key];
            }

            m_Logger.WriteLine("Init base value", 2);
            InitBaseValue();
            ConstructDefResList();

            //Stats[0] = SubtypeId;
            //Stats[1] = Energy;
            //Stats[2] = IsOverchargeActive;
            //Stats[3] = MaxEnergy;
            //Stats[4] = ChargeRate;
            //Stats[5] = ChargeDelay;
            //Stats[6] = OverchargeDuration;
            //Stats[7] = m_BaseOverchargeDefResBonus;
            //Stats[8] = PowerConsumption;
            //Stats[9] = m_Def;
            //Stats[10] = m_Res;



            // SAN check;
            logString = "\n";
            logString += "  SubtypuId = " + SubtypeId + "\n";
            logString += "  m_MaxPluginsCount = " + m_BaseMaxPluginsCount + "\n";
            logString += "  m_BaseMaxEnergy = " + m_BaseMaxEnergy + "\n";
            logString += "  m_BaseChargeRate = " + m_BaseChargeRate + "\n";
            logString += "  m_BaseChargeDelay = = " + m_BaseChargeDelay + "\n";
            logString += "  m_BaseOverchargeDuration = " + m_BaseOverchargeDuration + "\n";
            logString += "  m_BaseOverchargeDefBonus = " + m_BaseOverchargeDefBonus + "\n";
            logString += "  m_BaseOverchargeResBonus = " + m_BaseOverchargeResBonus + "\n";
            logString += "  m_BasePowerConsumption = " + m_BasePowerConsumption + "\n";

            foreach (var key in _properties.BaseDef.Keys)
            {
                logString += "  m_BaseDef[" + key.String + "] = " + m_BaseDef[key] + "\n";
            }
            foreach (var key in _properties.BaseRes.Keys)
            {
                logString += "  m_BaseRes[" + key.String + "] = " + m_BaseRes[key] + "\n";
            }
            m_Logger.WriteLine(logString, 2);
            // ;
        }

        public void Update(int _ticks)
        {
            m_Ticks += _ticks;
            if (m_Ticks >= 2000000000)
                m_Ticks -= 2000000000;

            if (!m_IsTurnedOn)
            {
                m_OverchargeRemainingTicks = 0;
                m_Logger.WriteLine("Shield is turned off (skip " + _ticks + " ticks) (internal ticks: " + m_Ticks + ")", 5);
                return;
            }

            m_Logger.WriteLine("Updating shield (skip " + _ticks + " ticks) (internal ticks: " + m_Ticks + ")", 5);
            string logString = ("  Energy: " + (int)Energy);

            // recharge shield;
            if (IsOverchargeActive)
                m_ChargeDelayRemainingTicks = 0;
            int chargeTicks = _ticks - m_ChargeDelayRemainingTicks;
            if (chargeTicks <= 0)
            {
                m_ChargeDelayRemainingTicks -= _ticks;
                logString += string.Format(" (delay {0:0.##}s)", m_ChargeDelayRemainingTicks / 60.0f);
            }
            else
            {
                m_ChargeDelayRemainingTicks = 0;
                if (Character.SuitEnergyLevel > 0.0f)
                {
                    double powerCost = PowerConsumption * chargeTicks / 60.0;
                    if (Energy < MaxEnergy)
                    {
                        //m_Logger.WriteLine("  PowerCost = " + powerCost + " (" + PowerConsumption / 60.0 + "/tick) (" + PowerConsumption + "/s)");
                        float chargeAmount = ChargeRate * (chargeTicks / 60.0f);
                        if (Character.SuitEnergyLevel > Constants.SHIELD_QUICKCHARGE_POWER_THRESHOLD)
                            chargeAmount *= 2.0f;

                        Energy += chargeAmount;
                        logString += string.Format(" -> {0:0}/{1:0} (+{2:0.##})", (int)Energy, (int)MaxEnergy, chargeAmount);
                        if (Energy > MaxEnergy)
                            Energy = MaxEnergy;

                        RequireSync = true;
                    }
                    else
                    {
                        Energy = MaxEnergy;
                        logString += " (maxed)";
                        powerCost *= 0.01;
                        //m_Logger.WriteLine("  PowerCost = " + powerCost + " (" + PowerConsumption / 60.0 + "/tick) (" + PowerConsumption + "/s) (upkeep)");
                    }
                    float suit = Character.SuitEnergyLevel;
                    MyVisualScriptLogicProvider.SetPlayersEnergyLevel(Character.ControllerInfo.ControllingIdentityId, Character.SuitEnergyLevel - (float)powerCost);

                    //m_Logger.WriteLine("  Suit Level: " + suit + " -> " + Character.SuitEnergyLevel + " (-" + (suit - Character.SuitEnergyLevel) + ")");
                }
                else
                {
                    logString += " (no power)";
                }
            }
            m_Logger.WriteLine(logString, 5);

            if (IsOverchargeActive)
            {
                int overchargeTicks = _ticks - m_OverchargeRemainingTicks;
                if (overchargeTicks <= 0)
                    m_OverchargeRemainingTicks -= _ticks;
                else
                {
                    m_OverchargeRemainingTicks = 0;
                    // TODO: (maybe) handle undercharge :))
                    DeactiveOvercharge();
                }

                RequireSync = true;
            }

        }

        private void ProcessPlugins()
        {
            m_Logger.WriteLine("Plugin List: ", 4);
            foreach (var plugin in m_Plugins)
            {
                m_Logger.WriteLine("  " + plugin.Key.String + " (" + plugin.Value + ")", 4);
            }

            // revert everything to base value;
            InitBaseValue();
            
            float powerCost = 1.0f;
            foreach (MyStringHash pluginId in m_Plugins.Keys)
            {
                if (!ShieldManager.PluginModifiers.ContainsKey(pluginId))
                    continue;

                Modifiers modifiers = ShieldManager.PluginModifiers[pluginId];
                foreach (var pair in modifiers)
                {
                    if (pair.Key == PluginModType.Capacity)
                    {
                        float mod = Utils.MyPow(1.0f + modifiers[PluginModType.Capacity], m_Plugins[pluginId]);
                        MaxEnergy = m_BaseMaxEnergy * mod;
                        MaxEnergyMod = 1.0f - mod;
                    }
                    else if (pair.Key.String.StartsWith("Def"))
                    {
                        string damageTypeString = pair.Key.String.Substring(3);
                        if (!m_BaseDef.ContainsKey(MyStringHash.GetOrCompute(damageTypeString)))
                            m_BaseDef[MyStringHash.GetOrCompute(damageTypeString)] = 0.0f;

                        float mod = Utils.MyPow(1.0f - pair.Value, m_Plugins[pluginId]);
                        float bypass = (1.0f - m_BaseDef[MyStringHash.GetOrCompute(damageTypeString)]) * mod;
                        if (IsOverchargeActive)
                            bypass *= OverchargeDefBonus;
                        m_Def[MyStringHash.GetOrCompute(damageTypeString)] = 1.0f - bypass;
                    }
                    else if (pair.Key.String.StartsWith("Res"))
                    {
                        string damageTypeString = pair.Key.String.Substring(3);
                        if (!m_BaseRes.ContainsKey(MyStringHash.GetOrCompute(damageTypeString)))
                            m_BaseRes[MyStringHash.GetOrCompute(damageTypeString)] = 0.0f;

                        float mod = Utils.MyPow(1.0f - pair.Value, m_Plugins[pluginId]);
                        float bypass = (1.0f - m_BaseRes[MyStringHash.GetOrCompute(damageTypeString)]) * mod;
                        if (IsOverchargeActive)
                            bypass *= OverchargeResBonus;
                        m_Res[MyStringHash.GetOrCompute(damageTypeString)] = 1.0f - bypass;
                    }
                }

                powerCost *= Utils.MyPow(PLUGIN_POWER_MOD, m_Plugins[pluginId]);
            }

            PowerConsumption = m_BasePowerConsumption * powerCost;

            // construct DefList, ResList;
            ConstructDefResList();

            RequireSync = true;
        }

        public static bool TakeDamage(ShieldEmitter _emitter, ref MyDamageInformation _damageInfo)
        {
            return false;

            // TODO: use this methd instead;
        }

        public bool TakeDamage(ref MyDamageInformation _damageInfo)
        {
            if (!m_IsTurnedOn)
            {
                m_Logger.WriteLine("Shield is turned off (Incoming " + _damageInfo.Amount + " " + _damageInfo.Type.String + " damage)", 1);
                return false;
            }
            if (!m_IsActive)
            {
                m_Logger.WriteLine("Shield is inactive (Incoming " + _damageInfo.Amount + " " + _damageInfo.Type.String + " damage)", 1);
                return false;
            }

            m_Logger.WriteLine("Incoming " + _damageInfo.Amount + " " + _damageInfo.Type.String + " damage", 1);
            if (Character == null)
            {
                m_Logger.WriteLine("  There is no Character to take damage (this should not happen)", 1);
                return false;
            }
            
            if (_damageInfo.Type == MyDamageType.Fall || _damageInfo.Type == MyDamageType.Environment)
            {
                if (m_ChargeDelayRemainingTicks < 60)
                    m_ChargeDelayRemainingTicks = 60; // Fall/Environment damage only delay 1 second;
            }
            else
            {
                m_ChargeDelayRemainingTicks = (int)(ChargeDelay * 60.0f); // 60 ticks per second;
            }

            if (Energy <= 0.0f)
            {
                m_Logger.WriteLine("  Shield depleted", 2);
                return false;
            }

            // TODO: process damage;
            float energyBeforeDamage = Energy; // debug;

            float defRate = GetDefenseAgainst(_damageInfo.Type);
            float resRate = GetResistanceAgainst(_damageInfo.Type);

            float shieldDamage = 0.0f;
            float healthDamage = 0.0f;

            if (defRate > 1.0f)
            {
                // TODO: handle case when Defense > 100%;
                /* When Defense > 100%, the projectile will be deflected with bonus damage */
                shieldDamage = _damageInfo.Amount;
            }
            else if (defRate >= 0.0f)
            {
                shieldDamage = _damageInfo.Amount * defRate;
                healthDamage = _damageInfo.Amount - shieldDamage;
            }
            else if (defRate < 0.0f)
            {
                healthDamage = _damageInfo.Amount * (1.0f - defRate);
            }
            shieldDamage *= (1.0f - resRate);

            float totalShieldDamage = 0.0f;
            while (shieldDamage != 0.0f)
            {
                if (Energy >= shieldDamage)
                {
                    totalShieldDamage += shieldDamage;
                    Energy -= shieldDamage;
                    if (Energy > MaxEnergy)
                        Energy = MaxEnergy;

                    break;
                }

                totalShieldDamage += Energy;
                shieldDamage -= Energy;
                Energy = 0.0f;

                // manual shield never trigger Overcharge;
                if (IsManual)
                    break;

                if (!TryActiveOvercharge())
                {
                    healthDamage += shieldDamage;
                    break;
                }
            }
            
            _damageInfo.Amount = healthDamage;

            m_Logger.WriteLine(string.Format("  Shield damage: {0:0.##} ({1:0.##}%) (res {2:0.##}%), health damage: {3:0.##}", totalShieldDamage, defRate * 100.0f, resRate * 100.0f, healthDamage), 2);
            m_Logger.WriteLine(string.Format("  Shield energy: {0:0.##} -> {1:0.##}", energyBeforeDamage, Energy), 2);

            return totalShieldDamage != 0.0f;
        }
        
        public void AddPlugins(ref List<MyStringHash> _newPlugins)
        {
            m_Logger.WriteLine("Adding Plugins");
            ++PluginChangedCalled;

            int count = 0;
            for (int i = 0; i < _newPlugins.Count && i < m_BaseMaxPluginsCount; ++i)
            {
                if (m_NewPlugins.ContainsKey(_newPlugins[i]))
                    m_NewPlugins[_newPlugins[i]] += 1;
                else
                    m_NewPlugins[_newPlugins[i]] = 1;
                ++count;
            }
            _newPlugins.RemoveRange(0, count);

            m_Logger.WriteLine("new = " + m_NewPlugins.Count + "old = " + m_Plugins.Count);

            bool isDirty = false;
            if (m_NewPlugins.Count != m_Plugins.Count)
                isDirty = true;
            else
            {
                foreach (var key in m_NewPlugins.Keys)
                {
                    if (!m_Plugins.ContainsKey(key) || m_Plugins[key] != m_NewPlugins[key])
                    {
                        isDirty = true;
                        break;
                    }
                }
            }

            if (isDirty)
            {
                m_Logger.WriteLine("> Plugin list changed (" + PluginsCount + " -> " + count + ")", 4);
                ++PluginChangedPerformed;

                m_Plugins.Clear();
                foreach (var pair in m_NewPlugins)
                {
                    m_Plugins[pair.Key] = pair.Value;
                }

                PluginsCount = count;
                ProcessPlugins();
            }
            
            m_NewPlugins.Clear();
        }

        private bool TryActiveOvercharge()
        {
            if (s_PoweKitDefinitionID == default(VRage.Game.MyDefinitionId))
            {
                m_Logger.WriteLine("Powerkit DefinitionID is not initialized");
                VRage.Game.MyDefinitionId.TryParse("ConsumableItem", "Powerkit", out s_PoweKitDefinitionID);
            }

            MyInventory inventory = Character.GetInventory() as MyInventory;
            if (inventory == null)
                return false;

            var item = inventory.FindItem(s_PoweKitDefinitionID);
            if (item.HasValue)
            {
                inventory.RemoveItems(item.Value.ItemId, 1, false);

                Energy = MaxEnergy;
                m_OverchargeRemainingTicks = (int)(OverchargeDuration * 60.0f);
                RequireSync = true;

                return true;
            }

            return false;
        }

        private void DeactiveOvercharge()
        {
            // TODO: deactive overcharge;


        }

        private void InitBaseValue()
        {
            MaxEnergy = m_BaseMaxEnergy;
            ChargeRate = m_BaseChargeRate;
            ChargeDelay = m_BaseChargeDelay;
            OverchargeDuration = m_BaseOverchargeDuration;
            OverchargeDefBonus = m_BaseOverchargeDefBonus;
            OverchargeResBonus = m_BaseOverchargeResBonus;
            PowerConsumption = m_BasePowerConsumption;

            foreach (var key in m_BaseDef.Keys)
            {
                m_Def[key] = m_BaseDef[key];
            }
            foreach (var key in m_BaseRes.Keys)
            {
                m_Res[key] = m_BaseRes[key];
            }
        }

        private void ConstructDefResList()
        {
            DefResList.Clear();

            foreach (var key in m_Def.Keys)
            {
                if (DefResList.ContainsKey(key))
                {
                    var pair = DefResList[key];
                    pair.Def = m_Def[key];
                    DefResList[key] = pair;
                }
                else
                {
                    DefResList[key] = new DefResPair() { Def = m_Def[key] };
                }
            }

            foreach (var key in m_Res.Keys)
            {
                if (DefResList.ContainsKey(key))
                {
                    var pair = DefResList[key];
                    pair.Res = m_Res[key];
                    DefResList[key] = pair;
                }
                else
                {
                    DefResList[key] = new DefResPair() { Res = m_Res[key] };
                }
            }
        }

        public float GetDefenseAgainst(MyStringHash _damageType)
        {
            if (m_Def.ContainsKey(_damageType))
                return m_Def[_damageType];
            return 0.0f;
        }

        public float GetResistanceAgainst(MyStringHash _damageType)
        {
            if (m_Res.ContainsKey(_damageType))
                return m_Res[_damageType];
            return 0.0f;
        }





    }
}
