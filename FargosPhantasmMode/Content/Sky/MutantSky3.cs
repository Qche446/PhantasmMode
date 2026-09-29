using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Sky
{
    public class MutantSky3 : CustomSky
    {
        private bool isActive = false;
        private float intensity = 0f;
        private float lifeIntensity = 0f;
        private float specialColorLerp = 0f;
        private Color? specialColor = null;
        private int delay = 0;
        private readonly int[] xPos = new int[50];
        private readonly int[] yPos = new int[50];

        // 雪花粒子系统
        private List<SnowParticle> particles = new List<SnowParticle>();
        private int spawnTimer = 0;
        private const int MaxParticles = 100;

        // 纹理
        private static Texture2D skyTexture;
        private static Texture2D staticTexture;
        private static Texture2D pixelTexture;

        // 粒子结构
        private struct SnowParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Rotation;
            public float Scale;
            public float Alpha;
            public List<Vector2> SnowPixels; // 随机像素点的局部偏移
        }

        public override void Update(GameTime gameTime)
        {
            const float increment = 0.01f;
            bool useSpecialColor = false;

            // 检查是否 MutantBoss 存活
            if (FargoSoulsUtil.BossIsAlive(ref EModeGlobalNPC.mutantBoss, ModContent.NPCType<MutantBoss>())
                && (Main.npc[EModeGlobalNPC.mutantBoss].ai[0] < 0 || Main.npc[EModeGlobalNPC.mutantBoss].ai[0] >= 10))
            {
                NPC mutant = Main.npc[EModeGlobalNPC.mutantBoss];
                intensity += increment;
                lifeIntensity = mutant.ai[0] < 0 ? 1f : 1f - (float)mutant.life / mutant.lifeMax;

                void ChangeColorIfDefault(Color color)
                {
                    if (specialColor == null)
                        specialColor = color;
                    if (specialColor != null && specialColor == color)
                        useSpecialColor = true;
                }

                // 根据不同攻击改变颜色（可扩展）
                switch ((int)mutant.ai[0])
                {
                    case -5:
                        if (mutant.ai[2] >= 420)
                            ChangeColorIfDefault(FargoSoulsUtil.AprilFools ? new Color(255, 180, 50) : Color.Cyan);
                        break;
                    case 10:
                        useSpecialColor = true;
                        specialColor = Color.Black;
                        specialColorLerp = 1f;
                        break;
                    case 19:
                        ChangeColorIfDefault(Color.Gray);
                        break;
                    case 27:
                        ChangeColorIfDefault(Color.Red);
                        break;
                    case 28:
                        ChangeColorIfDefault(Color.Gold);//金色
                        break;
                    case 36:
                        if (WorldSavingSystem.MasochistModeReal && mutant.ai[2] > 180 * 3 - 60)
                            ChangeColorIfDefault(Color.Blue);
                        break;
                    case 44:
                        ChangeColorIfDefault(Color.DeepPink);
                        break;
                    case 48:
                        ChangeColorIfDefault(Color.Purple);
                        break;
                    case 49:
                        ChangeColorIfDefault(Color.Black);
                        break;
                    case 50:
                        ChangeColorIfDefault(Color.OrangeRed);
                        break;
                    default:
                        break;
                }

                if (intensity > 1f)
                    intensity = 1f;
            }
            else
            {
                lifeIntensity -= increment;
                if (lifeIntensity < 0f)
                    lifeIntensity = 0f;

                specialColorLerp -= increment * 2;
                if (specialColorLerp < 0)
                    specialColorLerp = 0;

                intensity -= increment;
                if (intensity < 0f)
                {
                    intensity = 0f;
                    lifeIntensity = 0f;
                    specialColorLerp = 0f;
                    specialColor = null;
                    delay = 0;
                    Deactivate();
                    return;
                }
            }

            if (useSpecialColor)
            {
                specialColorLerp += increment * 2;
                if (specialColorLerp > 1)
                    specialColorLerp = 1;
            }
            else
            {
                specialColorLerp -= increment * 2;
                if (specialColorLerp < 0)
                {
                    specialColorLerp = 0;
                    specialColor = null;
                }
            }

            // 更新雪花粒子
            UpdateSnowParticles();
        }

        private Color ColorToUse(ref float opacity)
        {
            Color color = FargoSoulsUtil.AprilFools ? Color.OrangeRed : new(51, 244, 250);
            opacity = intensity * 0.5f + lifeIntensity * 0.5f;

            if (specialColorLerp > 0 && specialColor != null)
            {
                color = Color.Lerp(color, (Color)specialColor, specialColorLerp);
                if (specialColor == Color.Black)
                    opacity = Math.Min(1f, opacity + Math.Min(intensity, lifeIntensity) * 0.5f);
            }

            return color;
        }

        private void UpdateSnowParticles()
        {
            // 生成粒子
            spawnTimer--;
            if (spawnTimer <= 0)
            {
                spawnTimer = Main.rand.Next(6, 20);
                int count = Main.rand.Next(15, 25);
                for (int i = 0; i < count && particles.Count < MaxParticles; i++)
                {
                    int edge = Main.rand.Next(3);
                    Vector2 spawnPos;
                    if (edge == 0)
                        spawnPos = new Vector2(-20, Main.rand.NextFloat(Main.screenHeight));
                    else if (edge == 1)
                        spawnPos = new Vector2(Main.screenWidth + 20, Main.rand.NextFloat(Main.screenHeight));
                    else
                        spawnPos = new Vector2(Main.rand.NextFloat(Main.screenWidth), -20);

                    float speedX = Main.rand.NextFloat(-8f, -16f);
                    float speedY = Main.rand.NextFloat(12f, 24f);
                    if (Main.rand.NextBool(3)) speedX += Main.rand.NextFloat(-0.3f, 0.3f);

                    // 创建随机像素点（6~12个）
                    int pixelCount = Main.rand.Next(18, 25);
                    List<Vector2> pixels = new List<Vector2>(pixelCount);
                    for (int j = 0; j < pixelCount; j++)
                    {
                        float px = Main.rand.Next(-4, 5);
                        float py = Main.rand.Next(-4, 5);
                        pixels.Add(new Vector2(px, py));
                    }

                    particles.Add(new SnowParticle
                    {
                        Position = spawnPos + Main.screenPosition,
                        Velocity = new Vector2(speedX, speedY),
                        Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                        Scale = Main.rand.NextFloat(0.5f, 1.2f),
                        Alpha = Main.rand.NextFloat(0.6f, 1.0f),
                        SnowPixels = pixels
                    });
                }
            }

            // 更新已有粒子
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                SnowParticle p = particles[i];
                p.Position += p.Velocity;
                p.Rotation += 0.01f * p.Velocity.Y;
                p.Alpha -= 0.001f;
                particles[i] = p;

                if (p.Position.X - Main.screenPosition.X < -50 || p.Position.X - Main.screenPosition.X > Main.screenWidth + 50 ||
                    p.Position.Y - Main.screenPosition.Y > Main.screenHeight + 50 || p.Alpha <= 0f)
                {
                    particles.RemoveAt(i);
                }
            }
        }

        // 加载纹理（仅一次）
        private static void LoadTextures()
        {
            if (skyTexture == null)
                skyTexture = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Sky/MutantSky" + FargoSoulsUtil.TryAprilFoolsTexture, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            if (staticTexture == null)
                staticTexture = ModContent.Request<Texture2D>("FargowiltasSouls/Content/Sky/MutantStatic", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            if (pixelTexture == null)
            {
                pixelTexture = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/Snow", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value; // 2x2 白色点或你自己准备的像素纹理
            }
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth >= 0 && minDepth < 0)
            {
                LoadTextures();

                float opacity = 0f;
                Color color = ColorToUse(ref opacity);

                // 绘制主背景（法狗的天空纹理）
                spriteBatch.Draw(skyTexture,new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),color * opacity);


                // 原静态噪点效果（保持原方法的部分）
                if (--delay < 0)
                {
                    delay = Main.rand.Next(5 + (int)(85f * (1f - lifeIntensity)));
                    for (int i = 0; i < 50; i++)
                    {
                        xPos[i] = Main.rand.Next(Main.screenWidth);
                        yPos[i] = Main.rand.Next(Main.screenHeight);
                    }
                }

                for (int i = 0; i < 50; i++)
                {
                    int width = Main.rand.Next(3, 251);
                    spriteBatch.Draw(staticTexture,
                        new Rectangle(xPos[i] - width / 2, yPos[i], width, 3),
                        color * lifeIntensity * 0.75f);
                }
                // 绘制雪花粒子（随机点阵）
                foreach (SnowParticle p in particles)
                {
                    Color pixelColor = Color.White * p.Alpha * opacity; // 混合背景透明度
                    foreach (Vector2 offset in p.SnowPixels)
                    {
                        Vector2 rotatedOffset = Vector2.Transform(offset, Matrix.CreateRotationZ(p.Rotation));
                        Vector2 drawPos = p.Position + rotatedOffset * p.Scale - Main.screenPosition;
                        // 绘制2x2白色方块
                        spriteBatch.Draw(pixelTexture, drawPos, null, pixelColor, 0f,
                            Vector2.Zero, new Vector2(2f * p.Scale, 2f * p.Scale), SpriteEffects.None, 0f);
                    }
                }
            }
        }

        public override float GetCloudAlpha()
        {
            return 1f - intensity;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
        }

        public override void Deactivate(params object[] args)
        {
            isActive = false;
        }

        public override void Reset()
        {
            isActive = false;
            particles.Clear();
        }

        public override bool IsActive()
        {
            return isActive;
        }

        public override Color OnTileColor(Color inColor)
        {
            float dummy = 0f;
            Color skyColor = Color.Lerp(Color.White, ColorToUse(ref dummy), 0.5f);
            return Color.Lerp(skyColor, inColor, 1f - intensity);
        }
    }
}