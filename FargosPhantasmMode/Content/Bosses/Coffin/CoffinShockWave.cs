using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using Monochrome.Core.Graphics.Shaders;
using Terraria.GameContent;

namespace FargosPhantasmMode.Content.Bosses.Coffin;
/// <summary>
/// ai0持续时间，ai1初始半径,ai2为计时器
/// </summary>
public class CoffinShockWave : ModProjectile
{
    public override string Texture => FargoSoulsUtil.EmptyTexture;
    public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity;
    public override bool ShouldUpdatePosition() => false;
    ref float Timer => ref Projectile.ai[2];
    private float Vel => Projectile.ai[1] / Projectile.ai[0];
    public override void SetDefaults()
    {
        Projectile.friendly = false;
        Projectile.penetrate = -1;
        Projectile.width = 1;
        Projectile.height = 1;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
    }
    public override void AI()
    {
        Projectile.Opacity = Math.Clamp(Timer / 30, 0, 1);
        if (Timer >= Projectile.ai[0])
        {
            Projectile.Kill();
        }
        Timer++;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        var diagonalNoise = FargosTextureRegistry.WavyNoise;
        var blackTile = TextureAssets.MagicPixel;
        float R = Math.Abs(Projectile.ai[1] - Vel * Timer);
        MonoShader shader = PModeShaders.Library.GetShader("FargosPhantasmMode.ShockWaveShader");
        shader.TrySetParameter("screenPosition", Main.screenPosition);
        shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        shader.TrySetParameter("color1", Color.Purple);
        shader.TrySetParameter("color2", Color.MediumPurple);
        shader.TrySetParameter("maxOpacity", 0.2f * Projectile.Opacity);
        shader.TrySetParameter("Center", Projectile.Center);
        shader.TrySetParameter("Radius", R);
        shader.TrySetParameter("FadedWidth", Math.Clamp(R, 0, 100));
        Main.spriteBatch.GraphicsDevice.Textures[1] = diagonalNoise.Value;

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, shader.WrappedEffect, Main.GameViewMatrix.TransformationMatrix);
        Rectangle rekt = new(Main.screenWidth / 2, Main.screenHeight / 2, Main.screenWidth, Main.screenHeight);
        Main.spriteBatch.Draw(blackTile.Value, rekt, null, default, 0f, blackTile.Value.Size() * 0.5f, 0, 0f);
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        return false;
    }
}
