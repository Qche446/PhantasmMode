namespace Monochrome.Common.MonoUtil;

/// <summary>
/// 发射器把粒子往哪儿放。<b>它只管位置，不管朝向</b>——朝向一律由 <see cref="MonoEmitPattern.Direction"/>
/// 按"基准朝向 ± 散射半角"决定，两个维度可以自由组合。
/// </summary>
public enum MonoEmitShape
{
    /// <summary>就放在发射点上，半径不起作用。</summary>
    Point,

    /// <summary>在半径 <c>radius</c> 的圆盘里均匀取一点。</summary>
    Circle,

    /// <summary>落在半径 <c>radius</c> 的圆周上（圆环发射口）。</summary>
    Ring,
}

/// <summary>
/// 发射的取点与取朝向算术。<b>不引用任何 Terraria 类型，能直接进离线验收台。</b>
/// <para>
/// 随机数走的是注入的 <c>Func&lt;float&gt;</c>（返回 <c>[0,1)</c>），不是写死某个随机源：
/// 游戏侧传 <c>Main.rand.NextFloat</c> 的等价物，用例里传一个固定序列，于是"扇形角度范围对不对"
/// "圆环是不是真的贴在边上"这类事可以被断言钉住。
/// </para>
/// </summary>
public static class MonoEmitPattern
{
    /// <summary>一圈的弧度。</summary>
    public const float Tau = MathF.PI * 2f;

    /// <summary>
    /// 取一个发射朝向。结果落在 <c>[direction − spread, direction + spread]</c> 里；
    /// 散射半角给 <c>0</c> 时就是基准朝向，给 <see cref="MathF.PI"/> 时是全向。
    /// </summary>
    /// <param name="direction">基准朝向（弧度，<c>0</c> = 向右）。</param>
    /// <param name="spread">散射半角（弧度），负值按 0 处理。</param>
    /// <param name="unit">取一个 <c>[0,1)</c> 的随机数。</param>
    public static float Direction(float direction, float spread, Func<float> unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        float half = MathF.Max(0f, spread);
        if (half <= 0f)
            return direction;

        return direction + (unit() * 2f - 1f) * half;
    }

    /// <summary>
    /// 取一个相对发射点的位移。
    /// <para><see cref="MonoEmitShape.Circle"/> 在圆盘里均匀取点（用平方根半径，直接乘半径会往圆心挤）；
    /// <see cref="MonoEmitShape.Ring"/> 精确落在圆周上。</para>
    /// </summary>
    /// <param name="shape">形状。</param>
    /// <param name="radius">半径；非正数时一律返回原点。</param>
    /// <param name="unit">取一个 <c>[0,1)</c> 的随机数。</param>
    public static Vector2 Offset(MonoEmitShape shape, float radius, Func<float> unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (shape == MonoEmitShape.Point || radius <= 0f)
            return Vector2.Zero;

        float angle = unit() * Tau;
        float distance = shape == MonoEmitShape.Ring
            ? radius
            : radius * MathF.Sqrt(Math.Clamp(unit(), 0f, 1f));

        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }
}
