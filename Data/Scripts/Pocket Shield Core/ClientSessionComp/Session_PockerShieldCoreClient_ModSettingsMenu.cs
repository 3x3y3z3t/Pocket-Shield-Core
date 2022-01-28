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
                string s = string.Format("Client Update Interval: {0:0}{1:0}<reset> ({2:0.#}<reset>ups)", c_ColorTagNumber, m_Config.ClientUpdateInterval, ups);
                AppendDefaultString(ref s, m_Config.ClientUpdateInterval, Constants.CLIENT_UPDATE_INTERVAL);

                return s;
            }
        }

        internal string ShowPanelString
        {
            get { return "Show Panel: " + FormatBoolString(m_Config.ShowPanel); }
        }

        internal string ShowPanelBGString
        {
            get
            {
                string s;
                if (m_Config.ShowPanel)
                    s = "Show Panel Background: " + FormatBoolString(m_Config.ShowPanelBackground);
                else
                    s = c_ColorTagDefaultValue + "Show Panel Background: " + m_Config.ShowPanelBackground;
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
                    s = string.Format("Panel Position: ({0:0}{1:0}<reset>, {0:0}{2:0}<reset>)", c_ColorTagNumber, (int)m_Config.PanelPosition.X, (int)m_Config.PanelPosition.Y);
                else
                    s = c_ColorTagDefaultValue + "Panel Position: (" + (int)m_Config.PanelPosition.X + ", " + (int)m_Config.PanelPosition.Y + ")";
                if (m_Config.PanelPosition.X != Constants.PANEL_POS_X || m_Config.PanelPosition.Y != Constants.PANEL_POS_Y)
                    s += c_ColorTagDefaultValue + " default: (" + Constants.PANEL_POS_X + ", " + Constants.PANEL_POS_Y + ")";

                return s;
            }
        }
        internal Vector2D PanelPositionItemPos
        {
            get
            {
                return new Vector2D(
                    +(m_Config.PanelPosition.X - s_ViewportSize.X * 0.5) / (s_ViewportSize.X * 0.5),
                    -(m_Config.PanelPosition.Y - s_ViewportSize.Y * 0.5) / (s_ViewportSize.Y * 0.5) - (m_ShieldHudPanel.PanelSize.Y) / (s_ViewportSize.Y * 0.5));
            }
        }
        internal Vector2D PanelPositionItemSize
        {
            get
            {
                return new Vector2D(
                    +(m_ShieldHudPanel.PanelSize.X) / (s_ViewportSize.X * 0.5),
                    +(m_ShieldHudPanel.PanelSize.Y) / (s_ViewportSize.Y * 0.5));
            }
        }

#if false
        internal string ItemsCountString
        {
            get
            {
                ClientConfig config = ConfigManager.ClientConfig;
                string s = string.Format("Display Items Count: {0:0}{1:0}", c_NumberColorTag, config.DisplayItemsCount);
                AppendDefaultString(ref s, config.DisplayItemsCount, Constants.DISPLAY_ITEMS_COUNT);

                return s;
            }
        }
        internal float ItemsCountInitialPercent
        {
            get { return (ConfigManager.ClientConfig.DisplayItemsCount - 1.0f) / 4.0f; }
        }

        internal string MaxRangeString
        {
            get
            {
                ClientConfig config = ConfigManager.ClientConfig;
                string s = string.Format("Radar Max Range: {0:0}{1:0}", c_NumberColorTag, Utils.FormatDistanceAsString(config.RadarMaxRange));
                AppendDefaultString(ref s, config.RadarMaxRange, Constants.RADAR_MAX_RANGE);

                return s;
            }
        }

        internal string SensitivityString
        {
            get
            {
                ClientConfig config = ConfigManager.ClientConfig;
                string s = string.Format("Sensitivity: {0:0}{1:F2} m/s", c_NumberColorTag, config.TrajectorySensitivity * (60.0f / config.ClientUpdateInterval));
                if (config.TrajectorySensitivity != Constants.TRAJECTORY_SENSITIVITY)
                    s += string.Format("{0:0} default: {1:F2}", c_DefaultValueColorTag, Constants.TRAJECTORY_SENSITIVITY * (60.0f / config.ClientUpdateInterval));

                return s;
            }
        }
        internal float SensitivityInitialPercent
        {
            get { return ConfigManager.ClientConfig.TrajectorySensitivity / c_SensitivityClamp; }
        }

        internal string ShowMaxRangeString
        {
            get
            {
                ClientConfig config = ConfigManager.ClientConfig;
                string s;
                if (config.ShowPanel)
                    s = "Show Max Range: " + FormatBoolString(config.ShowMaxRangeIcon);
                else
                    s = c_DefaultValueColorTag + "Show Max Range: " + config.ShowMaxRangeIcon;
                AppendDefaultString(ref s, config.ShowMaxRangeIcon, Constants.SHOW_MAX_RANGE_ICON);

                return s;
            }
        }

        internal string ShowDisplayNameString
        {
            get
            {
                ClientConfig config = ConfigManager.ClientConfig;
                string s;
                if (config.ShowPanel)
                    s = "Show Signal Name: " + FormatBoolString(config.ShowSignalName);
                else
                    s = c_DefaultValueColorTag + "Show Signal Name: " + config.ShowSignalName;
                AppendDefaultString(ref s, config.ShowSignalName, Constants.SHOW_SIGNAL_NAME);

                return s;
            }
        }

        internal string PanelWidthString
        {
            get
            {
                ClientConfig config = ConfigManager.ClientConfig;
                string s;
                if (config.ShowPanel)
                    s = "Panel Width: " + c_NumberColorTag + (int)config.PanelWidth;
                else
                    s = c_DefaultValueColorTag + "Panel Width: " + (int)config.PanelWidth;
                AppendDefaultString(ref s, config.PanelWidth, Constants.PANEL_WIDTH);

                return s;
            }
        }
        internal float PanelWidthInitialPercent
        {
            get { return (ConfigManager.ClientConfig.PanelWidth - RadarPanel.PanelMinWidth - c_PanelWidthMinClamp) / (c_PanelWidthMaxClamp - RadarPanel.PanelMinWidth - c_PanelWidthMinClamp); }
        }

        internal string PaddingString
        {
            get
            {
                ClientConfig config = ConfigManager.ClientConfig;
                string s;
                if (config.ShowPanel)
                    s = "Padding: " + c_NumberColorTag + (int)config.Padding;
                else
                    s = c_DefaultValueColorTag + "Padding: " + (int)config.Padding;
                AppendDefaultString(ref s, config.Padding, Constants.PADDING);

                return s;
            }
        }
        internal float PaddingInitialPercent
        {
            get { return ConfigManager.ClientConfig.Padding / c_PaddingClamp; }

        }

        internal string MarginString
        {
            get
            {
                ClientConfig config = ConfigManager.ClientConfig;
                string s;
                if (config.ShowPanel)
                    s = "Margin: " + c_NumberColorTag + (int)config.Margin;
                else
                    s = c_DefaultValueColorTag + "Margin: " + (int)config.Margin;
                AppendDefaultString(ref s, config.Margin, Constants.MARGIN);

                return s;
            }
        }
        internal float MarginInitialPercent
        {
            get { return ConfigManager.ClientConfig.Margin / c_PaddingClamp; }
        }

        internal string ScaleString
        {
            get
            {
                ClientConfig config = ConfigManager.ClientConfig;
                string s;
                if (config.ShowPanel)
                    s = string.Format("Scale: {0:0}{1:F1}", c_NumberColorTag, config.ItemScale);
                else
                    s = string.Format("{0:0}Scale: {1:F1}", c_DefaultValueColorTag, config.ItemScale);
                AppendDefaultString(ref s, config.ItemScale, Constants.ITEM_SCALE);

                return s;
            }
        }
        internal float ScaleInitialPercent
        {
            get { return (ConfigManager.ClientConfig.ItemScale - 0.5f) / 1.0f; }
        }
#endif
        #endregion

        private MenuRootCategory m_RootCategory = null;

        private MenuItem m_ConfigVersionItem = null;
        private MenuSliderInput m_LogLevelItem = null;
        private MenuTextInput m_ClientUpdateIntervalItem = null;

        internal MenuItem m_ShowPanelItem = null;
        internal MenuItem m_ShowPanelBGItem = null;
        internal MenuScreenInput m_PanelPositionItem = null;

#if false
        private MenuSliderInput m_ItemsCountItem = null;
        private MenuTextInput m_MaxRangeItem = null;
        private MenuSliderInput m_SensitivityItem = null;

        internal MenuSubCategory m_HudConfigSubCategory = null;

        internal MenuItem m_ShowMaxRangeItem = null;
        internal MenuItem m_ShowDisplayNameItem = null;
        internal MenuSliderInput m_PanelWidthItem = null;
        internal MenuSliderInput m_PaddingItem = null;
        internal MenuSliderInput m_MarginItem = null;
        internal MenuSliderInput m_ScaleItem = null;

        internal static float s_CachedPanelWidth = ConfigManager.ClientConfig.PanelWidth;
        internal static float s_CachedPadding = ConfigManager.ClientConfig.Padding;
        internal static float s_CachedMargin = ConfigManager.ClientConfig.Margin;
        internal static float s_CachedScale = ConfigManager.ClientConfig.ItemScale;
#endif
        // TODO: this causes NRE on Logger;
        //internal static Vector2D s_CachedPanelPositionItemPos = ConfigManager.ClientConfig.PanelPosition;
        internal static Vector2D s_CachedPanelPositionItemPos = Vector2D.Zero;

        private bool ModSettings_InitMenu()
        {
            m_RootCategory = new MenuRootCategory(RootItemString, MenuRootCategory.MenuFlag.PlayerMenu, Constants.LOG_PREFIX + " Settings");
            m_ConfigVersionItem = new MenuItem(ConfigVersionString, m_RootCategory, Interactable: false);
            m_LogLevelItem = new MenuSliderInput(LogLevelString, m_RootCategory, LogLevelInitialPercent, "Adjust Slider to modify Log Level", LogLevelOnSubmit, ConstructLogLevelHelperString);
            m_ClientUpdateIntervalItem = new MenuTextInput(ClientUpdateIntervalString, m_RootCategory, "Enter an integer for Client Update Interval", ClientUpdateIntervalItemOnSubmit);

            new MenuItem("", m_RootCategory, null, false);
            m_ShowPanelItem = new MenuItem(ShowPanelString, m_RootCategory, ShowPanelOnClick);
            m_ShowPanelBGItem = new MenuItem(ShowPanelBGString, m_RootCategory, ShowPanelBGOnClick);
            m_PanelPositionItem = new MenuScreenInput(PanelPositionString, m_RootCategory, PanelPositionItemPos, PanelPositionItemSize, PanelPositionHelperString, PanelPositionOnSubmit, PanelPositionUpdate);

#if fasle
            m_ItemsCountItem = new MenuSliderInput(ItemsCountString, m_RootCategory, ItemsCountInitialPercent, "Adjust Slider to modify Items Count", ItemsCountOnSubmit, ConstructItemsCountHelperString);
            m_MaxRangeItem = new MenuTextInput(MaxRangeString, m_RootCategory, "Enter a decimal for Max Range", MaxRangeOnSubmit);
            m_SensitivityItem = new MenuSliderInput(SensitivityString, m_RootCategory, SensitivityInitialPercent, "Adjust Slider to modify Sensitivity (long slider for better control)", SensitivityOnSubmit, ConstructSensitivityHelperString);

            m_HudConfigSubCategory = new MenuSubCategory("Hud " + c_ReadOnlyValueColorTag + ">>>", m_RootCategory, "Hud Settings");

            m_ShowMaxRangeItem = new MenuItem(ShowMaxRangeString, m_HudConfigSubCategory, ShowMaxRangeOnClick);
            m_ShowDisplayNameItem = new MenuItem(ShowDisplayNameString, m_HudConfigSubCategory, ShowDisplayNameOnClick);
            m_PanelWidthItem = new MenuSliderInput(PanelWidthString, m_HudConfigSubCategory, PanelWidthInitialPercent, "Adjust Slider to modify Panel Width", PanelWidthOnSubmit, ConstructPanelWidthHelperString, PanelWidthOnCancel);
            m_PaddingItem = new MenuSliderInput(PaddingString, m_HudConfigSubCategory, PaddingInitialPercent, "Adjust Slider to modify Padding", PaddingOnSubmit, ConstructPaddingHelperString, PaddingOnCancel);
            m_MarginItem = new MenuSliderInput(MarginString, m_HudConfigSubCategory, MarginInitialPercent, "Adjust Slider to modify Margin", MarginOnSubmit, ConstructMarginHelperString, MarginOnCancel);
            m_ScaleItem = new MenuSliderInput(ScaleString, m_HudConfigSubCategory, ScaleInitialPercent, "Adjust Slider to modify Scale", ScaleOnSubmit, ConstructScaleHelperString, ScaleOnCancel);
#endif
            ModSettings_RefreshMenuInteractability();

            new MenuItem("", m_RootCategory, null, false);
            new MenuItem("——— Load Config ———", m_RootCategory, LoadConfig);
            new MenuItem("——— Save Config ———", m_RootCategory, SaveConfig);

            return true;
        }

        public void ModSettings_RefreshMenu()
        {
            RefreshSettingsMenuRootItem();

            m_ConfigVersionItem.Text = ConfigVersionString;

            m_LogLevelItem.Text = LogLevelString;
            m_LogLevelItem.InitialPercent = LogLevelInitialPercent;

            m_ClientUpdateIntervalItem.Text = ClientUpdateIntervalString;

            m_ShowPanelBGItem.Text = ShowPanelBGString;

            m_PanelPositionItem.Text = PanelPositionString;

            ModSettings_RefreshMenuInteractability();

#if false
            m_ItemsCountItem.Text = ItemsCountString;
            m_ItemsCountItem.InitialPercent = ItemsCountInitialPercent;

            m_MaxRangeItem.Text = MaxRangeString;

            m_SensitivityItem.Text = SensitivityString;
            m_SensitivityItem.InitialPercent = SensitivityInitialPercent;

            // HUD settings;
            m_ShowMaxRangeItem.Text = ShowMaxRangeString;

            m_ShowDisplayNameItem.Text = ShowDisplayNameString;

            m_PanelWidthItem.Text = PanelWidthString;
            m_PanelWidthItem.InitialPercent = PanelWidthInitialPercent;

            m_PaddingItem.Text = PaddingString;
            m_PaddingItem.InitialPercent = PaddingInitialPercent;

            m_MarginItem.Text = MarginString;
            m_MarginItem.InitialPercent = MarginInitialPercent;

            m_ScaleItem.Text = ScaleString;
            m_ScaleItem.InitialPercent = ScaleInitialPercent;
#endif
        }

        #region Callback Methods
        public void ModSettings_RefreshMenuInteractability()
        {
            // HACK! user will never get here when mod is disabled, so there is no need to check for .ModEnabled;

            //m_ShowPanelItem.Interactable = config.ShowPanel;
            m_ShowPanelItem.Text = ShowPanelString;

            m_ShowPanelBGItem.Interactable = m_Config.ShowPanel;
            m_ShowPanelBGItem.Text = ShowPanelBGString;

            m_PanelPositionItem.Interactable = m_Config.ShowPanel;
            m_PanelPositionItem.Text = PanelPositionString;

#if false
            m_ShowMaxRangeItem.Interactable = config.ShowPanel;
            m_ShowMaxRangeItem.Text = ShowMaxRangeString;

            m_ShowDisplayNameItem.Interactable = config.ShowPanel;
            m_ShowDisplayNameItem.Text = ShowDisplayNameString;

            m_PanelWidthItem.Interactable = config.ShowPanel;
            m_PanelWidthItem.Text = PanelWidthString;

            m_PaddingItem.Interactable = config.ShowPanel;
            m_PaddingItem.Text = PaddingString;

            m_MarginItem.Interactable = config.ShowPanel;
            m_MarginItem.Text = MarginString;

            m_ScaleItem.Interactable = config.ShowPanel;
            m_ScaleItem.Text = ScaleString;
#endif
        }

        public void RefreshSettingsMenuRootItem()
        {
            m_RootCategory.Text = RootItemString;
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
            UpdateHudConfigs();

            m_ShowPanelItem.Text = ShowPanelString;
            ModSettings_RefreshMenuInteractability();
        }

        internal void ShowPanelBGOnClick()
        {
            m_Config.ShowPanelBackground = !m_Config.ShowPanelBackground;
            UpdateHudConfigs();
            m_ShowPanelBGItem.Text = ShowPanelBGString;
        }

        private string PanelPositionHelperString
        {
            get
            {
                string s = string.Format("New Panel Position: ({0:0}{1:0}<reset>, {0:0}{2:0}<reset>) (Drag to reposition)",
                    c_ColorTagNumber, s_CachedPanelPositionItemPos.X, s_CachedPanelPositionItemPos.Y);
                return s;
            }
        }

        private void PanelPositionOnSubmit(Vector2D _vector)
        {
            m_Config.PanelPosition = s_CachedPanelPositionItemPos;
            UpdateHudConfigs();

            m_PanelPositionItem.Text = PanelPositionString;
            m_PanelPositionItem.Origin = PanelPositionItemPos;
        }

        private void PanelPositionUpdate(Vector2D _vector)
        {
            s_CachedPanelPositionItemPos = new Vector2D(
                (int)(+(_vector.X + 1.0) * s_ViewportSize.X * 0.5),
                (int)(-(_vector.Y - 1.0) * s_ViewportSize.Y * 0.5 - m_ShieldHudPanel.PanelSize.Y));

            m_PanelPositionItem.InputDialogTitle = PanelPositionHelperString;
            //MyAPIGateway.Utilities.ShowNotification("Current pos = " + offsX, config.ClientUpdateInterval * (int)(100.0f / 6.0f));
        }

#if false
        internal void ItemsCountOnSubmit(float _value)
        {
            ClientConfig config = ConfigManager.ClientConfig;
            config.DisplayItemsCount = MathHelper.RoundToInt(MathHelper.Lerp(1.0f, 5.0f, _value));
            UpdateHudConfigs();

            m_ItemsCountItem.Text = ItemsCountString;
            m_ItemsCountItem.InitialPercent = ItemsCountInitialPercent;
        }

        internal string ConstructItemsCountHelperString(float _value)
        {
            int intValue = MathHelper.RoundToInt(MathHelper.Lerp(1.0f, 5.0f, _value));
            string s = string.Format("Display Items Count: {0:0}{1:0}", c_NumberColorTag, intValue);
            //s += c_ReadOnlyValueColorTag + " Raw value: " + MathHelper.Lerp(1.0f, 5.0f, _value);

            return s;
        }

        internal void MaxRangeOnSubmit(string _string)
        {
            float value = 0;
            if (float.TryParse(_string, out value))
            {
                if (value >= 0.0f)
                {
                    ConfigManager.ClientConfig.RadarMaxRange = value;
                    m_MaxRangeItem.Text = MaxRangeString;
                }
            }
        }

        internal void SensitivityOnSubmit(float _value)
        {
            ClientConfig config = ConfigManager.ClientConfig;
            config.TrajectorySensitivity = MathHelper.Lerp(0.0f, c_SensitivityClamp, _value);

            m_SensitivityItem.Text = SensitivityString;
            m_SensitivityItem.InitialPercent = SensitivityInitialPercent;
        }

        internal string ConstructSensitivityHelperString(float _value)
        {
            ClientConfig config = ConfigManager.ClientConfig;
            string s = string.Format("Sensitivity: {0:0}{1:F2}", c_NumberColorTag, MathHelper.Lerp(0.0f, c_SensitivityClamp, _value) * (60.0f / config.ClientUpdateInterval));
            return s;
        }

        internal void ShowMaxRangeOnClick()
        {
            ConfigManager.ClientConfig.ShowMaxRangeIcon = !ConfigManager.ClientConfig.ShowMaxRangeIcon;
            UpdateHudConfigs();
            m_ShowMaxRangeItem.Text = ShowMaxRangeString;
        }

        internal void ShowDisplayNameOnClick()
        {
            ConfigManager.ClientConfig.ShowSignalName = !ConfigManager.ClientConfig.ShowSignalName;
            UpdateHudConfigs();
            m_ShowDisplayNameItem.Text = ShowDisplayNameString;
        }

        internal void PanelWidthOnSubmit(float _value)
        {
            ClientConfig config = ConfigManager.ClientConfig;
            config.PanelWidth = MathHelper.RoundToInt(MathHelper.Lerp(RadarPanel.PanelMinWidth + c_PanelWidthMinClamp, c_PanelWidthMaxClamp, _value));
            UpdateHudConfigs();

            m_PanelWidthItem.Text = PanelWidthString;
            m_PanelWidthItem.InitialPercent = PanelWidthInitialPercent;
            m_PanelPositionItem.Size = PanelPositionItemSize;
            s_CachedPanelWidth = config.PanelWidth;
        }

        internal string ConstructPanelWidthHelperString(float _value)
        {
            ClientConfig config = ConfigManager.ClientConfig;
            config.PanelWidth = MathHelper.RoundToInt(MathHelper.Lerp(RadarPanel.PanelMinWidth + c_PanelWidthMinClamp, c_PanelWidthMaxClamp, _value));
            string s = "Panel Width: " + c_NumberColorTag + MathHelper.RoundToInt(config.PanelWidth);
            //s += c_ReadOnlyValueColorTag + " Raw value: " + MathHelper.Lerp(RadarPanel.PanelMinWidth, c_PanelWidthClamp, _value);
            //s += " Cached = " + s_CachedPanelWidth;
            UpdateHudConfigs(); // HACK!!;

            return s;
        }

        internal void PanelWidthOnCancel()
        {
            ConfigManager.ClientConfig.PanelWidth = s_CachedPanelWidth;
            UpdateHudConfigs();
        }

        internal void PaddingOnSubmit(float _value)
        {
            ClientConfig config = ConfigManager.ClientConfig;
            config.Padding = MathHelper.RoundToInt(MathHelper.Lerp(0.0f, c_PaddingClamp, _value));
            UpdateHudConfigs();

            m_PaddingItem.Text = PaddingString;
            m_PaddingItem.InitialPercent = PaddingInitialPercent;
            s_CachedPadding = config.Padding;
        }

        internal string ConstructPaddingHelperString(float _value)
        {
            ClientConfig config = ConfigManager.ClientConfig;
            config.Padding = MathHelper.RoundToInt(MathHelper.Lerp(0.0f, c_PaddingClamp, _value));
            string s = "Padding: " + c_NumberColorTag + MathHelper.RoundToInt(config.Padding);
            //s += c_ReadOnlyValueColorTag + " Raw value: " + MathHelper.RoundToInt(MathHelper.Lerp(0.0f, c_PaddingClamp, _value));
            UpdateHudConfigs(); // HACK!!;

            return s;
        }

        internal void PaddingOnCancel()
        {
            ConfigManager.ClientConfig.Padding = s_CachedPadding;
            UpdateHudConfigs();
        }

        internal void MarginOnSubmit(float _value)
        {
            ClientConfig config = ConfigManager.ClientConfig;
            config.Margin = MathHelper.RoundToInt(MathHelper.Lerp(0.0f, c_PaddingClamp, _value));
            UpdateHudConfigs();

            m_MarginItem.Text = MarginString;
            m_MarginItem.InitialPercent = MarginInitialPercent;
            s_CachedMargin = config.Margin;
        }

        internal string ConstructMarginHelperString(float _value)
        {
            ClientConfig config = ConfigManager.ClientConfig;
            config.Margin = MathHelper.RoundToInt(MathHelper.Lerp(0.0f, c_PaddingClamp, _value));
            string s = "Margin: " + c_NumberColorTag + MathHelper.RoundToInt(config.Margin);
            //s += c_ReadOnlyValueColorTag + " Raw value: " + MathHelper.RoundToInt(MathHelper.Lerp(0.0f, c_PaddingClamp, _value));
            UpdateHudConfigs(); // HACK!!;

            return s;
        }

        internal void MarginOnCancel()
        {
            ConfigManager.ClientConfig.Margin = s_CachedMargin;
            UpdateHudConfigs();
        }

        internal void ScaleOnSubmit(float _value)
        {
            ClientConfig config = ConfigManager.ClientConfig;
            config.ItemScale = MathHelper.Lerp(0.5f, 1.5f, _value);
            UpdateHudConfigs();

            m_ScaleItem.Text = ScaleString;
            m_ScaleItem.InitialPercent = ScaleInitialPercent;
            s_CachedScale = config.ItemScale;
        }

        internal string ConstructScaleHelperString(float _value)
        {
            ClientConfig config = ConfigManager.ClientConfig;
            config.ItemScale = MathHelper.Lerp(0.5f, 1.5f, _value);
            string s = string.Format("Scale: {0:0}{1:F1}", c_NumberColorTag, config.ItemScale);
            //s += c_ReadOnlyValueColorTag + " Raw value: " + MathHelper.Lerp(0.5f, 1.5f, _value));
            UpdateHudConfigs(); // HACK!!;

            return s;
        }

        internal void ScaleOnCancel()
        {
            ConfigManager.ClientConfig.ItemScale = s_CachedScale;
            UpdateHudConfigs();

            //MyAPIGateway.Utilities.ShowNotification("Scale = " + ConfigManager.ClientConfig.ItemScale, 3000);
        }
#endif
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
                UpdatePanelConfig();
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
