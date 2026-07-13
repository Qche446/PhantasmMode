using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Common.MetaBalls
{
    public class LuMetaBallDrawfixed : ModSystem
    {
        public delegate void RenderTargetUpdateDelegate();
        public static event RenderTargetUpdateDelegate NewMetaBallRtEvent;
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
            NewMetaBallRtEvent?.Invoke();
            orig?.Invoke(self);
        }
        private void On_FilterManager_EndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            NewMetaBallRtEvent?.Invoke();
            orig?.Invoke(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
    }
}
