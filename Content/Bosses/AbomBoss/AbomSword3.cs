using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Assets.Sounds;
using FargowiltasSouls.Common.Graphics.Particles;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.Projectiles.Deathrays;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.AbomBoss
{
    public class AbomSword3 : AbomSpecialDeathray
    {
        public AbomSword3() : base(70) { } // 总存在时间70帧（40帧旋转 + 30帧淡出）

        // 旋转参数
        private const int RotationFrames = 40;           // 旋转持续40帧
        private const int TotalDuration = 70;            // 总持续时间70帧（40帧旋转 + 30帧淡出）
        private const float TotalRotationAngle = 3.665f; // 210度 = 3.665弧度

        // 旋转角度数组
        private float[] rotationAngles; // 预计算的每帧旋转角度（加速-减速）

        // 状态变量
        private float initialRotation = 0f; // 初始旋转角度
        private bool isRotating = false;    // 是否正在旋转
        private bool[] hasSpawnedDeathrayMark = new bool[3]; // 三个时间点的标记生成状态

        // 死亡射线标记生成参数
        private int[] deathrayMarkFrames = new int[3]; // 在1/4、1/2、3/4角度时生成

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.FargoSouls().DeletionImmuneRank = 2;
            Projectile.extraUpdates = 1;
            Projectile.netImportant = true;

            // 初始化标记状态数组
            for (int i = 0; i < hasSpawnedDeathrayMark.Length; i++)
                hasSpawnedDeathrayMark[i] = false;

            // 预计算旋转角度
            InitializeRotationAngles();
        }

        private void InitializeRotationAngles()
        {
            // 初始化旋转角度数组
            rotationAngles = new float[RotationFrames];

            // 使用二次函数计算每帧的旋转角度：ω = a * t * (40 - t)
            // 其中t为当前帧数（0-40），a为系数使得总旋转角度为210°

            // 计算系数a使得总角度为210°
            // 积分∫[0,40] a*t*(40-t) dt = a * [20t² - t³/3]从0到40 = a * (20*1600 - 64000/3) = a * (32000 - 21333.33) = a * 10666.67
            // 设总角度 = 210° = 3.665弧度
            // 所以 a = 3.665 / 10666.67 ≈ 0.0003435

            float a = TotalRotationAngle / 10666.67f; // 近似值，实际计算需要精确

            // 更精确的计算：使用离散求和
            float sum = 0f;
            for (int t = 0; t < RotationFrames; t++)
            {
                // 使用t+0.5作为中点值更准确
                float t_mid = t + 0.5f;
                float omega = a * t_mid * (RotationFrames - t_mid);
                rotationAngles[t] = omega;
                sum += omega;
            }

            // 调整系数使总角度精确为210°
            float adjustment = TotalRotationAngle / sum;
            for (int t = 0; t < RotationFrames; t++)
            {
                rotationAngles[t] *= adjustment;
            }

            // 验证：第0帧和第40帧速度接近0
            // 第0帧速度：a * 0.5 * 39.5 ≈ 0
            // 第39帧速度：a * 39.5 * 0.5 ≈ 0

            // 计算累积角度以确定标记生成时机
            float totalAngle = 0f;
            deathrayMarkFrames[0] = -1; // 1/4角度
            deathrayMarkFrames[1] = -1; // 1/2角度
            deathrayMarkFrames[2] = -1; // 3/4角度

            for (int i = 0; i < RotationFrames; i++)
            {
                totalAngle += rotationAngles[i];

                // 检查是否到达1/4角度（52.5°）
                if (deathrayMarkFrames[0] == -1 && totalAngle >= TotalRotationAngle * 0.25f)
                    deathrayMarkFrames[0] = i;

                // 检查是否到达1/2角度（105°）
                if (deathrayMarkFrames[1] == -1 && totalAngle >= TotalRotationAngle * 0.5f)
                    deathrayMarkFrames[1] = i;

                // 检查是否到达3/4角度（157.5°）
                if (deathrayMarkFrames[2] == -1 && totalAngle >= TotalRotationAngle * 0.75f)
                    deathrayMarkFrames[2] = i;
            }

            // 调试输出
            // Main.NewText($"总旋转角度: {MathHelper.ToDegrees(TotalRotationAngle):F1}°");
            // Main.NewText($"标记生成帧: 1/4={deathrayMarkFrames[0]}, 1/2={deathrayMarkFrames[1]}, 3/4={deathrayMarkFrames[2]}");
        }

        public override void AI()
        {
            base.AI();

            // 基础设置
            Vector2? vector78 = null;
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
            {
                Projectile.velocity = -Vector2.UnitY;
            }

            // 跟踪AbomBoss
            NPC abom = FargoSoulsUtil.NPCExists(Projectile.ai[1], ModContent.NPCType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBoss>());
            if (abom == null)
            {
                Projectile.Kill();
                return;
            }
            else
            {
                Projectile.Center = abom.Center;
            }

            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
            {
                Projectile.velocity = -Vector2.UnitY;
            }

            // 初始化
            if (Projectile.localAI[0] == 0f)
            {
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(FargosSoundRegistry.StyxGazer with { Volume = 2.0f }, Projectile.Center);
                }
                // 记录初始旋转角度
                initialRotation = Projectile.velocity.ToRotation();
                isRotating = true; // 立即开始旋转
            }

            // 时间计数器
            Projectile.localAI[0] += 1f;

            // 总时间70帧后消失
            if (Projectile.localAI[0] >= TotalDuration)
            {
                Projectile.Kill();
                return;
            }

            // ========== 阶段1：加速-减速旋转阶段 (0-39帧) ==========
            if (Projectile.localAI[0] < RotationFrames)
            {
                // 当前帧在旋转阶段的索引（0-39）
                int currentRotationFrame = (int)Projectile.localAI[0];

                // 计算当前帧的旋转角度（使用预计算的二次函数角度）
                // 旋转方向由Projectile.ai[0]的符号决定
                float rotationDirection = Math.Sign(Projectile.ai[0]);

                // 累积总旋转角度
                float totalRotation = 0f;
                for (int i = 0; i <= currentRotationFrame; i++)
                {
                    totalRotation += rotationAngles[i];
                }

                float currentRotation = initialRotation + rotationDirection * totalRotation;

                Projectile.rotation = currentRotation - 1.57079637f;
                Projectile.velocity = currentRotation.ToRotationVector2();

                // 根据旋转速度调整缩放和视觉效果强度
                float currentFrameRotation = rotationAngles[currentRotationFrame];
                float maxSpeed = 0f;
                for (int i = 0; i < RotationFrames; i++)
                {
                    if (rotationAngles[i] > maxSpeed)
                        maxSpeed = rotationAngles[i];
                }
                float rotationSpeedFactor = currentFrameRotation / maxSpeed;

                // 缩放效果：根据速度调整，最大1.2倍
                float baseScale = 1f;
                Projectile.scale = baseScale + MathHelper.Lerp(0f, 0.2f, rotationSpeedFactor);

                // ========== 每帧发射追踪镰刀 ==========
                if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 spawnPos = Projectile.Center;

                    // 镰刀数量：根据速度调整，速度越快发射越多
                    int minSickles = 14;
                    int maxSickles = 15;
                    int sickleCount = (int)MathHelper.Lerp(minSickles, maxSickles, rotationSpeedFactor);

                    for (int i = 1; i <= sickleCount; i++)
                    {
                        Vector2 spawnOffset = Projectile.velocity * 3000f / sickleCount * i;
                        int targetPlayer = Player.FindClosest(spawnPos + spawnOffset, 0, 0);

                        if (targetPlayer != -1)
                        {
                            Vector2 directionToPlayer = Main.player[targetPlayer].Center - (spawnPos + spawnOffset);
                            directionToPlayer.Normalize();

                            // 速度：根据速度调整
                            float speed = MathHelper.Lerp(4f, 9f, rotationSpeedFactor);

                            Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile),
                                spawnPos + spawnOffset, Projectile.velocity.RotatedBy(MathHelper.PiOver2 * rotationDirection),
                                ModContent.ProjectileType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomSickle>(),
                                Projectile.damage, // 正常伤害
                                0f, Projectile.owner);
                        }
                    }
                }

                // ========== 在三个时间点生成死亡射线标记 ==========
                for (int markIndex = 0; markIndex < 3; markIndex++)
                {
                    if (!hasSpawnedDeathrayMark[markIndex] && currentRotationFrame == deathrayMarkFrames[markIndex])
                    {
                        SpawnDeathrayMarks(currentRotation, rotationDirection, markIndex, rotationSpeedFactor);
                        hasSpawnedDeathrayMark[markIndex] = true;
                    }
                }

                // ========== 旋转视觉效果（增强版） ==========
                // 粒子数量根据速度调整
                int sparkCount = (int)MathHelper.Lerp(8, 20, rotationSpeedFactor);
                int trailCount = (int)MathHelper.Lerp(3, 8, rotationSpeedFactor);

                // 主粒子效果
                for (int i = 0; i < sparkCount; i++)
                {
                    if (Main.rand.NextBool(Math.Max(1, 8 - (int)(rotationSpeedFactor * 4))))
                    {
                        float lerper = i + Main.rand.NextFloat(-0.5f, 0.5f);
                        Vector2 spawnPos = Projectile.Center + lerper * Projectile.velocity * 3000f / sparkCount;

                        float particleSpeed = MathHelper.Lerp(3f, 12f, rotationSpeedFactor);
                        Vector2 vel = Projectile.velocity.RotatedBy(Math.PI / 2 * rotationDirection * -1);
                        vel *= particleSpeed;
                        vel = vel.RotatedByRandom(MathHelper.PiOver2 * 0.3f);

                        float particleSize = MathHelper.Lerp(0.15f, 0.5f, rotationSpeedFactor);
                        int particleLife = (int)MathHelper.Lerp(20, 40, rotationSpeedFactor);

                        Particle p = new RectangleParticle(spawnPos, vel, Color.OrangeRed,
                            particleSize, particleLife, true, true, Color.Yellow);
                        p.Spawn();
                    }
                }

                // 残影效果
                for (int i = 0; i < trailCount; i++)
                {
                    if (Main.rand.NextBool(3))
                    {
                        Vector2 trailPos = Projectile.Center +
                                          Projectile.velocity * Main.rand.NextFloat(100f, 2000f);
                        Vector2 trailVel = Projectile.velocity.RotatedBy(Math.PI / 2) *
                                          Main.rand.NextFloat(-2f, 2f);

                        int d = Dust.NewDust(trailPos, 0, 0, DustID.GemTopaz,
                            trailVel.X, trailVel.Y, 0, default, 2f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].scale *= 1.5f;
                        Main.dust[d].velocity *= 0.8f;
                    }
                }

                // 烟雾效果（高速时更明显）
                if (Main.rand.NextBool(Math.Max(1, 15 - (int)(rotationSpeedFactor * 8))))
                {
                    Vector2 smokePos = Projectile.Center +
                                      Projectile.velocity * Main.rand.NextFloat(300f, 1800f);
                    Vector2 smokeVel = Projectile.velocity.RotatedBy(Math.PI / 2 * rotationDirection) *
                                      MathHelper.Lerp(0.8f, 3f, rotationSpeedFactor);

                    int index = Gore.NewGore(Projectile.GetSource_FromThis(), smokePos, smokeVel,
                        Main.rand.Next(61, 64), 1f);
                    Main.gore[index].scale *= MathHelper.Lerp(0.2f, 0.5f, rotationSpeedFactor);
                    Main.gore[index].rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                }

                // 高速旋转时的特殊效果
                if (rotationSpeedFactor > 0.7f && Main.rand.NextBool(5))
                {
                    Vector2 shockwavePos = Projectile.Center +
                                          Projectile.velocity * Main.rand.NextFloat(500f, 1500f);
                    Vector2 shockwaveVel = Vector2.Normalize(Projectile.velocity.RotatedBy(Math.PI / 2)) * 15f;

                    for (int i = 0; i < 3; i++)
                    {
                        int d = Dust.NewDust(shockwavePos, 0, 0, DustID.Torch,
                            shockwaveVel.X, shockwaveVel.Y, 0, default, 3f);
                        Main.dust[d].noGravity = true;
                    }
                }
            }
            // ========== 阶段2：旋转后淡出阶段 (40-69帧) ==========
            else
            {
                // 保持最后的角度
                if (isRotating)
                {
                    isRotating = false;
                    // 计算最终旋转角度（旋转40帧后的角度）
                    float rotationDirection = Math.Sign(Projectile.ai[0]);
                    float finalTotalRotation = 0f;
                    for (int i = 0; i < RotationFrames; i++)
                    {
                        finalTotalRotation += rotationAngles[i];
                    }
                    float finalRotation = initialRotation + rotationDirection * finalTotalRotation;

                    Projectile.rotation = finalRotation - 1.57079637f;
                    Projectile.velocity = finalRotation.ToRotationVector2();
                }

                // 缩放效果：淡出
                float fadeProgress = (Projectile.localAI[0] - RotationFrames) / (TotalDuration - RotationFrames);
                Projectile.scale = MathHelper.Lerp(1f, 0f, fadeProgress);

                // 淡出时的粒子效果
                if (Main.rand.NextBool(3))
                {
                    Vector2 fadePos = Projectile.Center +
                                     Projectile.velocity * Main.rand.NextFloat(0f, 2000f);
                    Vector2 fadeVel = Main.rand.NextVector2Circular(2f, 2f);

                    int d = Dust.NewDust(fadePos, 0, 0, DustID.GemTopaz,
                        fadeVel.X, fadeVel.Y, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].scale *= 0.8f;
                }
            }

            // 碰撞检测
            float num805 = 3f;
            float num806 = Projectile.width;
            Vector2 samplingPoint = Projectile.Center;
            if (vector78.HasValue)
                samplingPoint = vector78.Value;

            float[] array3 = new float[(int)num805];
            for (int i = 0; i < array3.Length; i++)
                array3[i] = 3000f;

            float num807 = 0f;
            for (int num808 = 0; num808 < array3.Length; num808++)
                num807 += array3[num808];

            num807 /= num805;
            float amount = 0.5f;
            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], num807, amount);
        }

        private void SpawnDeathrayMarks(float currentRotation, float rotationDirection, int markIndex, float speedFactor)
        {
            if (!FargoSoulsUtil.HostCheck) return;

            // 根据速度因子调整死亡射线数量
            int deathrayCount = (int)MathHelper.Lerp(8, 15, speedFactor);
            const float baseSpacing = 200f;
            float spacing = baseSpacing * MathHelper.Lerp(1f, 0.7f, speedFactor); // 速度越快，间隔越密

            // 计算剑身起点（剑柄位置）
            float hiltOffset = 0f;
            Vector2 swordBase = Projectile.Center - Projectile.velocity * hiltOffset;

            // 计算垂直方向
            Vector2 perpendicularDirection = Projectile.velocity.RotatedBy(MathHelper.PiOver2 * rotationDirection);

            // 根据标记索引和速度因子调整音效
            float volume = 1.3f + markIndex * 0.3f + speedFactor * 0.2f;
            float pitch = -0.05f + markIndex * 0.15f + speedFactor * 0.1f;

            // 沿剑身方向生成标记
            for (int i = 0; i < deathrayCount; i++)
            {
                float distanceFromBase = i * spacing;
                Vector2 rayPos = swordBase + Projectile.velocity * distanceFromBase;

                if (distanceFromBase > 3000f) break;

                Vector2 rayDirection = perpendicularDirection;

                // 根据速度因子调整死亡射线持续时间
                int duration = (int)MathHelper.Lerp(90, 120, speedFactor);

                Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile),
                    rayPos, rayDirection,
                    ModContent.ProjectileType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomDeathrayMark>(),
                    (int)(Projectile.damage * MathHelper.Lerp(1.2f, 1.5f, speedFactor)), // 伤害随速度增加
                    0f, Projectile.owner, duration);
            }

            // 音效
            SoundEngine.PlaySound(SoundID.Item12 with
            {
                Volume = volume,
                Pitch = pitch
            }, Projectile.Center);

            // 根据速度因子调整视觉效果强度
            int particleCount = (int)MathHelper.Lerp(20, 40, speedFactor);
            float particleScale = MathHelper.Lerp(1.8f, 2.5f, speedFactor);

            for (int i = 0; i < particleCount; i++)
            {
                float lerpFactor = Main.rand.NextFloat();
                float distanceFromBase = MathHelper.Lerp(0f, deathrayCount * spacing, lerpFactor);
                Vector2 particlePos = swordBase + Projectile.velocity * distanceFromBase;

                Vector2 particleVel = perpendicularDirection * Main.rand.NextFloat(-4f, 4f) * (1f + speedFactor);
                particleVel += Projectile.velocity * Main.rand.NextFloat(-3f, 3f);

                Particle p = new RectangleParticle(particlePos, particleVel, Color.OrangeRed,
                    Main.rand.NextFloat(0.2f, 0.5f) * (1f + speedFactor * 0.5f),
                    (int)MathHelper.Lerp(20, 35, speedFactor), true, true, Color.Yellow);
                p.Spawn();

                // 高速旋转时的额外效果
                if (speedFactor > 0.6f && i % 2 == 0)
                {
                    Vector2 shockVel = perpendicularDirection * Main.rand.NextFloat(6f, 12f);
                    int d = Dust.NewDust(particlePos, 0, 0, DustID.Torch,
                        shockVel.X, shockVel.Y, 0, default, particleScale);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 1.2f;
                }
            }

            // 标记生成时的冲击波效果
            if (speedFactor > 0.5f)
            {
                Vector2 shockwaveCenter = swordBase + Projectile.velocity * (deathrayCount * spacing / 2f);

                for (int i = 0; i < 8; i++)
                {
                    Vector2 shockVel = Vector2.UnitX.RotatedBy(MathHelper.TwoPi / 8 * i) * 10f;
                    int d = Dust.NewDust(shockwaveCenter, 0, 0, DustID.GemTopaz,
                        shockVel.X, shockVel.Y, 0, default, 2f);
                    Main.dust[d].noGravity = true;
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // 击退效果：根据命中时的旋转速度调整
            float knockbackFactor = 1f;
            if (Projectile.localAI[0] < RotationFrames)
            {
                int currentRotationFrame = (int)Projectile.localAI[0];
                float currentFrameRotation = rotationAngles[Math.Min(currentRotationFrame, RotationFrames - 1)];
                float maxSpeed = 0f;
                for (int i = 0; i < RotationFrames; i++)
                {
                    if (rotationAngles[i] > maxSpeed)
                        maxSpeed = rotationAngles[i];
                }
                float speedFactor = currentFrameRotation / maxSpeed;
                knockbackFactor = MathHelper.Lerp(1f, 1.5f, speedFactor);
            }

            target.velocity.X = target.Center.X < Main.npc[(int)Projectile.ai[1]].Center.X ?
                -18f * knockbackFactor : 18f * knockbackFactor;
            target.velocity.Y = -12f * knockbackFactor;

            // 生成爆炸效果
            Projectile.NewProjectile(Terraria.Entity.InheritSource(Projectile),
                target.Center + Main.rand.NextVector2Circular(100, 100),
                Vector2.Zero, ModContent.ProjectileType<FargowiltasSouls.Content.Bosses.AbomBoss.AbomBlast>(),
                0, 0f, Projectile.owner);

            // Debuff
            target.AddBuff(BuffID.Bleeding, 300);
            if (WorldSavingSystem.EternityMode)
            {
                target.AddBuff(ModContent.BuffType<AbomFangBuff>(), 300);
            }
        }

        public float WidthFunction(float _) => Projectile.width * Projectile.scale * 2;

        // 颜色定义
        public static Color FromDecimal(double r, double g, double b, double a) =>
            new((int)(r * 255), (int)(g * 255), (int)(b * 255), (int)(a * 255));
        public static readonly Color darkColor = FromDecimal(0.85, 0.20, 0.05, 1);
        public static readonly Color midColor = FromDecimal(1.0, 0.40, 0.10, 1);
        public static readonly Color lightColor = FromDecimal(1.0, 0.95, 0.70, 1);

        public static Color ColorFunction(float _) => darkColor;

        public override bool PreDraw(ref Color lightColor)
        {
            // 根据旋转速度调整绘制效果
            float rotationSpeedFactor = 1f;
            if (Projectile.localAI[0] < RotationFrames)
            {
                int currentRotationFrame = (int)Projectile.localAI[0];
                float currentFrameRotation = rotationAngles[Math.Min(currentRotationFrame, RotationFrames - 1)];
                float maxSpeed = 0f;
                for (int i = 0; i < RotationFrames; i++)
                {
                    if (rotationAngles[i] > maxSpeed)
                        maxSpeed = rotationAngles[i];
                }
                rotationSpeedFactor = currentFrameRotation / maxSpeed;
            }

            DrawAccelDecelSwordDeathray(Projectile, drawDistance, WidthFunction, rotationSpeedFactor);
            return false;
        }

        public static void DrawAccelDecelSwordDeathray(Projectile projectile, float drawDistance,
            PrimitiveSettings.VertexWidthFunction widthFunction, float speedFactor = 1f)
        {
            if (projectile.velocity == Vector2.Zero)
                return;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive,
                SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);

            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.StyxGazerShader");
            Texture2D hiltTexture = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Bosses/AbomBoss/AbomSword").Value;

            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 offset = direction * projectile.scale * hiltTexture.Height;

            // 剑身位置
            Vector2 laserStartOffset = direction * -176 * projectile.scale;
            Vector2 laserStart = projectile.Center + offset * 2 + laserStartOffset;
            Vector2 laserEnd = laserStart + direction * drawDistance;

            // 创建绘制点
            Vector2[] baseDrawPoints = new Vector2[12]; // 增加点数使曲线更平滑
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(laserStart, laserEnd, i / (float)(baseDrawPoints.Length - 1f));

            // 设置着色器参数
            Color brightColor = Color.Lerp(midColor, Color.White, MathHelper.Clamp(speedFactor - 0.5f, 0f, 0.3f));
            shader.TrySetParameter("mainColor", brightColor);
            shader.TrySetParameter("fadeStart", false);

            // 使用熔岩纹理
            Texture2D fademap = FargosTextureRegistry.MagmaStreak.Value;
            FargoSoulsUtil.SetTexture1(fademap);

            // 根据速度因子调整绘制次数和残影效果
            int trailPasses = (int)MathHelper.Lerp(3, 5, MathHelper.Clamp(speedFactor, 0f, 1f));
            float trailWidthMultiplier = MathHelper.Lerp(1f, 1.5f, speedFactor);

            for (int j = 0; j < trailPasses; j++)
            {
                PrimitiveSettings primSettings = new(
                    t => widthFunction(t) * trailWidthMultiplier * (1f - j * 0.15f),
                    t => ColorFunction(t) * (1f - j * 0.2f),
                    Shader: shader);
                PrimitiveRenderer.RenderTrail(baseDrawPoints, primSettings, 35);
            }

            // 绘制剑柄
            Main.spriteBatch.UseBlendState(BlendState.Additive);

            // 残影效果：速度越快，残影越多越明显
            int afterimageCount = (int)MathHelper.Lerp(6, 12, MathHelper.Clamp(speedFactor, 0f, 1f));
            float afterimageSpread = MathHelper.Lerp(3f, 8f, MathHelper.Clamp(speedFactor, 0f, 1f));
            float afterimageAlphaBase = MathHelper.Lerp(0.3f, 0.6f, MathHelper.Clamp(speedFactor, 0f, 1f));

            for (int j = 0; j < afterimageCount; j++)
            {
                // 残影位置偏移：沿旋转方向分布
                float angleOffset = MathHelper.TwoPi * j / afterimageCount;
                Vector2 afterimageOffset = angleOffset.ToRotationVector2() * afterimageSpread;

                // 残影透明度：根据距离递减
                float afterimageAlpha = afterimageAlphaBase * (1f - (float)j / afterimageCount * 0.7f);
                Color glowColor = Color.Lerp(darkColor, lightColor, j * 0.1f) * afterimageAlpha;

                Main.EntitySpriteDraw(hiltTexture, projectile.Center + offset + afterimageOffset -
                    Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                    null, glowColor, direction.ToRotation() + MathHelper.PiOver2,
                    Vector2.UnitX * hiltTexture.Width / 2, projectile.scale * (1f - j * 0.05f),
                    SpriteEffects.None, 0);
            }

            Main.spriteBatch.ResetToDefault();

            // 主剑柄：根据速度因子调整颜色
            Color mainHiltColor = Color.Lerp(lightColor, Color.White, MathHelper.Clamp(speedFactor * 0.5f, 0f, 0.5f));
            float mainHiltScale = projectile.scale * MathHelper.Lerp(1f, 1.1f, speedFactor);

            Main.EntitySpriteDraw(hiltTexture, projectile.Center + offset -
                Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                null, mainHiltColor, direction.ToRotation() + MathHelper.PiOver2,
                Vector2.UnitX * hiltTexture.Width / 2, mainHiltScale,
                SpriteEffects.None, 0);

            // 高速旋转时的光晕效果
            if (speedFactor > 0.6f)
            {
                float glowIntensity = MathHelper.Clamp((speedFactor - 0.6f) * 2.5f, 0f, 1f);
                Color glowColor = Color.OrangeRed * glowIntensity * 0.7f;

                for (int i = 0; i < 3; i++)
                {
                    float glowScale = mainHiltScale * (1f + i * 0.1f);
                    float glowAlpha = 0.5f - i * 0.15f;

                    Main.EntitySpriteDraw(hiltTexture, projectile.Center + offset -
                        Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                        null, glowColor * glowAlpha, direction.ToRotation() + MathHelper.PiOver2,
                        Vector2.UnitX * hiltTexture.Width / 2, glowScale,
                        SpriteEffects.None, 0);
                }
            }

            Main.spriteBatch.ResetToDefault();
        }
    }
}