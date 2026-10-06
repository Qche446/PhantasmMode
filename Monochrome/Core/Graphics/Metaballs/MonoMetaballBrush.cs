using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.Metaballs;

/// <summary>
/// 把球写进"当前累积目标"的一次写入上下文，由 <see cref="MonoMetaballManager"/> 在累积期间传入
/// <see cref="MonoMetaballInstance.DrawField"/>。
/// <para>
/// 外部<b>不能</b>构造它（构造函数是 internal）——它只在一次累积过程中有意义。
/// 当前只提供 <see cref="Ball"/> 一种写入方式；将来要支持椭圆/贴图形状时在这里扩展。
/// </para>
/// </summary>
public sealed class MonoMetaballBrush
{
    private MonoShader? shader;
    private MonoCircleSettings? settings;
    private int segments;

    // 被 settings 里的 lambda 捕获，于是每颗球只改字段、不用每颗球 new 一份 settings。
    private float radius;
    private Color color;

    internal MonoMetaballBrush() { }

    /// <summary>开始一轮写入。由管理器在累积前调用。</summary>
    internal void Begin(MonoShader shader, BlendState blendState, int segments)
    {
        this.shader = shader;
        this.segments = segments;
        settings = new(
            _ => radius,
            _ => color,
            shader,
            BlendState: blendState,
            Space: MonoGraphicsSpace.World);
    }

    /// <summary>往当前累积目标里写一颗球。</summary>
    /// <param name="center">球心（世界坐标）。</param>
    /// <param name="radius">可见半径：画出来就是这么大（内部按当前阈值放大成支撑半径）。非正数直接忽略。</param>
    /// <param name="color">颜色。</param>
    /// <param name="strength">影响力缩放，默认 1。</param>
    /// <param name="influencePower">影响力手感幂；1 = 线性。</param>
    public void Ball(Vector2 center, float radius, Color color, float strength = 1f, float influencePower = 1f)
    {
        if (shader is null || settings is null || radius <= 0f)
            return;

        // 可见半径 → 支撑半径：这样等值面恰好落在 radius 上，与阈值无关。
        this.radius = MonoMetaballManager.Settings.SupportRadius(radius);
        this.color = color;
        shader.SetParameter("uStrength", strength)
              .SetParameter("uInfluencePower", Math.Max(0.0001f, influencePower));
        MonoPrimitiveRenderer.RenderCircle(center, settings, segments);
    }
}
