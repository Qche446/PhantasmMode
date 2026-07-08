using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Luminance.Core.Graphics;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Effects;
using Microsoft.Xna.Framework;

namespace FargosPhantasmMode.Common.MetaBalls
{
    public class LuMetaBallDrawfixed : ModSystem
    {
        public delegate void RenderTargetUpdateDelegate();
        public static event RenderTargetUpdateDelegate NewMetaBallRtEvent;
        public override void OnModLoad()
        {
            //On_Main.DrawProjectiles += On_Main_DrawProjectiles;
            On_FilterManager.EndCapture += FilterManager_EndCapture;
        }
        public override void OnModUnload()
        {
            //On_Main.DrawProjectiles -= On_Main_DrawProjectiles;
            On_FilterManager.EndCapture -= FilterManager_EndCapture;
        }
        private static void On_Main_DrawProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
        {
            NewMetaBallRtEvent?.Invoke();
            orig?.Invoke(self);
        }
        private static void FilterManager_EndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            NewMetaBallRtEvent?.Invoke();
            orig?.Invoke(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
    }
}
