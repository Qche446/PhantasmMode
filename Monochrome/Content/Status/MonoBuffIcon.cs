using System.Globalization;
using ReLogic.Graphics;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace Monochrome.Content.Status;

/// <summary>
/// 状态图标上的附加绘制。
/// <para>
/// tML 把图标绘制交给 <see cref="ModBuff.PreDraw"/> 与 <see cref="ModBuff.PostDraw"/>，
/// 参数是 <see cref="BuffDrawParams"/>。这里只提供那两处最常要的东西：一个贴住图标右下角的锚点，
/// 以及把一段文字画上去的方法。<b>层数从哪来、要不要画，由调用方决定</b>，库不猜。
/// </para>
/// <para>
/// 原版把「剩余时间」画在图标<b>下方</b>（<see cref="BuffDrawParams.TextPosition"/>），而且是在
/// <see cref="ModBuff.PostDraw"/> 之后才画，所以这里的右下角角标不会和它重叠。
/// </para>
/// <para>
/// 要接自己的着色器就直接在 <c>PostDraw</c> 里画，那里拿得到 <see cref="SpriteBatch"/>。
/// 注意它跑在原版界面批次中间，换 Effect 需要自己 Begin / End，代价见模块 README。
/// </para>
/// </summary>
public static class MonoBuffIcon
{
    /// <summary>角标的对齐点：图标右下角往里缩一点。</summary>
    /// <param name="drawParams">图标绘制的参数。</param>
    public static Vector2 BadgeAnchor(in BuffDrawParams drawParams)
        => new(drawParams.Position.X + drawParams.SourceRectangle.Width - 6f,
               drawParams.Position.Y + drawParams.SourceRectangle.Height - 2f);

    /// <summary>把一段文字画在图标右下角，文字的右下角贴住 <see cref="BadgeAnchor"/>。</summary>
    /// <param name="spriteBatch">绘制目标。</param>
    /// <param name="drawParams">图标绘制的参数。</param>
    /// <param name="text">要画的文字；空串时什么都不做。</param>
    /// <param name="scale">字号缩放。</param>
    /// <param name="textColor">文字颜色，默认白色。</param>
    /// <param name="outlineColor">描边颜色，默认黑色。</param>
    public static void DrawBadge(SpriteBatch spriteBatch, in BuffDrawParams drawParams, string text,
        float scale = 0.75f, Color? textColor = null, Color? outlineColor = null)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);

        if (string.IsNullOrEmpty(text))
            return;

        DynamicSpriteFont font = FontAssets.ItemStack.Value;
        Vector2 origin = font.MeasureString(text) * scale;
        Vector2 anchor = BadgeAnchor(drawParams);

        Utils.DrawBorderStringFourWay(spriteBatch, font, text, anchor.X, anchor.Y,
            textColor ?? Color.White, outlineColor ?? Color.Black, origin, scale);
    }

    /// <summary>把层数画成阿拉伯数字角标。层数小于 2 时什么都不画。</summary>
    /// <param name="spriteBatch">绘制目标。</param>
    /// <param name="drawParams">图标绘制的参数。</param>
    /// <param name="count">层数。</param>
    /// <param name="scale">字号缩放。</param>
    /// <param name="textColor">文字颜色，默认白色。</param>
    /// <param name="outlineColor">描边颜色，默认黑色。</param>
    public static void DrawCount(SpriteBatch spriteBatch, in BuffDrawParams drawParams, int count,
        float scale = 0.75f, Color? textColor = null, Color? outlineColor = null)
    {
        if (count < 2)
            return;

        DrawBadge(spriteBatch, drawParams, count.ToString(CultureInfo.InvariantCulture), scale, textColor, outlineColor);
    }

    /// <summary>把层数画成罗马数字角标。层数小于 2 时什么都不画。</summary>
    /// <param name="spriteBatch">绘制目标。</param>
    /// <param name="drawParams">图标绘制的参数。</param>
    /// <param name="count">层数。</param>
    /// <param name="scale">字号缩放。</param>
    /// <param name="textColor">文字颜色，默认白色。</param>
    /// <param name="outlineColor">描边颜色，默认黑色。</param>
    public static void DrawRomanCount(SpriteBatch spriteBatch, in BuffDrawParams drawParams, int count,
        float scale = 0.75f, Color? textColor = null, Color? outlineColor = null)
    {
        if (count < 2)
            return;

        DrawBadge(spriteBatch, drawParams, MonoRoman.ToRoman(count), scale, textColor, outlineColor);
    }
}
