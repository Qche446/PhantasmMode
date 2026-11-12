using Monochrome.Core.Graphics;
using Monochrome.Core.Graphics.Shaders;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace Monochrome.Core.UI;

/// <summary>
/// 模组列表里 Monochrome 那一行的观感，同时充当 <see cref="MonoRowSkins"/> 的参考实现：
/// 底板与名字各是一个回调，与别的模组注册时用的是同一套入口。
/// <para>
/// 画布是原版底板的那块矩形，画完把原底板与边框整块盖住——所以底色必须<b>不透明</b>，
/// 半透明会透出 tModLoader 的蓝色面板。坐标是 UI 单位，批次由原版开好。
/// </para>
/// </summary>
internal static class MonoModRowStyle
{
    /// <summary>这套皮肤认的模组名。</summary>
    internal const string TargetModName = "Monochrome";

    private const string GlassShaderName = "Monochrome.ModRowGlass";
    private const string NameShaderName = "Monochrome.ModRowName";

    /// <summary>名字描边的四个方向，偏移 1 个 UI 单位。</summary>
    private static readonly Vector2[] OutlineOffsets =
    [
        new(-1f, 0f),
        new(1f, 0f),
        new(0f, -1f),
        new(0f, 1f),
    ];

    private static readonly Color OutlineColor = new(8, 6, 16);

    private static float hoverBlend;

    /// <summary>画底板：圆角玻璃面，边框与内阴影由着色器沿着圆角算。</summary>
    internal static void Draw(MonoRowSkinContext context)
    {
        Rectangle bounds = new(
            (int)context.Outer.X,
            (int)context.Outer.Y,
            (int)context.Outer.Width,
            (int)context.Outer.Height);

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        SpriteBatch spriteBatch = context.Batch;

        // 行只有悬停与不悬停两种状态，直接跳变会闪；这里每帧朝目标插值。
        hoverBlend = MathHelper.Lerp(hoverBlend, context.Hovered ? 1f : 0f, 0.2f);

        if (!MonoShaderManager.TryGet(GlassShaderName, out MonoShader? shader) || shader is null)
        {
            MonoPanel.Draw(spriteBatch, bounds, Color.White);
            return;
        }

        shader.SetParameter("uTime", Main.GlobalTimeWrappedHourly);
        shader.SetParameter("uSource", SourceRect(bounds));
        shader.SetParameter("uHover", hoverBlend);
        shader.SetParameter("uBorder", Math.Max(2f, MonoPanel.BarSize * Main.UIScale));
        shader.SetParameter("uCorner", MonoPanel.CornerSize * Main.UIScale);

        // 玻璃后面垫的就是这个模组自己的图标，拉伸铺满整行。
        Texture2D? icon = ModContent.Request<Texture2D>("Monochrome/Core/UI/MonoRowSkin_Texture").Value;
        shader.SetTexture(icon ?? TextureAssets.MagicPixel.Value, 1);
        shader.SetParameter("uIconStrength", icon is null ? 0f : 1f);

        spriteBatch.End();

        using (MonoBatchScope.Begin(spriteBatch, SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, Main.UIScaleMatrix, effect: shader.Effect))
        {
            MonoPanel.Draw(spriteBatch, bounds, Color.White);
        }
        
        spriteBatch.ResetToDefaultUI(end: false);
    }

    /// <summary>画名字：先描一圈深色边，再用灰度缓慢起伏的渐变画正文。</summary>
    internal static void DrawName(MonoRowNameContext context)
    {
        SpriteBatch spriteBatch = context.Batch;
        DynamicSpriteFont font = context.Font;
        float scale = context.Scale;

        foreach (Vector2 offset in OutlineOffsets)
            spriteBatch.DrawString(font, context.Text, context.Position + offset, OutlineColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        if (!MonoShaderManager.TryGet(NameShaderName, out MonoShader? shader) || shader is null)
        {
            spriteBatch.DrawString(font, context.Text, context.Position, Color.LightGray, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
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

    /// <summary>把 UI 坐标的矩形换算成着色器要的屏幕矩形：xy 是尺寸，zw 是左上角。</summary>
    private static Vector4 SourceRect(Rectangle bounds)
    {
        Vector2 topLeft = Vector2.Transform(new Vector2(bounds.Left, bounds.Top), Main.UIScaleMatrix);
        Vector2 bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), Main.UIScaleMatrix);
        Vector2 size = bottomRight - topLeft;
        return new Vector4(size.X, size.Y, topLeft.X, topLeft.Y);
    }
}
