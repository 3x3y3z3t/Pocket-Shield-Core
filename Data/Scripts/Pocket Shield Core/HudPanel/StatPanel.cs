// ;
using Draygo.API;
using ExShared;
using System.Text;
using VRage.Utils;
using VRageMath;

using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace PocketShieldCore
{
    public partial class ShieldHudPanel
    {
        public class StatPanel
        {
            public const float PANEL_WIDTH_MAX = ShieldPanel.PANEL_WIDTH_MAX_HALF;
            public const float PANEL_HEIGHT_MAX = 46.0f;

            private const float c_DefIconOffs = 22.0f;
            private const float c_ResIconOffs = 70.0f;
            private const float c_IconSize = 30.0f;
            private const float c_TextScale = 12.0f;

            //public MyStringHash DamageType = MyStringHash.NullOrEmpty;
            public MyStringId Material = MyStringId.NullOrEmpty;
            public bool UvEnabled = false;
            public Vector2 UvSize = Vector2.Zero;
            public Vector2 UvOffset = Vector2.Zero;

            public int Slot { get; set; } = 0;
            public bool Visible { get; set; } = false;
            //public Vector2D Position { get; private set; } = Vector2D.Zero; /* Position is Top-Left. */
            public bool FirstUpdate { get; set; } = true;

            public bool HasStat { get; set; } = false;
            public float Def { get; set; } = 0.0f;
            public float Res { get; set; } = 0.0f;

            private ClientConfig m_Config = null;
            private Logger m_Logger = null;

            private StringBuilder m_DefLabelSB = null;
            private StringBuilder m_ResLabelSB = null;

            private HudAPIv2.BillBoardHUDMessage m_Separator = null;
            private HudAPIv2.BillBoardHUDMessage m_DefIcon = null;
            private HudAPIv2.BillBoardHUDMessage m_ResIcon = null;
            private HudAPIv2.HUDMessage m_DefLabel = null;
            private HudAPIv2.HUDMessage m_ResLabel = null;

            public StatPanel(ClientConfig _config, Logger _logger)
            {
                m_Config = _config;
                m_Logger = _logger;

                m_DefLabelSB = new StringBuilder("0%");
                m_ResLabelSB = new StringBuilder("0%");

                #region Text HUD API Initialization
                m_Separator = new HudAPIv2.BillBoardHUDMessage()
                {
                    Material = MyStringId.GetOrCompute("PocketShield_BG"),
                    BillBoardColor = FGColorPositive,
                    Blend = BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel
                };
                m_DefIcon = new HudAPIv2.BillBoardHUDMessage()
                {
                    Blend = BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel
                };
                m_ResIcon = new HudAPIv2.BillBoardHUDMessage()
                {
                    Blend = BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel
                };
                m_DefLabel = new HudAPIv2.HUDMessage()
                {
                    Message = m_DefLabelSB,
                    ShadowColor = Color.Black,
                    Blend = BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel
                };
                m_ResLabel = new HudAPIv2.HUDMessage()
                {
                    Message = m_ResLabelSB,
                    ShadowColor = Color.Black,
                    Blend = BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel
                };
                #endregion
            }

            public void Update()
            {
                m_DefLabelSB.Clear();
                m_DefLabelSB.Append(Utils.FormatPercent(Def));

                m_ResLabelSB.Clear();
                m_ResLabelSB.Append(Utils.FormatPercent(Res));

                if (Def > 0.0f)
                    m_DefIcon.BillBoardColor = FGColorPositive;
                else if (Def < 0.0f)
                    m_DefIcon.BillBoardColor = FGColorNegative;
                else
                    m_DefIcon.BillBoardColor = BGColorDark;

                if (Res > 0.0f)
                    m_ResIcon.BillBoardColor = FGColorPositive;
                else if (Res < 0.0f)
                    m_ResIcon.BillBoardColor = FGColorNegative;
                else
                    m_ResIcon.BillBoardColor = BGColorDark;
            }

            public void UpdateConfig()
            {
                float itemOffsX = (Slot % COLUMN_COUNT); // * ShieldHudPanel.PanelMaxWidthHalf;
                float itemOffsY = (Slot / COLUMN_COUNT); // * ShieldHudPanel.PanelMaxWidthHalf;

                float separatorOffsX = (itemOffsX + 5.0f) * m_Config.ItemScale;

                float defIconOffsX = (itemOffsX + c_DefIconOffs) * m_Config.ItemScale;
                float resIconOffsX = (itemOffsX + c_ResIconOffs) * m_Config.ItemScale;
                float defresIconOffsY = (itemOffsY + 0.0f) * m_Config.ItemScale;

                float defLblOffsX = (itemOffsX + c_DefIconOffs + c_IconSize * 0.5f) * m_Config.ItemScale - (float)m_DefLabel.GetTextLength().X * 0.5f;
                float resLblOffsX = (itemOffsX + c_ResIconOffs + c_IconSize * 0.5f) * m_Config.ItemScale - (float)m_ResLabel.GetTextLength().X * 0.5f;
                float defresLblOffsY = (itemOffsY + c_IconSize + 5.0f) * m_Config.ItemScale;

                m_Separator.Width = 5.0f * m_Config.ItemScale;
                m_Separator.Height = PANEL_HEIGHT_MAX * m_Config.ItemScale;
                m_Separator.Offset = new Vector2D(separatorOffsX, 0);

                m_DefIcon.Width = c_IconSize * m_Config.ItemScale;
                m_DefIcon.Height = c_IconSize * m_Config.ItemScale;
                m_DefIcon.Offset = new Vector2D(defIconOffsX, defresIconOffsY);

                m_ResIcon.Width = c_IconSize * m_Config.ItemScale;
                m_ResIcon.Height = c_IconSize * m_Config.ItemScale;
                m_ResIcon.Offset = new Vector2D(resIconOffsX, defresIconOffsY);

                m_DefLabel.Scale = c_TextScale * m_Config.ItemScale;
                m_DefLabel.Offset = new Vector2D(defLblOffsX, defresLblOffsY);

                m_ResLabel.Scale = c_TextScale * m_Config.ItemScale;
                m_ResLabel.Offset = new Vector2D(resLblOffsX, defresLblOffsY);
                
                if (FirstUpdate)
                {
                    m_DefIcon.Material = Material;
                    m_DefIcon.uvEnabled = UvEnabled;
                    m_DefIcon.uvSize = UvEnabled ? new Vector2(UvSize.X * 0.5f, UvSize.Y) : new Vector2(0.5f, 1.0f);
                    m_DefIcon.uvOffset = UvEnabled ? UvOffset : new Vector2(0.0f, 0.0f);

                    m_ResIcon.Material = Material;
                    m_ResIcon.uvEnabled = UvEnabled;
                    m_ResIcon.uvSize = UvEnabled ? new Vector2(UvSize.X * 0.5f, UvSize.Y) : new Vector2(0.5f, 1.0f);
                    m_ResIcon.uvOffset = UvEnabled ? new Vector2(UvOffset.X + m_ResIcon.uvSize.X, UvOffset.Y) : new Vector2(0.5f, 0.0f);
                    
                    FirstUpdate = false;
                }
            }

            public void UpdatePosition(Vector2D _position)
            {
                m_Separator.Origin = _position;
                m_DefIcon.Origin = _position;
                m_ResIcon.Origin = _position;
                m_DefLabel.Origin = _position;
                m_ResLabel.Origin = _position;
            }

            public void UpdateVisibility()
            {
                m_Separator.Visible = Visible && HasStat;
                m_DefIcon.Visible = Visible && HasStat;
                m_ResIcon.Visible = Visible && HasStat;
                m_DefLabel.Visible = Visible && HasStat;
                m_ResLabel.Visible = Visible && HasStat;
            }



        }

    }
}
