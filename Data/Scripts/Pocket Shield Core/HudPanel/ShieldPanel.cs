// ;
using Draygo.API;
using ExShared;
using System.Text;
using VRage.Utils;
using VRageMath;

using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using ShieldIconDrawInfo = PocketShieldCore.PocketShieldAPIV2.ShieldIconDrawInfo;

namespace PocketShieldCore
{
    public partial class ShieldHudPanel
    {
        class ShieldPanel
        {
            public const float PANEL_WIDTH_MAX = MARGIN + ProgressCircle.ICON_SIZE + MARGIN + c_ShieldBarWidth + MARGIN + c_ShieldLabelWidth;
            public const float PANEL_WIDTH_MAX_HALF = PANEL_WIDTH_MAX * 0.5f;

            public const float PANEL_HEIGHT_MAX = ProgressCircle.ICON_SIZE;

            private const float c_ShieldIconWidth = 24.0f;
            private const float c_ShieldBarWidth = 150.0f;
            private const float c_ShieldLabelWidth = 60.0f;


            public bool Visible { get; private set; } = false;
            //public Vector2D Position { get; private set; } = Vector2D.Zero;
            //public float Width { get { return PANEL_WIDTH_MAX * m_Config.ItemScale; } }
            //public float Height { get { return PANEL_HEIGHT_MAX * m_Config.ItemScale; } }


            private ProgressCircle m_OverchargeIcon = null;


            private StringBuilder m_ShieldLabelSB = null;

            private HudAPIv2.BillBoardHUDMessage m_ShieldIcon = null;
            private HudAPIv2.BillBoardHUDMessage m_ShieldBarBack = null;
            private HudAPIv2.BillBoardHUDMessage m_ShieldBarFore = null;
            private HudAPIv2.HUDMessage m_ShieldLabel = null;

            private readonly MyShieldData m_DataRef = null;
            private readonly ClientConfig m_Config = null;
            private readonly Logger m_Logger = null;

            public ShieldPanel(MyShieldData _dataRef, ClientConfig _config, Logger _logger)
            {
                m_DataRef = _dataRef;
                m_Config = _config;
                m_Logger = _logger;

                m_ShieldLabelSB = new StringBuilder("0");

                
                #region Text HUD API Initialization
                m_ShieldIcon = new HudAPIv2.BillBoardHUDMessage()
                {
                    Material = MyStringId.GetOrCompute("PocketShield_ShieldIcons"),
                    uvEnabled = true,
                    uvSize = new Vector2(Constants.ICON_ATLAS_UV_SIZE_X, Constants.ICON_ATLAS_UV_SIZE_Y),
                    uvOffset = new Vector2((Constants.ICON_SHIELD_0 % Constants.ICON_ATLAS_W) * Constants.ICON_ATLAS_UV_SIZE_X,
                                           (Constants.ICON_SHIELD_0 / Constants.ICON_ATLAS_W) * Constants.ICON_ATLAS_UV_SIZE_Y),
                    Blend = BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel
                };
                m_ShieldBarBack = new HudAPIv2.BillBoardHUDMessage()
                {
                    Material = MyStringId.GetOrCompute("PocketShield_ShieldBar"),
                    BillBoardColor = BGColor,
                    Blend = BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel
                };
                m_ShieldBarFore = new HudAPIv2.BillBoardHUDMessage()
                {
                    Material = MyStringId.GetOrCompute("PocketShield_ShieldBar"),
                    uvEnabled = true,
                    BillBoardColor = FGColorPositive,
                    Blend = BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel
                };
                m_ShieldLabel = new HudAPIv2.HUDMessage
                {
                    Message = m_ShieldLabelSB,
                    ShadowColor = Color.Black,
                    Blend = BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel
                };
                #endregion
                
                if (!m_DataRef.IsManual)
                m_OverchargeIcon = new ProgressCircle(_config, _logger);
                

            }

            public void Close()
            {

            }

            public void Update()
            {
                if (m_DataRef.HasShield && m_ShieldIconListInternal.ContainsKey(m_DataRef.SubtypeId))
                {
                    ShieldIconDrawInfo shieldIconInfo = m_ShieldIconListInternal[m_DataRef.SubtypeId];
                    m_ShieldIcon.Material = shieldIconInfo.Material;
                    m_ShieldIcon.uvEnabled = shieldIconInfo.UvEnabled;
                    m_ShieldIcon.uvSize = shieldIconInfo.UvSize;
                    m_ShieldIcon.uvOffset = shieldIconInfo.UvOffset;
                }
                m_ShieldIcon.BillBoardColor = m_DataRef.IsTurnedOn ? FGColorPositive : FGColorNegative;

                float percent = m_DataRef.Energy / m_DataRef.MaxEnergy;
                m_ShieldBarFore.uvSize = new Vector2(percent, 1.0f);
                m_ShieldBarFore.Width = percent * c_ShieldBarWidth * m_Config.ItemScale;
                
                m_ShieldLabelSB.Clear();
                m_ShieldLabelSB.Append((int)m_DataRef.Energy);
                //m_ShieldLabelSB.Append("99.9k");

                if (m_OverchargeIcon != null)
                {
                    m_OverchargeIcon.Visible = Visible;
                    m_OverchargeIcon.Percent = m_DataRef.EnergyRemainingPercent;
                }
                m_OverchargeIcon?.Update();
            }


            public void UpdateConfig()
            {
                m_ShieldIcon.Width = c_ShieldIconWidth * m_Config.ItemScale;
                m_ShieldIcon.Height = c_ShieldIconWidth * m_Config.ItemScale;
                m_ShieldIcon.Offset = new Vector2D(12.0 * m_Config.ItemScale, 12.0 * m_Config.ItemScale);

                m_ShieldBarBack.Width = c_ShieldBarWidth * m_Config.ItemScale;
                m_ShieldBarBack.Height = 11.0f * m_Config.ItemScale;
                m_ShieldBarBack.Offset = new Vector2D(50.0 * m_Config.ItemScale, 19.0 * m_Config.ItemScale);

                float percent = m_DataRef.Energy / m_DataRef.MaxEnergy;
                m_ShieldBarFore.Width = percent * c_ShieldBarWidth * m_Config.ItemScale;
                m_ShieldBarFore.Height = m_ShieldBarBack.Height;
                m_ShieldBarFore.Offset = m_ShieldBarBack.Offset;

                m_ShieldLabel.Offset = new Vector2D(205.0 * m_Config.ItemScale, 18.0 * m_Config.ItemScale);
                m_ShieldLabel.Scale = TEXT_SCALE * m_Config.ItemScale;

                m_OverchargeIcon?.UpdateConfig();


            }

            public void UpdatePosition(Vector2D _position)
            {
                m_ShieldIcon.Origin = _position;
                m_ShieldBarBack.Origin = _position;
                m_ShieldBarFore.Origin = _position;
                m_ShieldLabel.Origin = _position;

                m_OverchargeIcon?.UpdatePosition(_position);
            }

            public void UpdateVisibility(bool _visible)
            {
                Visible = _visible && m_DataRef.HasShield && m_Config.ShowPanel;

                m_ShieldIcon.Visible = Visible;
                m_ShieldBarFore.Visible = Visible;
                m_ShieldBarBack.Visible = Visible;
                m_ShieldLabel.Visible = Visible;

                if (m_OverchargeIcon != null)
                {
                    m_OverchargeIcon.Visible = Visible;
                }
            }



        }
    }
}
