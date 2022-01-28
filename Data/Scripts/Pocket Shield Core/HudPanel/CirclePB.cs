// ;
using Draygo.API;
using ExShared;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Utils;
using VRageMath;

using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace PocketShieldCore
{
    class CirclePB
    {
        private const float c_IconSize = 32.0f;

        public bool Visible { get; set; }
        public Vector2D Position { get; set; } /* Position is Top-Left. */
        public Color Color { get; set; }

        public float Percent { get; set; } /* The percentage of this Progressbar, from 0 - 1. */


        private static Vector2 s_UvOffset = new Vector2(0.0f, 0.25f);
        private static Vector2 s_UvSize = new Vector2(0.25f, 0.25f);
        private List<HudAPIv2.BillBoardTriHUDMessage> m_TriParts;

        private static List<Vector2> s_FixedPoints;

        static CirclePB()
        {
            /* Point list:
             *  4   0   1
             *  
             *  _   _   _
             * 
             *  3   _   2
             */

            s_FixedPoints = new List<Vector2>(5);
            s_FixedPoints.Add(new Vector2(0.5f, 0.0f)); // 0;
            s_FixedPoints.Add(new Vector2(1.0f, 0.0f)); // 1;
            s_FixedPoints.Add(new Vector2(1.0f, 1.0f)); // 2;
            s_FixedPoints.Add(new Vector2(0.0f, 1.0f)); // 3;
            s_FixedPoints.Add(new Vector2(0.0f, 0.0f)); // 4;
        }

        public CirclePB()
        {

            m_TriParts = new List<HudAPIv2.BillBoardTriHUDMessage>(5);

            for (int i = 0; i < 5; ++i)
            {
                m_TriParts.Add(new HudAPIv2.BillBoardTriHUDMessage()
                {
                    //Material = MyStringId.GetOrCompute("PocketShield_ShieldIcons"),
                    Material = MyStringId.GetOrCompute("PocketShield_BG"),
                    Width = c_IconSize,
                    Height = c_IconSize,
                    BillBoardColor = new Color(187, 233, 246),
                    P0 = new Vector2(0.5f, 0.5f),// * s_UvSize + s_UvOffset,
                    Blend = BlendTypeEnum.PostPP,
                    Options = HudAPIv2.Options.Pixel,
                });
            }


            UpdateItemCardConfig();
            /* 
            BG: 80 92 103
            FG: 187 233 246
            AnimatedSegment: 212 251 254 0.7
            */

        }

        ~CirclePB()
        {
            m_TriParts.Clear();
        }

        public void UpdateItemCard()
        {
            if (!Visible || Percent == 0.0f)
            {
                for (int i = 0; i < 5; ++i)
                    m_TriParts[i].Visible = false;

                return;
            }

            #region Do Not Open! You have been warned.
            else if (Percent <= 0.125f)
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

            foreach (var triPart in m_TriParts)
            {
                //triPart.P1 = triPart.P1 * s_UvSize + s_UvOffset;
                //triPart.P2 = triPart.P2 * s_UvSize + s_UvOffset;
            }


            //ClientLogger.Log("TriParts Pos = " + Position.ToString());


        }

        public void UpdateItemCardConfig()
        {
            foreach (var part in m_TriParts)
            {
                part.Origin = Position;
                part.Scale = 1.0f;
            }



            //ClientLogger.Log("TriParts Pos = " + Position.ToString());


            UpdateItemCard();
        }


    }
}
