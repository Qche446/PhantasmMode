using FargosPhantasmMode.Content.Render;

namespace FargosPhantasmMode.Content.Rarities
{
    public class SupremeDeathRarity : ModRarity
    {
        public override Color RarityColor => SupremeDeathTextRender.RarityColor;

        /// <summary>画出物品名的文字特效。返回 true 表示已自绘，调用方应跳过原版绘制。</summary>
        public static bool RenderTooltip(DrawableTooltipLine line) => SupremeDeathTextRender.Draw(line);
    }
}
