using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Sky
{
    public class MainMenuBackgroundSky : ModSurfaceBackgroundStyle
    {
        private float intensity = 0.6f;
        private float lifeIntensity = 1f;
        private float specialColorLerp = 0f;
        private Color? specialColor = null;
        private List<Particle> particles = new List<Particle>();
        private int spawnTimer = 0;
        private const int MaxParticles = 100;

        // 纯白像素纹理，用于绘制雪花点
        private static Texture2D pixelTexture = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/Snow").Value;
        // 樱花纹理（保持使用贴图）
        private static Texture2D cherryTexture = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/CherryBlossom").Value;
        public override void ModifyFarFades(float[] fades, float transitionSpeed)
        {
            for (int i = 0; i < fades.Length; i++)
            {
                if (i == Slot)
                {
                    fades[i] += transitionSpeed;
                    if (fades[i] > 1f)
                        fades[i] = 1f;
                }
                else
                {
                    fades[i] -= transitionSpeed;
                    if (fades[i] < 0f)
                        fades[i] = 0f;
                }
            }
        }

        public int fadeIn = 0;

        private Color ColorToUse(ref float opacity)
        {
            Color color = new Color(51, 255, 191);
            opacity = intensity * 0.5f + lifeIntensity * 0.5f;
            opacity *= Math.Min(fadeIn / 60f, 1);

            if (specialColorLerp > 0 && specialColor != null)
            {
                color = Color.Lerp(color, (Color)specialColor, specialColorLerp);
                if (specialColor == Color.Black)
                    opacity = Math.Min(1f, opacity + Math.Min(intensity, lifeIntensity) * 0.5f);
            }
            return color;
        }
        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Rotation;
            public float Scale;
            public float Alpha;
            public int TextureIndex; // 0 = 雪花(像素点), 1 = 樱花(贴图)

            // 雪花像素点的相对偏移坐标列表（相对于粒子中心）
            public List<Vector2> SnowPixels;
        }

        public override bool PreDrawCloseBackground(SpriteBatch spriteBatch)
        {
            fadeIn++;

            // 绘制主背景（用户的 MenuSky 纹理，全屏缩放）
            Texture2D menuSky = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/MenuSky").Value;
            Texture2D pixelTexture = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/Snow").Value;
            Texture2D cherryTexture = ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/CherryBlossom").Value;

        Vector2 drawOffset = Vector2.Zero;
            float xScale = (float)Main.screenWidth / menuSky.Width;
            float yScale = (float)Main.screenHeight / menuSky.Height;
            float scale = xScale;

            if (xScale != yScale)
            {
                if (yScale > xScale)
                {
                    scale = yScale;
                    drawOffset.X -= (menuSky.Width * scale - Main.screenWidth) * 0.5f;
                }
                else
                    drawOffset.Y -= (menuSky.Height * scale - Main.screenHeight) * 0.5f;
            }

            // 结束原版批次，开始自定义绘制（使用 LinearClamp 平滑缩放）
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            // 绘制主背景
            spriteBatch.Draw(menuSky, drawOffset, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            // ---------- 粒子生成 ----------
            spawnTimer--;
            if (spawnTimer <= 0)
            {
                spawnTimer = Main.rand.Next(3, 10); 
                int count = Main.rand.Next(15, 25);    
                for (int i = 0; i < count && particles.Count < MaxParticles; i++)
                {
                    // 从屏幕边缘随机出现（左、右、上）
                    int edge = Main.rand.Next(3);
                    Vector2 spawnPos;
                    if (edge == 0)
                        spawnPos = new Vector2(-20, Main.rand.NextFloat(Main.screenHeight));
                    else if (edge == 1)
                        spawnPos = new Vector2(Main.screenWidth + 20, Main.rand.NextFloat(Main.screenHeight));
                    else
                        spawnPos = new Vector2(Main.rand.NextFloat(Main.screenWidth), -20);

                    float speedX = Main.rand.NextFloat(-0.5f, -1.5f);
                    float speedY = Main.rand.NextFloat(1f, 2.5f);
                    if (Main.rand.NextBool(3)) speedX += Main.rand.NextFloat(-0.3f, 0.3f);

                    int type = 0; // 0=雪花, 1=樱花

                    // 创建粒子
                    Particle p = new Particle
                    {
                        Position = spawnPos,
                        Velocity = new Vector2(speedX, speedY),
                        Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                        Scale = Main.rand.NextFloat(0.5f, 1.2f),
                        Alpha = Main.rand.NextFloat(0.6f, 1.0f),
                        TextureIndex = type,
                        SnowPixels = null
                    };

                    // 如果是雪花（type==0），生成随机像素点（6~12个）
                    if (type == 0)
                    {
                        int pixelCount = Main.rand.Next(8, 13);
                        List<Vector2> pixels = new List<Vector2>(pixelCount);
                        for (int j = 0; j < pixelCount; j++)
                        {
                            float px = Main.rand.Next(-3, 4); // -3..3
                            float py = Main.rand.Next(-3, 4);
                            pixels.Add(new Vector2(px, py));
                        }
                        p.SnowPixels = pixels;
                    }
                    particles.Add(p);
                }
            }

            // ---------- 粒子更新 ----------
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];
                p.Position += p.Velocity;
                p.Rotation += 0.01f * p.Velocity.Y;
                p.Alpha -= 0.001f;
                particles[i] = p;

                // 移除超出屏幕或透明度为零的粒子
                if (p.Position.X < -50 || p.Position.X > Main.screenWidth + 50 ||
                    p.Position.Y > Main.screenHeight + 50 || p.Alpha <= 0f)
                {
                    particles.RemoveAt(i);
                }
            }

            // ---------- 绘制粒子 ----------
            foreach (Particle p in particles)
            {
                if (p.TextureIndex == 0) // 雪花 - 用白色像素点绘制
                {
                    Color pixelColor = Color.White * p.Alpha;
                    // 遍历所有像素点
                    foreach (Vector2 offset in p.SnowPixels)
                    {
                        // 应用旋转和缩放
                        Vector2 rotatedOffset = Vector2.Transform(offset, Matrix.CreateRotationZ(p.Rotation));
                        Vector2 drawPos = p.Position + rotatedOffset * p.Scale;

                        // 绘制2x2像素的白色方块（可通过改变大小调整视觉效果）
                        spriteBatch.Draw(pixelTexture, drawPos, null, pixelColor, 0f,
                            Vector2.Zero, new Vector2(2f * p.Scale, 2f * p.Scale), SpriteEffects.None, 0f);
                    }
                }
                else // 樱花 - 使用贴图
                {
                    // 注意：这里是使用固定纹理绘制，和用户原先一样
                    Vector2 origin = cherryTexture.Size() / 2f;
                    Color particleColor = new Color(255, 182, 193) * p.Alpha; // 粉红色
                    spriteBatch.Draw(cherryTexture, p.Position, null, particleColor, p.Rotation,
                        origin, p.Scale/1.5f, SpriteEffects.None, 0f);
                }
            }

            // 结束自定义批次，恢复默认
            spriteBatch.End();
            spriteBatch.Begin();

            // 阻止基类绘制默认背景
            return false;
        }
    }
}