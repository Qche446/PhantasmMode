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
        private static List<Particle> particles = [];
        private int spawnTimer = 0;
        private const int MaxParticles = 100;
        //private static Texture2D menuSky => ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/MenuSky").Value;
        //private static Texture2D pixelTexture => ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/Snow").Value;
        //private static Texture2D cherryTexture => ModContent.Request<Texture2D>("FargosPhantasmMode/Content/Sky/CherryBlossom").Value;
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
            public int TextureIndex; 
            public List<Vector2> SnowPixels;
        }

        public override bool PreDrawCloseBackground(SpriteBatch spriteBatch)
        {
            fadeIn++;
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

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            spriteBatch.Draw(menuSky, drawOffset, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            spawnTimer--;
            if (spawnTimer <= 0)
            {
                spawnTimer = Main.rand.Next(3, 10); 
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

                    float speedX = Main.rand.NextFloat(-0.5f, -1.5f);
                    float speedY = Main.rand.NextFloat(1f, 2.5f);
                    if (Main.rand.NextBool(3)) speedX += Main.rand.NextFloat(-0.3f, 0.3f);

                    int type = 0; 
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
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];
                p.Position += p.Velocity;
                p.Rotation += 0.01f * p.Velocity.Y;
                p.Alpha -= 0.001f;
                particles[i] = p;
                if (p.Position.X < -50 || p.Position.X > Main.screenWidth + 50 ||
                    p.Position.Y > Main.screenHeight + 50 || p.Alpha <= 0f)
                {
                    particles.RemoveAt(i);
                }
            }
            foreach (Particle p in particles)
            {
                if (p.TextureIndex == 0) 
                {
                    Color pixelColor = Color.White * p.Alpha;
                    foreach (Vector2 offset in p.SnowPixels)
                    {
                        Vector2 rotatedOffset = Vector2.Transform(offset, Matrix.CreateRotationZ(p.Rotation));
                        Vector2 drawPos = p.Position + rotatedOffset * p.Scale;
                        spriteBatch.Draw(pixelTexture, drawPos, null, pixelColor, 0f,
                            Vector2.Zero, new Vector2(2f * p.Scale, 2f * p.Scale), SpriteEffects.None, 0f);
                    }
                }
                else 
                {
                    Vector2 origin = cherryTexture.Size() / 2f;
                    Color particleColor = new Color(255, 182, 193) * p.Alpha; 
                    spriteBatch.Draw(cherryTexture, p.Position, null, particleColor, p.Rotation,
                        origin, p.Scale/1.5f, SpriteEffects.None, 0f);
                }
            }
            spriteBatch.End();
            spriteBatch.Begin();
            return false;
        }
    }
}