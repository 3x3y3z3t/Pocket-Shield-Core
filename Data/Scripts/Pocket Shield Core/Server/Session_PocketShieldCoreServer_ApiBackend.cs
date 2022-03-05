// ;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Utils;

using ServerData = VRage.MyTuple<
    string,
    System.Collections.Generic.Dictionary<VRage.Utils.MyStringHash, System.Collections.Generic.Dictionary<VRage.Utils.MyStringHash, float>>,
    System.Collections.Generic.List<System.Delegate>>;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreServer
    {
        private List<string> m_ApiBackend_RegisteredMod = null;
        private List<Delegate> m_ApiBackend_ExposedMethods = null;

        private void ApiBackend_ModMessageHandle(object _payload)
        {
            string msg = _payload as string;
            if (msg != null)
            {
                m_Logger.WriteLine("msg = " + msg, 4);
                if (msg.StartsWith(PocketShieldAPIV2.STR_REGISTER_MOD))
                {
                    int pos = msg.IndexOf('=');
                    string reqVer = msg.Substring(PocketShieldAPIV2.STR_REGISTER_MOD.Length, pos - PocketShieldAPIV2.STR_REGISTER_MOD.Length);
                    string modinfo = msg.Substring(PocketShieldAPIV2.STR_REGISTER_MOD.Length + PocketShieldAPIV2.STR_API_VERSION.Length + 1);
                    m_ApiBackend_RegisteredMod.Add(modinfo);

                    m_Logger.WriteLine("Registering mod " + modinfo + " (" + reqVer + ")..", 0);
                    ApiBackend_HandleRequestV2();

                    Blueprints_UpdateBlueprintData(true);
                }
                else if (msg.StartsWith(PocketShieldAPIV2.STR_UNREGISTER_MOD))
                {
                    string modinfo = msg.Substring(PocketShieldAPIV2.STR_UNREGISTER_MOD.Length + 1);

                    m_Logger.WriteLine("UnRegistering mod " + modinfo + "..", 0);
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

        private void ApiBackend_HandleRequestV2()
        {
            ServerData data = new ServerData()
            {
                Item1 = "Server Version=" + PocketShieldAPIV2.SERVER_BACKEND_VERSION,
                Item2 = ShieldManager.PluginModifiers,
                Item3 = m_ApiBackend_ExposedMethods
            };

            MyAPIGateway.Utilities.SendModMessage(PocketShieldAPIV2.MOD_ID, data);
        }



    }
}
