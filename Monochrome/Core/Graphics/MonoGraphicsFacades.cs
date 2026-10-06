using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Graphics.RenderTargets;
using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics;

/// <summary>
/// 渲染目标的短别名。渲染目标一律走池化或受管生命周期，不要随手 <c>new RenderTarget2D</c>。
/// </summary>
public static class MonoRt
{
    /// <summary>从池中借一块渲染目标，释放租约即归还。专用服务器上返回 null，调用方必须判空。</summary>
    public static MonoRenderTargetPool.Lease? Rent(int width, int height, SurfaceFormat format = SurfaceFormat.Color, DepthFormat depth = DepthFormat.None)
        => MonoRenderTargetPool.Rent(width, height, format, depth);

    /// <summary>建一块跟随屏幕分辨率的受管渲染目标：首次访问时创建，分辨率变化时自动重建。</summary>
    public static MonoManagedRenderTarget ScreenSized(bool resetOnResize = true, bool collectWhenIdle = true)
        => new(MonoManagedRenderTarget.CreateScreenSizedTarget, resetOnResize, collectWhenIdle);

    /// <summary>用自定义工厂建一块受管渲染目标。</summary>
    public static MonoManagedRenderTarget Create(Func<int, int, RenderTarget2D> initializer, bool resetOnResize = true, bool collectWhenIdle = true)
        => new(initializer, resetOnResize, collectWhenIdle);
}

/// <summary>
/// 图元绘制的短别名，逐个转发到 <see cref="MonoPrimitiveRenderer"/>。
/// <para>
/// 这些是绕过 <see cref="SpriteBatch"/> 的原始绘制，必须落在 <c>spriteBatch.End()</c> 与下一次 <c>Begin()</c>
/// 之间；批量状态用 <see cref="MonoSpriteBatchExtensions"/> 的助手处理。参数的单位说明与行为细节见
/// <see cref="MonoPrimitiveRenderer"/> 上的同名方法。
/// </para>
/// </summary>
public static class MonoPrim
{
    #region 网格接口

    /// <summary>把点列渲染成一条一次性提交的索引三角形带。<c>pointsToCreate</c> 是重采样后的点数，默认取输入点数。</summary>
    public static void RenderTrail(IReadOnlyList<Vector2> positions, MonoPrimitiveSettings settings, int? pointsToCreate = null)
        => MonoPrimitiveRenderer.RenderTrail(positions, settings, pointsToCreate);

    /// <summary><see cref="RenderTrail(IReadOnlyList{Vector2}, MonoPrimitiveSettings, int?)"/> 的惰性序列版本。</summary>
    public static void RenderTrail(IEnumerable<Vector2> positions, MonoPrimitiveSettings settings, int? pointsToCreate = null)
        => MonoPrimitiveRenderer.RenderTrail(positions, settings, pointsToCreate);

    /// <summary>渲染实心圆（三角形扇），<c>sideCount</c> 是切片数。</summary>
    public static void RenderCircle(Vector2 center, MonoCircleSettings settings, int sideCount = 128)
        => MonoPrimitiveRenderer.RenderCircle(center, settings, sideCount);

    /// <summary>渲染圆形描边（圆环带），<c>pointCount</c> 是分段数。</summary>
    public static void RenderCircleEdge(Vector2 center, MonoCircleEdgeSettings settings, int pointCount = 128)
        => MonoPrimitiveRenderer.RenderCircleEdge(center, settings, pointCount);

    /// <summary>
    /// 渲染一个居中的贴图四边形（可旋转）。
    /// <b>把四边形画进非全屏渲染目标时，必须给出 <c>projectionWidth</c> / <c>projectionHeight</c></b>，
    /// 否则投影按屏幕尺寸算，只会盖住目标的一部分。
    /// </summary>
    public static void RenderQuad(Texture2D texture, Vector2 center, Vector2 scale, float rotation, Color color, MonoShader? shader = null, BlendState? blendState = null, MonoGraphicsSpace space = MonoGraphicsSpace.World, int? projectionWidth = null, int? projectionHeight = null)
        => MonoPrimitiveRenderer.RenderQuad(texture, center, scale, rotation, color, shader, blendState, space, projectionWidth, projectionHeight);

    #endregion

    #region 即时图元（蓝图 §11.8）

    /// <summary>画一条线段，<c>width</c> 是线宽（像素）。</summary>
    public static void DrawLine(Vector2 start, Vector2 end, Color color, float width = 1f, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => MonoPrimitiveRenderer.DrawLine(start, end, color, width, space);

    /// <summary>画一个空心圆，<c>radius</c> 是半径，<c>width</c> 是线宽。</summary>
    public static void DrawCircle(Vector2 center, float radius, Color color, float width = 1f, int segments = 32, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => MonoPrimitiveRenderer.DrawCircle(center, radius, color, width, segments, space);

    /// <summary>画一段圆弧，<c>sweepAngle</c> 可正可负。</summary>
    public static void DrawArc(Vector2 center, float radius, float startAngle, float sweepAngle, Color color, float width = 1f, int segments = 24, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => MonoPrimitiveRenderer.DrawArc(center, radius, startAngle, sweepAngle, color, width, segments, space);

    /// <summary>画一个实心胶囊。</summary>
    public static void DrawCapsule(Vector2 start, Vector2 end, float radius, Color color, int capSegments = 16, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => MonoPrimitiveRenderer.DrawCapsule(start, end, radius, color, capSegments, space);

    /// <summary>画一条虚线（整条一次提交）。<c>dashLength</c> 是段长、<c>gapLength</c> 是空隙，都用像素。</summary>
    public static void DrawDashedLine(Vector2 start, Vector2 end, float dashLength, float gapLength, Color color, float thickness = 1f, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => MonoPrimitiveRenderer.DrawDashedLine(start, end, dashLength, gapLength, color, thickness, space);

    /// <summary>填充一个简单多边形（凹凸均可，不允许自交）。</summary>
    public static void FillPolygon(IReadOnlyList<Vector2> points, Color color, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => MonoPrimitiveRenderer.FillPolygon(points, color, space);

    /// <summary>画多边形的描边；<c>closed</c> 决定首尾是否相连。</summary>
    public static void StrokePolygon(IReadOnlyList<Vector2> points, Color color, float thickness = 1f, bool closed = true, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => MonoPrimitiveRenderer.StrokePolygon(points, color, thickness, closed, space);

    /// <summary>画一条任意次贝塞尔曲线，控制点至少 2 个。</summary>
    public static void DrawBezier(IReadOnlyList<Vector2> controlPoints, Color color, float thickness = 1f, int segments = 24, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => MonoPrimitiveRenderer.DrawBezier(controlPoints, color, thickness, segments, space);

    /// <summary>画一个四角异色的矩形渐变。</summary>
    public static void DrawGradientRect(Vector2 position, Vector2 size, Color topLeft, Color topRight, Color bottomLeft, Color bottomRight, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => MonoPrimitiveRenderer.DrawGradientRect(position, size, topLeft, topRight, bottomLeft, bottomRight, space);

    /// <summary>画一个四角异色的矩形渐变（整数矩形重载）。</summary>
    public static void DrawGradientRect(Rectangle rectangle, Color topLeft, Color topRight, Color bottomLeft, Color bottomRight, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => MonoPrimitiveRenderer.DrawGradientRect(rectangle, topLeft, topRight, bottomLeft, bottomRight, space);

    #endregion
}
