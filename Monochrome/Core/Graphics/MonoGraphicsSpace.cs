namespace Monochrome.Core.Graphics;

/// <summary>
/// 绘制所用的坐标空间。<b>每个绘制 API 都必须显式声明它，不允许靠约定。</b>
/// <para>
/// 把世界坐标直接丢给 UI 绘制（或反过来）会导致"分辨率一变、缩放一改，特效就飘走"，
/// 这是图形代码最常见的 bug。因此这个枚举不是装饰：它同时决定
/// <b>是否减去 <c>Main.screenPosition</c></b> 与<b>用哪个投影矩阵</b>。
/// </para>
/// </summary>
public enum MonoGraphicsSpace
{
    /// <summary>
    /// 世界空间。传入的是世界坐标，渲染前会减去 <c>Main.screenPosition</c>；
    /// 投影矩阵包含 <c>Main.GameViewMatrix.Zoom</c> 与反转重力的翻转，与 Luminance 的
    /// <c>CalculatePrimitiveMatrices</c> 逐行一致。
    /// </summary>
    World,

    /// <summary>
    /// 屏幕空间。传入的已经是屏幕像素坐标（左上角为原点、Y 向下），
    /// <b>不会</b>再减去 <c>Main.screenPosition</c>；投影矩阵是不含缩放的
    /// <c>CreateOrthographicOffCenter(0, width, height, 0, -1, 1)</c>。
    /// 适合在主 SpriteBatch 之外直接按像素摆放的调试可视化。
    /// </summary>
    Screen,

    /// <summary>
    /// UI 空间。传入的是 UI 坐标，先乘 <c>Main.UIScaleMatrix</c> 换算到屏幕像素，
    /// 再套用与 <see cref="Screen"/> 相同的正交投影；同样不减去 <c>Main.screenPosition</c>。
    /// <b>必须在 UI 绘制阶段调用</b>（<c>Main.UIScaleMatrix</c> 由绘制循环写入），
    /// 否则会用到上一帧的值。
    /// </summary>
    UI
}
