// ;
using Draygo.API;
using ExShared;
using System.Collections.Generic;
using System.Text;
using VRage.Utils;
using VRageMath;

using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace PocketShieldCore
{
    public enum ItemType
    {
        BonusEnergy,
    }

    public class ItemCard
    {
        private const float c_DefIconOffs = 22.0f;
        private const float c_ResIconOffs = 70.0f;
        private const float c_IconSize = 30.0f;
        public const float c_Height = 46.0f;
        private const float c_TextScale = 12.0f;

        //public MyStringHash DamageType = MyStringHash.NullOrEmpty;
        public MyStringId Material = MyStringId.NullOrEmpty;
        public bool UvEnabled = false;
        public Vector2 UvSize = Vector2.Zero;
        public Vector2 UvOffset = Vector2.Zero;

        public int Slot { get; set; } = 0;
        public bool Visible { get; set; } = false;
        public Vector2D Origin { get; set; } = Vector2D.Zero; /* Position is Top-Left. */
        
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

        public ItemCard(ClientConfig _config, Logger _logger)
        {
            m_Config = _config;
            m_Logger = _logger;
            
            m_DefLabelSB = new StringBuilder("0%");
            m_ResLabelSB = new StringBuilder("0%");

            #region Text HUD API Initialization
            m_Separator = new HudAPIv2.BillBoardHUDMessage()
            {
                Material = MyStringId.GetOrCompute("PocketShield_BG"),
                BillBoardColor = ShieldHudPanel.FGColorPositive,
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

        public void UpdateItemCard()
        {
            m_Separator.Visible = Visible;
            m_DefIcon.Visible = Visible;
            m_ResIcon.Visible = Visible;
            m_DefLabel.Visible = Visible;
            m_ResLabel.Visible = Visible;

            if (!Visible)
                return;

            m_DefLabelSB.Clear();
            m_DefLabelSB.Append(Utils.FormatPercent(Def));

            m_ResLabelSB.Clear();
            m_ResLabelSB.Append(Utils.FormatPercent(Res));

            if (Def > 0.0f)
                m_DefIcon.BillBoardColor = ShieldHudPanel.FGColorPositive;
            else if (Def < 0.0f)
                m_DefIcon.BillBoardColor = ShieldHudPanel.FGColorNegative;
            else
                m_DefIcon.BillBoardColor = ShieldHudPanel.BGColorDark;

            if (Res > 0.0f)
                m_ResIcon.BillBoardColor = ShieldHudPanel.FGColorPositive;
            else if (Res < 0.0f)
                m_ResIcon.BillBoardColor = ShieldHudPanel.FGColorNegative;
            else
                m_ResIcon.BillBoardColor = ShieldHudPanel.BGColorDark;

            float itemOffsX = (Slot % ShieldHudPanel.ColumnCount) * ShieldHudPanel.PanelMaxWidthHalf;
            float itemOffsY = (Slot / ShieldHudPanel.ColumnCount) * ShieldHudPanel.PanelMaxWidthHalf;

            float separatorOffsX = (itemOffsX + 5.0f) * m_Config.ItemScale;

            float defIconOffsX = (itemOffsX + c_DefIconOffs) * m_Config.ItemScale;
            float resIconOffsX = (itemOffsX + c_ResIconOffs) * m_Config.ItemScale;
            float defresIconOffsY = (itemOffsY + 0.0f) * m_Config.ItemScale;

            float defLblOffsX = (itemOffsX + c_DefIconOffs + c_IconSize * 0.5f) * m_Config.ItemScale - (float)m_DefLabel.GetTextLength().X * 0.5f;
            float resLblOffsX = (itemOffsX + c_ResIconOffs + c_IconSize * 0.5f) * m_Config.ItemScale - (float)m_ResLabel.GetTextLength().X * 0.5f;
            float defresLblOffsY = (itemOffsY + c_IconSize + 5.0f) * m_Config.ItemScale;

            m_Separator.Origin = new Vector2D(Origin.X + separatorOffsX, Origin.Y);
            m_Separator.Width = 5.0f * m_Config.ItemScale;
            m_Separator.Height = c_Height * m_Config.ItemScale;

            m_DefIcon.Material = Material;
            m_DefIcon.Origin = new Vector2D(Origin.X + defIconOffsX, Origin.Y + defresIconOffsY);
            m_DefIcon.Width = c_IconSize * m_Config.ItemScale;
            m_DefIcon.Height = c_IconSize * m_Config.ItemScale;
            m_DefIcon.uvEnabled = UvEnabled;
            m_DefIcon.uvSize = UvEnabled ? new Vector2(UvSize.X * 0.5f, UvSize.Y) : new Vector2(0.5f, 1.0f);
            m_DefIcon.uvOffset = UvEnabled ? UvOffset : new Vector2(0.0f, 0.0f);

            m_ResIcon.Material = Material;
            m_ResIcon.Origin = new Vector2D(Origin.X + resIconOffsX, Origin.Y + defresIconOffsY);
            m_ResIcon.Width = c_IconSize * m_Config.ItemScale;
            m_ResIcon.Height = c_IconSize * m_Config.ItemScale;
            m_ResIcon.uvEnabled = UvEnabled;
            m_ResIcon.uvSize = UvEnabled ? new Vector2(UvSize.X * 0.5f, UvSize.Y) : new Vector2(0.5f, 1.0f);
            m_ResIcon.uvOffset = UvEnabled ? new Vector2(UvOffset.X + m_ResIcon.uvSize.X, UvOffset.Y) : new Vector2(0.5f, 0.0f);

            m_DefLabel.Origin = new Vector2D(Origin.X + defLblOffsX, Origin.Y + defresLblOffsY);
            m_DefLabel.Scale = c_TextScale * m_Config.ItemScale;

            m_ResLabel.Origin = new Vector2D(Origin.X + resLblOffsX, Origin.Y + defresLblOffsY);
            m_ResLabel.Scale = c_TextScale * m_Config.ItemScale;
            
        }
        


    }



}
