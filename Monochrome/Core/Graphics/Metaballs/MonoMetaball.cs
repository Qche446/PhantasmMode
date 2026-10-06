namespace Monochrome.Core.Graphics.Metaballs;

/// <summary>
/// 一颗一次性提交的融合球：只有数据，没有行为。适合"每帧按当前情况算一下位置就交出去"的场合。
/// 想要自己的运动、缩放与生命周期，请用 <see cref="MonoMetaballInstance"/>。
/// <para>
/// 几何只有一个圆，"融合"来自多个球的影响力场相加后取等值面：<c>sum(Influence)</c> 超过阈值的
/// 地方连成一片，所以两个球靠近时会自然粘连。本结构只管数据与场函数，绘制由
/// <see cref="MonoMetaballManager"/> 负责。
/// </para>
/// </summary>
/// <param name="Position">球心（世界坐标）。</param>
/// <param name="Radius">可见半径：球看起来就是这么大。阈值只影响融合的积极性，不影响尺寸。</param>
/// <param name="Color">颜色。</param>
/// <param name="Strength">影响力缩放，默认 1。</param>
public readonly record struct MonoMetaball(Vector2 Position, float Radius, Color Color, float Strength = 1f)
{
    /// <summary>
    /// 归一化衰减：<c>clamp(1 - d / 支撑半径, 0, 1)</c>，其中支撑半径由
    /// <see cref="MonoMetaballSettings.SupportRadius"/> 从 <see cref="Radius"/> 放大而来。
    /// <b>不含 <see cref="Strength"/>，也不含手感幂</b>——这两个都在采样侧施加。
    /// </summary>
    /// <param name="point">采样点（世界坐标）。</param>
    /// <returns>[0,1] 的归一化衰减。</returns>
    public float Falloff(Vector2 point)
        => FalloffAt(Position, MonoMetaballManager.Settings.SupportRadius(Radius), point);

    /// <summary>
    /// 与 <see cref="Falloff"/> 同式的静态版本，供 <see cref="MonoMetaballInstance"/> 复用同一个公式。
    /// <b>注意这里的 <paramref name="radius"/> 是支撑半径，不是可见半径</b>——先过
    /// <see cref="MonoMetaballSettings.SupportRadius"/> 再传进来。
    /// </summary>
    /// <param name="center">球心。</param>
    /// <param name="radius">支撑半径。</param>
    /// <param name="point">采样点。</param>
    public static float FalloffAt(Vector2 center, float radius, Vector2 point)
    {
        if (radius <= 0f)
            return 0f;
        return MathHelper.Clamp(1f - Vector2.Distance(point, center) / radius, 0f, 1f);
    }

    /// <summary>
    /// 线性衰减的影响力：<c>Falloff(point) * Strength</c>。
    /// <para>
    /// 这是蓝图 §11.5 里的 <c>Influence1</c>——边界清晰、融合范围最窄。
    /// 想要更黏的手感是把 <see cref="Falloff"/> 取幂（<c>Influence2</c>/<c>Influence3</c>），
    /// 而那发生在采样侧（着色器 / <see cref="MonoMetaballManager.Sample"/>），不属于单个球的定义。
    /// </para>
    /// </summary>
    /// <param name="point">采样点（世界坐标）。</param>
    /// <returns>[0, Strength] 的影响力。</returns>
    public float Influence(Vector2 point) => Falloff(point) * Strength;
}
