// ;
using Draygo.API;
using ExShared;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;

namespace PocketShieldCore
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public partial class Session_PocketShieldCoreClient : MySessionComponentBase
    {
        private static Vector2 s_ViewportSize = Vector2.Zero;

        public bool IsServer { get; private set; }
        public bool IsDedicated { get; private set; }

        private bool m_IsTextHudModMissingConfirmed = false;
        private bool m_IsSetupDone = false;
        
        private bool m_IsHudVisible = false;
        private float m_HudBGOpacity = 1.0f;

        private int m_Ticks = 0;

        private Logger m_Logger = null;
        private ClientConfig m_Config = null;

        private ShieldHudPanel m_ShieldHudPanel = null;
        private HudAPIv2 m_TextHudAPI = null;

        private MyShieldData m_ShieldData = new MyShieldData();

        public override void LoadData()
        {
            m_Logger = new Logger("client");
            m_Config = new ClientConfig("client_config.ini", m_Logger);

            m_Logger.LogLevel = m_Config.LogLevel;

            m_Logger.WriteLine("Setting up..");

            MyAPIGateway.Gui.GuiControlRemoved += Gui_GuiControlRemoved;

            MyAPIGateway.Utilities.RegisterMessageHandler(PocketShieldAPI.MOD_ID, ApiBackend_ModMessageHandle);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Constants.MSG_HANDLER_ID_SYNC, Sync_HandleSyncShieldData);

        }

        protected override void UnloadData()
        {
            m_Logger.WriteLine("Shutting down..");

            foreach (string mod in m_ApiBackend_RegisteredMod)
            {
                m_Logger.WriteLine("  > Warning < Mod " + mod + " has not called DeInit() yet");
            }

            MyAPIGateway.Gui.GuiControlRemoved -= Gui_GuiControlRemoved;
            //MyAPIGateway.Utilities.MessageEntered -= ChatCommand_MessageEnteredHandle;

            MyAPIGateway.Utilities.UnregisterMessageHandler(PocketShieldAPI.MOD_ID, ApiBackend_ModMessageHandle);
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Constants.MSG_HANDLER_ID_SYNC, Sync_HandleSyncShieldData);

            ShieldHudPanel.CleanupStatics();

            if (m_TextHudAPI != null)
                m_TextHudAPI.Close();
            
            m_Logger.WriteLine("Shutdown Done");
            m_Logger.Close();
        }

        public override void BeforeStart()
        {
            IsServer = (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer);
            IsDedicated = IsServer && MyAPIGateway.Utilities.IsDedicated;
            if (IsDedicated)
                return;

            m_TextHudAPI = new HudAPIv2(InitTextHudCallback);
            ShieldHudPanel.Debug = true;
            //ApiBackend_LogRegisteredMod();

            m_Logger.WriteLine("  IsServer = " + IsServer);
            m_Logger.WriteLine("  IsDedicated = " + IsDedicated);

            m_Logger.WriteLine("Setup Done.");
            m_IsSetupDone = true;
        }

        public override void UpdateAfterSimulation()
        {
            ++m_Ticks;
            // clear ticks count;
            if (m_Ticks >= 2000000000)
                m_Ticks -= 2000000000;

            if (!m_IsSetupDone)
                return;
            
            if (m_Ticks % m_Config.ClientUpdateInterval == 0)
            {
                if (!m_IsTextHudModMissingConfirmed && !m_TextHudAPI.Heartbeat && m_Ticks >= 300)
                {
                    m_Logger.WriteLine("Text Hud API still hasn't recieved heartbeat.", 3);
                    //MyAPIGateway.Utilities.ShowNotification("Text HUD API mod is missing. HUD will not be displayed.", (config.ClientUpdateInterval * (int)(100.0f / 6.0f)), MyFontEnum.Red);
                    MyAPIGateway.Utilities.ShowNotification("Text HUD API mod is missing. HUD will not be displayed.", 3000, MyFontEnum.Red);
                    m_Logger.WriteLine("  Text Hud API mod is missing.", 3);
                    m_IsTextHudModMissingConfirmed = true;
                }

                UpdateFakeShieldStat();

                if (m_ShieldHudPanel != null)
                {
                    m_ShieldHudPanel.CacheIconLists();
                }
            }

            UpdateHitEffectDuration(1);

            // Attempt to steal from https://github.com/THDigi/BuildInfo/blob/master/Data/Scripts/BuildInfo/Systems/GameConfig.cs#L52...
            // Stealing In Progress...
            if (MyAPIGateway.Input.IsNewGameControlPressed(Sandbox.Game.MyControlsSpace.TOGGLE_HUD) ||
               MyAPIGateway.Input.IsNewGameControlPressed(Sandbox.Game.MyControlsSpace.PAUSE_GAME))
            {
                UpdateHudConfigs();
                UpdatePanelConfig();
            }

            if (m_ShieldHudPanel != null)
            {
                m_ShieldHudPanel.UpdatePanel();
                m_ShieldHudPanel.DebugUpdate();
            }

            // end of method;
            return;
        }

        public override void Draw()
        {
            // gets called 60 times a second after all other update methods, regardless of framerate, game pause or MyUpdateOrder.
            // NOTE: this is the only place where the camera matrix (MyAPIGateway.Session.Camera.WorldMatrix) is accurate, everywhere else it's 1 frame behind.

            Draw_DrawShieldEffects();
        }

        private void Gui_GuiControlRemoved(object _obj)
        {
            // Attempt to steal from https://github.com/THDigi/BuildInfo/blob/master/Data/Scripts/BuildInfo/Systems/GameConfig.cs#L58...
            // Stealing In Progress...
            try
            {
                if (_obj.ToString().EndsWith("ScreenOptionsSpace")) // closing options menu just assumes you changed something so it'll re-check config settings
                {
                    UpdateHudConfigs();
                    UpdatePanelConfig();
                }
            }
            catch (Exception _e)
            {
                m_Logger.WriteLine(_e.Message);
            }
        }

        private void InitTextHudCallback()
        {
            m_Logger.WriteLine("Starting InitTextHudCallback()", 5);

            UpdateHudConfigs();

            m_ShieldHudPanel = new ShieldHudPanel(m_ShieldData, m_Config, m_Logger);
            UpdatePanelConfig();

            ModSettings_InitMenu();

            m_Logger.WriteLine("InitTextHudCallback() done", 5);
        }

        private void UpdateHitEffectDuration(int _ticks)
        {
            foreach (var data in m_DrawList)
            {
                data.Ticks -= _ticks;
            }

            int removed = m_DrawList.RemoveAll((OtherCharacterShieldData _data) => { return _data.Ticks <= 0; });
        }

        private void UpdateFakeShieldStat()
        {
            if (m_ShieldData.PlayerSteamUserId == 0U)
                return;

            // TODO: update fake shield stat;
            // this may not be necessary, since server sync shield stat frequently;



            //m_ShieldHudPanel.RequireUpdate = true;


        }

        private void UpdateHudConfigs()
        {
            if (MyAPIGateway.Session.Config != null)
            {
                m_HudBGOpacity = MyAPIGateway.Session.Config.HUDBkOpacity;
                m_IsHudVisible = MyAPIGateway.Session.Config.HudState != 0; // 0 = Off, 1 = Hints, 2 = Basic;
            }
            if (MyAPIGateway.Session.Camera != null)
            {
                s_ViewportSize = MyAPIGateway.Session.Camera.ViewportSize;
            }
        }

        private void UpdatePanelConfig()
        {
            if (m_ShieldHudPanel != null)
            {
                m_ShieldHudPanel.Visible = m_IsHudVisible;
                m_ShieldHudPanel.BackgroundOpacity = m_HudBGOpacity;
                m_ShieldHudPanel.UpdatePanelConfig();
            }
        }



    }
}
