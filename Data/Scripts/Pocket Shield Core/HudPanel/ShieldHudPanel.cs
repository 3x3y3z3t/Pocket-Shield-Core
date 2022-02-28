// ;
using Draygo.API;
using ExShared;
using System.Collections.Generic;
using System.Text;
using VRage.Utils;
using VRageMath;

using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using ShieldIconDrawInfo = PocketShieldCore.PocketShieldAPIV2.ShieldIconDrawInfo;
using StatIconDrawInfo = PocketShieldCore.PocketShieldAPIV2.StatIconDrawInfo;

namespace PocketShieldCore
{
    public partial class ShieldHudPanel
    {
        public const float TEXT_SCALE = 15.0f;
        public const float MARGIN = 5.0f;
        public const float HEIGHT_MIN = ShieldPanel.PANEL_HEIGHT_MAX;
        public const float MANUAL_PANEL_HEIGHT_NEG = 10.0f;

        public const int COLUMN_COUNT = 2; /* Numbers of items on one row, a.k.a Column Count. */
        

        public static readonly Color PlateColor = new Color(41, 54, 62);
        public static readonly Color BGColor = Utils.CalculateBGColor(new Color(80, 92, 103), 0.5f); // new Color(80, 92, 103);
        public static readonly Color BGColorDark = Color.FromNonPremultiplied(80, 92, 103, 127);
        public static readonly Color FGColorPositive = new Color(162, 232, 252);
        public static readonly Color FGColorNegative = new Color(186, 8, 2, 223);

        public static List<List<object>> ShieldIconPropertiesList { get; private set; }
        public static List<List<object>> ItemCardIconPropertiesList { get; private set; }

        public bool RequireConfigUpdate { get; set; } = true;

        public bool Visible { get; set; } = false;
        public float BackgroundOpacity { get; set; }
        public Vector2D Position { get { return m_Position; } }
        public float Width { get { return m_Width; } }
        public float Height { get { if (m_Height > ShieldPanel.PANEL_HEIGHT_MAX) return m_Height; return ShieldPanel.PANEL_HEIGHT_MAX; } }

        
        private bool HasAnyShield { get { return m_ManualDataRef.HasShield || m_AutoDataRef.HasShield; } }

        private Vector2D m_Position = Vector2D.Zero;
        private float m_Width = ShieldPanel.PANEL_WIDTH_MAX;
        private float m_Height = ShieldPanel.PANEL_HEIGHT_MAX;

        private float m_CachedStatPanelHeight = 0.0f;
        private int m_RowCount = 0;
        private int m_LastItemCount = 0;

        private MyShieldData m_ManualDataRef = null;
        private MyShieldData m_AutoDataRef = null;
        private readonly ClientConfig m_Config = null;
        private readonly Logger m_Logger = null;

        private static Dictionary<MyStringHash, ShieldIconDrawInfo> m_ShieldIconListInternal = null;
        private static Dictionary<MyStringHash, StatIconDrawInfo> m_StatIconListInternal = null;

        private ShieldPanel m_ManualShieldPanel = null;
        private ShieldPanel m_AutoShieldPanel = null;
        private Dictionary<MyStringHash, StatPanel> m_StatPanels = null;
        
        private HudAPIv2.BillBoardHUDMessage m_BackgroundMidPlate = null;

        #region Debug
        public static bool Debug { get; set; }
        private StringBuilder m_DebugLabelSB = null;
        private HudAPIv2.HUDMessage m_DebugLabel = null;
        #endregion


        static ShieldHudPanel()
        {
            Debug = false;
            ShieldIconPropertiesList = new List<List<object>>();
            ItemCardIconPropertiesList = new List<List<object>>();
            m_ShieldIconListInternal = new Dictionary<MyStringHash, ShieldIconDrawInfo>(MyStringHash.Comparer);
            m_StatIconListInternal = new Dictionary<MyStringHash, StatIconDrawInfo>(MyStringHash.Comparer);
        }

        public ShieldHudPanel(MyShieldData _manualShieldData, MyShieldData _autoShieldData, ClientConfig _config, Logger _logger)
        {
            m_ManualDataRef = _manualShieldData;
            m_AutoDataRef = _autoShieldData;
            m_Config = _config;
            m_Logger = _logger;

            #region Text HUD API Initialization
            m_BackgroundMidPlate = new HudAPIv2.BillBoardHUDMessage()
            {
                Material = MyStringId.GetOrCompute("PocketShield_BG"),
                Blend = BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel
            };
            
            #endregion


            
            m_ManualShieldPanel = new ShieldPanel(m_ManualDataRef, m_Config, m_Logger);
            m_AutoShieldPanel = new ShieldPanel(m_AutoDataRef, m_Config, m_Logger);
            m_StatPanels = new Dictionary<MyStringHash, StatPanel>(MyStringHash.Comparer);




            #region Debug
            m_DebugLabelSB = new StringBuilder("DEBUG");
            m_DebugLabel = new HudAPIv2.HUDMessage()
            {
                Message = m_DebugLabelSB,
                Visible = Debug,
                Origin = new Vector2D(20.0, 100.0),
                Scale = 12.0f,
                InitialColor = Color.Yellow,
                Blend = BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel
            };
            #endregion

        }

        public static void Close()
        {
            ShieldIconPropertiesList = null;
            ItemCardIconPropertiesList = null;
            m_ShieldIconListInternal = null;
            m_StatIconListInternal = null;
        }

        public void UpdatePanel()
        {
            m_ManualShieldPanel.Update();
            m_AutoShieldPanel.Update();

            foreach (var panel in m_StatPanels.Values)
            {
                panel.HasStat = false;
            }

            int itemCount = 0;
            foreach (var pair in m_AutoDataRef.DefResList)
            {
                if (!m_StatPanels.ContainsKey(pair.Key))
                    continue;

                StatPanel panel = m_StatPanels[pair.Key];
                panel.Slot = itemCount;
                panel.Def = pair.Value.Def;
                panel.Res = pair.Value.Res;
                panel.HasStat = true;
                panel.Update();
                panel.UpdateVisibility();
                ++itemCount;
            }

            if (m_LastItemCount != itemCount)
            {
                m_LastItemCount = itemCount;
                m_RowCount = MathHelper.CeilToInt(itemCount / COLUMN_COUNT);
                RequireConfigUpdate = true;
            }

            if (RequireConfigUpdate)
                UpdatePanelConfig();
        }

        public void UpdatePanelConfig()
        {
            m_CachedStatPanelHeight = (m_RowCount * StatPanel.PANEL_HEIGHT_MAX + (m_RowCount - 1) * MARGIN);
            m_Height = 0.0f;
            m_Width = ShieldPanel.PANEL_WIDTH_MAX * m_Config.ItemScale;

            if (m_ManualDataRef.HasShield)
                m_Height += ShieldPanel.PANEL_HEIGHT_MAX - MANUAL_PANEL_HEIGHT_NEG;

            if (m_AutoDataRef.HasShield)
                m_Height += ShieldPanel.PANEL_HEIGHT_MAX;

            if (m_AutoDataRef.DefResList.Count > 0)
            {
                m_Height += MARGIN;
                m_Height += m_CachedStatPanelHeight;
            }

            m_Height += MARGIN;

            m_Height *= m_Config.ItemScale;
            m_CachedStatPanelHeight *= m_Config.ItemScale;

            m_BackgroundMidPlate.Width = m_Width;
            m_BackgroundMidPlate.Height = m_Height;
            m_BackgroundMidPlate.BillBoardColor = Utils.CalculateBGColor(PlateColor, BackgroundOpacity);

            foreach (var panel in m_StatPanels.Values)
            {
                panel.UpdateConfig();
            }
            m_ManualShieldPanel.UpdateConfig();
            m_AutoShieldPanel.UpdateConfig();
            
            UpdatePanelPosition();
            UpdatePanelVisibility();

            RequireConfigUpdate = false;
        }

        public void UpdatePanelPosition()
        {
            m_Position.X = m_Config.PanelPosition.X;
            m_Position.Y = m_Config.PanelPosition.Y - m_Height;

            m_BackgroundMidPlate.Origin = m_Position;

            double nextItemY = m_Position.Y;
            if (m_ManualDataRef.HasShield)
                m_ManualShieldPanel.UpdatePosition(new Vector2D(m_Position.X, nextItemY));

            if (m_ManualDataRef.HasShield && m_AutoDataRef.HasShield)
                nextItemY += (ShieldPanel.PANEL_HEIGHT_MAX - MANUAL_PANEL_HEIGHT_NEG) * m_Config.ItemScale;

            if (m_AutoDataRef.HasShield)
                m_AutoShieldPanel.UpdatePosition(new Vector2D(m_Position.X, nextItemY));

            if (m_AutoDataRef.DefResList.Count > 0)
            {
                nextItemY += (ShieldPanel.PANEL_HEIGHT_MAX + MARGIN) * m_Config.ItemScale;
                foreach (var panel in m_StatPanels.Values)
                {
                    int x = panel.Slot % COLUMN_COUNT;
                    int y = panel.Slot / COLUMN_COUNT;
                    panel.UpdatePosition(new Vector2D(
                        m_Position.X + x * StatPanel.PANEL_WIDTH_MAX * m_Config.ItemScale,
                        nextItemY + y * (StatPanel.PANEL_HEIGHT_MAX + MARGIN) * m_Config.ItemScale));
                }
            }
        }

        public void UpdatePanelVisibility()
        {
            m_BackgroundMidPlate.Visible = Visible && HasAnyShield && m_Config.ShowPanel && m_Config.ShowPanelBackground;

            if (m_ManualShieldPanel != null)
                m_ManualShieldPanel.UpdateVisibility(Visible);
            if (m_AutoShieldPanel != null)
                m_AutoShieldPanel.UpdateVisibility(Visible);

            foreach (StatPanel panel in m_StatPanels.Values)
            {
                panel.Visible = Visible;
                panel.UpdateVisibility();
            }
            
        }

        #region Debug
        private string[] indicators = new string[]
        {
            ">----",
            "->---",
            "-->--",
            "--->-",
            "---->"
        };
        int indInd = 0;

        public void DebugUpdate()
        {
            m_DebugLabel.Visible = Debug;
            if (!Debug)
                return;

            m_DebugLabel.Scale = 15.0f;

            ++indInd;
            if (indInd >= indicators.Length)
                indInd -= indicators.Length;

            m_DebugLabelSB.Clear();
            DebugLine("MyShieldData", indicators[indInd]);
            DebugLine("SubtypeId", m_AutoDataRef.SubtypeId, 1);
            DebugLine("Energy", (int)m_AutoDataRef.Energy + "/" + (int)m_AutoDataRef.MaxEnergy, 1);
            DebugLine("OverchargeRemainingPercent", m_AutoDataRef.OverchargeRemainingPercent, 1);
            foreach (var key in m_AutoDataRef.DefResList.Keys)
            {
                if (m_AutoDataRef.DefResList[key].IsZero)
                    continue;

                string s = "[" + c_ColorValue + key.String + c_ColorLabel + "] "
                    + c_ColorValue + m_AutoDataRef.DefResList[key].Def + c_ColorLabel + " Def, "
                    + c_ColorValue + m_AutoDataRef.DefResList[key].Res + c_ColorLabel + " Res";
            }
            DebugLine("ItemCardCount", m_StatPanels.Count);


        }

        const string c_ColorLabel = "<color=255,255,255,255>";
        const string c_ColorValue = "<color=255,255,0,255>";
        private void DebugLine(string _label, object _value, int _indent = 0)
        {
            string s = "";
            for (int i = 0; i < _indent; ++i)
                s += "  ";
            s += c_ColorLabel + _label + ": " + c_ColorValue + _value + "\n";
            m_DebugLabelSB.Append(s);
        }
        #endregion

        
        private float GetDef(MyStringHash _damageType)
        {
            if (m_AutoDataRef.DefResList.ContainsKey(_damageType))
                return m_AutoDataRef.DefResList[_damageType].Def;

            return 0.0f;
        }

        private float GetRes(MyStringHash _damageType)
        {
            if (m_AutoDataRef.DefResList.ContainsKey(_damageType))
                return m_AutoDataRef.DefResList[_damageType].Res;

            return 0.0f;
        }

        public void CacheIconLists()
        {
            if (ShieldIconPropertiesList.Count != m_StatIconListInternal.Count)
            {
                foreach (var item in ShieldIconPropertiesList)
                {
                    ShieldIconDrawInfo info = new ShieldIconDrawInfo(item);
                    if (!m_ShieldIconListInternal.ContainsKey(info.SubtypeId))
                        m_ShieldIconListInternal[info.SubtypeId] = info;
                }
            }

            if (ItemCardIconPropertiesList.Count != m_StatIconListInternal.Count)
            {
                foreach (var item in ItemCardIconPropertiesList)
                {
                    StatIconDrawInfo info = new StatIconDrawInfo(item);
                    if (!m_StatIconListInternal.ContainsKey(info.DamageType))
                    {
                        m_StatIconListInternal[info.DamageType] = info;

                        m_StatPanels[info.DamageType] = new StatPanel(m_Config, m_Logger)
                        {
                            Material = info.Material,
                            UvEnabled = info.UvEnabled,
                            UvSize = info.UvSize,
                            UvOffset = info.UvOffset
                        };
                        m_StatPanels[info.DamageType].UpdateConfig();
                    }
                }
            }
        }

        public static void CleanupStatics()
        {
            ShieldIconPropertiesList.Clear();
            ShieldIconPropertiesList = null;

            ItemCardIconPropertiesList.Clear();
            ItemCardIconPropertiesList = null;
        }


    }
}
