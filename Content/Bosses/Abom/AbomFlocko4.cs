using FargowiltasSouls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Abom
{
    public class AbomFlocko4 : AbomFlocko
    {
        // 配置参数
        private const float DistanceFromBoss = 900f; // 与BOSS保持的距离
        private const float AttackStartTime = 60f;   // 开始攻击时间（帧）
        private const float AttackInterval = 6f;     // 攻击间隔（帧）
        private const float BaseAngle = MathHelper.PiOver2; // 基础角度（垂直向下）
        private const float AngleAmplitude = 12 * MathHelper.Pi / 180f; // 角度变化幅度
        private const float AngleFrequency = 2f * MathHelper.Pi / 120f; // 角度变化频率（周期180帧）
        private const float AttackAngleSpread = 75 * MathHelper.Pi / 180f; // 攻击角度散布

        public override string Texture => "Terraria/Images/NPC_352"; // 

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Abominable Frost Flock");
            Main.projFrames[Projectile.type] = Main.npcFrameCount[NPCID.IceQueen];
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 420;
            Projectile.alpha = 0;
            Projectile.penetrate = -1;
            Projectile.scale = 1.5f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            NPC npc = FargoSoulsUtil.NPCExists(Projectile.ai[0], ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>());
            if (npc == null)
            {
                Projectile.Kill();
                return;
            }
            Projectile.localAI[0]++; 

            float targetAngle = Projectile.ai[1] * MathHelper.Pi / 180; 
            
            Vector2 targetPosition = npc.Center + Vector2.UnitX.RotatedBy(targetAngle) * DistanceFromBoss;

            // 平滑移动到目标位置
            Vector2 direction = targetPosition - Projectile.Center;
            float length = direction.Length();

            if (length > 10f)
            {
                direction /= 8f;
                Projectile.velocity = (Projectile.velocity * 23f + direction) / 24f;
            }
            else
            {
                if (Projectile.velocity.Length() < 12f)
                    Projectile.velocity *= 0.95f;
            }
            if (Projectile.localAI[0] > AttackStartTime &&
                (Projectile.localAI[0] - AttackStartTime) % AttackInterval == 0)
            {
                SoundEngine.PlaySound(SoundID.Item27, Projectile.position);

                if (FargoSoulsUtil.HostCheck)
                {
                    float currentAngleVariation = AngleAmplitude *
                        (float)Math.Sin(Projectile.localAI[0] * AngleFrequency * Projectile.ai[2]);
                    float attackAngle = BaseAngle + currentAngleVariation;

                    for (int i = -1; i <= 1; i++)
                    {
                        float spreadAngle = attackAngle + i * AttackAngleSpread;
                        Vector2 velocity = Vector2.UnitX.RotatedBy(spreadAngle) * 12f;

                        // 发射AbomFrostWave
                        int proj = Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            Projectile.Center,
                            velocity,
                            ModContent.ProjectileType<AbomFrostWave>(), 
                            Projectile.damage,
                            Projectile.knockBack,
                            Projectile.owner
                        );

                        
                    }
                }
            }
            Projectile.rotation = Math.Min(MathHelper.PiOver2, Projectile.velocity.X / 16f);
            Projectile.frame = 0;
            SpawnParticles();
        }

        private void SpawnParticles()
        {
            if (Main.dedServ)
                return;
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.Ice,
                    Projectile.velocity.X * 0.2f,
                    Projectile.velocity.Y * 0.2f,
                    100, default, 1.5f
                );
                dust.noGravity = true;
                dust.velocity *= 0.5f;
            }

        }


        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture2D13.Height / Main.projFrames[Projectile.type];
            int frameY = frameHeight * Projectile.frame;
            Rectangle rectangle = new(0, frameY, texture2D13.Width, frameHeight);
            Vector2 origin = rectangle.Size() / 2f;

            SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // 绘制拖影
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = Color.Lerp(Color.Cyan, Color.White, 0.5f) * 0.5f;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float rotation = Projectile.oldRot[i];

                Main.EntitySpriteDraw(
                    texture2D13,
                    value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY),
                    rectangle,
                    color27,
                    rotation,
                    origin,
                    Projectile.scale,
                    effects,
                    0
                );
            }

            // 绘制主体
            Main.EntitySpriteDraw(
                texture2D13,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                rectangle,
                Projectile.GetAlpha(lightColor) ,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.Ice,
                    Main.rand.NextFloat(-5f, 5f),
                    Main.rand.NextFloat(-5f, 5f),
                    100, default, 1.5f
                );
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item27, Projectile.position);
        }
    }
}