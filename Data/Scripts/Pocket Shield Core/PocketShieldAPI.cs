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
using System.Collections.Immutable;
using VRage;
using VRage.Utils;
using VRageMath;

namespace PocketShieldCore
{
    public class PocketShieldAPI
    {
        /// <summary> Determines which side returned for a specific request. </summary>
        public enum ReturnSide { Server, Client }

        /// <summary>
        /// Basic properties used to construct a ShieldEmitter.
        /// ShieldEmitter will not be construct without these properties passed to the constructor. </summary>
        public class ShieldEmitterProperties
        {
            public ImmutableList<object> Data { get { return m_Data.ToImmutableList(); } }

            /// <summary> 
            /// SubtypeId of shield emitter item in inventory.
            /// This is used to differentiate between ShieldEmitter types. </summary>
            public MyStringHash SubtypeId
            {
                get { return (MyStringHash)m_Data[0]; }
                set { m_Data[0] = value; }
            }

            /// <summary> Max number of Plugins that this Emitter supports. </summary>
            public int MaxPluginsCount
            {
                get { return (int)m_Data[1]; }
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

            /// <summary> A list of Base Defense against each damage type. </summary>
            public Dictionary<MyStringHash, float> BaseDef
            {
                get { return (Dictionary<MyStringHash, float>)m_Data[9]; }
            }

            /// <summary> A list of Base Resistance against each damage type. </summary>
            public Dictionary<MyStringHash, float> BaseRes
            {
                get { return (Dictionary<MyStringHash, float>)m_Data[10]; }
            }

            private readonly List<object> m_Data = new List<object>()
            {
                MyStringHash.NullOrEmpty,               /* SubtypeId */
                0,                                      /* MaxPluginsCount */
                0.0f,                                   /* BaseMaxEnergy */
                0.0f,                                   /* BaseChargeRate */
                0.0f,                                   /* BaseChargeDelay */
                0.0f,                                   /* BaseOverchargeDuration */
                0.0f,                                   /* BaseOverchargeDefBonus */
                0.0f,                                   /* BaseOverchargeResBonus */
                0.0,                                    /* BasePowerConsumption */
                new Dictionary<MyStringHash, float>(),  /* BaseDef */
                new Dictionary<MyStringHash, float>()   /* BaseRes */
            };

            /// <summary> Construct a ShieldEmitterProperties object from a set of data. Data will not be validate. </summary>
            /// <param name="_data"> Data to be copied </param>
            public ShieldEmitterProperties(IList<object> _data)
            {
                if (_data != null)
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
            public void ReplaceData(IList<object> _data)
            {
                if (_data == null)
                    Clear();

                for (int i = 0; i < m_Data.Count && i < _data.Count; ++i)
                {
                    m_Data[i] = _data[i];
                }
            }

        }

        /// <summary>
        /// Basic properties used to draw a shield icon.
        /// If no data is supplied, a default icon will be drawn. </summary>
        public class ShieldIconDrawInfo
        {
            public ImmutableList<object> Data { get { return m_Data.ToImmutableList(); } }

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

            private readonly List<object> m_Data = new List<object>()
            {
                MyStringHash.NullOrEmpty,   /* SubtypeId */
                MyStringId.NullOrEmpty,     /* Material */
                false,                      /* UvEnabled */
                Vector2.Zero,               /* UvSize */
                Vector2.Zero                /* UvOffset */
            };

            /// <summary> Construct a ShieldIconDrawInfo object from a set of data. Data will not be validate. </summary>
            /// <param name="_data"> Data to be copied </param>
            public ShieldIconDrawInfo(IList<object> _data)
            {
                if (_data != null)
                {
                    for (int i = 0; i < m_Data.Count && i < _data.Count; ++i)
                    {
                        m_Data[i] = _data[i];
                    }
                }
            }

            /// <summary> Clears this ShieldIconDrawInfo object (resets everything to 0) without destroy the object.
            /// This is useful when you want to clear a cached the object allocation. </summary>
            public void Clear()
            {
                SubtypeId = MyStringHash.NullOrEmpty;
                Material = MyStringId.NullOrEmpty;
                UvEnabled = false;
                UvSize = Vector2.Zero;
                UvOffset = Vector2.Zero;
            }

            /// <summary> Replaces this ShieldIconDrawInfo object with a set of data.
            /// This method doesn't allocate memory. Data will not be validated </summary>
            /// <param name="_data"> Data to be copied </param>
            public void ReplaceData(IList<object> _data)
            {
                if (_data == null)
                    Clear();

                for (int i = 0; i < m_Data.Count && i < _data.Count; ++i)
                {
                    m_Data[i] = _data[i];
                }
            }

        }

        /// <summary>
        /// Basic properties used to draw a Defense/Resistance stat item card for a specific damage type.
        /// If no data is supplied, this damage type's stat will not be displayed. </summary>
        public class ItemCardDrawInfo
        {
            public ImmutableList<object> Data { get { return m_Data.ToImmutableList(); } }

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

            private readonly List<object> m_Data = new List<object>()
            {
                MyStringHash.NullOrEmpty,   /* SubtypeId */
                MyStringId.NullOrEmpty,     /* Material */
                false,                      /* UvEnabled */
                Vector2.Zero,               /* UvSize */
                Vector2.Zero                /* UvOffset */
            };

            /// <summary> Construct a ItemCardDrawInfo object from a set of data. Data will not be validate. </summary>
            /// <param name="_data"> Data to be copied </param>
            public ItemCardDrawInfo(IList<object> _data)
            {
                if (_data != null)
                {
                    for (int i = 0; i < m_Data.Count && i < _data.Count; ++i)
                    {
                        m_Data[i] = _data[i];
                    }
                }
            }

            /// <summary> Clears this ItemCardDrawInfo object (resets everything to 0) without destroy the object.
            /// This is useful when you want to clear a cached the object allocation. </summary>
            public void Clear()
            {
                DamageType = MyStringHash.NullOrEmpty;
                Material = MyStringId.NullOrEmpty;
                UvEnabled = false;
                UvSize = Vector2.Zero;
                UvOffset = Vector2.Zero;
            }

            /// <summary> Replaces this ItemCardDrawInfo object with a set of data.
            /// This method doesn't allocate memory. Data will not be validated </summary>
            /// <param name="_data"> Data to be copied </param>
            public void ReplaceData(IList<object> _data)
            {
                if (_data == null)
                    Clear();

                for (int i = 0; i < m_Data.Count && i < _data.Count; ++i)
                {
                    m_Data[i] = _data[i];
                }
            }

        }


        #region Internal Stuff
        public const long MOD_ID = 2739353433L;

        public const string STR_API_VERSION = "Ver1";
        public const string STR_REGISTER_MOD = "RegMod";
        public const string STR_UNREGISTER_MOD = "UnRegMod";
        public const string STR_INTERNAL_REGISTER_CALLBACK = "RegEvt";
        public const string STR_INTERNAL_UNREGISTER_CALLBACK = "UnRegEvt";
        #endregion

        public const string VERSION = "1.0";

        /// <summary> Flag to determine if PocketShieldAPI is registered server-side and ready or not. </summary>
        public static bool ServerReady { get { if (s_Instance != null) return s_Instance.m_IsServerReady; return false; } }

        /// <summary> Flag to determine if PocketShieldAPI is registered client-side and ready or not. </summary>
        public static bool ClientReady { get { if (s_Instance != null) return s_Instance.m_IsClientReady; return false; } }

        /// <summary> Backend API version. </summary>
        private static string ServerBackendVersion = "";

        /// <summary> Backend API version. </summary>
        private static string ClientBackendVersion = "";

        /// <summary> Stores error message on LAST ERROR OCCURED. Successfull operation will not clear this message. </summary>
        public static string LastErrorMessage { get; private set; }

        private static PocketShieldAPI s_Instance = null;

        private string m_ModInfo = "";
        private bool m_IsServerReady = false;
        private bool m_IsClientReady = false;

        private Action<ReturnSide> m_RegisterFinishedCallback = null;

        private List<Func<MyStringHash, IList<object>>> m_RegisteredMethods = new List<Func<MyStringHash, IList<object>>>();
        private List<Delegate> m_ApiMethods = null;
        /// <summary> 
        /// Stores Plugins' Bonus modifier accross all mod that use PocketShield.<br/>
        /// Do NOT modify this member, it will affect other mod's registered data and may crash them.
        /// Use GetPluginModifier() and SetPluginModifier() instead. </summary>
        private Dictionary<MyStringHash, float> m_PluginBonusModifiers = null;

        private List<IList<object>> m_ShieldIconList = null;
        private List<IList<object>> m_ItemCardList = null;


        /// <summary>
        /// Initialize PocketShieldAPI and register with Core mod.
        /// You need to pass a string to identify your mod. This string will be useful to see which mod won't Close properly.
        /// You can pass a callback for when register is done. <summary>
        /// <param name="_modInfo"> A string to identify your mod </param>
        /// <param name="_registerFinishedCallback"> A callback to call when register is done </param>
        public static void Init(string _modInfo, Action<ReturnSide> _registerFinishedCallback = null)
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(MOD_ID, PocketShieldAPI_ModRegisterReturnHandle);

            s_Instance = new PocketShieldAPI(_modInfo, _registerFinishedCallback);
            MyAPIGateway.Utilities.SendModMessage(MOD_ID, STR_REGISTER_MOD + STR_API_VERSION + "=" + _modInfo);
        }

        /// <summary> Unregister PocketShieldAPI from Core mod.
        /// This also unregister any callbacks that you have registered AND not unregistered before. </summary>
        /// <returns> Always true </returns>
        public static bool Close()
        {
            if (ServerReady)
            {
                for (int i = s_Instance.m_RegisteredMethods.Count - 1; i >= 0; --i)
                {
                    if (UnRegisterEmitterPropertiesConstructCallback(s_Instance.m_RegisteredMethods[i]))
                    { }
                }
            }

            if (ClientReady)
            {

            }

            MyAPIGateway.Utilities.SendModMessage(MOD_ID, STR_UNREGISTER_MOD + "=" + s_Instance.m_ModInfo);
            MyAPIGateway.Utilities.UnregisterMessageHandler(MOD_ID, PocketShieldAPI_ModRegisterReturnHandle);

            s_Instance.m_IsServerReady = false;
            s_Instance.m_IsClientReady = false;

            s_Instance = null;

            return true;
        }

        #region Server-side API Methods
        /// <summary> Register a callback that compiles ShieldEmitterProperties to be used when constructing a ShieldEmitter. </summary>
        /// <param name="_callback"> A function that compiles ShieldEmitterProperties and returns it as an object array </param>
        /// <returns> False when API is not Ready, otherwise true </returns>
        public static bool RegisterCompileEmitterPropertiesCallback(Func<MyStringHash, IList<object>> _callback)
        {
            if (!ServerReady)
            {
                LastErrorMessage = "PocketShield API (server) is not initialized";
                return false;
            }

            s_Instance.m_ApiMethods.Add(_callback);
            s_Instance.m_RegisteredMethods.Add(_callback);
            return true;
        }

        /// <summary> Unregister a callback that was registered previously using RegisterCompileEmitterPropertiesCalback(). </summary>
        /// <param name="_callback"> Previously registered callback </param>
        /// <returns> False when API is not Ready, otherwise true </returns>
        public static bool UnRegisterEmitterPropertiesConstructCallback(Func<MyStringHash, IList<object>> _callback)
        {
            if (!ServerReady)
            {
                LastErrorMessage = "PocketShield API (server) is not initialized";
                return false;
            }

            s_Instance.m_ApiMethods.Remove(_callback);
            s_Instance.m_RegisteredMethods.Remove(_callback);
            return true;
        }

        public static float GetPluginModifier(MyStringHash _subtypeId)
        {
            if (!ServerReady)
            {
                LastErrorMessage = "PocketShield API is not initialized";
                return 0.0f;
            }

            if (s_Instance.m_PluginBonusModifiers.ContainsKey(_subtypeId))
                return s_Instance.m_PluginBonusModifiers[_subtypeId];

            return 0.0f;
        }

        public static void SetPluginModifier(MyStringHash _subtypeId, float _value)
        {
            if (!ServerReady)
            {
                LastErrorMessage = "PocketShield API is not initialized";
                return;
            }

            s_Instance.m_PluginBonusModifiers[_subtypeId] = _value;
        }
        #endregion

        #region Client-side API Methods
        /// <summary> Register a custom shield icon to be drawn on HUD Panel. </summary>
        /// <param name="_data"> Data for a custom shield icon </param>
        /// <returns> False when API is not Ready, otherwise true </returns>
        public static bool RegisterShieldIcon(ShieldIconDrawInfo _data)
        {
            if (!ClientReady)
            {
                LastErrorMessage = "PocketShield API (client) is not initialized";
                return false;
            }

            s_Instance.m_ShieldIconList.Add(_data.Data);
            return true;
        }

        /// <summary> Register a set of two icons for Defense and Resistance stat for a single damage type, that will be drawn on HUD Panel. </summary>
        /// <param name="_data"> Data for a set of two icons </param>
        /// <returns> False when API is not Ready, otherwise true </returns>
        public static bool RegisterStatIcons(ItemCardDrawInfo _data)
        {
            if (!ClientReady)
            {
                LastErrorMessage = "PocketShield API (client) is not initialized";
                return false;
            }

            s_Instance.m_ItemCardList.Add(_data.Data);
            return true;
        }
        #endregion

        #region Internal Stuff
        private PocketShieldAPI(string _modInfo, Action<ReturnSide> _registerFinishedCallback = null)
        {
            ServerBackendVersion = "";
            ClientBackendVersion = "";
            LastErrorMessage = "";

            m_ModInfo = _modInfo;
            m_RegisterFinishedCallback = _registerFinishedCallback;
        }

        private static void PocketShieldAPI_ModRegisterReturnHandle(object _payload)
        {
            // this one should not happens;
            if (s_Instance == null)
                return;
            if (_payload == null)
                return;

            if (_payload is MyTuple<string, List<Delegate>, Dictionary<MyStringHash, float>>)
            {
                if (ServerReady)
                {
                    LastErrorMessage = "PocketShieldAPI (server) is already initialized";
                    return;
                }

                var data = (MyTuple<string, List<Delegate>, Dictionary<MyStringHash, float>>)_payload;
                if (!data.Item1.StartsWith("Server") || data.Item2 == null || data.Item3 == null)
                {
                    LastErrorMessage = "Data received from Server APIBackend, but it seems to be corrupted";
                    return;
                }

                ServerBackendVersion = data.Item1.Substring(13);
                if (ServerBackendVersion != VERSION)
                    LastErrorMessage = "API Version mismatch, you should update your PocketShieldAPI.cs file, or things may break";

                s_Instance.m_ApiMethods = data.Item2;
                s_Instance.m_PluginBonusModifiers = data.Item3;
                s_Instance.m_IsServerReady = true;

                s_Instance.m_RegisterFinishedCallback?.Invoke(ReturnSide.Server);

                return;
            }

            if (_payload is MyTuple<string, List<IList<object>>, List<IList<object>>>)
            {
                if (ClientReady)
                {
                    LastErrorMessage = "PocketShieldAPI (client) is already initialized";
                    return;
                }

                var data = (MyTuple<string, List<IList<object>>, List<IList<object>>>)_payload;
                if (!data.Item1.StartsWith("Client") || data.Item2 == null || data.Item3 == null)
                {
                    LastErrorMessage = "Data received from Client APIBackend, but it seems to be corrupted";
                    return;
                }

                ClientBackendVersion = data.Item1.Substring(13);
                if (ClientBackendVersion != VERSION)
                    LastErrorMessage = "API Version mismatch, you should update your PocketShieldAPI.cs file, or things may break";

                s_Instance.m_ShieldIconList = data.Item2;
                s_Instance.m_ItemCardList = data.Item3;
                s_Instance.m_IsClientReady = true;

                s_Instance.m_RegisterFinishedCallback?.Invoke(ReturnSide.Client);

                return;
            }
        }
        #endregion

    }
}
