using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu
{
    /// <summary>
    /// ai[0]ai[1]记录目标地点，ai[2]记录延时（40）localAI[0]计时,localAI[1]AI[2]记录初始速度
    /// </summary>
    public class MoonBolt : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.PhantasmalBolt;
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.PhantasmalBolt);
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 0;
            Projectile.scale *= 1.5f;
            Projectile.alpha = 100;
        }
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = Main.projFrames[ProjectileID.PhantasmalBolt];
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 30;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }
        public override void AI()
        {
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[1] = Projectile.velocity.X;
                Projectile.localAI[2] = Projectile.velocity.Y;
                if (Main.rand.NextBool(2))
                {
                    //SoundEngine.PlaySound(in SoundID.Item124, Projectile.position);
                }
                else
                {
                    //SoundEngine.PlaySound(in SoundID.Item125, Projectile.position);
                }
                Vector2 vector18 = Vector2.Normalize(Projectile.velocity);
                int num81 = Main.rand.Next(5, 10);
                for (int num82 = 0; num82 < num81; num82++)
                {
                    int num83 = Dust.NewDust(Projectile.Center, 0, 0, DustID.Vortex, 0f, 0f, 100);
                    Main.dust[num83].velocity.Y -= 1f;
                    Main.dust[num83].velocity += vector18 * 2f;
                    Main.dust[num83].position -= Vector2.One * 4f;
                    Main.dust[num83].noGravity = true;
                }
                Projectile.netUpdate = true;
            }
            
            if (++Projectile.localAI[0] <= Projectile.ai[2])
            {
                Vector2 targetPos = new (Projectile.ai[0], Projectile.ai[1]);
                float progress = Projectile.localAI[0] / Projectile.ai[2];
                float angleoffset = (targetPos - Projectile.Center).ToRotation() - Projectile.velocity.ToRotation();
                if (angleoffset > MathF.PI)
                    angleoffset -= 2 * MathF.PI;
                if (angleoffset < -MathF.PI)
                    angleoffset += 2 * MathF.PI;

                Projectile.velocity = 0.9f * Projectile.velocity.RotatedBy(MathHelper.SmoothStep(0, angleoffset, progress));
            }
            else if (Projectile.localAI[0] == Projectile.ai[2] + 1)
            {
                Vector2 oldvel = new (Projectile.localAI[1], Projectile.localAI[2]);
                float speed = oldvel.Length();
                Projectile.velocity = speed * Projectile.velocity.SafeNormalize(Vector2.Zero);
                Projectile.netUpdate = true;
            }
            else if (Projectile.localAI[0] > Projectile.ai[2] + 1 && Projectile.localAI[0] < Projectile.ai[2] + 51)
            {
                Projectile.velocity *= 1.04f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            float num110 = Projectile.velocity.Length();
            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= (byte)((double)num110 * 0.9);
            }
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            int num60 = Dust.NewDust(Projectile.Center, 0, 0, DustID.Vortex, 0f, 0f, 100);
            Main.dust[num60].noGravity = true;
            Main.dust[num60].velocity = 0.5f * Projectile.velocity + Main.rand.NextVector2Circular(2, 2);
            Main.dust[num60].position -= Vector2.One * 4f;
            Main.dust[num60].scale = 0.8f;
            if (++Projectile.frameCounter >= 9)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= 5)
                {
                    Projectile.frame = 0;
                }
            }
            DelegateMethods.v3_1 = new Vector3(1f, 0.6f, 0.2f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * 4f, 40f, DelegateMethods.CastLightOpen);
        }
        public override void SendExtraAI(BinaryWriter binaryWriter)
        {
            base.SendExtraAI(binaryWriter);
            binaryWriter.Write(Projectile.localAI[0]);
            binaryWriter.Write(Projectile.localAI[1]);
            binaryWriter.Write(Projectile.localAI[2]);
        }
        public override void ReceiveExtraAI(BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(binaryReader);
            Projectile.localAI[0] = binaryReader.ReadSingle();
            Projectile.localAI[1] = binaryReader.ReadSingle();
            Projectile.localAI[2] = binaryReader.ReadSingle();
        }
        public float WidthFunction(float completionRatio)
        {
            float baseWidth = Projectile.scale * Projectile.width * 1.3f;
            return MathHelper.SmoothStep(baseWidth, 3.5f, completionRatio);
        }

        public Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(Color.Teal, Color.Transparent, completionRatio) * 0.6f;
        }
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.BlobTrail");
            FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedStreak.Value);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(WidthFunction, ColorFunction, _ => Projectile.Size * 0.5f, Pixelate: true, Shader: shader), 30);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = TextureAssets.Projectile[Projectile.type].Value;
            int num = TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type];
            int y = num * Projectile.frame;
            Rectangle rectangle = new(0, y, value.Width, num);
            Vector2 origin = rectangle.Size() / 2f;
            Vector2 vector = Projectile.rotation.ToRotationVector2() * (value.Width - Projectile.width) / 2f;

            vector = Vector2.Zero;
            SpriteEffects effects = ((Projectile.spriteDirection <= 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);

            float alphaMultiplier = 1;
            Color drawColor = lightColor * alphaMultiplier;
            Main.EntitySpriteDraw(value, Projectile.Center + vector - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), rectangle, Projectile.GetAlpha(drawColor), Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 128) * (1f - (float)Projectile.alpha / 255f);
        }
        public override void OnKill(int timeLeft)
        {
            int num371 = Main.rand.Next(5, 10);
            for (int num372 = 0; num372 < num371; num372++)
            {
                int num373 = Dust.NewDust(Projectile.Center, 0, 0, DustID.Vortex, 0f, 0f, 100, default(Color), 0.5f);
                Dust dust148 = Main.dust[num373];
                Dust dust3 = dust148;
                dust3.velocity *= 1.6f;
                Main.dust[num373].velocity.Y -= 1f;
                dust148 = Main.dust[num373];
                dust3 = dust148;
                dust3.position -= Vector2.One * 4f;
                Main.dust[num373].position = Vector2.Lerp(Main.dust[num373].position, Projectile.Center, 0.5f);
                Main.dust[num373].noGravity = true;
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (WorldSavingSystem.MasochistModeReal)
            {
                target.AddBuff(ModContent.BuffType<ShadowflameBuff>(), 300);
                target.AddBuff(BuffID.Bleeding, 600);
                target.AddBuff(BuffID.Obstructed, 15);
            }

            target.AddBuff(ModContent.BuffType<BerserkedBuff>(), 120);
            target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 120);
        }
    }
}
