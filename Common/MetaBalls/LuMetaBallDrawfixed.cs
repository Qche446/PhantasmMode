using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Common.MetaBalls
{
    public class LuMetaBallDrawfixed : ModSystem
    {
        //public delegate void RenderTargetUpdateDelegate();
        //public static event RenderTargetUpdateDelegate NewMetaBallRtEvent;
        public override void OnModLoad()
        {
            On_Main.DrawProjectiles += On_Main_DrawProjectiles;
            //On_FilterManager.EndCapture += On_FilterManager_EndCapture;
        }
        public override void OnModUnload()
        {
            On_Main.DrawProjectiles -= On_Main_DrawProjectiles;
            //On_FilterManager.EndCapture -= On_FilterManager_EndCapture;
        }
        private static void On_Main_DrawProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
        {
            FixedDraw();
            orig?.Invoke(self);
        }
        public static void FixedDraw()
        {
            var list = ModContent.GetContent<MetaballType>().Where(c => c is FixedMetaball);
            foreach (var instance in list)
            {
                if (instance.ShouldRender)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    for (int i = 0; i < instance.LayerTargets.Count; i++)
                    {
                        instance.PrepareShaderForTarget(i);
                        Main.spriteBatch.Draw(instance.LayerTargets[i], Main.screenLastPosition - Main.screenPosition, Color.White);
                    }
                    Main.spriteBatch.End();
                }
            }
        }
    }
}
