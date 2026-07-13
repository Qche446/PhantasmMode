using FargowiltasSouls.Assets.ExtraTextures;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace FargosPhantasmMode.Content.Render
{
    public static class BossBarRender
    {
        /// <summary>
        /// 双色混合脉冲式绘制
        /// </summary>
        public static void DrawDoubleColorPulse(Texture2D barTexture, Vector2 barTopLeft, Rectangle barFrame, float scale, float lifeRatio, Color color1, Color color2, float omiga)
        {
            ManagedShader healthBarShader = ShaderManager.GetShader("FargosPhantasmMode.BossBarShader");
            Texture2D noise = FargosTextureRegistry.WavyNoise.Value;

            Main.spriteBatch.GraphicsDevice.Textures[1] = noise;

            healthBarShader.TrySetParameter("lifeRatio", lifeRatio);
            healthBarShader.TrySetParameter("imageSize", barTexture.Size());
            healthBarShader.TrySetParameter("sourceRectangle", new Vector4(barFrame.X, barFrame.Y, barFrame.Width, barFrame.Height));
            healthBarShader.TrySetParameter("color1", color1);
            healthBarShader.TrySetParameter("color2", color2);
            healthBarShader.TrySetParameter("omiga", omiga);
            healthBarShader.Apply();
            Vector2 stretchScale = new(scale / barFrame.Width, 1f);
            Main.spriteBatch.Draw(barTexture, barTopLeft, barFrame, Color.White, 0f, Vector2.Zero, stretchScale, 0, 0f);
        }
        public static void DrawVerticalRolling(Texture2D barTexture, Vector2 barTopLeft, Rectangle barFrame, float scale, float lifeRatio, Color color1, Color color2, float omiga)
        {
            ManagedShader healthBarShader = ShaderManager.GetShader("FargosPhantasmMode.BossBarShader");
            Texture2D noise = FargosTextureRegistry.Techno1Noise.Value;
            Main.spriteBatch.GraphicsDevice.Textures[1] = noise;

            healthBarShader.TrySetParameter("lifeRatio", lifeRatio);
            healthBarShader.TrySetParameter("imageSize", barTexture.Size());
            healthBarShader.TrySetParameter("sourceRectangle", new Vector4(barFrame.X, barFrame.Y, barFrame.Width, barFrame.Height));
            healthBarShader.TrySetParameter("color1", color1);
            healthBarShader.TrySetParameter("color2", color2);
            healthBarShader.TrySetParameter("omiga", omiga);
            healthBarShader.Apply("VerticalRolling");
            Vector2 stretchScale = new(scale / barFrame.Width, 1f);
            Main.spriteBatch.Draw(barTexture, barTopLeft, barFrame, Color.White, 0f, Vector2.Zero, stretchScale, 0, 0f);
        }
    }
}
