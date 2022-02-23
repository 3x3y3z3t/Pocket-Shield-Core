// ;
using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreServer
    {
        internal const string c_ChatCmdPrefix = "/PShield";

        private void ChatCommand_MessageEnteredHandle(string _messageText, ref bool _sendToOthers)
        {
            m_Logger.WriteLine(">> Ultilities_MessageEntered triggered <<", 5);
            if (MyAPIGateway.Session.Player == null)
                return;

            if (!_messageText.StartsWith(c_ChatCmdPrefix))
                return;

            m_Logger.WriteLine("  Chat Command captured: " + _messageText, 1);
            ChatCommand_ProcessCommands(_messageText);

            _sendToOthers = false;
        }

        private bool ChatCommand_ProcessCommands(string _commands)
        {
            string[] commands = _commands.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (commands.Length <= 1)
            {
                MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] [Server] You didn't specify any command", 2000);
                return false;
            }

            for (int i = 1; i < commands.Length; ++i)
            {
                string cmd = commands[i].Trim();

                m_Logger.WriteLine("    Processing command " + i + ": " + cmd, 1);
                if (ChatCommand_ProcessSingleCommand(cmd))
                    MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] [Server] Command executed.", 2000);
                else
                    MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] [Server] Command execution failed. See log for more info.", 2000);
            }

            return true;
        }

        private bool ChatCommand_ProcessSingleCommand(string _command)
        {
            if (_command == "ReloadCfg")
            {
                m_Logger.WriteLine("      Executing reload command", 1);
                if (m_Config.LoadConfigFile())
                {
                    MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] [Server] Config reloaded", 2000);
                    m_Logger.LogLevel = m_Config.LogLevel;

                    foreach (var loggers in m_ShieldManager_ShieldLoggers.Values)
                    {
                        if (loggers.Item1 != null)
                            loggers.Item1.Suppressed = m_Config.SuppressAllShieldLog;
                        if (loggers.Item2 != null)
                            loggers.Item2.Suppressed = m_Config.SuppressAllShieldLog;
                    }
                }
                else
                {
                    MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] [Server] Config reload failed (Default is loaded instead)", 2000);
                }
                return true;
            }

            if (_command == "SaveCfg")
            {
                m_Logger.WriteLine("      Executing save command", 1);
                if (m_Config.SaveConfigFile())
                    MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] [Server] Config saved", 2000);
                else
                    MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] [Server] Config saving failed", 2000);
                return true;
            }

            #region Debug
            //if (_command == "EmitterCount")
            //{
            //    m_Logger.WriteLine("      Executing EmitterCount command", 1);

            //    MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] [Server] Emitter Cunt: " + m_ShieldEmitters.Count, 5000);
            //    m_Logger.WriteLine("    Emitter count: " + m_ShieldEmitters.Count, 1);

            //    return true;
            //}

            if (_command == "LoadedCfg")
            {
                m_Logger.WriteLine("  Executing LoadedCfg command");
                MyIni iniData = new MyIni();
                m_Config.PackIniData(ref iniData);
                string configs = iniData.ToString();
                MyAPIGateway.Utilities.ShowMissionScreen(
                    screenTitle: "Loaded Configs",
                    currentObjectivePrefix: "",
                    screenDescription: configs,
                    okButtonCaption: "Close"
                );
                return true;
            }

            if (_command == "PeekCfg")
            {
                m_Logger.WriteLine("  Executing PeekCfg command");
                string configs = m_Config.PeekConfigFile();
                MyAPIGateway.Utilities.ShowMissionScreen(
                    screenTitle: "Raw Config File",
                    currentObjectivePrefix: "",
                    currentObjective: m_Config.Filename,
                    screenDescription: configs,
                    okButtonCaption: "Close"
                );
                return true;
            }
            #endregion

            MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] [Server] Unknown Command [" + _command + "]", 2000);
            m_Logger.WriteLine("      Unknown command [" + _command + "]", 1);
            return false;
        }


    }
}
