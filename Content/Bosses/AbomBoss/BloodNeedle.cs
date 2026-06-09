using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Projectiles.Masomode
{
    public class BloodNeedle : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_756";

        // 添加字段来存储AbomBoss相关状态
        private bool? hasAbomBoss = null;
        private NPC abomBoss = null;
        private int abomBossPhase = 0;
        //后置减速模块
        private const float SlowdownDistance = 600f; // 减速区域半径
        private const float SlowSpeed = 6.5f;       // 减速后的速度
        private float originalSpeed;                // 原始速度（存储）
        private bool isSlowed = false;              // 是否处于减速状态
        private float slowdownTimer = 0f;           // 减速计时器（用于平滑过渡）
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Blood Thorn");
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 420;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.scale *= 0.7f;
        }

        public override bool? CanDamage()
        {
            if (Projectile.alpha > 100)
                return false;

            return base.CanDamage();
        }
        public override void OnSpawn(IEntitySource source)
        {
            // 保存初始速度
            originalSpeed = Projectile.velocity.Length();
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;

            float length = 200;
            length = (float)Math.Sqrt(2 * length * length);
            length *= 0.9f;

            float dummy = 0f;
            Vector2 offset = length / 2 * Projectile.scale * Projectile.rotation.ToRotationVector2();
            Vector2 end = Projectile.Center - offset;
            Vector2 tip = Projectile.Center + offset;

            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), end, tip, 6f * Projectile.scale, ref dummy))
                return true;

            return false;
        }

        private bool CheckForAbomBoss()
        {
            // 如果已经检查过，直接返回结果
            if (hasAbomBoss.HasValue)
                return hasAbomBoss.Value;

            // 查找AbomBoss
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>())
                {
                    abomBoss = Main.npc[i];
                    abomBossPhase = (int)abomBoss.localAI[3];
                    hasAbomBoss = true;
                    return true;
                }
            }

            hasAbomBoss = false;
            return false;
        }

        public override void AI()
        {
            // 检查是否有AbomBoss在场
            bool isAbomBossActive = CheckForAbomBoss();
            CheckPlayerDistanceAndSlowdown();

            // 如果有AbomBoss在场，应用特殊规则
            if (isAbomBossActive && abomBoss != null)
            {
                // 限制最大速度
                /*
                float maxSpeed = 36f;
                if (Projectile.velocity.Length() > maxSpeed)
                {
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * maxSpeed;
                }

                */
            }

            // 原始AI逻辑
            if (Projectile.localAI[0] == 0)
                Projectile.frame = Main.rand.Next(Main.projFrames[Projectile.type]);

            if (++Projectile.localAI[0] < 360f)
                Projectile.velocity *= 1.10f;

            Projectile.alpha -= 9;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (!Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                Lighting.AddLight(Projectile.Center, TorchID.Crimson);

                if (Projectile.alpha == 0)
                    Projectile.tileCollide = true;
            }

            // 如果是AbomBoss生成的弹幕，可以添加额外特效
            if (isAbomBossActive && Main.rand.NextBool(15))
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.GemTopaz, 0f, 0f, 0, default, 0.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.3f;
            }
            SpawnParticles();
        }
        private void CheckPlayerDistanceAndSlowdown()
        {
            bool wasSlowed = isSlowed;
            Player nearestPlayer = null;
            float nearestDistance = float.MaxValue;

            // 查找最近的玩家
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    float distance = (Projectile.Center - player.Center).Length();
                    
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestPlayer = player;
                    }
                }
            }

            // 检测是否在减速区域内
            bool shouldSlow = nearestPlayer != null && nearestDistance < SlowdownDistance;

            if (shouldSlow != isSlowed)
            {
                // 状态变化
                slowdownTimer = 0f;
                isSlowed = shouldSlow;

                
            }

            // 应用速度变化（平滑过渡）
            ApplySpeedChange();
        }
        private void ApplySpeedChange()
        {
            slowdownTimer += 0.05f; // 调整过渡速度
            if (slowdownTimer > 1f)
                slowdownTimer = 1f;

            float targetSpeed = isSlowed ? SlowSpeed : originalSpeed;
            float currentSpeed = Projectile.velocity.Length();

            // 平滑过渡速度
            float newSpeed = MathHelper.Lerp(currentSpeed, targetSpeed, slowdownTimer);

            if (newSpeed > 0)
            {
                // 保持方向不变，只改变速度大小
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Projectile.velocity = direction * newSpeed;
            }
        }
        private void SpawnParticles()
        {
            // 减速时生成更多粒子
            int particleRate = isSlowed ? 2 : 5;

            if (Main.rand.NextBool(particleRate))
            {
                int dustType = isSlowed ? DustID.Blood : DustID.LifeDrain;
                float dustScale = isSlowed ? 1.8f : 1.2f;
                Color dustColor = isSlowed ? new Color(255, 200, 200) : default;

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height) * 0.5f,
                    dustType,
                    -Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    0,
                    dustColor,
                    dustScale
                );
                dust.noGravity = true;
            }
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCDeath11, Projectile.Center);

            for (int i = 0; i < 30; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.LifeDrain);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity += Projectile.oldVelocity * Main.rand.NextFloat(0.5f);
                Main.dust[d].velocity *= 2f;
                Main.dust[d].scale += 2f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            SpriteEffects effects = SpriteEffects.None;

            Color color26 = lightColor;
            color26 = Projectile.GetAlpha(color26);

            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                Color color27 = color26 * 0.75f;
                color27 *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture2D13, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin2, Projectile.scale, effects, 0);
            }

            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color26, Projectile.rotation, origin2, Projectile.scale, effects, 0);
            return false;
        }
    }
}