using Monochrome.Common.MonoUtil.Text;
using ReLogic.Graphics;
using Terraria.GameContent;

namespace Monochrome.Core.Graphics.Text;

/// <summary>
/// 文字绘制：测量、锚点、多行、描边与阴影，以及换行 / 裁切 / 打字机三段字符串处理。
/// <para>
/// <b>它解决什么。</b>原版给的手段是裸的 <c>Utils.DrawBorderStringFourWay</c>：位置、锚点、多行、
/// 行距都得自己算，而 <c>origin</c> 到底该不该乘缩放这件事每个调用点都会重新猜一遍。这里把"一段文字
/// 按锚点摆在某个点上"收成一次调用，排版算术在 <see cref="MonoTextLayout"/> 里。
/// </para>
/// <para>
/// <b>坐标系由调用方决定。</b>传 <see cref="MonoGraphicsSpace.World"/> 时这里会先减去
/// <c>Main.screenPosition</c>；<see cref="MonoGraphicsSpace.Screen"/> 与 <see cref="MonoGraphicsSpace.UI"/>
/// 按原样用。批次由调用方开好——<b>本类从不 Begin/End</b>，界面相位与它天然相容。
/// </para>
/// <para>
/// <b>它是无状态的</b>：没有缓存、没有静态字段，所以不需要挂 <c>Unload</c> 清理。单行绘制不分配；
/// 多行会按行数分配一个 Y 数组，并逐行 <c>Substring</c>。
/// </para>
/// </summary>
public static class MonoText
{
    /// <summary>界面正文字体。样式里没给字体时用它。</summary>
    public static DynamicSpriteFont DefaultFont => FontAssets.MouseText.Value;

    /// <summary>取这次绘制要用的字体。</summary>
    /// <param name="style">样式。</param>
    public static DynamicSpriteFont Resolve(in MonoTextStyle style) => style.Font ?? DefaultFont;

    /// <summary>
    /// 一段文字的尺寸（<b>缩放之后</b>）：宽度取最宽那行，高度是整块。
    /// 空串返回 <see cref="Vector2.Zero"/>。
    /// </summary>
    /// <param name="text">文字，可以含 <c>\n</c>。</param>
    /// <param name="style">样式。</param>
    public static Vector2 Measure(string? text, in MonoTextStyle style)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        // 抄一份到局部：in 参数不能被 lambda 捕获（多行那条路要用闭包），抄一次同时也省掉了重复的解引用。
        MonoTextStyle local = style;
        DynamicSpriteFont font = Resolve(in local);
        int lineCount = CountLines(text);
        if (lineCount == 1)
            return font.MeasureString(text) * local.Scale;

        float widest = 0f;
        ForEachLine(text, line => widest = Math.Max(widest, font.MeasureString(line).X * local.Scale));

        float lineHeight = ScaledLineHeight(font, in local);
        return new Vector2(widest, MonoTextLayout.BlockHeight(lineCount, lineHeight, local.ExtraLineSpacing));
    }

    /// <summary>
    /// 把一段文字按锚点画出来，返回它的尺寸（缩放之后）。
    /// <para>文字里的 <c>\n</c> 按换行处理：每一行各按 <see cref="MonoTextStyle.Anchor"/> 的横向分量对齐，
    /// 纵向按整块对齐。</para>
    /// </summary>
    /// <param name="spriteBatch">已经 <c>Begin</c> 的批次。</param>
    /// <param name="text">文字。</param>
    /// <param name="position">锚点要落在的位置。</param>
    /// <param name="style">样式。</param>
    /// <param name="space">坐标空间；<c>World</c> 会先减去 <c>Main.screenPosition</c>。</param>
    public static Vector2 Draw(SpriteBatch spriteBatch, string? text, Vector2 position, in MonoTextStyle style,
        MonoGraphicsSpace space = MonoGraphicsSpace.UI)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);

        if (Main.dedServ || string.IsNullOrEmpty(text))
            return Vector2.Zero;

        MonoTextStyle local = style;
        DynamicSpriteFont font = Resolve(in local);
        Vector2 at = space == MonoGraphicsSpace.World ? position - Main.screenPosition : position;

        int lineCount = CountLines(text);
        if (lineCount == 1)
        {
            Vector2 size = font.MeasureString(text);
            DrawLine(spriteBatch, font, text, at, size * local.Anchor, in local);
            return size * local.Scale;
        }

        float lineHeight = ScaledLineHeight(font, in local);
        float[] ys = new float[lineCount];
        MonoTextLayout.FillLineY(ys, lineCount, lineHeight, local.ExtraLineSpacing, local.Anchor.Y, at.Y);

        float widest = 0f;
        int index = 0;
        ForEachLine(text, line =>
        {
            Vector2 size = font.MeasureString(line);
            widest = Math.Max(widest, size.X * local.Scale);
            DrawLine(spriteBatch, font, line, new Vector2(at.X, ys[index]), size * local.Anchor, in local);
            index++;
        });

        return new Vector2(widest, MonoTextLayout.BlockHeight(lineCount, lineHeight, local.ExtraLineSpacing));
    }

    /// <summary>
    /// 按宽度自动换行，返回一段插好了 <c>\n</c> 的文字。断行规则由字库自己决定（英文按词、中日韩按字）。
    /// </summary>
    /// <param name="text">原文。</param>
    /// <param name="maxWidth">最大宽度（<b>缩放之后</b>的像素）。</param>
    /// <param name="style">样式。</param>
    public static string Wrap(string? text, float maxWidth, in MonoTextStyle style)
        => Resolve(style).CreateWrappedText(text ?? string.Empty, Unscaled(maxWidth, in style));

    /// <summary>按宽度裁切，放不下的部分换成省略号。</summary>
    /// <param name="text">原文。</param>
    /// <param name="maxWidth">最大宽度（<b>缩放之后</b>的像素）。</param>
    /// <param name="style">样式。</param>
    public static string Crop(string? text, float maxWidth, in MonoTextStyle style)
        => Resolve(style).CreateCroppedText(text ?? string.Empty, Unscaled(maxWidth, in style));

    /// <summary>
    /// 打字机：只取前 <paramref name="visibleChars"/> 个字符。按字符数算，中文与英文一样，
    /// 超出范围会自动夹进 <c>[0, 长度]</c>。
    /// <para>原版的 <c>Utils.DrawBorderString</c> 也带一个"最多显示几个字"的参数，但那里
    /// <c>Substring</c> 的结果被丢掉了，实际不生效——所以这里自己切。</para>
    /// </summary>
    /// <param name="text">原文。</param>
    /// <param name="visibleChars">要显示几个字符。</param>
    public static string Visible(string? text, int visibleChars)
    {
        text ??= string.Empty;
        int count = Math.Clamp(visibleChars, 0, text.Length);
        return count == text.Length ? text : text[..count];
    }

    /// <summary>一行的缩放后高度。</summary>
    private static float ScaledLineHeight(DynamicSpriteFont font, in MonoTextStyle style)
        => MathF.Max(1f, font.LineSpacing * style.Scale);

    /// <summary>把宽度从"缩放之后"换回"字库自己的单位"。</summary>
    private static float Unscaled(float width, in MonoTextStyle style)
        => MathF.Max(1f, width / MathF.Max(0.0001f, style.Scale));

    /// <summary>画一行：先阴影，再描边或本体。</summary>
    private static void DrawLine(SpriteBatch spriteBatch, DynamicSpriteFont font, string line, Vector2 at,
        Vector2 origin, in MonoTextStyle style)
    {
        if (style.Shadow)
        {
            spriteBatch.DrawString(font, line, at + new Vector2(style.ShadowOffset), style.ShadowColor, 0f, origin,
                style.Scale, SpriteEffects.None, 0f);
        }

        if (style.Outline)
        {
            Utils.DrawBorderStringFourWay(spriteBatch, font, line, at.X, at.Y, style.Color, style.OutlineColor, origin,
                style.Scale);
            return;
        }

        spriteBatch.DrawString(font, line, at, style.Color, 0f, origin, style.Scale, SpriteEffects.None, 0f);
    }

    private static int CountLines(string text)
    {
        int count = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
                count++;
        }
        return count;
    }

    /// <summary>逐行调用 <paramref name="action"/>，行尾的 <c>\r</c> 不算进内容。每一行会复制一个子串。</summary>
    private static void ForEachLine(string text, Action<string> action)
    {
        int start = 0;
        for (int i = 0; i <= text.Length; i++)
        {
            if (i != text.Length && text[i] != '\n')
                continue;

            int length = i - start;
            if (length > 0 && text[start + length - 1] == '\r')
                length--;

            action(length == 0 ? string.Empty : text.Substring(start, length));
            start = i + 1;
        }
    }
}
