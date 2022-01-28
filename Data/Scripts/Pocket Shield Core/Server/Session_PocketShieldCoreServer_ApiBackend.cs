// ;
using ExShared;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using VRage;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreServer
    {
        private List<string> m_ApiBackend_RegisteredMod = new List<string>();
        private List<Delegate> m_ApiBackend_EmitterPropCallbacks = new List<Delegate>();
        private int m_ApiBackend_CallbacksCount = 0;

        private void ApiBackend_ModMessageHandle(object _payload)
        {
            string msg = _payload as string;
            if (msg != null)
            {
                m_Logger.WriteLine("msg = " + msg, 4);
                if (msg.StartsWith(PocketShieldAPI.STR_REGISTER_MOD))
                {
                    int pos = msg.IndexOf('=');
                    string reqVer = msg.Substring(PocketShieldAPI.STR_REGISTER_MOD.Length, pos - PocketShieldAPI.STR_REGISTER_MOD.Length);
                    string modinfo = msg.Substring(PocketShieldAPI.STR_REGISTER_MOD.Length + PocketShieldAPI.STR_API_VERSION.Length + 1);
                    m_ApiBackend_RegisteredMod.Add(modinfo);

                    m_Logger.WriteLine("Registering mod " + modinfo + " (" + reqVer + ")..", 0);
                    if (reqVer == "Ver1")
                    {
                        ApiBackend_HandleRequestV1();
                    }
                    
                    Blueprints_UpdateBlueprintData(true);
                }
                else if (msg.StartsWith(PocketShieldAPI.STR_UNREGISTER_MOD))
                {
                    string modinfo = msg.Substring(PocketShieldAPI.STR_UNREGISTER_MOD.Length + 1);
                    m_Logger.WriteLine("UnRegistering mod " + modinfo + "..", 0);
                    m_Logger.WriteLine("  Mod unregistered " + (m_ApiBackend_CallbacksCount - m_ApiBackend_EmitterPropCallbacks.Count) + " callbacks", 0);
                    m_ApiBackend_CallbacksCount = m_ApiBackend_EmitterPropCallbacks.Count;

                    m_ApiBackend_RegisteredMod.Remove(modinfo);
                }
            }
        }

        private void ApiBackend_LogRegisteredMod()
        {
            m_Logger.WriteLine("Total registered mod: " + m_ApiBackend_RegisteredMod.Count, 1);
            foreach (string mod in m_ApiBackend_RegisteredMod)
            {
                m_Logger.WriteLine("  " + mod, 1);
            }
        }

        private void ApiBackend_HandleRequestV1()
        {
            MyTuple<string, List<Delegate>, Dictionary<MyStringHash, float>> data = new MyTuple<string, List<Delegate>, Dictionary<MyStringHash, float>>
            {
                Item1 = "Server Version=" + Constants.API_BACKEND_VERSION,
                Item2 = m_ApiBackend_EmitterPropCallbacks,
                Item3 = ShieldEmitter.s_PluginBonusModifiers
            };
            
            MyAPIGateway.Utilities.SendModMessage(PocketShieldAPI.MOD_ID, data);
        }


    }
}
