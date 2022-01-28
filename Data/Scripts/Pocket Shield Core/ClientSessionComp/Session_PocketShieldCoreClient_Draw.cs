// ;
using ExShared;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace PocketShieldCore
{
    public partial class Session_PocketShieldCoreClient
    {
        //private Dictionary<long, OtherCharacterShieldData> m_ShieldDamageEffects = new Dictionary<long, OtherCharacterShieldData>();
        private List<OtherCharacterShieldData> m_DrawList = new List<OtherCharacterShieldData>();

        private const float RADIUS = 1.20f;
        private const MySimpleObjectRasterizer RASTERIZATION = MySimpleObjectRasterizer.SolidAndWireframe;
        private const int WIRE_DIVIDE_RATIO = 20;
        private const float LINE_THICKNESS = 0.004f;
        private const float INTENSITY = 5;

        //float percent = 0.0f;
        //int timer = 0;
        private void Draw_DrawShieldEffects()
        {
            if (MyAPIGateway.Session == null)
                return;

            IMyEntity targetEntity = null;

            m_DrawList.Clear();
            m_DrawList.Add(new OtherCharacterShieldData()
            {
                EntityId = MyAPIGateway.Session.Player.Character.EntityId,
                ShieldAmountPercent = 0.9f,
                Ticks = 5
            });

            targetEntity = MyAPIGateway.Entities.GetEntity((IMyEntity _ent) => { return _ent.Name == "HELPME"; });
            if (targetEntity != null)
            {
                m_DrawList.Add(new OtherCharacterShieldData()
                {
                    EntityId = targetEntity.EntityId,
                    ShieldAmountPercent = 0.2f,
                    Ticks = 8
                });
            }
            targetEntity = null;

            Color drawColor = Color.White;
            
            foreach (var data in m_DrawList)
            {
                targetEntity = MyAPIGateway.Entities.GetEntityById(data.EntityId);
                if (targetEntity == null)
                    continue;

                GenerateColorFromGradientPercent(out drawColor, data.ShieldAmountPercent, (float)data.Ticks / Constants.HIT_EFFECT_LIVE_TICKS);

                var characterMatrix = targetEntity.WorldMatrix;
                var adjMatrix = MatrixD.CreateWorld(characterMatrix.Up * 1.0 + targetEntity.GetPosition(), characterMatrix.Down, characterMatrix.Forward);

                try
                {
                    MySimpleObjectDraw.DrawTransparentSphere(ref adjMatrix, RADIUS, ref drawColor, RASTERIZATION, WIRE_DIVIDE_RATIO,
                        MyStringId.GetOrCompute("Square"), MyStringId.GetOrCompute("Square"), LINE_THICKNESS,
                        blendType: BlendTypeEnum.Standard, intensity: INTENSITY);

                }
                catch (Exception _e)
                {
                    m_Logger.WriteLine("  > Exception < Error during drawing shield effects: " + _e.Message, 0);
                }

                if (data.ShouldPlaySound)
                {
                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition("ArcParticleElectricalDischarge", targetEntity.GetPosition());
                    data.ShouldPlaySound = false;
                }

            }



        }

        public void GenerateColorFromGradientPercent(out Color _color, float _percent, float _alphaPercent)
        {

#if true
            float hue = (_percent * 180.0f) / 360.0f;
            _color = ColorExtensions.HSVtoColor(new Vector3(hue, 1.0f, 1.0f));
            //ClientLogger.Log("percent = " + _percent + ", hue = " + hue + ", color = " + _color.ToString());

            //float baseAlpha = MathHelper.Lerp(48.0f, 48.0f, _percent);
            _color.A = (byte)(_alphaPercent * 223.0f);
#else
            // R FF FF 00
            // G 00 FF FF
            // B 00 00 FF
            int r, g, b, a;

            if (_percent < 0.5f)
            {
                r = 255;
                b = 0;
            }
            else
            {
                r = (int)MathHelper.Lerp(255.0f, 0.0f, (_percent - 0.5f) / 0.5f);
                b = (int)MathHelper.Lerp(0.0f, 255.0f, (_percent - 0.5f) / 0.5f);
            }

            if (_percent > 0.5f)
                g = 255;
            else
                g = (int)MathHelper.Lerp(0.0f, 255.0f, _percent / 0.5f);

            float baseAlpha = MathHelper.Lerp(48.0f, 48.0f, _percent);
            a = (int)MathHelper.Lerp(baseAlpha, 191, _alphaPercent);

            _color = Color.FromNonPremultiplied(r, g, b, a);
#endif



        }
    }
}
