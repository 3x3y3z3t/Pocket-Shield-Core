// ;
using Draygo.API;
using ExShared;
using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;

using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace PocketShieldCore
{
    class ProgressCircle
    {
        public const float ICON_SIZE = 48.0f;

        public float Percent { get; set; } = 0.0f;

        public bool Visible { get; set; } = false;
        //public Vector2D Position { get; private set; } = Vector2D.Zero; /* Position is Top-Left. */
        
        private readonly List<HudAPIv2.BillBoardTriHUDMessage> m_TriParts = null;
        private readonly HudAPIv2.BillBoardHUDMessage m_OriginPoint = null;

        private readonly ClientConfig m_Config = null;
        private readonly Logger m_Logger = null;

        /* Point list:
         *  4   0   1
         * 
         *  _       _
         * 
         *  3   _   2
         */
        private static List<Vector2> s_FixedPoints = new List<Vector2>(5)
        {
            new Vector2(0.5f, 0.0f), // 0;
            new Vector2(1.0f, 0.0f), // 1;
            new Vector2(1.0f, 1.0f), // 2;
            new Vector2(0.0f, 1.0f), // 3;
            new Vector2(0.0f, 0.0f), // 4;
        };
        
        private static Vector2 s_UvSize = new Vector2(Constants.ICON_ATLAS_UV_SIZE_X, Constants.ICON_ATLAS_UV_SIZE_Y);
        private static Vector2 s_UvOffset = new Vector2((Constants.ICON_OVERCHARGE % Constants.ICON_ATLAS_W) * Constants.ICON_ATLAS_UV_SIZE_X,
                                                        (Constants.ICON_OVERCHARGE / Constants.ICON_ATLAS_W) * Constants.ICON_ATLAS_UV_SIZE_Y);

        public ProgressCircle(ClientConfig _config, Logger _logger)
        {
            m_Config = _config;
            m_Logger = _logger;

            m_TriParts = new List<HudAPIv2.BillBoardTriHUDMessage>(5);
            
            
            for (int i = 0; i < 5; ++i)
            {
                m_TriParts.Add(new HudAPIv2.BillBoardTriHUDMessage()
                {
                    Material = MyStringId.GetOrCompute("PocketShield_OverchargeIcon"),
                    //Material = MyStringId.GetOrCompute("PocketShield_BG"),
                    BillBoardColor = ShieldHudPanel.FGColorPositive,
                    P0 = new Vector2(0.5f, 0.5f),
                    Blend = BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel,
                });
            }
            
            m_OriginPoint = new HudAPIv2.BillBoardHUDMessage()
            {
                Visible = false,
                Material = MyStringId.GetOrCompute("Square"),
                Width = 4.0f,
                Height = 4.0f,
                BillBoardColor = ShieldHudPanel.FGColorPositive,
                Blend = BlendTypeEnum.PostPP,
                Options = HudAPIv2.Options.Pixel,
            };

        }

        public void Close()
        {
            s_FixedPoints = null;
        }
            
        public void Update()
        {
            if (!Visible || Percent <= 0.0f)
            {
                m_TriParts[0].Visible = false;
                m_TriParts[1].Visible = false;
                m_TriParts[2].Visible = false;
                m_TriParts[3].Visible = false;
                m_TriParts[4].Visible = false;
                return;
            }

            if (Percent > 1.0f)
                Percent = 1.0f;

            #region Do Not Open! You have been warned.
            if (Percent <= 0.125f)
            {
                m_TriParts[0].Visible = Visible;
                m_TriParts[1].Visible = false;
                m_TriParts[2].Visible = false;
                m_TriParts[3].Visible = false;
                m_TriParts[4].Visible = false;

                double alpha = Percent * 2.0 * Math.PI;
                float x = 0.5f * (float)Math.Tan(alpha);
                m_TriParts[0].P1 = s_FixedPoints[0];
                m_TriParts[0].P2 = new Vector2(0.5f + x, 0.0f);
            }
            else if (Percent <= 0.375f)
            {
                m_TriParts[0].Visible = Visible;
                m_TriParts[1].Visible = Visible;
                m_TriParts[2].Visible = false;
                m_TriParts[3].Visible = false;
                m_TriParts[4].Visible = false;

                m_TriParts[0].P1 = s_FixedPoints[0];
                m_TriParts[0].P2 = s_FixedPoints[1];

                double alpha = (Percent - 0.125f) * 2.0 * Math.PI;
                float x = 0.5f * (float)Math.Tan(MathHelperD.PiOver4 - alpha);
                m_TriParts[1].P1 = s_FixedPoints[1];
                m_TriParts[1].P2 = new Vector2(1.0f, 0.5f - x);
            }
            else if (Percent <= 0.625f)
            {
                m_TriParts[0].Visible = Visible;
                m_TriParts[1].Visible = Visible;
                m_TriParts[2].Visible = Visible;
                m_TriParts[3].Visible = false;
                m_TriParts[4].Visible = false;

                m_TriParts[0].P1 = s_FixedPoints[0];
                m_TriParts[0].P2 = s_FixedPoints[1];

                m_TriParts[1].P1 = s_FixedPoints[1];
                m_TriParts[1].P2 = s_FixedPoints[2];

                double alpha = (Percent - 0.375f) * 2.0 * Math.PI;
                float x = 0.5f * (float)Math.Tan(MathHelperD.PiOver4 - alpha);
                m_TriParts[2].P1 = s_FixedPoints[2];
                m_TriParts[2].P2 = new Vector2(0.5f + x, 1.0f);

            }
            else if (Percent <= 0.875f)
            {
                m_TriParts[0].Visible = Visible;
                m_TriParts[1].Visible = Visible;
                m_TriParts[2].Visible = Visible;
                m_TriParts[3].Visible = Visible;
                m_TriParts[4].Visible = false;

                m_TriParts[0].P1 = s_FixedPoints[0];
                m_TriParts[0].P2 = s_FixedPoints[1];

                m_TriParts[1].P1 = s_FixedPoints[1];
                m_TriParts[1].P2 = s_FixedPoints[2];

                m_TriParts[2].P1 = s_FixedPoints[2];
                m_TriParts[2].P2 = s_FixedPoints[3];

                double alpha = (Percent - 0.625f) * 2.0 * Math.PI;
                float x = 0.5f * (float)Math.Tan(MathHelperD.PiOver4 - alpha);
                m_TriParts[3].P1 = s_FixedPoints[3];
                m_TriParts[3].P2 = new Vector2(0.0f, 0.5f + x);
            }
            else
            {
                m_TriParts[0].Visible = Visible;
                m_TriParts[1].Visible = Visible;
                m_TriParts[2].Visible = Visible;
                m_TriParts[3].Visible = Visible;
                m_TriParts[4].Visible = Visible;

                m_TriParts[0].P1 = s_FixedPoints[0];
                m_TriParts[0].P2 = s_FixedPoints[1];

                m_TriParts[1].P1 = s_FixedPoints[1];
                m_TriParts[1].P2 = s_FixedPoints[2];

                m_TriParts[2].P1 = s_FixedPoints[2];
                m_TriParts[2].P2 = s_FixedPoints[3];

                m_TriParts[3].P1 = s_FixedPoints[3];
                m_TriParts[3].P2 = s_FixedPoints[4];

                double alpha = (Percent - 0.875f) * 2.0 * Math.PI;
                float x = 0.5f * (float)Math.Tan(MathHelperD.PiOver4 - alpha);
                m_TriParts[4].P1 = s_FixedPoints[4];
                m_TriParts[4].P2 = new Vector2(0.5f - x, 0.0f);
            }
            #endregion
            
            
        }

        public void UpdateConfig()
        {
            foreach (var part in m_TriParts)
            {
                part.Width = ICON_SIZE * m_Config.ItemScale;
                part.Height = ICON_SIZE * m_Config.ItemScale;
                part.Offset = new Vector2D(-ICON_SIZE * m_Config.ItemScale, -ICON_SIZE * m_Config.ItemScale);
            }
        

            //ClientLogger.Log("TriParts Pos = " + Position.ToString());
            
        }


        public void UpdatePosition(Vector2D _position)
        {
            foreach (var part in m_TriParts)
            {
                part.Origin = _position;
            }
        }

    }
}
