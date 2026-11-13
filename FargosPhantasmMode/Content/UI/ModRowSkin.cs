using Monochrome.Core.Graphics;
using Monochrome.Core.Graphics.Shaders;
using Monochrome.Core.UI;
using ReLogic.Graphics;
using Terraria.UI;
using Terraria.UI.Chat;

namespace FargosPhantasmMode.Content.UI;
public static class PhantasmModRowSkin
{
    public const string ModName = "FargosPhantasmMode";
    private const string PanelShaderName = "FargosPhantasmMode.PhantasmPanel";
    private const string NameShaderName = "FargosPhantasmMode.PhantasmName";

    public static void Draw(MonoRowSkinContext context)
    {
        (SpriteBatch spriteBatch, UIElement row, string modName, CalculatedStyle outer, CalculatedStyle inner) = context;

        Rectangle bounds = new((int)outer.X, (int)outer.Y, (int)outer.Width, (int)outer.Height);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        // 行只有悬停与不悬停两种状态，直接跳变会闪；这里每帧朝目标插值。
        hoverBlend = MathHelper.Lerp(hoverBlend, context.Hovered ? 1f : 0f, 0.2f);

        if (!PModeShaders.Library.TryGet(PanelShaderName, out MonoShader shader))
        {
            MonoPanel.Draw(spriteBatch, bounds, Color.White);
            return;
        }

        shader.SetParameter("uTime", Main.GlobalTimeWrappedHourly);
        shader.SetParameter("uSource", PanelSource(bounds));
        shader.SetParameter("uHover", hoverBlend);
        shader.SetParameter("uPixel", 1f * Main.UIScale);

        spriteBatch.End();

        using (MonoBatchScope.Begin(spriteBatch, SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, Main.UIScaleMatrix, effect: shader.Effect)) 
        {
            MonoPanel.Draw(spriteBatch, bounds, Color.White);
        }
        spriteBatch.ResetToDefaultUI(end: false);
    }
    private static float hoverBlend; //悬停强度

    private static readonly Vector2[] OutlineOffsets = [new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f)];

    // 名字的蓝白渐变。颜色按屏幕横坐标逐像素算，所以是连续的，不是逐字跳。
    public static void DrawName(MonoRowNameContext context)
    {
        SpriteBatch spriteBatch = context.Batch;
        DynamicSpriteFont font = context.Font;
        float scale = context.Scale;

        // 黑色描边：四向各偏 1 UI 单位
        for (int i = 0; i < 4; i++)
            spriteBatch.DrawString(font, context.Text, context.Position +  1.5f * OutlineOffsets[i], Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        if (!PModeShaders.Library.TryGet(NameShaderName, out MonoShader shader))
        {
            spriteBatch.DrawString(font, context.Text, context.Position, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            return;
        }

        Vector2 size = ChatManager.GetStringSize(font, context.Text, Vector2.One) * scale;
        Vector2 topLeft = Vector2.Transform(context.Position, Main.UIScaleMatrix);
        Vector2 bottomRight = Vector2.Transform(context.Position + size, Main.UIScaleMatrix);

        shader.SetParameter("uSource", new Vector4(bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y, topLeft.X, topLeft.Y));
        shader.SetParameter("uTime", Main.GlobalTimeWrappedHourly);
        shader.SetParameter("uHover", hoverBlend);

        spriteBatch.End();

        using (MonoBatchScope.Begin(spriteBatch, SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, Main.UIScaleMatrix, effect: shader.Effect))
        {
            spriteBatch.DrawString(font, context.Text, context.Position, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        spriteBatch.ResetToDefaultUI(end: false);
    }

    /// <summary>把 UI 坐标的矩形换算成着色器要的面板屏幕矩形：xy 是尺寸，zw 是左上角。</summary>
    private static Vector4 PanelSource(Rectangle bounds)
    {
        Vector2 topLeft = Vector2.Transform(new Vector2(bounds.Left, bounds.Top), Main.UIScaleMatrix);
        Vector2 bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), Main.UIScaleMatrix);
        Vector2 size = bottomRight - topLeft;
        return new Vector4(size.X, size.Y, topLeft.X, topLeft.Y);
    }
}
