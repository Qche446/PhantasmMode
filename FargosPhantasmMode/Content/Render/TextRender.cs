using FargosPhantasmMode.Assets.ExtraTextures;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

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
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.LinearWrap, null, null, null, Main.UIScaleMatrix);
            
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
    }
}
