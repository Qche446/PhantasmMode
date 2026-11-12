namespace Monochrome.Core.UI;

/// <summary>
/// 原版 <c>UIPanel</c> 的九宫格底板，供行皮肤与自绘面板直接调用。
/// <para>
/// 与 <c>UIPanel.DrawPanel</c> 是同一套切分：四角按原尺寸画，四边沿一个轴拉伸，中心两个轴一起拉伸。
/// 贴图按 12px 圆角 + 4px 边条布局，因此必须是 28×28 见方；原版 <c>Images/UI/PanelBackground</c> 正是这个尺寸。
/// </para>
/// <para>
/// 批次与坐标空间由调用方负责，这里只按给定矩形落笔，不改任何绘制状态。
/// </para>
/// </summary>
public static class MonoPanel
{
    /// <summary>角块边长：贴图的圆角部分按 1:1 画。</summary>
    public const int CornerSize = 12;

    /// <summary>边条厚度：四边与中心夹在两层角块之间的那一条。</summary>
    public const int BarSize = 4;

    /// <summary>原版面板底板（<c>Images/UI/PanelBackground</c>，28×28）。</summary>
    public static Texture2D Background => Main.Assets.Request<Texture2D>("Images/UI/PanelBackground").Value;

    /// <summary>用原版底板画一块面板。</summary>
    /// <param name="spriteBatch">已经 <c>Begin</c> 过的批次。</param>
    /// <param name="bounds">面板矩形，UI 单位。</param>
    /// <param name="color">着色。</param>
    public static void Draw(SpriteBatch spriteBatch, Rectangle bounds, Color color)
        => Draw(spriteBatch, bounds, Background, color);

    /// <summary>用自定义底板画一块面板；贴图必须是 <c>(CornerSize + BarSize) * 2</c> 见方、布局与 <see cref="Background"/> 相同。</summary>
    /// <param name="spriteBatch">已经 <c>Begin</c> 过的批次。</param>
    /// <param name="bounds">面板矩形，UI 单位。</param>
    /// <param name="texture">九宫格底板贴图。</param>
    /// <param name="color">着色。</param>
    public static void Draw(SpriteBatch spriteBatch, Rectangle bounds, Texture2D texture, Color color)
    {
        int x = bounds.X;
        int y = bounds.Y;
        int right = bounds.Right - CornerSize;
        int bottom = bounds.Bottom - CornerSize;
        int middleWidth = bounds.Width - CornerSize * 2;
        int middleHeight = bounds.Height - CornerSize * 2;

        // 四角：贴图四角各取一块，原尺寸画。
        spriteBatch.Draw(texture, new Rectangle(x, y, CornerSize, CornerSize), new Rectangle(0, 0, CornerSize, CornerSize), color);
        spriteBatch.Draw(texture, new Rectangle(right, y, CornerSize, CornerSize), new Rectangle(CornerSize + BarSize, 0, CornerSize, CornerSize), color);
        spriteBatch.Draw(texture, new Rectangle(x, bottom, CornerSize, CornerSize), new Rectangle(0, CornerSize + BarSize, CornerSize, CornerSize), color);
        spriteBatch.Draw(texture, new Rectangle(right, bottom, CornerSize, CornerSize), new Rectangle(CornerSize + BarSize, CornerSize + BarSize, CornerSize, CornerSize), color);

        // 四边：只在一个轴上拉伸。
        spriteBatch.Draw(texture, new Rectangle(x + CornerSize, y, middleWidth, CornerSize), new Rectangle(CornerSize, 0, BarSize, CornerSize), color);
        spriteBatch.Draw(texture, new Rectangle(x + CornerSize, bottom, middleWidth, CornerSize), new Rectangle(CornerSize, CornerSize + BarSize, BarSize, CornerSize), color);
        spriteBatch.Draw(texture, new Rectangle(x, y + CornerSize, CornerSize, middleHeight), new Rectangle(0, CornerSize, CornerSize, BarSize), color);
        spriteBatch.Draw(texture, new Rectangle(right, y + CornerSize, CornerSize, middleHeight), new Rectangle(CornerSize + BarSize, CornerSize, CornerSize, BarSize), color);

        // 中心：两个轴一起拉伸。
        spriteBatch.Draw(texture, new Rectangle(x + CornerSize, y + CornerSize, middleWidth, middleHeight), new Rectangle(CornerSize, CornerSize, BarSize, BarSize), color);
    }
}
