using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Abom
{
    public class AbomSaucer2 : ModNPC
    {
        public override string Texture => "FargowiltasSouls/Content/Bosses/AbomBoss/AbomSaucer";
        // 配置参数
        private const float OrbitDistance = 500f; // 绕圈距离
        private const float OrbitSpeed = 0.045f;  // 旋转速度（弧度/帧）
        private const int AttackStartTime = 60;   // 开始攻击时间（帧）
        private const int AttackInterval = 6;     // 攻击间隔（帧）
        private const int Lifetime = 360;         // 总生存时间（帧）

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Lightning Saucer");
            NPCID.Sets.TrailCacheLength[NPC.type] = 5;
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.CantTakeLunchMoney[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(NPC.type);

            NPC.AddDebuffImmunities(
            [
                BuffID.Confused,
                BuffID.Chilled,
                BuffID.OnFire,
                BuffID.Suffocation,
                ModContent.BuffType<LethargicBuff>(),
                ModContent.BuffType<ClippedWingsBuff>(),
                ModContent.BuffType<MutantNibbleBuff>(),
                ModContent.BuffType<OceanicMaulBuff>(),
                ModContent.BuffType<LightningRodBuff>(),
            ]);

            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                PortraitScale = 1f
            });
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(
                   ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>()],
                   quickUnlock: true
               );
            bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                new FlavorTextBestiaryInfoElement($"Mods.FargowiltasSouls.Bestiary.{Name}")
            ]);
        }

        public override void SetDefaults()
        {
            NPC.width = 25;
            NPC.height = 25;
            NPC.defense = 90;
            NPC.lifeMax = 600;
            NPC.scale = 2f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.aiStyle = -1;

            NPC.dontTakeDamage = true;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.damage = (int)(NPC.damage * 0.5f);
            NPC.lifeMax = (int)(NPC.lifeMax * 0.5f * balance);
        }

        public override bool CanHitPlayer(Player target, ref int CooldownSlot)
        {
            return false; // 保持不直接伤害玩家，通过弹幕攻击
        }

        public override void AI()
        {
            // 更新计时器
            NPC.ai[1]++; // ai[1]作为通用计时器

            // 检查是否超过生存时间
            if (NPC.ai[1] > Lifetime)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    NPC.life = 0;
                    NPC.HitEffect();
                    NPC.checkDead();
                    NPC.active = false;
                }
                return;
            }

            // 寻找目标玩家
            Player targetPlayer = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    float distance = Vector2.Distance(NPC.Center, player.Center);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        targetPlayer = player;
                        NPC.target = i; // 设置目标索引
                    }
                }
            }


            // 运动逻辑：围绕玩家旋转
            if (NPC.ai[1] < 30) // 前30帧平滑接近目标位置
            {
                Vector2 desiredPosition = targetPlayer.Center + Vector2.UnitX.RotatedBy(NPC.ai[2]) * OrbitDistance;
                Vector2 direction = desiredPosition - NPC.Center;

                direction /= 8f;
                NPC.velocity = (NPC.velocity * 19f + direction) / 20f;
            }
            else // 30帧后开始稳定旋转
            {
                // 更新旋转角度
                NPC.ai[2] -= OrbitSpeed; // 旋转方向
                if (NPC.ai[2] < (float)-Math.PI)
                    NPC.ai[2] += 2 * (float)Math.PI;
                else if (NPC.ai[2] > (float)Math.PI)
                    NPC.ai[2] -= 2 * (float)Math.PI;

                // 计算目标位置
                Vector2 targetPosition = targetPlayer.Center + Vector2.UnitX.RotatedBy(NPC.ai[2]) * OrbitDistance;
                Vector2 direction = targetPosition - NPC.Center;

                // 平滑移动
                direction /= 8f;
                NPC.velocity = (NPC.velocity * 19f + direction) / 20f;
            }

            // 攻击逻辑：60帧后开始，每6帧攻击一次
            NPC abom = FargoSoulsUtil.NPCExists(NPC.ai[0], ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>());
            if (NPC.ai[1] > AttackStartTime && (NPC.ai[1] - AttackStartTime) % AttackInterval == 0)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 toPlayer = targetPlayer.Center - NPC.Center;
                    if (toPlayer != Vector2.Zero)
                    {
                        toPlayer.Normalize();

                        // 发射闪电预警弹幕
                        Projectile.NewProjectile(
                            NPC.GetSource_FromThis(),
                            NPC.Center,
                            toPlayer,
                            ModContent.ProjectileType<AbomLightningTelegraph>(),
                            FargoSoulsUtil.ScaledProjectileDamage(abom.damage), 
                            0f,
                            Main.myPlayer
                        );
                        Projectile.NewProjectile(
                            NPC.GetSource_FromThis(),
                            NPC.Center,
                            toPlayer.RotatedBy(MathHelper.Pi / 3),
                            ModContent.ProjectileType<AbomLightningTelegraph>(),
                            FargoSoulsUtil.ScaledProjectileDamage(abom.damage),
                            0f,
                            Main.myPlayer
                            );
                        Projectile.NewProjectile(
                        NPC.GetSource_FromThis(),
                        NPC.Center,
                        toPlayer.RotatedBy(-MathHelper.Pi / 3),
                        ModContent.ProjectileType<AbomLightningTelegraph>(),
                        FargoSoulsUtil.ScaledProjectileDamage(abom.damage),
                        0f,
                        Main.myPlayer
                        );
                    }
                }
            }

            // 视觉特效：粒子系统
            SpawnParticles();

            // 摆动动画（保持与原版相同）
            if (NPC.localAI[1] == 0) // 初始化摆动方向
                NPC.localAI[1] = Main.rand.NextBool() ? 1 : -1;

            NPC.rotation = (float)Math.Sin(2 * Math.PI * NPC.localAI[0]++ / 90)
                * (float)Math.PI / 8f
                * NPC.localAI[1];

            if (NPC.localAI[0] > 180)
                NPC.localAI[0] = 0;

            
        }

        // 粒子生成方法
        private void SpawnParticles()
        {
            // 只在客户端生成粒子
            if (Main.dedServ)
                return;

            // 移动轨迹粒子（橙金色）
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(
                    NPC.position - new Vector2(2, 2),
                    NPC.width + 4, NPC.height + 4,
                    DustID.GoldFlame, 0f, 0f, 100, default, 1.5f
                );
                dust.noGravity = true;
                dust.velocity *= 0.5f;
                dust.velocity += NPC.velocity * 0.2f;
            }

            // 外围光晕粒子
            if (Main.rand.NextBool(5))
            {
                Vector2 offset = Main.rand.NextVector2Circular(NPC.width * 0.8f, NPC.height * 0.8f);
                Dust dust = Dust.NewDustDirect(
                    NPC.Center + offset - new Vector2(4, 4),
                    8, 8,
                    DustID.Torch, 0f, 0f, 100, default, 2f
                );
                dust.noGravity = true;
                dust.velocity = offset * 0.02f;
            }

            // 攻击时的额外粒子（攻击开始后）
            if (NPC.ai[1] > AttackStartTime && Main.rand.NextBool(10))
            {
                Vector2 direction = Main.player[NPC.target].Center - NPC.Center;
                if (direction != Vector2.Zero)
                {
                    direction.Normalize();
                    Dust dust = Dust.NewDustDirect(
                        NPC.Center - new Vector2(4, 4),
                        8, 8,
                        DustID.GemTopaz,
                        direction.X * 3f, direction.Y * 3f,
                        100, default, 2f
                    );
                    dust.noGravity = true;
                }
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 3; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemTopaz, 0f, 0f, 0, default, 1f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 3f;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 30; i++)
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GemTopaz, 0f, 0f, 0, default, 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 12f;
                }
            }
        }

        public override Color? GetAlpha(Color drawColor)
        {
            return Color.White;
        }

        public override bool CheckActive()
        {
            return false; // 不自然消失
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // 保持与原版相同的拖影绘制逻辑
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            Rectangle rectangle = NPC.frame;
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = drawColor;
            color26 = NPC.GetAlpha(color26);

            SpriteEffects effects = NPC.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // 绘制拖影
            for (int i = 0; i < NPCID.Sets.TrailCacheLength[NPC.type]; i++)
            {
                Color color27 = color26 * 0.5f;
                color27 *= (float)(NPCID.Sets.TrailCacheLength[NPC.type] - i) / NPCID.Sets.TrailCacheLength[NPC.type];
                Vector2 value4 = NPC.oldPos[i];
                float num165 = NPC.rotation;
                Main.EntitySpriteDraw(
                    texture2D13,
                    value4 + NPC.Size / 2f - screenPos + new Vector2(0, NPC.gfxOffY),
                    new Rectangle?(rectangle),
                    color27,
                    num165,
                    origin2,
                    NPC.scale,
                    effects,
                    0
                );
            }
            // 绘制主体
            Main.EntitySpriteDraw(
                texture2D13,
                NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY),
                new Rectangle?(rectangle),
                color26,
                NPC.rotation,
                origin2,
                NPC.scale,
                effects,
                0
            );

            return false;
        }
    }
}