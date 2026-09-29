using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace FargosPhantasmMode.Content.Render
{
    /// <summary>
    /// 尺度随时间减少的粒子，可用于火焰
    /// </summary>
    public class FirePartiRe
    {
        static List<Particle> particles = new List<Particle>();
        const int MaxParticles = 1000;
        //Texture2D texture = ModContent.Request<Texture2D>("FargosPhantasmMode/Assets/ModDusts/CosmicFlame", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        public struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Scale;
            public float Alpha;
            public bool active;
        }
        public static void SpawnParticle(Particle p)
        {
            particles.Add(new Particle
            {
                Position = p.Position,
                Velocity = p.Velocity,
                Scale = p.Scale,
                Alpha = p.Alpha,
                active = p.active
            });
        }
        public static void UpdateParticle()
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];
                p.Position += p.Velocity;
                p.Scale -= 0.016f;
                p.Velocity *= 0.99f;
                if (p.Scale <= 0)
                    p.active = false;
                particles[i] = p;

                if (!p.active)
                {
                    particles.RemoveAt(i);
                }
            }
        }
        public static void AllDraw(SpriteBatch sb, Texture2D tex)
        {
            foreach (Particle d in particles)
            {
                if (d.active)
                {
                    sb.Draw(tex, d.Position, null, Color.White, 0, tex.Size() / 2, d.Scale, SpriteEffects.None, 0);
                }
            }
        }
    }
    /// <summary>
    /// 一般用于闪电
    /// </summary>
    public class LightningPartiRe
    {
        static List<Particle> particles = new List<Particle>();
        const int MaxParticles = 1000;
        private static readonly Random lightningRand = new Random();
        //Texture2D texture = ModContent.Request<Texture2D>("FargosPhantasmMode/Assets/ModDusts/CosmicFlame", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        public struct Particle
        {
            public List<Vector2> Points;      
            public float Alpha;               
            public float LifeTime;            
            public float MaxLifeTime;         
            public float Width;               
            public Color Color;               
        }
        
        /// <summary>
        /// start，end决定起始点，segement决定偏折段数，displacement决定偏折程度(最大值)
        /// </summary>
        public static void SpawnParticle(Vector2 start, Vector2 end, int segments, float displacement)
        {
            List<Vector2> points = new List<Vector2> { start };
            Vector2 dir = end - start;
            Vector2 norm = dir.SafeNormalize(Vector2.UnitX);
            Vector2 perp = new Vector2(-norm.Y, norm.X); // 垂直方向

            for (int i = 1; i < segments; i++)
            {
                float t = (float)i / (float)segments;
                Vector2 basePos = start + dir * t;
                // 随机扰动（闪电的锯齿特征）
                float offset = (float)(lightningRand.NextDouble() * 2 - 1) * displacement * (1 - Math.Abs(t - 0.5f) * 2);
                points.Add(basePos + perp * offset);
            }
            points.Add(end);
            particles.Add(new Particle
            {
                Points = points,
                Alpha = 1f,
                LifeTime = 20,
                //MaxLifeTime = 20,
                Width = 3f,
                Color = new Color(105, 216, 255) // 淡蓝白色闪电
            });
        }
        public static void UpdateParticle()
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];
                p.LifeTime--;
                if (p.LifeTime < 10)
                    p.Alpha = p.LifeTime / 10f;
                //p.Width = 3 * Math.Abs(p.LifeTime - 10) / 10f;
                particles[i] = p;

                if (p.LifeTime <= 0)
                {
                    particles.RemoveAt(i);
                }
            }
        }
        /// <summary>
        /// 材质一般可用Terraria.GameContent.TextureAssets.MagicPixel.Value
        /// </summary>
        public static void AllDraw(SpriteBatch sb)
        {
            foreach (Particle d in particles)
            {
                for (int i = 0; i < d.Points.Count - 1; i++)
                {
                    Vector2 start = d.Points[i] + Main.screenPosition;
                    Vector2 end = d.Points[i + 1] + Main.screenPosition;
                    Luminance.Common.Utilities.Utilities.DrawBloomLine(sb, start, end, d.Color * d.Alpha, d.Width);
                }
            }
        }
    }
}
