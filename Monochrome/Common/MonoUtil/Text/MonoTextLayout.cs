namespace Monochrome.Common.MonoUtil.Text;

/// <summary>
/// 多行文字的排版算术：把一块文字按锚点摆到某个点上。
/// <para>
/// <b>不引用任何 Terraria 类型</b>，能直接进离线验收台——这里全是纯算术，而"锚点算错"是那种
/// 只在特定缩放与行数下才看得出来的毛病，正适合用用例钉住。
/// </para>
/// <para>
/// <b>它管什么。</b>行宽由游戏侧的字库算好之后，这里只回答"第几行该画在哪"。两行之间的纵向推进是
/// <c>行高 + 额外行距</c>；横向各行的锚点都落在同一个 <c>position.X</c> 上，意思是
/// <b>每一行各按自己的宽度对齐</b>，而不是整块按最宽那行左对齐之后各行齐头。
/// </para>
/// </summary>
public static class MonoTextLayout
{
    /// <summary>整块的高度：<c>行数 × 行高 + (行数 − 1) × 额外行距</c>。没有行时是 0。</summary>
    /// <param name="lineCount">行数。</param>
    /// <param name="lineHeight">单行高度（缩放之后）。</param>
    /// <param name="extraLineSpacing">行与行之间额外加的像素。</param>
    public static float BlockHeight(int lineCount, float lineHeight, float extraLineSpacing)
    {
        if (lineCount <= 0)
            return 0f;

        if (lineCount == 1)
            return lineHeight;

        return lineCount * lineHeight + (lineCount - 1) * extraLineSpacing;
    }

    /// <summary>
    /// 逐行算出"锚点该落的那个点"的 Y，写进 <paramref name="destination"/>。
    /// <para>
    /// 返回值是实际写了几行（取行数与目标长度里小的那个）。<c>anchorY</c> 是 <c>[0,1]</c> 的竖直对齐：
    /// 0 = 整块顶边贴住 <paramref name="positionY"/>，1 = 底边贴住。
    /// </para>
    /// </summary>
    /// <param name="destination">逐行 Y 的落点。</param>
    /// <param name="lineCount">行数。</param>
    /// <param name="lineHeight">单行高度（缩放之后）。</param>
    /// <param name="extraLineSpacing">行与行之间额外加的像素。</param>
    /// <param name="anchorY">竖直锚点，<c>[0,1]</c>；超出范围按夹取处理。</param>
    /// <param name="positionY">锚点要落在的 Y。</param>
    public static int FillLineY(Span<float> destination, int lineCount, float lineHeight, float extraLineSpacing,
        float anchorY, float positionY)
    {
        int count = Math.Min(lineCount, destination.Length);
        if (count <= 0)
            return 0;

        anchorY = Math.Clamp(anchorY, 0f, 1f);
        float top = positionY - anchorY * BlockHeight(count, lineHeight, extraLineSpacing);

        for (int i = 0; i < count; i++)
            destination[i] = top + anchorY * lineHeight + i * (lineHeight + extraLineSpacing);

        return count;
    }
}
