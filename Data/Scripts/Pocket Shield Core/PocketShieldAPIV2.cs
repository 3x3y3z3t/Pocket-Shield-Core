/** Pocket Shield Core API v1
 * 
 * ==============================
 * Quick Modding Guide
 * ==============================
 * 
 * To register your mod with Pocket Shield Core, follow these steps:
 * 
 * 1. Grab one of the API files and put it in your mod (you are looking at one of those).
 * 2. Call API init somewhere in your Session Component:
 * 
 *                  PocketShieldAPI.Init("Your Mod Name");
 * 
 * 3. Call API shutdown in UnloadData:
 * 
 *                  PocketShieldAPI.Close();
 * 
 * 
 * ==============================
 * Optional
 * ==============================
 * 
 * 1. Call          PocketShieldAPI.RegisterCompileEmitterPropertiesCallback(callback);
 * 
 * to register a callback for using when creating Shield Emitter item. 
 * Your callback will be called, and a Shield Emitter item will be created or not 
 * depends on what your callback will return. Remember to UnRegister it when mod unload.
 * 
 * 2. Call          PocketShieldAPI.SetPluginModifier(pluginSubtypeId, modAmount);
 * 
 * to set modifier amount for a Plugin item. Value can be overwritten.
 * 
 * 3. Call          PocketShieldAPI.RegisterShieldIcon(ShieldIconDrawInfo);
 * 
 * to set custom *shield* icon on HUD Panel. Texture atlas supported.
 * 
 * 4. Call          PocketShieldAPI.RegisterStatIcons(ItemCardDrawInfo);
 * 
 * to set custom pair of *defense-resistance stat* icons on HUD Panel. 
 * I call it a *DefRes Pair*, they always comes in pair of two icons 
 * (Def on the left, Res on the right). Texture atlas supported.
 * 
 * 
 * You can take a look at full documentation here:
 * 
 */


using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;

using ServerData = VRage.MyTuple<
    string,
    System.Collections.Generic.Dictionary<VRage.Utils.MyStringHash, System.Collections.Generic.Dictionary<VRage.Utils.MyStringHash, float>>,
    System.Collections.Generic.List<System.Delegate>>;

using ClientData = VRage.MyTuple<
    string,
    System.Collections.Generic.List<System.Collections.Generic.List<object>>,
    System.Collections.Generic.List<System.Collections.Generic.List<object>>>;

namespace PocketShieldCore
{
    public class PocketShieldAPIV2
    {
        /// <summary> Determines which side returned for a specific request. </summary>
        public enum ReturnSide { Server = 1 << 0, Client = 1 << 1 }

        public static class PluginModType
        {
            public static MyStringHash Capacity = MyStringHash.GetOrCompute("Capacity");

            public static MyStringHash DefBullet = MyStringHash.GetOrCompute("DefBullet");
            public static MyStringHash ResBullet = MyStringHash.GetOrCompute("ResBullet");

            public static MyStringHash DefExplosion = MyStringHash.GetOrCompute("DefExplosion");
            public static MyStringHash ResExplosion = MyStringHash.GetOrCompute("ResExplosion");

            public static MyStringHash DefEnvironment = MyStringHash.GetOrCompute("DefEnvironment");
            public static MyStringHash ResEnvironment = MyStringHash.GetOrCompute("ResEnvironment");
        }

        /// <summary>
        /// 
        /// </summary>
        public class ShieldEmitterStats
        {

        }

        /// <summary>
        /// Basic properties used to construct a ShieldEmitter.
        /// ShieldEmitter will not be construct without these properties passed to the constructor. </summary>
        public class ShieldEmitterProperties
        {
            #region Public Properties
            /// <summary> 
            /// SubtypeId of shield emitter item in inventory.
            /// This is used to differentiate between ShieldEmitter types. </summary>
            public MyStringHash SubtypeId
            {
                get { return (MyStringHash)m_Data[0]; }
                set { m_Data[0] = value; }
            }

            /// <summary>
            /// Determine if this ShieldEmitter is a manual one.
            /// A Manual Emitter only emit shield when active (manually). </summary>
            public bool IsManual
            {
                get { return (bool)m_Data[1]; }
                set { m_Data[1] = value; }
            }

            /// <summary> Base Max energy. Shield is down when Energy reachs 0. </summary>
            public float BaseMaxEnergy
            {
                get { return (float)m_Data[2]; }
                set { m_Data[2] = value; }
            }

            /// <summary> Base Energy Charge rate. Shield will be charged this amount of Energy per second. </summary>
            public float BaseChargeRate
            {
                get { return (float)m_Data[3]; }
                set { m_Data[3] = value; }
            }

            /// <summary> Base Charge delay. Shield will stop charging on damage taken, and will wait for this amount of second until start charging again. </summary>
            public float BaseChargeDelay
            {
                get { return (float)m_Data[4]; }
                set { m_Data[4] = value; }
            }

            /// <summary> Base Overcharge duration. Overcharge will last this amount of second. </summary>
            public float BaseOverchargeDuration
            {
                get { return (float)m_Data[5]; }
                set { m_Data[5] = value; }
            }

            /// <summary> Base Defense bonus during Overcharge. 1.0f means +100% </summary>
            public float BaseOverchargeDefBonus
            {
                get { return (float)m_Data[6]; }
                set { m_Data[6] = value; }
            }

            /// <summary> Base Resistannce bonus during Overcharge. 1.0f means +100% </summary>
            public float BaseOverchargeResBonus
            {
                get { return (float)m_Data[7]; }
                set { m_Data[7] = value; }
            }

            /// <summary> Base Power consumption. Emitter will consume this amount of power per second. 0.01f means -1% per second. </summary>
            public double BasePowerConsumption
            {
                get { return (double)m_Data[8]; }
                set { m_Data[8] = value; }
            }

            /// <summary> Max number of Plugins that this Emitter supports. </summary>
            public int MaxPluginsCount
            {
                get { return (int)m_Data[9]; }
                set { m_Data[9] = value; }
            }

            /// <summary> A list of Base Defense against each damage type. </summary>
            public Dictionary<MyStringHash, float> BaseDef
            {
                get { return (Dictionary<MyStringHash, float>)m_Data[10]; }
            }

            /// <summary> A list of Base Resistance against each damage type. </summary>
            public Dictionary<MyStringHash, float> BaseRes
            {
                get { return (Dictionary<MyStringHash, float>)m_Data[11]; }
            }
            #endregion

            /// <summary> Construct a ShieldEmitterProperties object from a set of data. Data will not be validate. </summary>
            /// <param name="_data"> Data to be copied </param>
            public ShieldEmitterProperties(List<object> _data)
            {
                if (_data != null && _data.Count == RawData.Count)
                {
                    for (int i = 0; i < m_Data.Count && i < _data.Count; ++i)
                    {
                        m_Data[i] = _data[i];
                    }
                }
            }

            /// <summary> Clears this ShieldEmitterProperties object (resets everything to 0) without destroy the object.
            /// This is useful when you want to clear a cached the object allocation. </summary>
            public void Clear()
            {
                SubtypeId = MyStringHash.NullOrEmpty;

                IsManual = false;
                MaxPluginsCount = 0;
                BaseMaxEnergy = 0.0f;
                BaseChargeRate = 0.0f;
                BaseChargeDelay = 0.0f;
                BaseOverchargeDuration = 0.0f;
                BaseOverchargeDefBonus = 0.0f;
                BaseOverchargeResBonus = 0.0f;
                BasePowerConsumption = 0.0;

                BaseDef.Clear();
                BaseRes.Clear();
            }

            /// <summary> Replaces this ShieldEmitterProperties object with a set of data.
            /// This method doesn't allocate memory. Data will not be validated </summary>
            /// <param name="_data"> Data to be copied </param>
            public void ReplaceData(List<object> _data)
            {
                if (_data == null)
                    Clear();

                for (int i = 0; i < m_Data.Count && i < _data.Count; ++i)
                {
                    m_Data[i] = _data[i];
                }
            }

            #region Internal Stuff
            /// <summary> Raw data. Do not modify this list. </summary>
            public List<object> RawData { get { return m_Data; } }

            private readonly List<object> m_Data = new List<object>()
            {
                MyStringHash.NullOrEmpty,               /* SubtypeId */
                false,                                  /* IsManual */
                0.0f,                                   /* BaseMaxEnergy */
                0.0f,                                   /* BaseChargeRate */
                0.0f,                                   /* BaseChargeDelay */
                0.0f,                                   /* BaseOverchargeDuration */
                0.0f,                                   /* BaseOverchargeDefBonus */
                0.0f,                                   /* BaseOverchargeResBonus */
                0.0,                                    /* BasePowerConsumption */
                0,                                      /* MaxPluginsCount */
                new Dictionary<MyStringHash, float>(),  /* BaseDef */
                new Dictionary<MyStringHash, float>()   /* BaseRes */
            };
            #endregion

        }

        /// <summary>
        /// Basic properties used to draw a shield icon.
        /// If no data is supplied, a default icon will be drawn. </summary>
        public class ShieldIconDrawInfo
        {
            #region Public Properties
            /// <summary> 
            /// SubtypeId of shield emitter to assign to.
            /// This is used to differentiate icons between ShieldEmitter types. </summary>
            public MyStringHash SubtypeId
            {
                get { return (MyStringHash)m_Data[0]; }
                set { m_Data[0] = value; }
            }

            /// <summary>  Material name. This can be a whole texture or an atlas one. </summary>
            public MyStringId Material
            {
                get { return (MyStringId)m_Data[1]; }
                set { m_Data[1] = value; }
            }

            /// <summary> Enable UV mode. Set this to true if you use an atlas texture. </summary>
            public bool UvEnabled
            {
                get { return (bool)m_Data[2]; }
                set { m_Data[2] = value; }
            }

            public Vector2 UvSize
            {
                get { return (Vector2)m_Data[3]; }
                set { m_Data[3] = value; }
            }

            public Vector2 UvOffset
            {
                get { return (Vector2)m_Data[4]; }
                set { m_Data[4] = value; }
            }
            #endregion

            /// <summary> Construct a ShieldIconDrawInfo object from a set of data. Data will not be validate. </summary>
            /// <param name="_data"> Data to be copied </param>
            public ShieldIconDrawInfo(List<object> _data)
            {
                if (_data != null)
                {
                    for (int i = 0; i < m_Data.Count && i < _data.Count; ++i)
                    {
                        m_Data[i] = _data[i];
                    }
                }
            }

            #region Internal Stuff
            /// <summary> Raw data. Do not modify this list. </summary>
            public List<object> RawData { get { return m_Data; } }

            private readonly List<object> m_Data = new List<object>()
            {
                MyStringHash.NullOrEmpty,   /* SubtypeId */
                MyStringId.NullOrEmpty,     /* Material */
                false,                      /* UvEnabled */
                Vector2.Zero,               /* UvSize */
                Vector2.Zero                /* UvOffset */
            };
            #endregion

        }

        /// <summary>
        /// Basic properties used to draw a Defense/Resistance stat item card for a specific damage type.
        /// If no data is supplied, this damage type's stat will not be displayed. </summary>
        public class StatIconDrawInfo
        {
            #region Public Properties
            /// <summary> Damage type of this icon(s). </summary>
            public MyStringHash DamageType
            {
                get { return (MyStringHash)m_Data[0]; }
                set { m_Data[0] = value; }
            }

            /// <summary>  Material name. This can be a whole texture or an atlas one. </summary>
            public MyStringId Material
            {
                get { return (MyStringId)m_Data[1]; }
                set { m_Data[1] = value; }
            }

            /// <summary> Enable UV mode. Set this to true if you use an atlas texture. </summary>
            public bool UvEnabled
            {
                get { return (bool)m_Data[2]; }
                set { m_Data[2] = value; }
            }

            public Vector2 UvSize
            {
                get { return (Vector2)m_Data[3]; }
                set { m_Data[3] = value; }
            }

            public Vector2 UvOffset
            {
                get { return (Vector2)m_Data[4]; }
                set { m_Data[4] = value; }
            }
            #endregion

            /// <summary> Construct a ItemCardDrawInfo object from a set of data. Data will not be validate. </summary>
            /// <param name="_data"> Data to be copied </param>
            public StatIconDrawInfo(List<object> _data)
            {
                if (_data != null)
                {
                    for (int i = 0; i < m_Data.Count && i < _data.Count; ++i)
                    {
                        m_Data[i] = _data[i];
                    }
                }
            }

            #region Internal Stuff
            /// <summary> Raw data. Do not modify this list. </summary>
            public List<object> RawData { get { return m_Data; } }

            private readonly List<object> m_Data = new List<object>()
            {
                MyStringHash.NullOrEmpty,   /* SubtypeId */
                MyStringId.NullOrEmpty,     /* Material */
                false,                      /* UvEnabled */
                Vector2.Zero,               /* UvSize */
                Vector2.Zero                /* UvOffset */
            };
            #endregion

        }

        public const long MOD_ID = 2739353433L; // Core mod's ID;

        public const string SERVER_BACKEND_VERSION = "2.1";
        public const string CLIENT_BACKEND_VERSION = "2.0";
        public const string STR_API_VERSION = "Ver2";

        /// <summary> Flag to determine if PocketShieldAPI is registered server-side and ready or not. </summary>
        public static bool ServerReady { get { if (s_Instance != null) return s_Instance.m_IsServerReady; return false; } }

        /// <summary> Flag to determine if PocketShieldAPI is registered client-side and ready or not. </summary>
        public static bool ClientReady { get { if (s_Instance != null) return s_Instance.m_IsClientReady; return false; } }

        /// <summary> Backend API version. </summary>
        private static string ServerBackendVersion = "";

        /// <summary> Backend API version. </summary>
        private static string ClientBackendVersion = "";


        /// <summary>
        /// Initialize PocketShieldAPI and register with Core mod.
        /// You need to pass a string to identify your mod. This string will be useful to see which mod won't Close properly.
        /// You can pass a callback for when register is done. <summary>
        /// <param name="_modInfo"> A string to identify your mod </param>
        /// <param name="_registerFinishedCallback"> A callback to call when register is done </param>
        /// <param name="_logCallback"> A Log function to call when the API want to log something (you need to provide the "write" function) </param>
        public static void Init(string _modInfo, Action<ReturnSide> _registerFinishedCallback = null, Action<string> _logCallback = null)
        {
            _logCallback?.Invoke(STR_LOG_PREFIX + "PocketShield API is initializing");

            MyAPIGateway.Utilities.RegisterMessageHandler(MOD_ID, PocketShieldAPIV2_ModRegisterReturnHandle);

            s_Instance = new PocketShieldAPIV2(_modInfo, _registerFinishedCallback, _logCallback);
            MyAPIGateway.Utilities.SendModMessage(MOD_ID, STR_REGISTER_MOD + STR_API_VERSION + "=" + _modInfo);

            s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API initialization done");
        }

        /// <summary> Unregister PocketShieldAPI from Core mod.
        /// This also unregister any callbacks that you have registered AND not unregistered before. </summary>
        /// <returns> Always true </returns>
        public static bool Close()
        {
            s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API is shutting down");
            if (ServerReady)
            {

            }

            if (ClientReady)
            {

            }

            MyAPIGateway.Utilities.SendModMessage(MOD_ID, STR_UNREGISTER_MOD + "=" + s_Instance.m_ModInfo);
            MyAPIGateway.Utilities.UnregisterMessageHandler(MOD_ID, PocketShieldAPIV2_ModRegisterReturnHandle);

            s_Instance.m_IsServerReady = false;
            s_Instance.m_IsClientReady = false;

            s_Instance = null;

            s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API shut down");
            return true;
        }

        #region Server-side API Methods
        public static bool Server_RegisterEmitter(MyStringHash _subtypeId, ShieldEmitterProperties _properties)
        {
            if (!ServerReady)
            {
                s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API (server) is not initialized");
                return false;
            }

            if (_properties == null)
                return false;

            bool success = ((Func<List<object>, bool>)s_Instance.m_Ref_ExposedFunctions[2]).Invoke(_properties.RawData);
            if (!success)
            {
                s_LogFunc?.Invoke(STR_LOG_PREFIX + "Could not register emitter " + _properties.SubtypeId);
                return false;
            }

            return true;
        }
        
        public static Dictionary<MyStringHash, float> Server_GetPluginModifiers(MyStringHash _subtypeId)
        {
            if (!ServerReady)
            {
                s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API (server) is not initialized");
                return null;
            }

            if (s_Instance.m_Ref_PluginBonusModifiers.ContainsKey(_subtypeId))
                return s_Instance.m_Ref_PluginBonusModifiers[_subtypeId];

            return null;
        }

        public static float Server_GetPluginModifier(MyStringHash _subtypeId, MyStringHash _modifierType)
        {
            if (!ServerReady)
            {
                s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API (server) is not initialized");
                return 0.0f;
            }

            if (s_Instance.m_Ref_PluginBonusModifiers.ContainsKey(_subtypeId) && s_Instance.m_Ref_PluginBonusModifiers[_subtypeId].ContainsKey(_modifierType))
                return s_Instance.m_Ref_PluginBonusModifiers[_subtypeId][_modifierType];

            return 0.0f;
        }

        public static bool Server_SetPluginModifiers(MyStringHash _subtypeId, Dictionary<MyStringHash, float> _modifiers)
        {
            if (!ServerReady)
            {
                s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API (server) is not initialized");
                return false;
            }

            s_Instance.m_Ref_PluginBonusModifiers[_subtypeId] = _modifiers;
            return true;
        }

        public static bool Server_SetPluginModifiers(MyStringHash _subtypeId, MyStringHash _modifierType, float _modifierValue)
        {
            if (!ServerReady)
            {
                s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API (server) is not initialized");
                return false;
            }

            if (!s_Instance.m_Ref_PluginBonusModifiers.ContainsKey(_subtypeId))
                s_Instance.m_Ref_PluginBonusModifiers[_subtypeId] = new Dictionary<MyStringHash, float>(MyStringHash.Comparer);

            s_Instance.m_Ref_PluginBonusModifiers[_subtypeId][_modifierType] = _modifierValue;
            return true;
        }

        public static bool Server_ActivateManualShield(long _character, bool _isActivate)
        {
            if (!ServerReady)
            {
                s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API (server) is not initialized");
                return false;
            }

            ((Action<long, bool>)s_Instance.m_Ref_ExposedFunctions[0]).Invoke(_character, _isActivate);
            return true;
        }

        public static bool Server_TurnShieldOnOff(long _character, bool _turnOn, bool _isManualShield)
        {
            if (!ServerReady)
            {
                s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API (server) is not initialized");
                return false;
            }

            ((Action<long, bool, bool>)s_Instance.m_Ref_ExposedFunctions[1]).Invoke(_character, _turnOn, _isManualShield);
            return true;
        }
        #endregion


        #region Client-side API Methods
        /// <summary> Register a custom shield icon to be drawn on HUD Panel. </summary>
        /// <param name="_data"> Data for a custom shield icon </param>
        /// <returns> False when API is not Ready, otherwise true </returns>
        public static bool Client_RegisterShieldIcon(ShieldIconDrawInfo _data)
        {
            if (!ClientReady)
            {
                s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API (Client) is not initialized");
                return false;
            }

            s_Instance.m_Ref_ShieldIconList.Add(_data.RawData);
            return true;
        }

        /// <summary> Register a set of two icons for Defense and Resistance stat for a single damage type, that will be drawn on HUD Panel. </summary>
        /// <param name="_data"> Data for a set of two icons </param>
        /// <returns> False when API is not Ready, otherwise true </returns>
        public static bool Client_RegisterStatIcon(StatIconDrawInfo _data)
        {
            if (!ClientReady)
            {
                s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API (Client) is not initialized");
                return false;
            }

            s_Instance.m_Ref_StatIconList.Add(_data.RawData);
            return true;
        }
        #endregion




        #region Internal Stuff
        public const string STR_LOG_PREFIX = "[PocketShieldAPIV2] ";
        public const string STR_REGISTER_MOD = "RegMod";
        public const string STR_UNREGISTER_MOD = "UnRegMod";
        public const string STR_INTERNAL_REGISTER_CALLBACK = "RegEvt";
        public const string STR_INTERNAL_UNREGISTER_CALLBACK = "UnRegEvt";

        private static PocketShieldAPIV2 s_Instance = null;
        private static Action<string> s_LogFunc = null;

        private string m_ModInfo = "";
        private bool m_IsServerReady = false;
        private bool m_IsClientReady = false;

        private Action<ReturnSide> m_RegisterFinishedCallback = null;

        Dictionary<MyStringHash, Dictionary<MyStringHash, float>> m_Ref_PluginBonusModifiers = null;
        private List<List<object>> m_Ref_ShieldIconList = null;
        private List<List<object>> m_Ref_StatIconList = null;
        private List<Delegate> m_Ref_ExposedFunctions = null;

        private PocketShieldAPIV2(string _modInfo, Action<ReturnSide> _registerFinishedCallback, Action<string> _logCallback)
        {
            ServerBackendVersion = "";
            ClientBackendVersion = "";

            m_ModInfo = _modInfo;
            m_RegisterFinishedCallback = _registerFinishedCallback;
            s_LogFunc = _logCallback;
        }

        private static void PocketShieldAPIV2_ModRegisterReturnHandle(object _payload)
        {
            // this one should not happens;
            if (s_Instance == null)
                return;

            if (_payload == null)
                return;

            if (_payload is ServerData)
            {
                if (ServerReady)
                {
                    s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShield API (server) is already initialized");
                    return;
                }

                var data = (ServerData)_payload;
                if (!data.Item1.StartsWith("Server") || data.Item2 == null || data.Item3 == null)
                {
                    s_LogFunc?.Invoke(STR_LOG_PREFIX + "Data received from Server APIBackend, but it seems to be corrupted");
                    return;
                }

                ServerBackendVersion = data.Item1.Substring(13);
                if (ServerBackendVersion != SERVER_BACKEND_VERSION)
                    s_LogFunc?.Invoke(STR_LOG_PREFIX + "API Version mismatch (current: " + SERVER_BACKEND_VERSION + ", latest: " + ServerBackendVersion + "), you should update your PocketShieldAPI.cs file, or things may break");
                
                s_Instance.m_Ref_PluginBonusModifiers = data.Item2;
                s_Instance.m_Ref_ExposedFunctions = data.Item3;
                s_Instance.m_IsServerReady = true;

                s_Instance.m_RegisterFinishedCallback?.Invoke(ReturnSide.Server);

                return;
            }

            if (_payload is ClientData)
            {
                if (ClientReady)
                {
                    s_LogFunc?.Invoke(STR_LOG_PREFIX + "PocketShieldAPI (client) is already initialized");
                    return;
                }

                var data = (ClientData)_payload;
                if (!data.Item1.StartsWith("Client") || data.Item2 == null || data.Item3 == null)
                {
                    s_LogFunc?.Invoke(STR_LOG_PREFIX + "Data received from Client APIBackend, but it seems to be corrupted");
                    return;
                }

                ClientBackendVersion = data.Item1.Substring(13);
                if (ClientBackendVersion != CLIENT_BACKEND_VERSION)
                    s_LogFunc?.Invoke(STR_LOG_PREFIX + "API Version mismatch (current: " + CLIENT_BACKEND_VERSION + ", latest: " + ClientBackendVersion + "), you should update your PocketShieldAPI.cs file, or things may break");

                s_Instance.m_Ref_ShieldIconList = data.Item2;
                s_Instance.m_Ref_StatIconList = data.Item3;
                s_Instance.m_IsClientReady = true;

                s_Instance.m_RegisterFinishedCallback?.Invoke(ReturnSide.Client);

                return;
            }
        }

        private static void PocketShieldAPIV2_EmitterListUpdateCallback()
        {

        }
        #endregion

    }
}
