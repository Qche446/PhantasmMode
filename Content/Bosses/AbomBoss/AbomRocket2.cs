using FargowiltasSouls.Content.Buffs;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.AbomBoss
{
    public class AbomRocket2 : ModProjectile
    {
        // 移植自 PillarNebulaBlaze 的追踪参数
        private int trackingTimer = 0;
        private const int MAX_TRACKING_TIME = 40; // 追踪135帧（45*3）
        private float turnSpeed = 0.1f; // 转向速度，对应 PillarNebulaBlaze 的 ai[0]

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Advanced Rocket");
            Main.projFrames[Projectile.type] = 3; // 保持3帧动画
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.alpha = 0;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            // 移植 PillarNebulaBlaze 的追踪逻辑，但不依赖星云柱
            trackingTimer++;
            bool shouldCreateParticles = Main.rand.NextBool(3); // 从每帧改为1/3概率

            // 如果有有效的追踪目标
            if (trackingTimer < MAX_TRACKING_TIME)
            {
                // 获取目标玩家
                int targetIndex = -1;

                // 如果 ai[0] 是有效的玩家索引，使用该玩家
                if (Projectile.ai[0] >= 0 && Projectile.ai[0] < Main.maxPlayers)
                {
                    targetIndex = (int)Projectile.ai[0];
                }
                else
                {
                    // 否则寻找最近玩家
                    targetIndex = Player.FindClosest(Projectile.Center, 0, 0);
                    if (targetIndex != -1)
                    {
                        Projectile.ai[0] = targetIndex; // 保存目标索引
                    }
                }

                // 如果找到有效目标，进行追踪
                if (targetIndex != -1 && Main.player[targetIndex].active && !Main.player[targetIndex].dead)
                {
                    // 移植 PillarNebulaBlaze 的角度插值追踪算法
                    float currentRotation = Projectile.velocity.ToRotation();
                    Vector2 directionToTarget = Main.player[targetIndex].Center - Projectile.Center;
                    float targetAngle = directionToTarget.ToRotation();

                    // 使用角度插值平滑转向目标
                    Projectile.velocity = new Vector2(Projectile.velocity.Length(), 0f)
                        .RotatedBy(currentRotation.AngleLerp(targetAngle, turnSpeed));
                }
            }

            // 移植 PillarNebulaBlaze 的基础AI参数和动画
            if (Projectile.ai[1] == 0.0)
            {
                Projectile.ai[1] = 1f;
                Projectile.localAI[0] = -Main.rand.Next(48);
                SoundEngine.PlaySound(SoundID.Item34, Projectile.position);
            }

            // 透明度处理（移植自 PillarNebulaBlaze）
            Projectile.alpha = Projectile.alpha - 40;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            // 动画帧更新（保持 AbomRocket 的3帧动画）
            Projectile.spriteDirection = Projectile.direction;
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 3) // 每3帧更新一次
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 3)
                    Projectile.frame = 0;
            }

            // 旋转（保持 AbomRocket 的旋转方式）
            Projectile.rotation = Projectile.velocity.ToRotation() + (float)Math.PI / 2;

            // 粒子效果（移植并保持 AbomRocket 的金色火焰粒子）
            // 移植 PillarNebulaBlaze 的粒子生成逻辑，但使用 AbomRocket 的金色火焰粒子
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == 48.0)
                Projectile.localAI[0] = 0.0f;
            else if (Projectile.alpha == 0)
            {
                if (Main.rand.NextBool(3))
                {
                    Vector2 vector2_2 = Vector2.UnitX * -30f;
                    Vector2 vector2_3 = -Vector2.UnitY.RotatedBy(Projectile.localAI[0] * 0.130899697542191 + 3.14159274101257, new Vector2())
                        * new Vector2(8f, 10f) - Projectile.rotation.ToRotationVector2() * 10f;

                    // 使用金色火焰粒子
                    int index2 = Dust.NewDust(Projectile.Center, 0, 0, DustID.GoldFlame, 0.0f, 0.0f, 160, new Color(), 1f);
                    Main.dust[index2].scale = 1.2f;
                    Main.dust[index2].noGravity = true;
                    Main.dust[index2].position = Projectile.Center + vector2_3 + Projectile.velocity * 2f;
                    Main.dust[index2].velocity = Vector2.Normalize(Projectile.Center + Projectile.velocity * 2f * 8f - Main.dust[index2].position)
                        * 2f + Projectile.velocity * 2f;
                }
            }

            

            // 保持 AbomRocket 的尾部火焰粒子
            Vector2 vector21 = Vector2.UnitY.RotatedBy(Projectile.rotation, new Vector2()) * 8f * 2;
            int index21 = Dust.NewDust(Projectile.Center, 0, 0, DustID.GoldFlame, 0.0f, 0.0f, 0, new Color(), 1f);
            Main.dust[index21].position = Projectile.Center + vector21;
            Main.dust[index21].scale = 1f;
            Main.dust[index21].noGravity = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // 保持 AbomRocket 的debuff效果
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<FargowiltasSouls.Content.Buffs.Boss.AbomFangBuff>(), 300);
                //target.AddBuff(ModContent.BuffType<Defenseless>(), 300);
                //target.AddBuff(BuffID.Confused, 180);
            }
            target.AddBuff(BuffID.BrokenArmor, 300);
            Projectile.timeLeft = 0; // 击中后立即爆炸
        }

        public override void OnKill(int timeLeft)
        {
            // 保持 AbomRocket 的爆炸效果
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 112;
            Projectile.position.X -= Projectile.width / 2;
            Projectile.position.Y -= Projectile.height / 2;

            // 烟雾粒子
            for (int index = 0; index < 2; ++index)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0.0f, 0.0f, 100, new Color(), 1.5f);

            // 金色火焰粒子
            for (int index1 = 0; index1 < 25; ++index1)
            {
                int index2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0.0f, 0.0f, 0, new Color(), 2.5f);
                Main.dust[index2].noGravity = true;
                Dust dust1 = Main.dust[index2];
                dust1.velocity *= 3f;
                int index3 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0.0f, 0.0f, 100, new Color(), 1.5f);
                Dust dust2 = Main.dust[index3];
                dust2.velocity *= 2f;
                Main.dust[index3].noGravity = true;
            }

            // Gore效果
            for (int index1 = 0; index1 < 1; ++index1)
            {
                int index2 = Gore.NewGore(Projectile.GetSource_FromThis(),
                    Projectile.position + new Vector2(Projectile.width * Main.rand.Next(100) / 100f,
                    Projectile.height * Main.rand.Next(100) / 100f) - Vector2.One * 10f,
                    new Vector2(), Main.rand.Next(61, 64), 1f);
                Gore gore = Main.gore[index2];
                gore.velocity *= 0.3f;
                Main.gore[index2].velocity.X += Main.rand.Next(-10, 11) * 0.05f;
                Main.gore[index2].velocity.Y += Main.rand.Next(-10, 11) * 0.05f;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // 保持 AbomRocket 的透明度处理
            return Color.White * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 保持 AbomRocket 的绘制代码
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type];
            int y3 = num156 * Projectile.frame;
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                new Microsoft.Xna.Framework.Rectangle?(rectangle), Projectile.GetAlpha(lightColor),
                Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}