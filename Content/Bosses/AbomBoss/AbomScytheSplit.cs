using FargowiltasSouls;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.AbomBoss
{
    public class AbomScytheSplit : ModProjectile
    {
        public override string Texture => "FargowiltasSouls/Content/Bosses/AbomBoss/AbomDeathScythe";

        private bool hasSpawnedPortal = false;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Abominationn Scythe");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            CooldownSlot = 1;

            Projectile.scale = 2f;
        }

        public override void AI()
        {
            /*if (Projectile.localAI[0] == 0)
            {
                Projectile.localAI[0] = 1;
                SoundEngine.PlaySound(SoundID.Item71, Projectile.Center);
            }*/
            CheckPlayerDistanceAndSpawnPortal();
            Projectile.rotation += 1f;

            if (--Projectile.ai[0] <= 0)
                Projectile.Kill();
        }

        private void CheckPlayerDistanceAndSpawnPortal()
        {
            // 如果已经生成过门户，则跳过
            if (hasSpawnedPortal || Projectile.ai[1] >= 0)
                return;

            // 查找最近的玩家
            Player nearestPlayer = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.Distance(Projectile.Center) < 1500f) // 先检查大范围
                {
                    float distance = Vector2.Distance(Projectile.Center, player.Center);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestPlayer = player;
                    }
                }
            }

            // 如果找到玩家且距离小于150
            if (nearestPlayer != null && nearestDistance < 150f)
            {
                // 尝试获取ShadowflamePortal的弹幕类型
                int portalType = ModContent.ProjectileType<Projectiles.Masomode.ShadowflamePortal>();

                if (portalType > 0)
                {
                    // 只在主机生成
                    if (FargoSoulsUtil.HostCheck)
                    {
                        for (int i = 0;i < 3; i++)
                        {
                            Vector2 direct = Vector2.UnitX.RotatedBy((i * 2 / 3 - 1 / 6) * MathHelper.Pi);
                            // 生成ShadowflamePortal
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center+150*direct,
                                Vector2.Zero, // 门户不移动
                                portalType,
                                Projectile.damage / 2, // 减少伤害
                                Projectile.knockBack,
                                Projectile.owner
                            );
                        }
                    }
                }
                
                // 标记已生成门户
                hasSpawnedPortal = true;

            }
        }
        public override void OnKill(int timeLeft)
        {
            /*
            int dustMax = Projectile.ai[1] >= 0 ? 50 : 25;
            float speed = Projectile.ai[1] >= 0 ? 15 : 6;
            for (int i = 0; i < dustMax; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleCrystalShard, Scale: 3.5f);
                Main.dust[d].velocity *= speed;
                Main.dust[d].noGravity = true;
            }
                        */
            int sparkMax = Projectile.ai[1] >= 0 ? 20 : 10;
            float speed = Projectile.ai[1] >= 0 ? 15 : 6;
            Color purpl = new(102f / 255f, 84f / 255f, 150f / 255f);
            for (int i = 0; i < sparkMax; i++)
            {
                Particle p = new SparkParticle(Projectile.Center, Main.rand.NextVector2CircularEdge(speed, speed) * Main.rand.NextFloat(0.4f, 0.7f), purpl, Main.rand.NextFloat(1f, 1.5f), 17, true, Color.White);
                p.Spawn();
            }
            EnhancedDeathEffect();
            if (Projectile.ai[1] >= 0)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    int p = Player.FindClosest(Projectile.Center, 0, 0);
                    if (p != -1)
                    {
                        Vector2 vel = Projectile.ai[1] == 0 ? Vector2.Normalize(Projectile.velocity) : Projectile.SafeDirectionTo(Main.player[p].Center);
                        vel *= 30f;
                        int max = Projectile.ai[1] == 0 ? 7 : WorldSavingSystem.MasochistModeReal ? 12 : 10;
                        for (int i = 0; i < max; i++)
                        {
                            Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile), Projectile.Center, vel.RotatedBy(MathHelper.TwoPi / max * i), ModContent.ProjectileType<AbomSickle3>(), Projectile.damage, Projectile.knockBack, Projectile.owner, p);
                        }
                    }
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
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<AbomFangBuff>(), 300);
                //target.AddBuff(ModContent.BuffType<Unstable>(), 240);
                //target.AddBuff(ModContent.BuffType<Buffs.Masomode.BerserkedBuff>(), 120);
            }
            target.AddBuff(BuffID.Bleeding, 600);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int num156 = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value.Height / Main.projFrames[Projectile.type]; //ypos of lower right corner of sprite to draw
            int y3 = num156 * Projectile.frame; //ypos of upper left corner of sprite to draw
            Rectangle rectangle = new(0, y3, texture2D13.Width, num156);
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = lightColor;
            color26 = Projectile.GetAlpha(color26);

            int max = ProjectileID.Sets.TrailCacheLength[Projectile.type];
            if (Projectile.velocity == Vector2.Zero)
                max /= 2;
            for (int i = 0; i < max; i++)
            {
                Color color27 = color26;
                color27 *= (float)(max - i) / max;
                Vector2 value4 = Projectile.oldPos[i];
                float num165 = Projectile.oldRot[i];
                Main.EntitySpriteDraw(texture2D13, value4 + Projectile.Size / 2f - Main.screenPosition + new Vector2(0, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), color27, num165, origin2, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(rectangle), Projectile.GetAlpha(lightColor), Projectile.rotation, origin2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 0) * Projectile.Opacity;
        }
    }
}