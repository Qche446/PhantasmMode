using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using FargosPhantasmMode.Assets.ExtraTextures;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using System;
using Terraria.GameContent;

namespace FargosPhantasmMode.Content.Render
{
    /// <summary>
    /// 用于文字本身的绘制
    /// </summary>
    public class TextRender
    {
        /// <summary>
        /// 灼烧文字效果绘制
        /// </summary>
        /// <param name="line"></param>文字行对象，包含了文字内容和位置等信息
        /// <param name="burnIntensity"></param>灼烧程度
        /// <param name="windDirection"></param>风向
        /// <param name="emberColor"></param>暗烬
        /// <param name="flameColor"></param>焰色
        /// <param name="brightflameColor"></param>亮焰
        /// <param name="tipColor"></param>焰尖
        public static void BurnDraw(DrawableTooltipLine line, float burnIntensity, Vector2 windDirection, Color emberColor, Color flameColor, Color brightflameColor, Color tipColor)
        {
            Main.spriteBatch.End(); //end and begin main.spritebatch to apply a shader
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Main.UIScaleMatrix);
            
            var diagonalNoise = PhantasmTextureRegistry.FireNoise;
            ManagedShader shader = ShaderManager.GetShader("FargosPhantasmMode.BurnText");
            Main.spriteBatch.GraphicsDevice.Textures[1] = diagonalNoise.Value;
            shader.TrySetParameter("burnIntensity", burnIntensity); 
            shader.TrySetParameter("windDirection", windDirection);  
            shader.TrySetParameter("emberColor", emberColor);     // 暗烬 
            shader.TrySetParameter("flameColor", flameColor);   // 火焰主色 
            shader.TrySetParameter("brightFlameColor", brightflameColor); // 亮焰 
            shader.TrySetParameter("tipColor", tipColor);// 焰尖 
            shader.Apply("AutoloadPass");
            
            Utils.DrawBorderString(Main.spriteBatch, line.Text, new Vector2(line.X, line.Y), Color.White, 1f, 0f, 0f); //draw the tooltip manually
            //Utils.DrawBorderStringFourWay(Main.spriteBatch, (ReLogic.Graphics.DynamicSpriteFont)FontAssets.MouseText, line.Text, line.X, line.Y, Color.White, Color.White, Vector2.Zero); //draw the tooltip manually
            Main.spriteBatch.End(); //then end and begin again to make remaining tooltip lines draw in the default way
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
        }
        /// <summary>
        /// 火焰粒子管理器 —— 在文字位置生成飘散的灰烬 Dust
        /// </summary>
        public static class FlameParticleManager
        {
            private static Random _rand = new Random();
            /// <summary>
            /// 在指定文字区域生成火焰粒子
            /// </summary>
            /// <param name="textPosition">文字左上角屏幕坐标</param>
            /// <param name="text">文字内容（用于估算宽度）</param>
            /// <param name="scale">文字缩放</param>
            /// <param name="intensity">火焰强度 0~1，控制粒子数量和大小</param>
            public static void SpawnFlameParticles(
                DrawableTooltipLine line,
                float scale = 1f,
                float intensity = 0.6f)
            {
                string text = line.Text;
                Vector2 textPosition = new (line.X, line.Y);
                if (intensity <= 0f) return;
                // 估算文字宽度
                float textWidth = line.Font.MeasureString(text).X * scale;
                float textHeight = line.Font.MeasureString(text).Y * scale;
                // 每帧生成的粒子数（受强度影响）
                int particleCount = (int)(intensity * 12);
                for (int i = 0; i < particleCount; i++)
                {
                    SpawnSingleEmber(textPosition, textWidth, textHeight, intensity);
                }
            }
            private static void SpawnSingleEmber(
                Vector2 origin, float width, float height, float intensity)
            {
                // 在文字矩形区域内随机生成位置（偏上方和偏右侧概率更高）
                float xOffset = (float)(_rand.NextDouble() * width);
                float yBias = (float)Math.Pow(_rand.NextDouble(), 2); // 偏向顶部
                float yOffset = yBias * height * 1.2f - height * 0.1f;
                Vector2 spawnPos = origin + new Vector2(xOffset, yOffset) + Main.screenPosition;
                // 速度：向右上方飘散，带随机扰动
                float speedX = 0.8f + (float)_rand.NextDouble() * 2.5f;   // 向右
                float speedY = -3f - (float)_rand.NextDouble() * 6.0f;  // 向上
                Vector2 velocity = new Vector2(speedX, speedY);
                velocity += new Vector2(
                    (float)(_rand.NextDouble() - 0.5) * 1.5f,  // 水平随机
                    (float)(_rand.NextDouble() - 0.5) * 1.0f   // 垂直随机
                );
                // 选择 Dust 类型：交替使用火焰和灰烬
                int dustType = _rand.Next(4) switch
                {
                    0 => 6,    // 火焰 Dust
                    1 => 127,  // 灰烬
                    2 => 174,  // 火星
                    _ => 6,    // 默认火焰
                };
                Dust dust = Dust.NewDustPerfect(
                    spawnPos,
                    dustType,
                    velocity,
                    Alpha: 0,
                    newColor: GetEmberColor(),
                    Scale: 0.5f + (float)_rand.NextDouble() * 1.5f * intensity
                );
                // 自定义行为
                dust.noGravity = true;          // 不受重力
                dust.fadeIn = 0.9f;            // 渐入时间
                dust.noLight = false;          // 发光
                dust.rotation = (float)_rand.NextDouble() * MathHelper.TwoPi;
                dust.velocity *= intensity;    // 强度影响速度
            }
            /// <summary>
            /// 返回随机灰烬颜色（与渐变颜色对应）
            /// </summary>
            private static Color GetEmberColor()
            {
                float t = (float)_rand.NextDouble();
                return t switch
                {
                    < 0.3f => new Color(40, 5, 0),           // 暗烬
                    < 0.6f => new Color(220, 80, 0),         // 火焰橙
                    < 0.85f => new Color(255, 180, 20),      // 金黄
                    _ => new Color(255, 240, 150)            // 白黄焰尖
                };
            }
        }
    }
}
