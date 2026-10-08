namespace Monochrome.Common.MonoUtil.Physics;

/// <summary>
/// 一条约束：拿到整条质点数组，按需要把若干质点挪一点。
/// <para>
/// <b>投影只读索引、不持有求解器</b>，所以同一条约束可以被不同求解器复用，
/// 也可以在离线验收台里直接对一组 <see cref="Vector2"/> 调用。
/// </para>
/// <para>
/// <b>约定</b>：投影是位置层面的修正，不返回力、不写速度。逆质量为 0 的质点必须原封不动——
/// 这是"钉住"的物理含义，绳索端点在约束里被推走会让整条链慢慢漂。
/// </para>
/// </summary>
public abstract class MonoConstraint
{
    /// <summary>把本约束投影一次。两个跨度都由调用方持有，投影只就地改写位置。</summary>
    /// <param name="positions">全部质点的位置，索引即质点编号。</param>
    /// <param name="inverseMass">全部质点的逆质量，<c>0</c> 表示钉住。</param>
    public abstract void Project(Span<Vector2> positions, ReadOnlySpan<float> inverseMass);
}

/// <summary>
/// 距离约束：把两个质点拉回 <see cref="Rest"/> 的间距。绳索的每一段、布的相邻格与对角格、
/// 以及"隔一个"的弯曲约束都是它。
/// <para>
/// 修正量按逆质量分摊：两端都可动则各走一半，一端钉住则由另一端走完全部。两端都钉住时它什么都不做。
/// </para>
/// </summary>
public sealed class MonoDistanceConstraint : MonoConstraint
{
    /// <summary>第一个质点编号。</summary>
    public readonly int A;

    /// <summary>第二个质点编号。</summary>
    public readonly int B;

    /// <summary>期望间距（像素）。</summary>
    public float Rest;

    /// <summary>刚度，<c>1</c> 为一次投影就完全满足，<c>0</c> 为完全不约束。</summary>
    public float Stiffness;

    /// <summary>建立一条距离约束。</summary>
    /// <param name="a">第一个质点编号。</param>
    /// <param name="b">第二个质点编号。</param>
    /// <param name="rest">期望间距（像素）。</param>
    /// <param name="stiffness">刚度，取 0–1。</param>
    public MonoDistanceConstraint(int a, int b, float rest, float stiffness = 1f)
    {
        if (a < 0)
            throw new ArgumentOutOfRangeException(nameof(a), a, "质点编号不能为负。");
        if (b < 0)
            throw new ArgumentOutOfRangeException(nameof(b), b, "质点编号不能为负。");
        if (a == b)
            throw new ArgumentException("距离约束的两端不能是同一个质点。", nameof(b));

        A = a;
        B = b;
        Rest = rest;
        Stiffness = stiffness;
    }

    /// <inheritdoc/>
    public override void Project(Span<Vector2> positions, ReadOnlySpan<float> inverseMass)
    {
        float weightSum = inverseMass[A] + inverseMass[B];
        if (weightSum <= 0f || Stiffness <= 0f)
            return;

        Vector2 delta = positions[B] - positions[A];
        float length = delta.Length();
        if (length <= MonoUtil.Epsilon)
            return;

        float correction = (length - Rest) / length * Stiffness;
        positions[A] += delta * (correction * inverseMass[A] / weightSum);
        positions[B] -= delta * (correction * inverseMass[B] / weightSum);
    }
}
