using FargowiltasSouls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.Abom
{
    public class AbomSaucer3 : ModNPC
    {
        public override string Texture => "FargowiltasSouls/Content/Bosses/AbomBoss/AbomSaucer";

        // 配置参数
        private float FixedDistance = 1200f;     // 与AbomBoss的固定距离
        private const int AttackStartTime = 60;       // 开始攻击时间（帧）
        private const int AttackInterval = 3;         // 攻击间隔（帧）每3帧发射一个
        private const int Lifetime = 480;             // 总生存时间（帧）
        private const float AttackAngleAmplitude = MathHelper.PiOver2; // 90° = π/2 弧度
        private const float RotationSpeed = 0.015f;    // 顺时针绕圈速度

        // 存储初始角度和计算数据
        private float fixedAngle;                     // 相对于AbomBoss的固定角度
        private bool hasReachedPosition = false;      // 是否已到达目标位置

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Static Blood Saucer");
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
                ModContent.BuffType<FargowiltasSouls.Content.Buffs.Masomode.LethargicBuff>(),
                ModContent.BuffType<FargowiltasSouls.Content.Buffs.Masomode.ClippedWingsBuff>(),
                ModContent.BuffType<FargowiltasSouls.Content.Buffs.Masomode.MutantNibbleBuff>(),
                ModContent.BuffType<FargowiltasSouls.Content.Buffs.Masomode.OceanicMaulBuff>(),
                ModContent.BuffType<FargowiltasSouls.Content.Buffs.Masomode.LightningRodBuff>(),
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

            // 获取AbomBoss（通过ai[0]存储的whoAmI）
            NPC abomBoss = FargoSoulsUtil.NPCExists(NPC.ai[0], ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>());
            if (abomBoss == null)
            {
                // 如果AbomBoss不存在，自动销毁
                if (FargoSoulsUtil.HostCheck)
                {
                    NPC.life = 0;
                    NPC.HitEffect();
                    NPC.checkDead();
                    NPC.active = false;
                }
                return;
            }
            if (abomBoss.ai[0] == -2)
            {
                NPC.life = 0;
                NPC.HitEffect();
                NPC.checkDead();
                NPC.active = false;
            }

            // 初始化：从ai[2]获取固定角度
            if (NPC.ai[1] == 1)
            {
                fixedAngle = NPC.ai[2]; // ai[2]传入的固定角度
                hasReachedPosition = false;
            }
            // 更新角度：匀速顺时针绕圈
            fixedAngle -= RotationSpeed; // 顺时针旋转，角度递减

            // 计算目标位置
            Vector2 desiredPosition = CalculateDesiredPosition(abomBoss);

            if (!hasReachedPosition)
            {
                Vector2 direction = desiredPosition - NPC.Center;
                float distanceToTarget = direction.Length();
                // 如果距离很近，认为已到达
                if (distanceToTarget < 5f)
                {
                    hasReachedPosition = true;
                    NPC.Center = desiredPosition;
                    NPC.velocity = Vector2.Zero;
                }
                else
                {
                    float moveSpeed = 0.8f; // 提高移动速度
                    NPC.velocity = (NPC.velocity * 0.5f) + (direction * moveSpeed); // 减少惯性权重
                                                                                    // 如果距离较远，增加额外速度
                    if (distanceToTarget > 50f)
                    {
                        NPC.velocity += direction * 0.05f;
                    }
                }
            }
            else
            {
                // 已到达目标位置，直接计算期望位置并移动
                Vector2 direction = desiredPosition - NPC.Center;
                float distanceToTarget = direction.Length();
                float moveSpeed = distanceToTarget > 100f ? 1.0f : 0.4f;
                NPC.velocity = (NPC.velocity * 0.3f) + (direction * moveSpeed);

                // 如果AbomBoss移动很快，增加跟随力度
                if (abomBoss.velocity.Length() > 5f)
                {
                    NPC.velocity += abomBoss.velocity * 0.5f;
                }
            }

            if (NPC.ai[1] > AttackStartTime && (NPC.ai[1] - AttackStartTime) % AttackInterval == 0)
            {
                LaunchBloodNeedleAttack();
            }
            // 视觉特效：粒子系统
            SpawnParticles();
            // 轻微的摆动动画（比AbomSaucer2更轻微）
            if (NPC.localAI[1] == 0) // 初始化摆动方向
                NPC.localAI[1] = Main.rand.NextBool() ? 1 : -1;
            // 更轻微的旋转摆动
            NPC.rotation = (float)Math.Sin(2 * Math.PI * NPC.localAI[0]++ / 120)
                * (float)Math.PI / 16f
                * NPC.localAI[1];
            if (NPC.localAI[0] > 240)
                NPC.localAI[0] = 0;
        }

        /// <summary>
        /// 计算相对于AbomBoss的期望位置
        /// </summary>
        private Vector2 CalculateDesiredPosition(NPC abomBoss)
        {
            // 基本位置：固定角度和距离
            Vector2 basePosition = abomBoss.Center + Vector2.UnitX.RotatedBy(fixedAngle) * FixedDistance;

            // 添加微小的正弦波动，使飞碟有轻微的运动
            float waveOffset = (float)Math.Sin(NPC.ai[1] * 0.03f) * 10f;
            Vector2 waveVector = Vector2.UnitX.RotatedBy(fixedAngle + MathHelper.PiOver2) * waveOffset;

            return basePosition + waveVector;
        }

        /// <summary>
        /// 发射BloodNeedle弹幕
        /// </summary>
        private void LaunchBloodNeedleAttack()
        {
            if (!FargoSoulsUtil.HostCheck)
                return;
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
            Vector2 baseangle2 = Vector2.Normalize(targetPlayer.Center - NPC.Center);
            // 正弦波动：sin(π * 经过的帧数 / 30) * 90°
            float timeSinceAttackStart = NPC.ai[1] - AttackStartTime;
            float sinValue = (float)Math.Sin(MathHelper.Pi * timeSinceAttackStart / 36f);
            float angleOffset = sinValue * AttackAngleAmplitude; // 90° = π/2

            // 计算发射速度
            float speed = 24; 
            Vector2 velocity = baseangle2.RotatedBy(angleOffset ) * speed;

            
            NPC abom = FargoSoulsUtil.NPCExists(NPC.ai[0], ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>());

            // 生成弹幕
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                NPC.Center,
                velocity,
                ModContent.ProjectileType<Projectiles.Masomode.BloodNeedle>(),
                FargoSoulsUtil.ScaledProjectileDamage(abom.damage),
                0f,
                Main.myPlayer
            );



            // 攻击音效
            SoundEngine.PlaySound(SoundID.NPCDeath11, NPC.Center);

            // 攻击特效
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    NPC.Center,
                    DustID.Blood,
                    velocity.RotatedByRandom(MathHelper.ToRadians(15)) * 0.5f,
                    0,
                    default,
                    1.5f
                );
                dust.noGravity = true;
            }
        }

        /// <summary>
        /// 生成粒子效果
        /// </summary>
        private void SpawnParticles()
        {
            // 只在客户端生成粒子
            if (Main.dedServ)
                return;

            // 移动轨迹粒子（血红色）
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(
                    NPC.position - new Vector2(2, 2),
                    NPC.width + 4, NPC.height + 4,
                    DustID.Blood, 0f, 0f, 100, default, 1.5f
                );
                dust.noGravity = true;
                dust.velocity *= 0.3f;
                dust.velocity += NPC.velocity * 0.1f;
            }

            // 外围光晕粒子（深红色）
            if (Main.rand.NextBool(4))
            {
                Vector2 offset = Main.rand.NextVector2Circular(NPC.width * 0.6f, NPC.height * 0.6f);
                Dust dust = Dust.NewDustDirect(
                    NPC.Center + offset - new Vector2(4, 4),
                    8, 8,
                    DustID.LifeDrain, 0f, 0f, 100, new Color(180, 0, 0), 1.8f
                );
                dust.noGravity = true;
                dust.velocity = offset * 0.01f;
            }

            // 攻击时的额外粒子（攻击开始后）
            if (NPC.ai[1] > AttackStartTime && Main.rand.NextBool(8))
            {
                // 向攻击方向发射粒子
                float timeSinceAttackStart = NPC.ai[1] - AttackStartTime;
                float sinValue = (float)Math.Sin(MathHelper.Pi * timeSinceAttackStart / 30f);
                float angleOffset = sinValue * AttackAngleAmplitude;
                float finalAngle = MathHelper.PiOver2 + angleOffset;

                Vector2 direction = Vector2.UnitX.RotatedBy(finalAngle);

                Dust dust = Dust.NewDustDirect(
                    NPC.Center - new Vector2(4, 4),
                    8, 8,
                    DustID.Blood,
                    direction.X * 2f, direction.Y * 2f,
                    100, new Color(200, 0, 0), 2f
                );
                dust.noGravity = true;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 3; i++)
            {
                int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 3f;
            }

            if (NPC.life <= 0)
            {
                for (int i = 0; i < 30; i++)
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 0f, 0f, 0, default, 2.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 12f;
                }

                // 血爆效果
                SoundEngine.PlaySound(SoundID.NPCDeath1, NPC.Center);
            }
        }

        public override Color? GetAlpha(Color drawColor)
        {
            // 随时间淡入
            float alpha = MathHelper.Clamp(NPC.ai[1] / 60f, 0f, 1f);
            return Color.Lerp(drawColor * 0.5f, Color.White, alpha);
        }

        public override bool CheckActive()
        {
            return false; // 不自然消失
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Npc[NPC.type].Value;
            Rectangle rectangle = NPC.frame;
            Vector2 origin2 = rectangle.Size() / 2f;

            Color color26 = drawColor;
            color26 = NPC.GetAlpha(color26);

            // 攻击时颜色更红
            if (NPC.ai[1] > AttackStartTime)
            {
                float attackIntensity = MathHelper.Clamp((NPC.ai[1] - AttackStartTime) / 100f, 0f, 1f);
                color26 = Color.Lerp(color26, new Color(255, 150, 150, color26.A), attackIntensity * 0.3f);
            }

            SpriteEffects effects = NPC.spriteDirection > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // 绘制拖影
            for (int i = 0; i < NPCID.Sets.TrailCacheLength[NPC.type]; i++)
            {
                Color color27 = color26 * 0.3f;
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
                    NPC.scale * 0.9f,
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

            // 绘制攻击指示器（攻击时显示）
            /*
            if (NPC.ai[1] > AttackStartTime)
            {
                DrawAttackIndicator(spriteBatch, screenPos);
            }
            */
            return false;
        }

        /// <summary>
        /// 绘制攻击方向指示器
        /// </summary>
        private void DrawAttackIndicator(SpriteBatch spriteBatch, Vector2 screenPos)
        {
            // 计算当前攻击角度
            float timeSinceAttackStart = NPC.ai[1] - AttackStartTime;
            float sinValue = (float)Math.Sin(MathHelper.Pi * timeSinceAttackStart / 30f);
            float angleOffset = sinValue * AttackAngleAmplitude;
            float finalAngle = MathHelper.PiOver2 + angleOffset;

            // 绘制一条指示线
            Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            Vector2 startPos = NPC.Center;
            Vector2 endPos = startPos + Vector2.UnitX.RotatedBy(finalAngle) * 100f;

            Vector2 lineVector = endPos - startPos;
            float rotation = lineVector.ToRotation();
            float length = lineVector.Length();

            // 使用脉冲效果
            float pulse = (float)Math.Sin(timeSinceAttackStart * 0.2f) * 0.3f + 0.7f;
            Color indicatorColor = new Color(255, 50, 50, (int)(150 * pulse));

            Main.EntitySpriteDraw(
                pixel,
                startPos - screenPos,
                new Rectangle(0, 0, 1, 1),
                indicatorColor,
                rotation,
                new Vector2(0, 0.5f),
                new Vector2(length, 3f),
                SpriteEffects.None,
                0
            );

            // 绘制指示箭头
            Texture2D arrowTexture = Terraria.GameContent.TextureAssets.Extra[ExtrasID.Voronoi].Value; // 使用一个小箭头纹理
            if (arrowTexture != null)
            {
                Main.EntitySpriteDraw(
                    arrowTexture,
                    endPos - screenPos,
                    null,
                    indicatorColor,
                    rotation + MathHelper.PiOver2,
                    arrowTexture.Size() / 2f,
                    0.5f,
                    SpriteEffects.None,
                    0
                );
            }
        }
    }
}