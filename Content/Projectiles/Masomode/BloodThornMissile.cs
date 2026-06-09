using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Projectiles.Masomode
{
    public class BloodThornMissile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_756";

        // 添加字段来存储AbomBoss相关状态
        private bool? hasAbomBoss = null;
        private NPC abomBoss = null;
        private int abomBossPhase = 0;

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
            Projectile.timeLeft = 600;
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

            // 如果有AbomBoss在场，应用特殊规则
            if (isAbomBossActive && abomBoss != null)
            {
                // 根据AbomBoss的阶段调整参数
                if (abomBossPhase == 1) // 第一阶段
                {
                    // 限制最大速度为12
                    float maxSpeed = 16f;
                    if (Projectile.velocity.Length() > maxSpeed)
                    {
                        Projectile.velocity = Vector2.Normalize(Projectile.velocity) * maxSpeed;
                    }

                    // 设置生存时间（略大于计算值133帧，确保有足够时间到达）
                    if (Projectile.timeLeft > 300) // 5秒
                    {
                        Projectile.timeLeft = 300;
                    }
                }
                else if (abomBossPhase >= 2) // 第二阶段
                {
                    // 限制最大速度
                    float maxSpeed = 12f;
                    if (Projectile.velocity.Length() > maxSpeed)
                    {
                        Projectile.velocity = Vector2.Normalize(Projectile.velocity) * maxSpeed;
                    }

                    // 设置生存时间（略大于计算值171帧）
                    if (Projectile.timeLeft > 300) // 5秒
                    {
                        Projectile.timeLeft = 300;
                    }
                }

                // 碰撞检测：当弹幕完全显现且接近AbomBoss时消失
                if (Projectile.alpha == 0)
                {
                    float distance = Vector2.Distance(Projectile.Center, abomBoss.Center);
                    // 碰撞距离为弹幕和Boss半径之和的60%，使检测更宽松
                    float collisionDistance = (Projectile.width + abomBoss.width) * 0.5f * 0.6f;

                    if (distance < collisionDistance)
                    {
                        Projectile.Kill();
                        return;
                    }

                    // 可选：如果距离非常近但未碰撞，可以提前消失（避免穿模）
                    if (distance < 50f && Main.rand.NextBool(5))
                    {
                        int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                            DustID.Blood, 0f, 0f, 0, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].velocity *= 0.5f;
                    }
                }
            }

            // 原始AI逻辑
            if (Projectile.localAI[0] == 0)
                Projectile.frame = Main.rand.Next(Main.projFrames[Projectile.type]);

            if (++Projectile.localAI[0] < 90f)
                Projectile.velocity *= 1.05f;

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