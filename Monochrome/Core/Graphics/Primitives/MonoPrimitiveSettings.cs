using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.Primitives;

/// <summary>
/// GPU 拖尾的配置。位置属于哪个坐标空间由 <see cref="Space"/> 决定。
/// </summary>
/// <param name="WidthFunction">沿轨迹的宽度函数，入参 <c>t ∈ [0,1]</c>（0 为尾巴，1 为头部），返回该处的条带全宽（像素）；条带以中心线为轴向两侧各展开一半。</param>
/// <param name="ColorFunction">沿轨迹的颜色函数，入参同上；颜色由 GPU 在顶点间插值。</param>
/// <param name="OffsetFunction">可选的位置偏移，在减去 <c>Main.screenPosition</c> 之前加在世界坐标上，适合做整体摆动而不改传入的点列。</param>
/// <param name="Smoothen">是否用 Catmull-Rom 样条重采样点列。点列稀疏时开（默认）；关掉按最近索引取样、不插值，省算力。</param>
/// <param name="Shader">自定义图元着色器；为 <see langword="null"/> 时走内置 <c>BasicEffect</c> 回退（只用顶点色）。</param>
/// <param name="ProjectionWidth">投影区域宽度，默认 <c>Main.screenWidth</c>；只有画进非全屏渲染目标时才需要改。</param>
/// <param name="ProjectionHeight">投影区域高度，默认 <c>Main.screenHeight</c>。</param>
/// <param name="UseUnscaledMatrix">强制使用不含缩放的正交投影（像素化图元）。它只覆盖矩阵，不改坐标换算：<see cref="MonoGraphicsSpace.World"/> 依旧减 <c>Main.screenPosition</c>。</param>
/// <param name="BlendState">混合状态，默认 <see cref="BlendState.AlphaBlend"/>。</param>
/// <param name="InitialVertexPositionsOverride">用一对显式位置替换第一个路径点的左右顶点（按 <see cref="Space"/> 给出的坐标），让拖尾头部对齐到精确位置而不由法线推出；两个分量都为零时视为未设置。</param>
/// <param name="Space">传入位置所属的坐标空间，默认 <see cref="MonoGraphicsSpace.World"/>。</param>
public sealed record MonoPrimitiveSettings(
    Func<float, float> WidthFunction,
    Func<float, Color> ColorFunction,
    Func<float, Vector2>? OffsetFunction = null,
    bool Smoothen = true,
    MonoShader? Shader = null,
    int? ProjectionWidth = null,
    int? ProjectionHeight = null,
    bool UseUnscaledMatrix = false,
    BlendState? BlendState = null,
    (Vector2 Left, Vector2 Right)? InitialVertexPositionsOverride = null,
    MonoGraphicsSpace Space = MonoGraphicsSpace.World)
{
    /// <summary>等宽单色拖尾的快捷构造：整条轨迹用同一个宽度与颜色。</summary>
    /// <param name="width">条带全宽（像素）。</param>
    /// <param name="color">整条轨迹的颜色。</param>
    /// <param name="shader">可选的自定义着色器。</param>
    /// <param name="space">坐标空间。</param>
    public static MonoPrimitiveSettings Solid(float width, Color color, MonoShader? shader = null, MonoGraphicsSpace space = MonoGraphicsSpace.World)
        => new(_ => width, _ => color, Shader: shader, Space: space);
}

/// <summary>实心圆的配置（三角形扇填充）。</summary>
/// <param name="RadiusFunction">沿角度的半径函数，入参 <c>t ∈ [0,1]</c> 对应 <c>[0, 2π)</c>。可做心形/星形等变半径圆。</param>
/// <param name="ColorFunction">沿角度的颜色函数，入参同上。</param>
/// <param name="Shader">自定义着色器，默认走顶点色回退路径。</param>
/// <param name="ProjectionWidth">投影区域宽度，默认 <c>Main.screenWidth</c>。</param>
/// <param name="ProjectionHeight">投影区域高度，默认 <c>Main.screenHeight</c>。</param>
/// <param name="UseUnscaledMatrix">强制使用不含缩放的投影，见 <see cref="MonoPrimitiveSettings.UseUnscaledMatrix"/>。</param>
/// <param name="BlendState">混合状态，默认 <see cref="BlendState.AlphaBlend"/>。</param>
/// <param name="Space">坐标空间，只有 <see cref="MonoGraphicsSpace.World"/> 会减去 <c>Main.screenPosition</c>。</param>
public sealed record MonoCircleSettings(
    Func<float, float> RadiusFunction,
    Func<float, Color> ColorFunction,
    MonoShader? Shader = null,
    int? ProjectionWidth = null,
    int? ProjectionHeight = null,
    bool UseUnscaledMatrix = false,
    BlendState? BlendState = null,
    MonoGraphicsSpace Space = MonoGraphicsSpace.World);

/// <summary>圆形描边（圆环带）的配置。</summary>
/// <param name="EdgeWidthFunction">沿角度的环厚函数，入参 <c>t ∈ [0,1]</c>。环带由内径向外径单侧展开。</param>
/// <param name="RadiusFunction">沿角度的内径函数，入参同上。</param>
/// <param name="ColorFunction">沿角度的颜色函数，入参同上。</param>
/// <param name="Shader">自定义着色器，默认走顶点色回退路径。</param>
/// <param name="ProjectionWidth">投影区域宽度，默认 <c>Main.screenWidth</c>。</param>
/// <param name="ProjectionHeight">投影区域高度，默认 <c>Main.screenHeight</c>。</param>
/// <param name="UseUnscaledMatrix">强制使用不含缩放的投影，见 <see cref="MonoPrimitiveSettings.UseUnscaledMatrix"/>。</param>
/// <param name="BlendState">混合状态，默认 <see cref="BlendState.AlphaBlend"/>。</param>
/// <param name="Space">坐标空间，只有 <see cref="MonoGraphicsSpace.World"/> 会减去 <c>Main.screenPosition</c>。</param>
public sealed record MonoCircleEdgeSettings(
    Func<float, float> EdgeWidthFunction,
    Func<float, float> RadiusFunction,
    Func<float, Color> ColorFunction,
    MonoShader? Shader = null,
    int? ProjectionWidth = null,
    int? ProjectionHeight = null,
    bool UseUnscaledMatrix = false,
    BlendState? BlendState = null,
    MonoGraphicsSpace Space = MonoGraphicsSpace.World);
