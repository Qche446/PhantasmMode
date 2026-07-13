using FargowiltasSouls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Projectiles.Masomode
{
    public class ShadowflamePortal : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_673";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Shadowflame Portal");
        }

        public override void SetDefaults()
        {
            Projectile.width = 82;
            Projectile.height = 82;
            Projectile.aiStyle = -1;
            Projectile.alpha = 255;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.scale *= 0.8f;

            Projectile.timeLeft = 80;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.alpha -= 12;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.Shadowflame);
            Main.dust[d].noGravity = true;
            Main.dust[d].velocity *= 4f;
            Main.dust[d].scale += 0.5f;
            SpawnEnhancedParticles();
        }

        public override Color? GetAlpha(Color lightColor) => new Color(200, 150, 255, 150) * Projectile.Opacity;

        public override void OnKill(int timeLeft)
        {
            EnhancedDeathEffect();
            if (FargoSoulsUtil.HostCheck)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 speed = 5f * Projectile.ai[0].ToRotationVector2().RotatedByRandom(MathHelper.ToRadians(360));
                    float ai1 = Main.rand.Next(10, 80) * (1f / 1000f);
                    if (Main.rand.NextBool())
                        ai1 *= -1f;
                    float ai0 = Main.rand.Next(10, 80) * (1f / 1000f);
                    if (Main.rand.NextBool())
                        ai0 *= -1f;
                    Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, speed, ProjectileID.ShadowFlame, Projectile.damage, 0f, Main.myPlayer, ai0, ai1);
                }
            }
        }
        private void EnhancedDeathEffect()
        {
            // 创建爆炸粒子环
            for (int i = 0; i < 36; i++)
            {
                float angle = MathHelper.TwoPi * i / 36f;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(4f, 8f);

                int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.Shadowflame);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = velocity;
                Main.dust[d].scale = Main.rand.NextFloat(2f, 3f);
            }

            // 添加紫色水晶粒子
            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.PurpleCrystalShard);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = velocity;
                Main.dust[d].scale = Main.rand.NextFloat(1.5f, 2.5f);
            }

            
        }
        private void SpawnEnhancedParticles()
        {
            // 中心暗影火焰粒子
            int d = Dust.NewDust(Projectile.Center, 0, 0, DustID.Shadowflame);
            Main.dust[d].noGravity = true;
            Main.dust[d].velocity *= 6f; // 增加速度
            Main.dust[d].scale += 0.8f; // 增加大小

            // 添加额外的粒子类型
            if (Main.rand.NextBool(2))
            {
                int d2 = Dust.NewDust(Projectile.Center + Main.rand.NextVector2Circular(40, 40), 0, 0, DustID.PurpleTorch);
                Main.dust[d2].noGravity = true;
                Main.dust[d2].velocity = Main.rand.NextVector2Circular(2f, 2f);
                Main.dust[d2].scale = Main.rand.NextFloat(1.5f, 2.5f);
            }

            
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;
            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Projectile.GetAlpha(lightColor), Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}