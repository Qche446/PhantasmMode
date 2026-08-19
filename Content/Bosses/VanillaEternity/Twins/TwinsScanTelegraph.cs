using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    /// <summary>
    /// ai[0]魔焰自己，ai[1]时间, ai[2]初始角度范围
    /// 自动对齐方向，位置绑定魔焰自己
    /// </summary>
    public class TwinsScanTelegraph : ModProjectile
    {
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        public ref float Timer => ref Projectile.localAI[2];
        public ref float AngleRange => ref Projectile.localAI[1];
        public ref float Direct => ref Projectile.localAI[0];
        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.scale = 1;
            Projectile.damage = 0;
            Projectile.aiStyle = -1;
            Projectile.alpha = 50;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 480;
            Projectile.hostile = true;
        }
        public override bool ShouldUpdatePosition() => false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => false;
        public override void AI()
        {
            NPC spaz = FargoSoulsUtil.NPCExists(Projectile.ai[0], NPCID.Spazmatism, NPCID.Retinazer);
            Projectile.Center = P_Retinazer.ShootPos(spaz);
            Direct = spaz.rotation + MathHelper.PiOver2;
            AngleRange = MathHelper.Lerp(Projectile.ai[2], 0, Timer / Projectile.ai[1]);
            Projectile.Opacity = 1/*MathHelper.Lerp(0, 1, Timer / Projectile.ai[1])*/;
            if (++Timer > Projectile.ai[1])
            {
                Projectile.Kill();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            var blackTile = TextureAssets.MagicPixel;
            var noise = FargosTextureRegistry.Techno1Noise;
            /*
            ManagedShader shader = ShaderManager.GetShader("FargosPhantasmMode.ScanTelegraphShader");
            Main.spriteBatch.GraphicsDevice.Textures[1] = noise.Value;
            shader.TrySetParameter("color", Color.Green);
            shader.TrySetParameter("screenPosition", Main.screenPosition);
            shader.TrySetParameter("screenSize", Main.ScreenSize);
            shader.TrySetParameter("Center", Projectile.Center);
            shader.TrySetParameter("R", 1000);
            shader.TrySetParameter("Direct", Direct);
            shader.TrySetParameter("AngleRange", AngleRange);
            shader.TrySetParameter("Opacticy", Projectile.Opacity);
            */
            Vector2 pos = Projectile.Center;
            float timeLerp = MathF.Pow(1 - Timer / Projectile.ai[2], 0.5f);
            float radius = 500 + 500 * timeLerp;
            float arcAngle = Direct;
            float arcWidth = AngleRange * timeLerp;
            Color color = Color.Green;
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.DestroyerScanTelegraph");
            shader.TrySetParameter("colorMult", 7.35f);
            shader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
            shader.TrySetParameter("radius", radius);
            shader.TrySetParameter("arcAngle", arcAngle.ToRotationVector2());
            shader.TrySetParameter("arcWidth", arcWidth);
            shader.TrySetParameter("anchorPoint", pos);
            shader.TrySetParameter("screenPosition", Main.screenPosition);
            shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
            shader.TrySetParameter("maxOpacity", 1);
            shader.TrySetParameter("color", color.ToVector4());

            Main.spriteBatch.GraphicsDevice.Textures[1] = noise.Value;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, shader.WrappedEffect, Main.GameViewMatrix.TransformationMatrix);
            Rectangle rekt = new(Main.screenWidth / 2, Main.screenHeight / 2, Main.screenWidth, Main.screenHeight);
            Main.spriteBatch.Draw(blackTile.Value, rekt, null, default, 0f, blackTile.Value.Size() * 0.5f, 0, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return true;
        }
    }
}
