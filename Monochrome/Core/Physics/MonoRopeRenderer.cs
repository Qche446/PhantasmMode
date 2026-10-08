using Monochrome.Common.MonoUtil.Physics;
using Monochrome.Core.Graphics;
using Monochrome.Core.Graphics.Primitives;

namespace Monochrome.Core.Physics;

/// <summary>
/// 绳索的绘制：把位置列喂给图元层的条带渲染，一次提交画完整条。
/// <para>
/// <b>调用时机</b>：绕过 SpriteBatch 的原始绘制必须落在一次 <c>End()</c> 与下一次 <c>Begin()</c> 之间。
/// 直接画在屏幕上时用 <c>On_Main.DrawDust</c>（与粒子层、Metaball 层同一个时机）；
/// 在 <c>PlayerDrawLayer</c> 之类批次已经开着的地方调用会破坏那一批绘制。
/// 需要同时管住图层顺序与染料着色器时，走 <see cref="DrawToTarget"/> 先画进渲染目标，
/// 再用普通的 <c>DrawData</c> 把渲染目标当贴图画上去。
/// </para>
/// <para>
/// <b>方向</b>：<c>t = 编号 / (段数 − 1)</c>，不做反向。锚点在中间的绳索上，
/// 锚点的位置就是 <see cref="MonoRope.AnchorRatio"/>；<c>t</c> 不作为"头/尾"解释。
/// </para>
/// </summary>
public static class MonoRopeRenderer
{
    private static readonly List<Vector2> scratch = [];

    /// <summary>把一条绳索画成条带。</summary>
    /// <param name="rope">要绘制的绳索。</param>
    /// <param name="settings">条带配置。</param>
    /// <param name="pointsToCreate">重采样点数，默认与段数相同。</param>
    public static void Draw(MonoRope rope, MonoPrimitiveSettings settings, int? pointsToCreate = null)
    {
        if (Main.dedServ || rope is null || settings is null)
            return;

        FillScratch(rope, Vector2.Zero);
        MonoPrimitiveRenderer.RenderTrail(scratch, settings, pointsToCreate);
    }

    /// <summary>
    /// 把一条绳索画进一张渲染目标，坐标是"点列减去 <paramref name="origin"/>"的局部坐标。
    /// <para>
    /// 它只负责画，<b>切换与恢复渲染目标、清屏由调用方做</b>——那是绘制状态的所有权，不该藏在渲染器里。
    /// <see cref="MonoPrimitiveSettings.ProjectionWidth"/> / <see cref="MonoPrimitiveSettings.ProjectionHeight"/>
    /// 与坐标空间会被覆盖成"以渲染目标为投影区域的屏幕空间"。
    /// </para>
    /// <para>
    /// 条带用预乘 alpha 的 <see cref="BlendState.AlphaBlend"/> 画进目标，之后再以普通贴图绘制回去时
    /// 用同一个混合状态即可，不会把 alpha 乘两遍。
    /// </para>
    /// </summary>
    /// <param name="rope">要绘制的绳索。</param>
    /// <param name="target">目标渲染目标。</param>
    /// <param name="origin">渲染目标左上角在点列所在坐标系里的位置。</param>
    /// <param name="settings">条带配置。</param>
    /// <param name="pointsToCreate">重采样点数。</param>
    public static void DrawToTarget(MonoRope rope, RenderTarget2D target, Vector2 origin, MonoPrimitiveSettings settings, int? pointsToCreate = null)
    {
        if (Main.dedServ || rope is null || target is null || settings is null || target.IsDisposed)
            return;

        FillScratch(rope, origin);

        MonoPrimitiveSettings local = settings with
        {
            Space = MonoGraphicsSpace.Screen,
            UseUnscaledMatrix = false,
            ProjectionWidth = target.Width,
            ProjectionHeight = target.Height,
        };

        MonoPrimitiveRenderer.RenderTrail(scratch, local, pointsToCreate);
    }

    /// <summary>等宽单色绳索的快捷绘制。</summary>
    /// <param name="rope">要绘制的绳索。</param>
    /// <param name="width">条带全宽（像素）。</param>
    /// <param name="color">整条的颜色。</param>
    /// <param name="space">坐标空间，默认世界。</param>
    public static void Draw(MonoRope rope, float width, Color color, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => Draw(rope, MonoPrimitiveSettings.Solid(width, color, space: space));

    /// <summary>把点列按编号顺序写进复用列表，同时减去 <paramref name="origin"/>。</summary>
    private static void FillScratch(MonoRope rope, Vector2 origin)
    {
        Vector2[] points = rope.Points;
        scratch.Clear();
        for (int i = 0; i < points.Length; i++)
            scratch.Add(points[i] - origin);
    }
}
