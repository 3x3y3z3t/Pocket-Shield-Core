// ;
using Draygo.API;
using ExShared;
using System.Collections.Generic;
using System.Text;
using VRage.Utils;
using VRageMath;

using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using ShieldIconDrawInfo = PocketShieldCore.PocketShieldAPI.ShieldIconDrawInfo;
using ItemCardDrawInfo = PocketShieldCore.PocketShieldAPI.ItemCardDrawInfo;
using System;

namespace PocketShieldCore
{
    public class ShieldHudPanel
    {
        public const int ColumnCount = 2; /* Numbers of items on one row, a.k.a Column Count. */
        private const float c_TextScale = 15.0f;
        private const float c_ShieldIconWidth = 24.0f;
        private const float c_ShieldBarWidth = 150.0f;
        private const float c_ShieldLabelWidth = 60.0f;
        private const float c_Margin = 5.0f;

        public const float PanelMaxWidth = (15.0f * 2.0f + c_ShieldIconWidth + c_ShieldBarWidth + c_ShieldLabelWidth);
        public const float PanelMaxWidthHalf = PanelMaxWidth * 0.5f;

        public static readonly Color PlateColor = new Color(41, 54, 62);
        public static readonly Color BGColor = new Color(80, 92, 103);
        public static readonly Color BGColorDark = Color.FromNonPremultiplied(80, 92, 103, 127);
        public static readonly Color FGColorPositive = new Color(162, 232, 252);
        public static readonly Color FGColorNegative = new Color(186, 8, 2, 223);

        public static List<IList<object>> ShieldIconPropertiesList { get; private set; }
        public static List<IList<object>> ItemCardIconPropertiesList { get; private set; }

        public bool RequireUpdate { get; set; } = true;
        public bool Visible { get; set; }

        public Vector2D Origin { get; private set; } = Vector2D.Zero;

        public Vector2 PanelSize { get { return m_PanelSize; } }
        public float PanelExtraHeight { get { return m_PanelExtraHeight; } }


        public float BackgroundOpacity { get; set; }

        
        private Vector2 m_PanelSize = new Vector2();
        private float m_PanelBaseHeight = 0.0f;
        private float m_PanelExtraHeight = 0.0f;
        private int m_RowCount = 0;
        private int m_LastItemCount = 0;

        private MyShieldData m_DataRef = null;
        private ClientConfig m_Config = null;
        private Logger m_Logger = null;
        
        private Dictionary<MyStringHash, ShieldIconDrawInfo> m_ShieldIconListInternal = new Dictionary<MyStringHash, ShieldIconDrawInfo>(MyStringHash.Comparer);
        private Dictionary<MyStringHash, ItemCardDrawInfo> m_ItemCardIconListInternal = new Dictionary<MyStringHash, ItemCardDrawInfo>(MyStringHash.Comparer);

        #region Text HUD API
        private StringBuilder m_ShieldLabelSB = null;

        private HudAPIv2.BillBoardHUDMessage m_BackgroundMidPlate = null;
        private HudAPIv2.BillBoardHUDMessage m_ShieldIcon = null;
        private HudAPIv2.BillBoardHUDMessage m_ShieldBarBack = null;
        private HudAPIv2.BillBoardHUDMessage m_ShieldBarFore = null;
        private HudAPIv2.HUDMessage m_ShieldLabel = null;
        private Dictionary<MyStringHash, ItemCard> m_ItemCards = new Dictionary<MyStringHash, ItemCard>(MyStringHash.Comparer);
        #endregion

        #region Debug
        public static bool Debug { get; set; }
        private StringBuilder m_DebugLabelSB = null;
        private HudAPIv2.HUDMessage m_DebugLabel = null;
        #endregion


        static ShieldHudPanel()
        {
            Debug = false;
            ShieldIconPropertiesList = new List<IList<object>>();
            ItemCardIconPropertiesList = new List<IList<object>>();
        }

        public ShieldHudPanel(MyShieldData _data, ClientConfig _config, Logger _logger)
        {
            m_DataRef = _data;
            m_Config = _config;
            m_Logger = _logger;

            m_ShieldLabelSB = new StringBuilder("0");

            #region Text HUD API Initialization
            m_BackgroundMidPlate = new HudAPIv2.BillBoardHUDMessage()
            {
                Material = MyStringId.GetOrCompute("PocketShield_BG"),
                Blend = BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel
            };
            m_ShieldIcon = new HudAPIv2.BillBoardHUDMessage()
            {
                Material = MyStringId.GetOrCompute("PocketShield_ShieldIcons"),
                BillBoardColor = FGColorPositive,
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

            //foreach (var info in ItemCardIconList)
            //{
            //    ItemCard item = new ItemCard(m_Config, m_Logger)
            //    {
            //        Visible = false,
            //        Material = info.Value.Material,
            //        UvEnabled = info.Value.UvEnabled,
            //        UvSize = info.Value.UvSize,
            //        UvOffset = info.Value.UvOffset
            //    };
            //    item.UpdateItemCard();

            //    m_ItemCards[info.Key] = item;
            //}
            #endregion

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

        ~ShieldHudPanel()
        {
            // HACK! we can do this because there is only one of this object exist in the whole session;
            ShieldIconPropertiesList = null;
            ItemCardIconPropertiesList = null;
        }

        public void UpdatePanel()
        {
            if (!RequireUpdate)
                return;

            Visible = m_DataRef.HasShield;

            m_BackgroundMidPlate.Visible = Visible && m_Config.ShowPanel && m_Config.ShowPanelBackground;

            m_ShieldIcon.Visible = Visible && m_Config.ShowPanel;
            if (m_DataRef.HasShield && m_ShieldIconListInternal.ContainsKey(m_DataRef.SubtypeId))
            {
                ShieldIconDrawInfo shieldIconInfo = m_ShieldIconListInternal[m_DataRef.SubtypeId];
                m_ShieldIcon.Material = shieldIconInfo.Material;
                m_ShieldIcon.uvEnabled = shieldIconInfo.UvEnabled;
                m_ShieldIcon.uvSize = shieldIconInfo.UvSize;
                m_ShieldIcon.uvOffset = shieldIconInfo.UvOffset;
            }

            float percent = m_DataRef.Energy / m_DataRef.MaxEnergy;
            m_ShieldBarFore.Visible = Visible && m_Config.ShowPanel;
            m_ShieldBarFore.uvSize = new Vector2(percent, 1.0f);
            m_ShieldBarFore.Width = percent * c_ShieldBarWidth * m_Config.ItemScale;

            m_ShieldBarBack.Visible = Visible && m_Config.ShowPanel;

            m_ShieldLabel.Visible = Visible && m_Config.ShowPanel;

            foreach (ItemCard item in m_ItemCards.Values)
            {
                if (item.Visible && !Visible)
                {
                    item.Visible = false;
                    item.UpdateItemCard();
                }
                else
                    item.Visible = false;
            }

            int itemCount = 0;
            foreach (var pair in m_DataRef.DefResList)
            {
                if (!m_ItemCards.ContainsKey(pair.Key))
                    continue;
                
                m_ItemCards[pair.Key].Visible = Visible && m_Config.ShowPanel;
                m_ItemCards[pair.Key].Slot = itemCount;
                m_ItemCards[pair.Key].Def = pair.Value.Def;
                m_ItemCards[pair.Key].Res = pair.Value.Res;
                m_ItemCards[pair.Key].UpdateItemCard();
                ++itemCount;
            }

            if (m_LastItemCount != itemCount)
            {
                m_LastItemCount = itemCount;
                m_RowCount = MathHelper.CeilToInt(itemCount / ColumnCount);
                UpdatePanelPosition();
            }

            m_ShieldLabelSB.Clear();
            m_ShieldLabelSB.Append((int)m_DataRef.Energy);
            //m_ShieldLabelSB.Append("99.9k");

            RequireUpdate = false;
        }

        public void UpdatePanelConfig()
        {
            m_PanelBaseHeight = (15.0f * 2.0f + c_ShieldIconWidth) * m_Config.ItemScale;

            m_PanelSize.X = PanelMaxWidth * m_Config.ItemScale;
            
            m_BackgroundMidPlate.Visible = Visible && m_Config.ShowPanel && m_Config.ShowPanelBackground;
            m_BackgroundMidPlate.Width = m_PanelSize.X;
            m_BackgroundMidPlate.BillBoardColor = Utils.CalculateBGColor(PlateColor, BackgroundOpacity);

            m_ShieldIcon.Visible = Visible && m_Config.ShowPanel;
            m_ShieldIcon.Width = c_ShieldIconWidth * m_Config.ItemScale;
            m_ShieldIcon.Height = c_ShieldIconWidth * m_Config.ItemScale;
            m_ShieldIcon.Offset = new Vector2D(15.0 * m_Config.ItemScale, 15.0 * m_Config.ItemScale);

            m_ShieldBarBack.Visible = Visible && m_Config.ShowPanel;
            m_ShieldBarBack.Width = c_ShieldBarWidth * m_Config.ItemScale;
            m_ShieldBarBack.Height = 11.0f * m_Config.ItemScale;
            m_ShieldBarBack.Offset = new Vector2D(55.0 * m_Config.ItemScale, 24.0 * m_Config.ItemScale);

            float percent = m_DataRef.Energy / m_DataRef.MaxEnergy;
            m_ShieldBarFore.Visible = m_ShieldBarBack.Visible;
            m_ShieldBarFore.Width = percent * c_ShieldBarWidth * m_Config.ItemScale;
            m_ShieldBarFore.Height = m_ShieldBarBack.Height;
            m_ShieldBarFore.Offset = m_ShieldBarBack.Offset;

            m_ShieldLabel.Visible = Visible && m_Config.ShowPanel;
            m_ShieldLabel.Offset = new Vector2D(210.0 * m_Config.ItemScale, 23.0 * m_Config.ItemScale);
            m_ShieldLabel.Scale = c_TextScale * m_Config.ItemScale;

            foreach (var key in m_DataRef.DefResList.Keys)
            {
                if (m_ItemCards.ContainsKey(key))
                {
                    m_ItemCards[key].Visible = Visible && m_Config.ShowPanel;
                }
            }

            UpdatePanelPosition();
            
            RequireUpdate = true;
        }

        private void UpdatePanelPosition()
        {
            m_PanelExtraHeight = (m_RowCount * ItemCard.c_Height + ((m_RowCount > 0) ? 5.0f : 0.0f)) * m_Config.ItemScale;
            m_PanelSize.Y = m_PanelBaseHeight + m_PanelExtraHeight;

            Origin = new Vector2D(m_Config.PanelPosition.X, m_Config.PanelPosition.Y + m_PanelBaseHeight - m_PanelSize.Y);
            
            m_BackgroundMidPlate.Origin = Origin;
            m_BackgroundMidPlate.Height = m_PanelSize.Y;
            m_ShieldIcon.Origin = Origin;
            m_ShieldBarBack.Origin = Origin;
            m_ShieldBarFore.Origin = Origin;
            m_ShieldLabel.Origin = Origin;

            foreach (var key in m_DataRef.DefResList.Keys)
            {
                if (m_ItemCards.ContainsKey(key))
                {
                    m_ItemCards[key].Origin = new Vector2D(Origin.X, Origin.Y + m_PanelBaseHeight);
                    m_ItemCards[key].UpdateItemCard();
                }
            }
        }

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
            DebugLine("SubtypeId", m_DataRef.SubtypeId, 1);
            DebugLine("Energy", (int)m_DataRef.Energy + "/" + (int)m_DataRef.MaxEnergy, 1);
            DebugLine("OverchargeRemainingPercent", m_DataRef.OverchargeRemainingPercent, 1);
            foreach (var key in m_DataRef.DefResList.Keys)
            {
                if (m_DataRef.DefResList[key].IsZero)
                    continue;

                string s = "[" + c_ColorValue + key.String + c_ColorLabel + "] "
                    + c_ColorValue + m_DataRef.DefResList[key].Def + c_ColorLabel + " Def, "
                    + c_ColorValue + m_DataRef.DefResList[key].Res + c_ColorLabel + " Res";
            }
            DebugLine("ItemCardCount", m_ItemCards.Count);


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






        private float GetDef(MyStringHash _damageType)
        {
            if (m_DataRef.DefResList.ContainsKey(_damageType))
                return m_DataRef.DefResList[_damageType].Def;

            return 0.0f;
        }

        private float GetRes(MyStringHash _damageType)
        {
            if (m_DataRef.DefResList.ContainsKey(_damageType))
                return m_DataRef.DefResList[_damageType].Res;

            return 0.0f;
        }

        public void CacheIconLists()
        {
            if (ShieldIconPropertiesList.Count != m_ItemCardIconListInternal.Count)
            {
                foreach (var item in ShieldIconPropertiesList)
                {
                    ShieldIconDrawInfo info = new ShieldIconDrawInfo(item);
                    if (!m_ShieldIconListInternal.ContainsKey(info.SubtypeId))
                        m_ShieldIconListInternal[info.SubtypeId] = info;
                }
            }

            if (ItemCardIconPropertiesList.Count != m_ItemCardIconListInternal.Count)
            {
                foreach (var item in ItemCardIconPropertiesList)
                {
                    ItemCardDrawInfo info = new ItemCardDrawInfo(item);
                    if (!m_ItemCardIconListInternal.ContainsKey(info.DamageType))
                    {
                        m_ItemCardIconListInternal[info.DamageType] = info;

                        ItemCard itemcard = new ItemCard(m_Config, m_Logger)
                        {
                            Visible = false,
                            Material = info.Material,
                            UvEnabled = info.UvEnabled,
                            UvSize = info.UvSize,
                            UvOffset = info.UvOffset
                        };
                        itemcard.UpdateItemCard();
                        m_ItemCards[info.DamageType] = itemcard;
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










#if false




        public float Percent { set { m_OverchargeIcon.Percent = value; UpdatePanelConfig(); } }
        

        //private Vector2D SubstatPanelPos { get { return ConfigManager.ClientConfig.PanelPosition + new Vector2D(0.0, 55.0); } }
        private static Vector2D SubstatPanelOffset { get { return new Vector2D(0.0, 55.0 * m_Config.ItemScale); } }

        public static Vector2 PanelSize
        {
            get
            {
                float bgWidth = Constants.PANEL_WIDTH * m_Config.ItemScale;
                //float bgHeight = config.Padding * 2.0f + config.DisplayItemsCount * ItemCard.ItemCardHeight + config.Margin * 2.0f * (config.DisplayItemsCount - 1);
                float bgHeight = (float)SubstatPanelOffset.Y + ItemCard.Height + Constants.MARGIN * config.ItemScale;
                //if (config.ShowMaxRangeIcon)
                //    bgHeight += config.Padding;

                //float cursorPosY = config.Padding;
                //if (config.ShowMaxRangeIcon)
                //{
                //    bgHeight += RadarRangeIconHeight + config.Padding;
                //    cursorPosY += RadarRangeIconHeight + config.Padding;
                //}

                return new Vector2(bgWidth, bgHeight);
            }
        }






        private CirclePB m_OverchargeIcon = null;
        

        private int m_IconTextureSlot = Constants.TEXTURE_BLANK;



        private HudAPIv2.BillBoardTriHUDMessage m_Part = null;
        private HudAPIv2.BillBoardHUDMessage m_ComparePart = null;

        internal static List<MyStringHash> s_RegisteredDamageType = new List<MyStringHash>();

        static ShieldHudPanel()
        {
            LastSlot = 0;

        }

        public ShieldHudPanel()
        {
            ClientConfig config = ConfigManager.ClientConfig;

            Visible = false;

            m_ItemCards = new List<ItemCard>(2);


            AddItemCard(new ItemCard());
            AddItemCard(new ItemCard());








            m_Part = new HudAPIv2.BillBoardTriHUDMessage()
            {
                Material = MyStringId.GetOrCompute("Atlas_E_01"),
                Visible = false,
                Origin = new Vector2D(0.0, 0.0),

                P0 = new Vector2(0.0f, 0.0f),
                P1 = new Vector2(0.0f, 1.0f),
                P2 = new Vector2(1.0f, 1.0f),

                Width = 256,
                Height = 256,
                BillBoardColor = Color.White,

                Blend = BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel
            };
            m_ComparePart = new HudAPIv2.BillBoardHUDMessage()
            {
                Material = MyStringId.GetOrCompute("Atlas_E_01"),
                Visible = false,

                Origin = new Vector2D(288.0, 0.0),
                Width = 256,
                Height = 256,
                BillBoardColor = Color.White,

                Blend = BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel
            };



            m_OverchargeIcon = new CirclePB()
            {
                //Position = new Vector2D(0.0, 0.0)
            };
            //m_OverchargeIcon.Percent = 1.0f;
#endif




#if false
            m_Background = new HudAPIv2.BillBoardHUDMessage()
            {
                Material = MyStringId.GetOrCompute("Pantenna_BG"),
                Origin = config.PanelPosition,
                Width = config.PanelWidth,
                Height = 0.0f,
                Visible = Visible,
                BillBoardColor = CalculateBGColor(),
                Blend = BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel
            };
            m_TextureSlot = Constants.TEXTURE_ANTENNA;
            m_RadarRangeIcon = new HudAPIv2.BillBoardHUDMessage()
            {
                Material = MyStringId.GetOrCompute("Pantenna_ShipIcons"),
                Origin = config.PanelPosition + new Vector2D(config.Padding, config.Padding),
                Width = RadarRangeIconHeight,
                Height = RadarRangeIconHeight,
                uvEnabled = true,
                uvSize = new Vector2(0.25f, 0.5f),
                uvOffset = new Vector2((m_TextureSlot % 4) * 0.25f, (m_TextureSlot / 4) * 0.5f),
                TextureSize = 1.0f,
                Visible = Visible,
                Blend = BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel
            };
            m_RadarRangeLabel = new HudAPIv2.HUDMessage()
            {
                Message = m_RadarRangeSB,
                //Origin = config.PanelPosition + new Vector2D(s_RadarRangeIconSize + config.Padding + config.SpaceBetweenItems, config.Padding + 8.0 * config.ItemScale),
                //Font = MyFontEnum.Red,
                Scale = RadarPanel.LabelScale,
                //InitialColor = color,
                ShadowColor = Color.Black,
                Visible = Visible,
                Blend = BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel | HudAPIv2.Options.Shadowing
            };

            Color color = Color.Darken(Color.FromNonPremultiplied(218, 62, 62, 255), 0.2);
            Vector2D cursorPos = config.PanelPosition + new Vector2D(config.Padding, RadarRangeIconHeight + config.Padding * 2.0f);
            //Color color = new Color(218, 62, 62);
            //Color color = Color.White;

            for (int i = 0; i < Constants.DISPLAY_ITEMS_COUNT; ++i)
            {
                ItemCard item = new ItemCard(cursorPos, color);
                cursorPos = item.NextItemPosition;
                m_ItemCards.Add(item);
            }

            UpdatePanelConfig();
        }

        public void UpdatePanel(ref MyShieldData _data)
        {
            if (_data.PlayerSteamUserId == 0U)
            {
                if (Visible)
                {
                    Visible = false;
                    UpdatePanelConfig();
                }
                return;
            }
            if (!Visible)
            {
                Visible = true;
                UpdatePanelConfig();
            }
                m_IconTextureSlot = Constants.TEXTURE_BLANK;
            
            if (_data.SubtypeId == MyStringHash.GetOrCompute(Constants.SUBTYPEID_EMITTER_BAS))
            {
                m_IconTextureSlot = Constants.TEXTURE_SHIELD_BAS;
            }
            else if (_data.SubtypeId == MyStringHash.GetOrCompute(Constants.SUBTYPEID_EMITTER_ADV))
            {
                m_IconTextureSlot = Constants.TEXTURE_SHIELD_ADV;
            }

            m_ShieldLabelSB.Clear();
            m_ShieldLabelSB.Append(Utils.FormatShieldValue(_data.Energy));

            m_ShieldIcon.uvOffset = new Vector2((m_IconTextureSlot % Constants.ICON_ATLAS_W) * c_UvSizeX, (m_IconTextureSlot / Constants.ICON_ATLAS_W) * c_UvSizeY);

            m_ShieldBarFore.uvSize = new Vector2(_data.EnergyRemainingPercent, 1.0f);
            m_ShieldBarFore.Width = c_ShieldBarWidth * _data.EnergyRemainingPercent;
               

            //m_TrajectoryIcon.uvOffset = new Vector2((m_TrajectoryTextureSlot % 4) * 0.25f, (m_TrajectoryTextureSlot / 4) * 0.5f);

            //m_OverchargeIcon.Percent = _data.OverchargeRemainingPercent;

            //ClientLogger.Log("_data.EnergyRemainingPercent = " + _data.EnergyRemainingPercent);
            m_OverchargeIcon.Percent = _data.EnergyRemainingPercent;

            m_OverchargeIcon.UpdateItemCard();

            //ClientLogger.Log("count =    " + m_ItemCards.Count);
            //ClientLogger.Log("defcount = " + _data.Def.Count);
            //ClientLogger.Log("rescount = " + _data.Res.Count);
            for (int i = 0; i < m_ItemCards.Count; ++i)
            {
                ItemCard item = m_ItemCards[i];
                if (i < _data.Def.Count)
                {
                    item.Def = _data.Def[i].Item2;
                    item.Res = _data.Res[i].Item2;

                    item.DefTextureSlot = GetDefTextureSlot(_data.Def[i].Item1);
                    item.ResTextureSlot = GetResTextureSlot(_data.Res[i].Item1);
                }
                else
                {
                    item.Def = 0.0f;
                    item.Res = 0.0f;
                }

                item.UpdateItemCard();
            }

        }

        public void UpdatePanelConfigOld()
        {
            ClientLogger.Log("UpdatePanelConfig()...", 5);
            ClientConfig config = ConfigManager.ClientConfig;







            m_OverchargeIcon.Visible = Visible && config.ShowPanel && false;
            //m_OverchargeIcon.Position = shieldIconPos;
            m_OverchargeIcon.Position = config.PanelPosition + m_ShieldIcon.Offset;
            m_OverchargeIcon.Color = FGColor;

            m_OverchargeIcon.UpdateItemCardConfig();
            

#if false
            

            float cursorPosY = (config.ShowMaxRangeIcon ? RadarRangeIconHeight + config.Padding : 0) + config.Padding;

            Logger.Log(">>   ItemCardSize = (" + ItemCard.ItemCardWidth + ", " + ItemCard.ItemCardHeight + ")", 5);
            Logger.Log(">>   bgWidth = " + panelSize.X, 5);
            Logger.Log(string.Format(">>   bgHeight = {0:0} * 2.0f + {1:0} * {2:0} + {3:0} * ({4:0} - 1) = {5:0}",
                config.Padding, config.DisplayItemsCount, ItemCard.ItemCardHeight, config.Margin * 2.0f, config.DisplayItemsCount, panelSize.Y), 5);
            Logger.Log(string.Format(">>   bgHeight = {0:0} + {1:0} + {2:0} = {3:0}",
                config.Padding * 3.0f, config.DisplayItemsCount * ItemCard.ItemCardHeight, config.Margin * 2.0f * (config.DisplayItemsCount - 1), panelSize.Y), 5);

            Logger.Log(">>   Visible = " + Visible + ", ShowPanelBG = " + config.ShowPanelBackground + ", ShowPanel = " + config.ShowPanel, 5);
            m_Background.Visible = Visible && config.ShowPanelBackground && config.ShowPanel;
            Logger.Log(">>     m_Background.Visible = " + m_Background.Visible, 5);
            m_Background.Origin = config.PanelPosition;
            m_Background.Width = panelSize.X;
            m_Background.Height = panelSize.Y;
            m_Background.BillBoardColor = CalculateBGColor();

            m_RadarRangeIcon.Visible = Visible && config.ShowMaxRangeIcon && config.ShowPanel;
            m_RadarRangeIcon.Origin = config.PanelPosition;
            m_RadarRangeIcon.Offset = new Vector2D(config.Padding, config.Padding);
            m_RadarRangeIcon.Width = RadarRangeIconHeight;
            m_RadarRangeIcon.Height = RadarRangeIconHeight;

            float offsY = config.Padding + RadarRangeIconHeight * 0.5f - Constants.MAGIC_LABEL_HEIGHT_16 * 0.5f * config.ItemScale;
            m_RadarRangeLabel.Visible = Visible && config.ShowMaxRangeIcon && config.ShowPanel;
            m_RadarRangeLabel.Origin = config.PanelPosition;
            m_RadarRangeLabel.Offset = new Vector2D(RadarRangeIconHeight + config.Padding + config.Margin * 2.0f, config.Padding + 8.0 * config.ItemScale);
            m_RadarRangeLabel.Scale = RadarPanel.LabelScale;
            Logger.Log(">>   Scale = " + config.ItemScale + ", Label Y = " + m_RadarRangeLabel.GetTextLength().Y, 5);

            Vector2D cursorPos = config.PanelPosition + new Vector2D(config.Padding, cursorPosY);
            for (int i = 0; i < Constants.DISPLAY_ITEMS_COUNT; ++i)
            {
                ItemCard item = m_ItemCards[i];
                item.Visible = Visible && config.ShowPanel;
                item.Position = cursorPos;
                item.UpdateItemCardConfig();
                cursorPos = item.NextItemPosition;
            }
#endif


        }

        private void AddItemCard(ItemCard _itemCard)
        {
            m_ItemCards.Add(_itemCard);
            LastSlot = m_ItemCards.Count;
        }

        private int GetDefTextureSlot(MyStringHash _damageType)
        {
            if (_damageType.String.EndsWith("Bullet"))
                return 2;

            if (_damageType.String.EndsWith("Explosion"))
                return 6;

            return 0;
        }
        
        private int GetResTextureSlot(MyStringHash _damageType)
        {
            if (_damageType.String.EndsWith("Bullet"))
                return 3;

            if (_damageType.String.EndsWith("Explosion"))
                return 7;

            return 0;
        }
    }
}
#endif

#if false
namespace Pantenna
{ 


    
    public partial class RadarPanel
    {
        public bool Visible { get; set; }
        public float BackgroundOpacity { get; set; }

        private Color m_HudBGColor = Color.White;
        private List<ItemCard> m_ItemCards = null; /* This keeps track of all 5 displayed item cards. */

        private int m_TextureSlot = 3;

        private static float RadarRangeIconHeight
        {
            get { return 30.0f * ConfigManager.ClientConfig.ItemScale; }
        }

        public static float LabelScale
        {
            get { return 16.0f * ConfigManager.ClientConfig.ItemScale; }
        }

        public static float PanelMinWidth
        {
            get { return ItemCard.ItemCardMinWidth + ConfigManager.ClientConfig.Padding * 2.0f; }
        }

        private StringBuilder m_RadarRangeSB = null;
        //private StringBuilder m_DummySB = null;

        private HudAPIv2.BillBoardHUDMessage m_Background = null;
        private HudAPIv2.BillBoardHUDMessage m_RadarRangeIcon = null;
        private HudAPIv2.HUDMessage m_RadarRangeLabel = null;

        public RadarPanel()
        {
        }

        ~RadarPanel()
        {
            m_ItemCards.Clear();
            m_ItemCards = null;

            //s_CharacterSize.Clear();
            //s_CharacterSize = null;

            m_RadarRangeSB = null;
            //m_DummySB = null;
        }

        public void UpdatePanel(List<SignalData> _signals)
        {
            ClientConfig config = ConfigManager.ClientConfig;

            m_RadarRangeSB.Clear();
            m_RadarRangeSB.Append(Utils.FormatDistanceAsString(config.RadarMaxRange));

            Logger.Log("  signal count: " + _signals.Count, 5);
            for (int i = 0; i < Constants.DISPLAY_ITEMS_COUNT; ++i)
            {
                ItemCard item = m_ItemCards[i];
                if (i >= _signals.Count || i >= config.DisplayItemsCount)
                {
                    Logger.Log("  updating item card " + i + ": i > _signal.Count, hide item", 5);
                    item.Visible = false;
                    item.UpdateItemCard();
                }
                else
                {
                    Logger.Log("  updating item card " + i + ": updating item", 5);
                    SignalData signal = _signals[i];

                    item.Visible = Visible && config.ShowPanel;
                    item.SignalType = signal.SignalType;
                    item.RelativeVelocity = signal.Velocity;
                    item.Distance = signal.Distance;

                    item.DisplayNameRawString = signal.DisplayName;

                    item.UpdateItemCard();
                }
            }
        }

        public void UpdatePanelConfig()
        {
        }


#endif
    }
}
