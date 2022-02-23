// ;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage;

using ClientData = VRage.MyTuple<
    string,
    System.Collections.Generic.List<System.Collections.Generic.List<object>>,
    System.Collections.Generic.List<System.Collections.Generic.List<object>>>;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreClient
    {
        private List<string> m_ApiBackend_RegisteredMod = new List<string>();

        private void ApiBackend_ModMessageHandle(object _payload)
        {
            string msg = _payload as string;
            if (msg != null)
            {
                m_Logger.WriteLine("msg = " + msg, 0);
                if (msg.StartsWith(PocketShieldAPI.STR_REGISTER_MOD))
                {
                    int pos = msg.IndexOf('=');
                    string reqVer = msg.Substring(PocketShieldAPI.STR_REGISTER_MOD.Length, pos - PocketShieldAPI.STR_REGISTER_MOD.Length);
                    string modinfo = msg.Substring(PocketShieldAPI.STR_REGISTER_MOD.Length + PocketShieldAPI.STR_API_VERSION.Length + 1);
                    m_ApiBackend_RegisteredMod.Add(modinfo);

                    ApiBackend_HandleRequestV2();

                    m_Logger.WriteLine("Registering mod " + modinfo + " (" + reqVer + ")..", 0);

                }
                else if (msg.StartsWith(PocketShieldAPI.STR_UNREGISTER_MOD))
                {
                    string modinfo = msg.Substring(PocketShieldAPI.STR_UNREGISTER_MOD.Length + 1);
                    m_Logger.WriteLine("modinfo = " + modinfo);
                    m_ApiBackend_RegisteredMod.Remove(modinfo);
                    m_Logger.WriteLine("UnRegistering mod " + modinfo + "..", 0);
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
            ClientData data = new ClientData()
            {
                Item1 = "Client Version=" + PocketShieldAPIV2.CLIENT_BACKEND_VERSION,
                Item2 = ShieldHudPanel.ShieldIconPropertiesList,
                Item3 = ShieldHudPanel.ItemCardIconPropertiesList
            };

            m_Logger.WriteLine("SendModMessage (one time)");
            MyAPIGateway.Utilities.SendModMessage(PocketShieldAPI.MOD_ID, data);
        }


    }
}
