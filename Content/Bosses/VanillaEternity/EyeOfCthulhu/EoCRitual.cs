using FargowiltasSouls;
using FargowiltasSouls.Assets.ExtraTextures;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.EyeOfCthulhu
{
    public class EoCRitual : BaseArena, IPixelatedPrimitiveRenderer
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Masomode/BloodScytheVanilla1";

        private const float realRotation = MathHelper.Pi / 180f;
        public float VisualScale = 0f;

        // 镰刀数量
        private const int ScytheCount = 40;

        // 保存每个镰刀的随机贴图索引（1-3）
        private int[] scytheTextures;

        // 保存每个镰刀的自转角度
        private float[] scytheRotations;

        // 保存每个镰刀的公转角度
        private float[] scytheOrbitAngles;

        // 保存每个镰刀的公转速度
        private float[] scytheOrbitSpeeds;

        // 保存每个镰刀的位置历史（用于像素化拖尾）
        private List<Vector2>[] scythePositionHistory;
        private const int PositionHistoryLength = 10;

        // 颜色系统
        private bool recolor;

        // 公转模式控制
        private float orbitSpeedMultiplier = 1f;

        // 镰刀大小缩放
        private float scytheScale = 1.5f;


        public EoCRitual() : base(realRotation, 1200f, NPCID.EyeofCthulhu, 87, visualCount: 0) { }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.hide = true;
            CooldownSlot = ImmunityCooldownID.Bosses;

            // 初始化镰刀数据
            InitializeScytheData();

            // 初始化位置历史
            InitializePositionHistory();

            // 检查是否需要重绘
            recolor = false && WorldSavingSystem.EternityMode && Projectile.ai[2] != 1 && !Main.bloodMoon;
        }

        private void InitializeScytheData()
        {
            scytheTextures = new int[ScytheCount];
            scytheRotations = new float[ScytheCount];
            scytheOrbitAngles = new float[ScytheCount];
            scytheOrbitSpeeds = new float[ScytheCount];

            // 为每个镰刀随机分配贴图和初始状态
            for (int i = 0; i < ScytheCount; i++)
            {
                scytheTextures[i] = Main.rand.Next(1, 4); // 随机1-3
                scytheRotations[i] = Main.rand.NextFloat(MathHelper.TwoPi);
                scytheOrbitAngles[i] = MathHelper.TwoPi * i / ScytheCount; // 均匀分布初始角度
                scytheOrbitSpeeds[i] = 0.025f; 
            }
        }

        private void InitializePositionHistory()
        {
            scythePositionHistory = new List<Vector2>[ScytheCount];
            for (int i = 0; i < ScytheCount; i++)
            {
                scythePositionHistory[i] = new List<Vector2>();
            }
        }

        protected override void Movement(NPC npc)
        {
            npc.TargetClosest();
            Player player = Main.player[npc.target];
            if (npc != null && npc.active )
            {
                // 跟随移动
                if (npc.life > 0.1f * npc.lifeMax)
                {
                    Projectile.velocity = npc.Center - Projectile.Center;
                    Projectile.velocity /= 20f;
                }
                else
                {
                    Projectile.velocity = player.Center - Projectile.Center;
                    Projectile.velocity /= 160f;
                }
                rotationPerTick = realRotation;
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
                rotationPerTick = -realRotation / 10f;
            }
        }

        public override void AI()
        {
            NPC npc = FargoSoulsUtil.NPCExists(Projectile.ai[1], npcType);
            if (npc != null)
            {
                Projectile.alpha -= 2;
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;

                Movement(npc);

                targetPlayer = npc.target;

                Player player = Main.LocalPlayer;
                if (player.active && !player.dead && !player.ghost)
                {
                    float distance = player.Distance(Projectile.Center);
                    if (distance > threshold && distance < threshold * 4f)
                    {
                        if (distance > threshold * 2f)
                        {
                            player.Incapacitate();
                            player.velocity.X = 0f;
                            player.velocity.Y = -0.4f;
                        }

                        Vector2 movement = Projectile.Center - player.Center;
                        float difference = movement.Length() - threshold;
                        movement.Normalize();
                        movement *= difference < 17f ? difference : 17f;
                        player.position += movement;
                    }
                }
                if (npc.life < 0.1f * npc.lifeMax)
                {
                    if (threshold > 1200 - 0.8f * (1200 - 12000 * (float)npc.life / (float)npc.lifeMax))
                    {
                        threshold -= 0.05f * (threshold - (1200 - 0.8f * (1200 - 12000 * (float)npc.life / (float)npc.lifeMax)));
                    }
                }
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.alpha += 50;
                if (Projectile.alpha > 255)
                {
                    Projectile.Kill();
                    return;
                }
            }
            
            // 逐渐显示
            if (VisualScale < 1)
                VisualScale += 0.01f;

            float radius = threshold;

            // 更新每个镰刀的公转、自转和位置历史
            for (int i = 0; i < ScytheCount; i++)
            {
                // 公转
                scytheOrbitAngles[i] += scytheOrbitSpeeds[i] * orbitSpeedMultiplier;
                if (scytheOrbitAngles[i] > MathHelper.TwoPi)
                    scytheOrbitAngles[i] -= MathHelper.TwoPi;

                // 自转
                scytheRotations[i] += 0.1f;
                if (scytheRotations[i] > MathHelper.TwoPi)
                    scytheRotations[i] -= MathHelper.TwoPi;

                
                // 计算镰刀当前位置
                Vector2 currentPosition = Projectile.Center + scytheOrbitAngles[i].ToRotationVector2() * (radius );

                // 更新位置历史（用于像素化拖尾）
                scythePositionHistory[i].Insert(0, currentPosition);
                if (scythePositionHistory[i].Count > PositionHistoryLength)
                    scythePositionHistory[i].RemoveAt(PositionHistoryLength);
            }

            // 根据时间调整公转速度（可选）
            orbitSpeedMultiplier = 1.2f ;

            // 镰刀大小波动
            scytheScale = 1.2f ;

            Projectile.timeLeft = 2;
            Projectile.scale = (1f - Projectile.alpha / 255f) * 2f;
            Projectile.ai[0] += rotationPerTick;
            if (Projectile.ai[0] > MathHelper.Pi)
            {
                Projectile.ai[0] -= 2f * MathHelper.Pi;
                Projectile.netUpdate = true;
            }
            else if (Projectile.ai[0] < -MathHelper.Pi)
            {
                Projectile.ai[0] += 2f * MathHelper.Pi;
                Projectile.netUpdate = true;
            }

            Projectile.localAI[0] = threshold;
        }

        public override void PostAI()
        {
            Projectile.hide = false;
        }

        public override bool CanHitPlayer(Player target)
        {
            return WorldSavingSystem.MasochistModeReal ? base.CanHitPlayer(target) : false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            base.OnHitPlayer(target, info);

            target.AddBuff(BuffID.Bleeding, 240);
            if (WorldSavingSystem.EternityMode)
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 240);

            if (WorldSavingSystem.MasochistModeReal)
            {
                target.AddBuff(ModContent.BuffType<BerserkedBuff>(), 120);
                target.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 120);
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (Projectile.hide)
                behindNPCsAndTiles.Add(index);
        }

        // 像素化拖尾的宽度函数
        public float WidthFunction(float completionRatio)
        {
            float baseWidth = 0.8f * Projectile.width;
            return MathHelper.SmoothStep(baseWidth, 3.5f, completionRatio);
        }

        // 像素化拖尾的颜色函数
        public Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(recolor ? Color.Teal : Color.DarkRed, Color.Transparent, completionRatio) * 0.7f;
        }

        // 实现IPixelatedPrimitiveRenderer接口
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader shader = ShaderManager.GetShader("FargowiltasSouls.BlobTrail");
            FargoSoulsUtil.SetTexture1(FargosTextureRegistry.FadedStreak.Value);

            // 为每个镰刀绘制像素化拖尾
            for (int i = 0; i < ScytheCount; i++)
            {
                if (scythePositionHistory[i].Count < 2)
                    continue;

                // 创建拖尾点数组
                Vector2[] trailPositions = new Vector2[scythePositionHistory[i].Count];
                for (int j = 0; j < scythePositionHistory[i].Count; j++)
                {
                    trailPositions[j] = scythePositionHistory[i][j];
                }

                // 绘制像素化拖尾
                PrimitiveRenderer.RenderTrail(trailPositions,
                    new(WidthFunction, ColorFunction, _ => Vector2.Zero, Pixelate: true, Shader: shader),
                    trailPositions.Length);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float radius = threshold ;

            // 使用Alpha混合模式绘制镰刀本体
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < ScytheCount; i++)
            {
                // 计算镰刀位置（基于公转角度）
                float orbitAngle = scytheOrbitAngles[i];
                Vector2 orbitPosition = Projectile.Center + orbitAngle.ToRotationVector2() * (radius );

                // 计算镰刀旋转（指向圆心并加上自转）
                float scytheRotation = orbitAngle + MathHelper.PiOver2 + scytheRotations[i];

                // 获取对应的贴图
                string texturePath = recolor ?
                    $"FargowiltasSouls/Content/Projectiles/Masomode/BloodScythe{scytheTextures[i]}" :
                    $"FargowiltasSouls/Content/Projectiles/Masomode/BloodScytheVanilla{scytheTextures[i]}";

                Texture2D texture = ModContent.Request<Texture2D>(texturePath).Value;

                Rectangle frame = texture.Bounds;
                Vector2 origin = frame.Size() / 2f;

                // 绘制主镰刀
                Color scytheColor = recolor ? Color.Teal : Color.White;
                scytheColor *= Projectile.Opacity;

                Main.EntitySpriteDraw(texture, orbitPosition - Main.screenPosition, frame,
                    scytheColor, scytheRotation, origin, scytheScale, SpriteEffects.None, 0);

                // 为部分镰刀添加微弱的发光环（使用加算混合）
                if ( Projectile.Opacity > 0.5f)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                    Texture2D glowTexture = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Projectiles/GlowRing").Value;
                    Vector2 glowDrawPosition = orbitPosition + orbitAngle.ToRotationVector2() * 2f;
                    Color glowColor = recolor ? Color.Teal : Color.DarkRed;

                    Main.EntitySpriteDraw(glowTexture, glowDrawPosition - Main.screenPosition, null,
                        glowColor, scytheRotation, glowTexture.Size() * 0.5f,
                        scytheScale * 0.4f, SpriteEffects.None, 0);
                    glowColor *= Projectile.Opacity * 0.6f; // 降低发光环透明度
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                }
            }

            return false;
        }

        // 重写OnKill方法，移除击杀时的金色粒子
        public override void OnKill(int timeLeft)
        {
            // 只保留少量紫色/红色粒子，移除金色粒子
            float modifier = (255f - Projectile.alpha) / 255f;
            float offset = threshold * modifier;
            int max = (int)(100 * modifier); // 减少粒子数量

            for (int i = 0; i < max; i++)
            {
                int d = Dust.NewDust(Projectile.Center, 0, 0, recolor ? DustID.GemAmethyst : DustID.Blood, Scale: 2f);
                Main.dust[d].velocity *= 3f;
                Main.dust[d].noGravity = true;
                Main.dust[d].position = Projectile.Center + offset * Vector2.UnitX.RotatedByRandom(2 * Math.PI);
            }
        }
    }
}