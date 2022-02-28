// ;
using Sandbox.ModAPI;
using VRageMath;
using static Draygo.API.HudAPIv2;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreClient
    {
        private const string c_ColorTagDefaultValue = "<color=128,128,128,72>";
        private const string c_ColorTagReadonlyValue = "<color=32,223,223,200>";
        private const string c_ColorTagNumber = "<color=223,223,32>";
        private const string c_ColorTagBoolTrue = "<color=32,223,32>";
        private const string c_ColorTagBoolFalse = "<color=223,32,32>";

        public const float c_SensitivityClamp = 1.0f;
        public const float c_PanelWidthMaxClamp = 1024.0f;
        public const float c_PanelWidthMinClamp = 64.0f;
        public const float c_PaddingClamp = 10.0f;




        #region Labels
        internal string RootItemString { get { return Constants.LOG_PREFIX; } }

        internal string ConfigVersionString
        {
            get { return "Config Version " + c_ColorTagReadonlyValue + m_Config.ConfigVersion; }
        }

        internal string LogLevelString
        {
            get
            {
                string s = "Log Level: ";
                if (m_Config.LogLevel < 0)
                    s += (c_ColorTagBoolFalse + "Off");
                else
                    s += (c_ColorTagNumber + m_Config.LogLevel);
                AppendDefaultString(ref s, m_Config.LogLevel, Constants.CLIENT_LOG_LEVEL);

                return s;
            }
        }
        internal float LogLevelInitialPercent
        {
            get { return (m_Config.LogLevel + 1.0f) / 6.0f; }
        }

        internal string ClientUpdateIntervalString
        {
            get
            {
                float ups = 60.0f / m_Config.ClientUpdateInterval;
                string s = string.Format("Client Update Interval: {0:0}{1:0}<reset> ({2:0.#}<reset> ups)", c_ColorTagNumber, m_Config.ClientUpdateInterval, ups);
                AppendDefaultString(ref s, m_Config.ClientUpdateInterval, Constants.CLIENT_UPDATE_INTERVAL);
                return s;
            }
        }

        internal string ShowPanelString
        {
            get
            {
                string s = "Show Panel: " + FormatBoolString(m_Config.ShowPanel);
                AppendDefaultString(ref s, m_Config.ShowPanel, Constants.SHOW_PANEL);
                return s;
            }
        }

        internal string ShowPanelBGString
        {
            get
            {
                string s = "Show Panel Background: " + FormatBoolString(m_Config.ShowPanelBackground);
                AppendDefaultString(ref s, m_Config.ShowPanelBackground, Constants.SHOW_PANEL_BG);
                return s;
            }
        }

        internal string PanelPositionString
        {
            get
            {
                string s;
                if (m_Config.ShowPanel)
                    s = string.Format("Panel Position: ({0}{1:0}<reset>, {0}{2:0}<reset>)", c_ColorTagNumber, (int)m_Config.PanelPosition.X, (int)m_Config.PanelPosition.Y);
                else
                    s = c_ColorTagDefaultValue + "Panel Position: (" + (int)m_Config.PanelPosition.X + ", " + (int)m_Config.PanelPosition.Y + ")";
                if (m_Config.PanelPosition.X != Constants.PANEL_POS_X || m_Config.PanelPosition.Y != Constants.PANEL_POS_Y)
                    s += c_ColorTagDefaultValue + " default: (" + Constants.PANEL_POS_X + ", " + Constants.PANEL_POS_Y + ")";

                return s;
            }
        }
        private string PanelPositionHelperString
        {
            get
            {
                double x, y;
                if (m_IsMoving)
                {
                    x = s_CachedPanelPositionItemPos.X;
                    y = s_CachedPanelPositionItemPos.Y;
                }
                else
                {
                    x = m_Config.PanelPosition.X;
                    y = m_Config.PanelPosition.Y;
                }
                string s = string.Format("New Panel Position: ({0}{1:0}<reset>, {0}{2:0}<reset>) (Drag to reposition)",
                       c_ColorTagNumber, x, y);
                return s;
            }
        }
        internal Vector2D PanelPositionItemPos
        {
            get
            {
                return new Vector2D(
                    +(m_Config.PanelPosition.X - s_ViewportSize.X * 0.5) / (s_ViewportSize.X * 0.5),
                    -(m_Config.PanelPosition.Y - s_ViewportSize.Y * 0.5) / (s_ViewportSize.Y * 0.5));
            }
        }
        internal Vector2D PanelPositionItemSize
        {
            get
            {
                return new Vector2D(
                    +(s_CachedPanelPositionItemSize.X) / (s_ViewportSize.X * 0.5),
                    +(s_CachedPanelPositionItemSize.Y) / (s_ViewportSize.Y * 0.5));
            }
        }

        internal string ScaleString
        {
            get
            {
                string s = string.Format("Scale: {0:0}{1:F1}", c_ColorTagNumber, m_Config.ItemScale);
                AppendDefaultString(ref s, m_Config.ItemScale, Constants.ITEM_SCALE);
                return s;
            }
        }
        internal float ScaleInitialPercent
        {
            get { return (m_Config.ItemScale - 0.5f) / 1.0f; }
        }
        #endregion

        private MenuRootCategory m_RootCategory = null;

        private MenuItem m_ConfigVersionItem = null;
        private MenuSliderInput m_LogLevelItem = null;
        private MenuTextInput m_ClientUpdateIntervalItem = null;

        private MenuItem m_ShowPanelItem = null;
        private MenuItem m_ShowPanelBGItem = null;
        private MenuScreenInput m_PanelPositionItem = null;
        private MenuSliderInput m_ScaleItem = null;

        private bool m_IsMoving = false;

        private static Vector2D s_CachedPanelPositionItemPos = Vector2D.Zero;
        private static Vector2D s_CachedPanelPositionItemSize = Vector2D.Zero;
        private static float s_CachedScale = 1.0f;

        private bool ModSettings_InitMenu()
        {
            s_CachedPanelPositionItemPos.X = m_Config.PanelPosition.X;
            s_CachedPanelPositionItemPos.Y = m_Config.PanelPosition.Y;

            s_CachedPanelPositionItemSize.X = m_ShieldHudPanel.Width;
            s_CachedPanelPositionItemSize.Y = m_ShieldHudPanel.Height;

            m_RootCategory = new MenuRootCategory(RootItemString, MenuRootCategory.MenuFlag.PlayerMenu, Constants.LOG_PREFIX + " Settings");
            m_ConfigVersionItem = new MenuItem(ConfigVersionString, m_RootCategory, Interactable: false);
            m_LogLevelItem = new MenuSliderInput(LogLevelString, m_RootCategory, LogLevelInitialPercent, "Adjust Slider to modify Log Level", LogLevelOnSubmit, ConstructLogLevelHelperString);
            m_ClientUpdateIntervalItem = new MenuTextInput(ClientUpdateIntervalString, m_RootCategory, "Enter an integer for Client Update Interval", ClientUpdateIntervalItemOnSubmit);

            new MenuItem("", m_RootCategory, null, false);
            m_ShowPanelItem = new MenuItem(ShowPanelString, m_RootCategory, ShowPanelOnClick);
            m_ShowPanelBGItem = new MenuItem(ShowPanelBGString, m_RootCategory, ShowPanelBGOnClick);
            m_PanelPositionItem = new MenuScreenInput(PanelPositionString, m_RootCategory, PanelPositionItemPos, PanelPositionItemSize, PanelPositionHelperString, PanelPositionOnSubmit, PanelPositionUpdate);
            m_ScaleItem = new MenuSliderInput(ScaleString, m_RootCategory, ScaleInitialPercent, "Adjust Slider to modify Scale", ScaleOnSubmit, ConstructScaleHelperString, ScaleOnCancel);

            //ModSettings_RefreshMenuInteractability();

            new MenuItem("", m_RootCategory, null, false);
            new MenuItem("——— Load Config ———", m_RootCategory, LoadConfig);
            new MenuItem("——— Save Config ———", m_RootCategory, SaveConfig);

            return true;
        }

        public void ModSettings_RefreshMenu()
        {
            s_CachedPanelPositionItemPos.X = m_Config.PanelPosition.X;
            s_CachedPanelPositionItemPos.Y = m_Config.PanelPosition.Y;

            m_RootCategory.Text = RootItemString;

            m_ConfigVersionItem.Text = ConfigVersionString;

            m_LogLevelItem.Text = LogLevelString;
            m_LogLevelItem.InitialPercent = LogLevelInitialPercent;

            m_ClientUpdateIntervalItem.Text = ClientUpdateIntervalString;

            m_ShowPanelItem.Text = ShowPanelString;

            m_ShowPanelBGItem.Text = ShowPanelBGString;

            m_PanelPositionItem.Text = PanelPositionString;
            m_PanelPositionItem.Origin = PanelPositionItemPos;
            m_PanelPositionItem.InputDialogTitle = PanelPositionHelperString;

            m_ScaleItem.Text = ScaleString;
            m_ScaleItem.InitialPercent = ScaleInitialPercent;

            //ModSettings_RefreshMenuInteractability();
        }

        #region Callback Methods
        public void ModSettings_RefreshMenuInteractability()
        {
            m_ShowPanelItem.Text = ShowPanelString;

            m_ShowPanelBGItem.Interactable = m_Config.ShowPanel;
            m_ShowPanelBGItem.Text = ShowPanelBGString;

            m_PanelPositionItem.Interactable = m_Config.ShowPanel;
            m_PanelPositionItem.Text = PanelPositionString;

            m_ScaleItem.Interactable = m_Config.ShowPanel;
            m_ScaleItem.Text = ScaleString;
        }

        internal void LogLevelOnSubmit(float _value)
        {
            m_Config.LogLevel = MathHelper.RoundToInt(MathHelper.Lerp(-1.0f, 5.0f, _value));

            m_LogLevelItem.Text = LogLevelString;
            m_LogLevelItem.InitialPercent = LogLevelInitialPercent;
        }

        internal string ConstructLogLevelHelperString(float _value)
        {
            int intValue = MathHelper.RoundToInt(MathHelper.Lerp(-1.0f, 5.0f, _value));
            string s = "Log Level: ";
            if (intValue < 0)
                s += (c_ColorTagBoolFalse + "Off");
            else
                s += (c_ColorTagNumber + intValue);
            //s += c_ReadOnlyValueColorTag + " Raw value: " + MathHelper.Lerp(-1.0f, 5.0f, _value);

            return s;
        }

        internal void ClientUpdateIntervalItemOnSubmit(string _string)
        {
            int value = 0;
            if (int.TryParse(_string, out value))
            {
                if (value > 0)
                {
                    m_Config.ClientUpdateInterval = value;
                    m_ClientUpdateIntervalItem.Text = ClientUpdateIntervalString;
                }
            }
        }

        internal void ShowPanelOnClick()
        {
            m_Config.ShowPanel = !m_Config.ShowPanel;
            m_ShieldHudPanel.UpdatePanelVisibility();

            m_ShowPanelItem.Text = ShowPanelString;
            //ModSettings_RefreshMenuInteractability();
        }

        internal void ShowPanelBGOnClick()
        {
            m_Config.ShowPanelBackground = !m_Config.ShowPanelBackground;
            m_ShieldHudPanel.UpdatePanelVisibility();
            m_ShowPanelBGItem.Text = ShowPanelBGString;
        }

        private void PanelPositionOnSubmit(Vector2D _vector)
        {
            //s_CachedPanelPositionItemPos.Y -= m_ShieldHudPanel.Height;
            m_Config.PanelPosition = s_CachedPanelPositionItemPos;
            //s_CachedPanelPositionItemPos.Y += m_ShieldHudPanel.Height;

            m_ShieldHudPanel?.UpdatePanelPosition();

            m_IsMoving = false;

            m_PanelPositionItem.Text = PanelPositionString;
            m_PanelPositionItem.Origin = PanelPositionItemPos;
        }

        private void PanelPositionUpdate(Vector2D _vector)
        {
            m_IsMoving = true;
            s_CachedPanelPositionItemPos.X = (int)(+(_vector.X + 1.0) * s_ViewportSize.X * 0.5);
            s_CachedPanelPositionItemPos.Y = (int)(-(_vector.Y - 1.0) * s_ViewportSize.Y * 0.5);

            m_PanelPositionItem.InputDialogTitle = PanelPositionHelperString;
        }

        internal void ScaleOnSubmit(float _value)
        {
            m_Config.ItemScale = MathHelper.Lerp(0.5f, 1.5f, _value);
            m_ShieldHudPanel.RequireConfigUpdate = true;
            m_ShieldHudPanel.UpdatePanelConfig();

            m_ScaleItem.Text = ScaleString;
            m_ScaleItem.InitialPercent = ScaleInitialPercent;
            s_CachedScale = m_Config.ItemScale;

            s_CachedPanelPositionItemPos.X = m_Config.PanelPosition.X;
            s_CachedPanelPositionItemPos.Y = m_Config.PanelPosition.Y;
            m_PanelPositionItem.Origin = PanelPositionItemPos;
        }

        internal string ConstructScaleHelperString(float _value)
        {
            m_Config.ItemScale = MathHelper.Lerp(0.5f, 1.5f, _value);
            string s = string.Format("Scale: {0:0}{1:F1}", c_ColorTagNumber, m_Config.ItemScale);
            //s += c_ReadOnlyValueColorTag + " Raw value: " + MathHelper.Lerp(0.5f, 1.5f, _value));
            m_ShieldHudPanel.RequireConfigUpdate = true;
            m_ShieldHudPanel.UpdatePanelConfig(); // HACK!!;

            return s;
        }

        internal void ScaleOnCancel()
        {
            m_Config.ItemScale = s_CachedScale;
            m_ShieldHudPanel.RequireConfigUpdate = true;
            m_ShieldHudPanel.UpdatePanelConfig();

            //MyAPIGateway.Utilities.ShowNotification("Scale = " + ConfigManager.ClientConfig.ItemScale, 3000);
        }
        #endregion

        #region Helper Methods
        internal static string FormatBoolString(bool _value)
        {
            return (_value ? c_ColorTagBoolTrue : c_ColorTagBoolFalse) + _value;
        }

        internal static void AppendDefaultString(ref string _string, bool _value, bool _defaultValue)
        {
            if (_value != _defaultValue)
                _string += (c_ColorTagDefaultValue + " default: " + _defaultValue);
        }

        internal static void AppendDefaultString(ref string _string, int _value, int _defaultValue)
        {
            if (_value != _defaultValue)
                _string += (c_ColorTagDefaultValue + " default: " + _defaultValue);
        }

        internal static void AppendDefaultString(ref string _string, float _value, float _defaultValue)
        {
            if (_value != _defaultValue)
                _string += (c_ColorTagDefaultValue + " default: " + _defaultValue);
        }
        #endregion

        public void LoadConfig()
        {
            if (m_Config.LoadConfigFile())
            {
                MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] Config reloaded", 2000);
                m_ShieldHudPanel.RequireConfigUpdate = true;
                m_ShieldHudPanel.UpdatePanelConfig();
                ModSettings_RefreshMenu();
            }
            else
            {
                MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] Config reload failed", 2000);
            }
        }

        public void SaveConfig()
        {
            if (m_Config.SaveConfigFile())
            {
                MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] Config saved", 2000);
            }
            else
            {
                MyAPIGateway.Utilities.ShowNotification("[" + Constants.LOG_PREFIX + "] Config saving failed", 2000);
            }
        }


    }
}
