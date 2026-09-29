using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Core.Systems
{
    public class ResolutionTracker : ModSystem
    {
        private static int lastWidth;
        private static int lastHeight;
        private static bool initialized;
        public override void PostUpdateEverything()
        {
            if (!initialized)
            {
                lastWidth = Main.screenWidth;
                lastHeight = Main.screenHeight;
                initialized = true;
                return;
            }
            if (Main.screenWidth != lastWidth || Main.screenHeight != lastHeight)
            {
                lastWidth = Main.screenWidth;
                lastHeight = Main.screenHeight;
                OnResolutionChanged(lastWidth, lastHeight);
            }
        }
        private void OnResolutionChanged(int width, int height)
        {
            Mod.Logger.Info($"分辨率改变: {width}x{height}");
        }
    }
}
