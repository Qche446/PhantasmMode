using Fargowiltas.Common.Configs;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Projectiles.Souls;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;

namespace FargosPhantasmMode.Content.Projectiles.Masomode
{
    public class P_SolarEnchFlare : SolarEnchFlare
    {
        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[base.Projectile.owner];

            Vector2 center = base.Projectile.Center;
            float num = base.Projectile.width / 2;
            Player localPlayer = Main.LocalPlayer;
            Asset<Texture2D> magicPixel = TextureAssets.MagicPixel;
            Asset<Texture2D> wavyNoise = FargosTextureRegistry.WavyNoise;
            float num2 = base.Projectile.Opacity * ModContent.GetInstance<FargoClientConfig>().TransparentFriendlyProjectiles;
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.SolarEnchantShader");
            shader.TrySetParameter("colorMult", 7.35f);
            shader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
            shader.TrySetParameter("radius", num * base.Projectile.scale);
            shader.TrySetParameter("anchorPoint", center);
            shader.TrySetParameter("screenPosition", Main.screenPosition);
            shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
            shader.TrySetParameter("playerPosition", localPlayer.Center);
            shader.TrySetParameter("maxOpacity", num2);
            Main.spriteBatch.GraphicsDevice.Textures[1] = wavyNoise.Value;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, shader.WrappedEffect, Main.GameViewMatrix.TransformationMatrix);
            Rectangle destinationRectangle = new Rectangle(Main.screenWidth / 2, Main.screenHeight / 2, Main.screenWidth, Main.screenHeight);
            Main.spriteBatch.Draw(magicPixel.Value, destinationRectangle, null, default(Color), 0f, magicPixel.Value.Size() * 0.5f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
