
using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Render;

namespace FargosPhantasmMode.Content.Rarities
{
    public class MechRarity : ModRarity
    {
        public override Color RarityColor => PhanUtil.MechColor();

        /// <summary>机械电路文字：自绘成功才返回 true，调用方据此决定要不要跳过原版绘制。</summary>
        public static bool RenderTooltip(DrawableTooltipLine line) => MechTextRender.Draw(line, MechTextRender.ColorRed, MechTextRender.ColorGreen, MechTextRender.ColorGreen, MechTextRender.ColorYellow);
    }
}
