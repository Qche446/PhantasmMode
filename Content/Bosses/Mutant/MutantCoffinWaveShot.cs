using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Mutant
{
    /// <summary>
    /// ai0决定偏角正弦内的相位·ai1 = whoami
    /// </summary>
    public class MutantCoffinWaveShot : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public override string Texture => "FargowiltasSouls/Content/Bosses/CursedCoffin/CoffinWaveShot";
        public Vector2 oldvel = Vector2.Zero;
        public bool Hided = false;
        public static readonly Color GlowColor = new (224, 196, 252, 0);
        private float shadow = 0;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.scale = 1f;
            Projectile.light = 1;
            Projectile.timeLeft = 280;
        }
        public override void AI()
        {
            NPC nPC = FargoSoulsUtil.NPCExists(Projectile.ai[2], ModContent.NPCType<MutantBoss>());
            
            if (nPC == null || nPC.ai[0] < 0)
            {
                Projectile.Kill();
                return;
            }
            if (nPC.localAI[0] == 1 && nPC.ai[0] == 28)
            {
                Hided = true;
                shadow = 1;
            }
            else
            {
                if (shadow > 0)
                {
                    shadow -= 0.04f;
                }
                Hided = false;
            }
            if (Projectile.localAI[0] == 0)
            {
                oldvel = Projectile.velocity;
            }
            if (Projectile.localAI[0] < 12)
            {
                Projectile.localAI[0]++;
                Projectile.scale = MathHelper.Lerp(0, 1, Projectile.localAI[0] / 12);
            }
            float rot = 0.7f * MathHelper.PiOver2 * MathF.Sin(MathF.Tau * (Projectile.ai[1] / 50f) + Projectile.ai[0]);
            Projectile.velocity = oldvel.RotatedBy(rot);
            
            
            Projectile.ai[1]++;
            /*
            if (Projectile.alpha < 200 && Hided)
            {
                Projectile.alpha = 200;
            }
            else if (Projectile.alpha > 0 && !Hided)
            {
                Projectile.alpha = 0;
            }
            */
        }
        public override bool CanHitPlayer(Player target)
        {
            if (shadow > 0.1f) 
                return false;
            return base.CanHitPlayer(target);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<ShadowflameBuff>(), 240);
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<MutantFangBuff>(), 180);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 600);
            }
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 40; i++)
            {
                int num = Dust.NewDust(base.Projectile.position, base.Projectile.width, base.Projectile.height, 173, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f));
                Main.dust[num].noGravity = true;
                Main.dust[num].velocity *= 3f;
            }
        }

        public float WidthFunction(float completionRatio)
        {
            return MathHelper.SmoothStep(base.Projectile.scale * (float)base.Projectile.width * 1.25f, 0f, completionRatio);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color value = Color.Lerp(Color.Lerp(Color.MediumPurple, Color.DeepPink, 0.5f), GlowColor, 0.5f);
            Color glowColor = GlowColor;
            glowColor.A = 100;
            float alphaMultiplier = 1 - 0.8f * shadow;
            return Color.Lerp(value, glowColor * 0.5f, completionRatio) * alphaMultiplier;
        }
        public override Color? GetAlpha(Color lightColor)
        {
            float alphaMultiplier = 1 - 0.5f * shadow;
            return lightColor * alphaMultiplier;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = TextureAssets.Projectile[base.Projectile.type].Value;
            int num = TextureAssets.Projectile[base.Projectile.type].Value.Height / Main.projFrames[base.Projectile.type];
            int y = num * base.Projectile.frame;
            Rectangle rectangle = new (0, y, value.Width, num);
            Vector2 origin = rectangle.Size() / 2f;
            Vector2 vector = base.Projectile.rotation.ToRotationVector2() * (value.Width - base.Projectile.width) / 2f;
            SpriteEffects effects = ((base.Projectile.spriteDirection <= 0) ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            
            float alphaMultiplier = 1 - 0.5f * shadow;
            Color drawColor = lightColor * alphaMultiplier;
            Main.EntitySpriteDraw(value, base.Projectile.Center + vector - Main.screenPosition + new Vector2(0f, base.Projectile.gfxOffY), rectangle, base.Projectile.GetAlpha(drawColor), base.Projectile.rotation, origin, base.Projectile.scale, effects);
            return false;
        }
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.BlobTrail");
            FargosTextureRegistry.FadedStreak.Value.SetTexture1();
            PrimitiveRenderer.RenderTrail(base.Projectile.oldPos, new PrimitiveSettings(WidthFunction, ColorFunction, (float _) => base.Projectile.Size * 0.5f, Smoothen: true, Pixelate: true, shader), 44);
        }
    }
}