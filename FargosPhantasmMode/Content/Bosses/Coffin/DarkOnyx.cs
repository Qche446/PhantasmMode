using System.Collections.Generic;
using FargosPhantasmMode.Common.MetaBalls;
using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using Luminance.Core.Graphics;
using Terraria.GameContent;

namespace FargosPhantasmMode.Content.Bosses.Coffin
{
    public class DarkOnyx : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public override string Texture => "Terraria/Images/Projectile_" + 661;

        public override void SetStaticDefaults()
        {
            Main.projFrames[base.Projectile.type] = Main.projFrames[661];
            ProjectileID.Sets.TrailCacheLength[base.Projectile.type] = 20;
            ProjectileID.Sets.TrailingMode[base.Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            base.Projectile.CloneDefaults(661);
            base.Projectile.penetrate = -1;
            base.Projectile.friendly = false;
            base.Projectile.hostile = true;
            base.Projectile.timeLeft = 60;
            base.Projectile.tileCollide = true;
            base.Projectile.scale *= 1.3f;
            base.Projectile.width = 13;
            base.Projectile.height = 13;
        }

        public override void AI()
        {
            ref int alpha = ref base.Projectile.alpha;
            if (alpha <= 0)
            {
                for (int num117 = 0; num117 < 3; num117++)
                {
                    int num118 = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, DustID.Granite);
                    Main.dust[num118].noGravity = true;
                    Main.dust[num118].velocity *= 0.3f;
                    Main.dust[num118].noLight = true;
                }
            }
            if (alpha > 0)
            {
                alpha -= 55;
                base.Projectile.scale = 1.3f;
                if (alpha < 0)
                {
                    alpha = 0;
                    float num119 = 16f;
                    for (int i = 0; (float)i < num119; i++)
                    {
                        Vector2 spinningpoint7 = Vector2.UnitX * 0f;
                        spinningpoint7 += -Vector2.UnitY.RotatedBy((float)i * ((float)Math.PI * 2f / num119)) * new Vector2(1f, 4f);
                        spinningpoint7 = spinningpoint7.RotatedBy(base.Projectile.velocity.ToRotation());
                        int num121 = Dust.NewDust(base.Projectile.Center, 0, 0, DustID.PurpleTorch);
                        Main.dust[num121].scale = 1.5f;
                        Main.dust[num121].noLight = true;
                        Main.dust[num121].noGravity = true;
                        Main.dust[num121].position = base.Projectile.Center + spinningpoint7;
                        Main.dust[num121].velocity = Main.dust[num121].velocity * 4f + base.Projectile.velocity * 0.3f;
                    }
                }
            }
            if (Main.rand.NextBool(3))
            {
                Vector2 vector = Vector2.Normalize(base.Projectile.velocity.RotatedByRandom(0.6283185482025146));
                float num122 = Math.Max(4f, base.Projectile.velocity.Length() / 2f);
                CosmicFireMetaBall cosmicFireMetaBall = ModContent.GetInstance<CosmicFireMetaBall>();
                ((MetaballType)cosmicFireMetaBall).CreateParticle(base.Projectile.Center, num122 * vector, 10f, 0f, 0f, 0f, 0f);
            }
            base.Projectile.rotation = base.Projectile.velocity.ToRotation() + (float)Math.PI / 2f;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(in CursedCoffin.SoulShotSFX, base.Projectile.Center);
            if (FargoSoulsUtil.HostCheck)
            {
                int cap = 1;
                for (int i = -cap; i <= cap; i++)
                {
                    Vector2 vel = Vector2.UnitY.RotatedBy(1.3f * (float)i * ((float)Math.PI * 2f) * (0.041f + Main.rand.NextFloat(-0.02f, 0.01f))) * (6 + Math.Abs(i));
                    Projectile.NewProjectile(base.Projectile.GetSource_FromThis(), base.Projectile.Bottom + oldVelocity, vel, ModContent.ProjectileType<CoffinDarkSouls>(), FargoSoulsUtil.ScaledProjectileDamage(base.Projectile.damage, 0.75f, 2), 1f, Main.myPlayer, 0f, -0.135f);
                }
            }
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            base.Projectile.position = base.Projectile.Center;
            base.Projectile.width = (base.Projectile.height = 160);
            base.Projectile.Center = base.Projectile.position;
            base.Projectile.maxPenetrate = -1;
            base.Projectile.penetrate = -1;
            SoundEngine.PlaySound(in SoundID.Item14, base.Projectile.position);
            Vector2 vector27 = base.Projectile.Center + Vector2.One * -20f;
            int num157 = 40;
            int num158 = num157;
            for (int i = 0; i < 4; i++)
            {
                int num160 = Dust.NewDust(vector27, num157, num158, DustID.Granite, 0f, 0f, 100, default, 1.5f);
                Main.dust[num160].position = base.Projectile.Center + Vector2.UnitY.RotatedByRandom(3.1415927410125732) * (float)Main.rand.NextDouble() * num157 / 2f;
            }
            for (int j = 0; j < 20; j++)
            {
                int num162 = Dust.NewDust(vector27, num157, num158, DustID.PurpleTorch, 0f, 0f, 200, default, 3.7f);
                Main.dust[num162].position = base.Projectile.Center + Vector2.UnitY.RotatedByRandom(3.1415927410125732) * (float)Main.rand.NextDouble() * num157 / 2f;
                Main.dust[num162].noGravity = true;
                Main.dust[num162].noLight = true;
                Dust dust80 = Main.dust[num162];
                Dust dust81 = dust80;
                dust81.velocity *= 3f;
                dust80 = Main.dust[num162];
                dust81 = dust80;
                dust81.velocity += base.Projectile.DirectionTo(Main.dust[num162].position) * (2f + Main.rand.NextFloat() * 4f);
                num162 = Dust.NewDust(vector27, num157, num158, DustID.PurpleTorch, 0f, 0f, 100, default, 1.5f);
                Main.dust[num162].position = base.Projectile.Center + Vector2.UnitY.RotatedByRandom(3.1415927410125732) * (float)Main.rand.NextDouble() * num157 / 2f;
                dust80 = Main.dust[num162];
                dust81 = dust80;
                dust81.velocity *= 2f;
                Main.dust[num162].noGravity = true;
                Main.dust[num162].fadeIn = 1f;
                Main.dust[num162].color = Color.Crimson * 0.5f;
                Main.dust[num162].noLight = true;
                dust80 = Main.dust[num162];
                dust81 = dust80;
                dust81.velocity += base.Projectile.DirectionTo(Main.dust[num162].position) * 8f;
            }
            for (int k = 0; k < 20; k++)
            {
                int num164 = Dust.NewDust(vector27, num157, num158, DustID.PurpleTorch, 0f, 0f, 0, default, 2.7f);
                Main.dust[num164].position = base.Projectile.Center + Vector2.UnitX.RotatedByRandom(3.1415927410125732).RotatedBy(base.Projectile.velocity.ToRotation()) * num157 / 2f;
                Main.dust[num164].noGravity = true;
                Main.dust[num164].noLight = true;
                Dust dust82 = Main.dust[num164];
                Dust dust83 = dust82;
                dust83.velocity *= 3f;
                dust82 = Main.dust[num164];
                dust83 = dust82;
                dust83.velocity += base.Projectile.DirectionTo(Main.dust[num164].position) * 2f;
            }
            for (int l = 0; l < 70; l++)
            {
                int num166 = Dust.NewDust(vector27, num157, num158, DustID.Granite, 0f, 0f, 0, default, 1.5f);
                Main.dust[num166].position = base.Projectile.Center + Vector2.UnitX.RotatedByRandom(3.1415927410125732).RotatedBy(base.Projectile.velocity.ToRotation()) * num157 / 2f;
                Main.dust[num166].noGravity = true;
                Dust dust84 = Main.dust[num166];
                Dust dust85 = dust84;
                dust85.velocity *= 3f;
                dust84 = Main.dust[num166];
                dust85 = dust84;
                dust85.velocity += base.Projectile.DirectionTo(Main.dust[num166].position) * 3f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            int num = TextureAssets.Projectile[base.Projectile.type].Value.Height / Main.projFrames[base.Projectile.type];
            int y = num * base.Projectile.frame;
            Rectangle rectangle = new Rectangle(0, y, value.Width, num);
            Vector2 origin = rectangle.Size() / 2f;
            Vector2 vector = base.Projectile.rotation.ToRotationVector2() * (value.Width - base.Projectile.width) / 2f;
            vector = Vector2.Zero;
            SpriteEffects effects = ((base.Projectile.spriteDirection <= 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            float alphaMultiplier = 1f;
            Color drawColor = lightColor * alphaMultiplier;
            Main.EntitySpriteDraw(value, base.Projectile.Center + vector - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, base.Projectile.GetAlpha(drawColor), base.Projectile.rotation, origin, base.Projectile.scale, effects);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 128) * (1f - (float)base.Projectile.alpha / 255f);
        }

        public float WidthFunction(float completionRatio)
        {
            float baseWidth = base.Projectile.scale * (float)base.Projectile.width * 1.3f;
            return MathHelper.SmoothStep(baseWidth, 3.5f, completionRatio);
        }

        public Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(Color.Purple, Color.Transparent, completionRatio) * 0.6f;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.BlobTrail");
            FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedStreak.Value);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(WidthFunction, ColorFunction, _ => base.Projectile.Size * 0.5f, true, true, shader, null, null, false, null), 30);
        }
    }
}