using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls;

namespace FargosPhantasmMode.Content.Bosses.AbomBoss
{
    public class AbomScytheSpin : ModProjectile
    {
        public override string Texture => "FargowiltasSouls/Content/Bosses/AbomBoss/AbomDeathScythe";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Abominationn Scythe");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;
                SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);
            }

            if (Projectile.timeLeft == 390)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
            else if (Projectile.timeLeft == 360)
            {
                SoundEngine.PlaySound(SoundID.Item84, Projectile.Center);
            }
            else if (Projectile.timeLeft < 360)
            {
                NPC abom = FargoSoulsUtil.NPCExists(Projectile.ai[0], ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>());
                if (abom == null)
                {
                    Projectile.Kill();
                    return;
                }
                Vector2 pivot = abom.Center;
                Projectile.velocity = (pivot - Projectile.Center).RotatedBy(Math.PI / 2 * Projectile.ai[1]);
                Projectile.velocity *= 2 * (float)Math.PI / 360;

                // 添加淡蓝色粒子效果 - 只在旋转时生成
                if (Main.rand.NextBool(3)) // 33%概率每帧生成粒子
                {
                    // 创建淡蓝色粒子
                    Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2, Projectile.height / 2);
                    Vector2 dustVel = Projectile.velocity * 0.3f + Main.rand.NextVector2Circular(0.5f, 0.5f);

                    // 使用电尘，颜色为淡蓝色
                    int dust = Dust.NewDust(dustPos, 0, 0, DustID.Electric, dustVel.X, dustVel.Y, 100, Color.LightBlue, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.8f;
                    Main.dust[dust].fadeIn = 0.8f;
                }

                // 添加旋转时边缘的发光粒子
                if (Main.rand.NextBool(10)) // 10%概率每帧生成
                {
                    // 在镰刀边缘生成发光粒子
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (Projectile.width / 2);
                    Vector2 edgePos = Projectile.Center + offset;

                    // 使用霜尘，颜色为青色
                    int dust = Dust.NewDust(edgePos, 0, 0, DustID.Frost, 0, 0, 100, Color.Cyan, 0.8f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = offset * 0.03f + Projectile.velocity * 0.2f;
                }
                
                if (Projectile.timeLeft == 270 || Projectile.timeLeft == 90 || Projectile.timeLeft == 180)
                {
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int p = Player.FindClosest(Projectile.Center, 0, 0);
                        if (p != -1)
                        {
                            Vector2 direction = Main.player[p].Center - Projectile.Center;
                            direction.Normalize();

                            // 发射镰刀
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                direction,
                                ModContent.ProjectileType<AbomLightningTelegraph>(),
                                Projectile.damage,
                                0f,
                                Main.myPlayer
                            );
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                -direction,
                                ModContent.ProjectileType<AbomLightningTelegraph>(),
                                Projectile.damage,
                                0f,
                                Main.myPlayer
                            );
                        }
                    }
                    // 播放暂停音效
                    SoundEngine.PlaySound(SoundID.Item92, Projectile.Center);

                    Projectile.netUpdate = true;
                }
                
            }

            Projectile.spriteDirection = (int)Projectile.ai[1];
            Projectile.rotation += Projectile.spriteDirection * 0.5f;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);

            // 原有的灰尘效果
            for (int index1 = 0; index1 < 20; ++index1)
            {
                int index2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0.0f, 0.0f, 0, new Color(), 1f);
                Main.dust[index2].noGravity = true;
                Main.dust[index2].noLight = true;
                Main.dust[index2].scale++;
                Main.dust[index2].velocity *= 4f;
            }

            // 添加淡蓝色爆炸粒子效果
            for (int i = 0; i < 15; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(2f, 2f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Electric, speed.X, speed.Y, 100, Color.LightBlue, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = speed;
            }
            
            // 发射5道镰刀
            if (FargoSoulsUtil.HostCheck)
            {
                int p = Player.FindClosest(Projectile.Center, 0, 0);
                if (p != -1)
                {
                    Vector2 baseDirection = Main.player[p].Center - Projectile.Center;
                    baseDirection.Normalize();

                    // 发射5道镰刀，在指向玩家方向的两侧均匀分布，间隔40°
                    float[] angles = { -80f, -40f, 0f, 40f, 80f }; // 总共3道，间隔40°

                    foreach (float angle in angles)
                    {
                        Vector2 direction = baseDirection.RotatedBy(MathHelper.ToRadians(angle));

                        // 发射AbomLightningTelegraph - 生成闪电预警弹幕
                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            Projectile.Center,
                            direction,
                            ModContent.ProjectileType<AbomLightningTelegraph>(),
                            Projectile.damage,
                            0f,
                            Main.myPlayer
                        );
                    }
                }
            }
            
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<FargowiltasSouls.Content.Buffs.Boss.AbomFangBuff>(), 300);
            }
            target.AddBuff(BuffID.Bleeding, 600);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type];
            int y3 = num156 * Projectile.frame;
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = lightColor;
            color26 = Projectile.GetAlpha(color26);

            SpriteEffects spriteEffects = Projectile.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // 添加淡蓝色拖影效果
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Projectile.type]; i++)
            {
                // 保持原有的拖影颜色计算方式，但添加淡蓝色色调
                float progress = (float)i / ProjectileID.Sets.TrailCacheLength[Projectile.type];
                Color trailColor = color26;
                // 添加淡蓝色色调
                trailColor = Color.Lerp(trailColor, Color.LightBlue, 0.4f * (1f - progress));
                trailColor *= (float)(ProjectileID.Sets.TrailCacheLength[Projectile.type] - i) / ProjectileID.Sets.TrailCacheLength[Projectile.type];

                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture2D13, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY),
                    new Microsoft.Xna.Framework.Rectangle?(rectangle), trailColor, num165, origin2, Projectile.scale * (1f - progress * 0.3f), spriteEffects, 0);
            }

            // 绘制主弹幕 - 保持原色调
            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                new Microsoft.Xna.Framework.Rectangle?(rectangle), Projectile.GetAlpha(lightColor), Projectile.rotation, origin2, Projectile.scale, spriteEffects, 0);

            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // 保持原色调不变
            return Color.White;
        }
    }
}