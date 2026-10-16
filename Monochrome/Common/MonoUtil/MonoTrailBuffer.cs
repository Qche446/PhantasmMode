using System.Collections;

namespace Monochrome.Common.MonoUtil;

/// <summary>
/// 固定容量的轨迹点缓冲：新点从头部进，满了挤掉最旧的。
/// <para>
/// 下标 0 是<b>最旧</b>的点（尾巴），<c>Count-1</c> 是最新的点（头），也就是 <c>MonoPrim.RenderTrail</c>
/// 要求的顺序，所以它可以直接当作那个参数传进去。
/// </para>
/// <para><b>不引用任何 Terraria 类型</b>，能直接进离线验收台：容量回绕、按距离丢弃、取点这些
/// 只在轨迹很长或速度很慢时才显形，正适合用用例钉住。</para>
/// </summary>
public sealed class MonoTrailBuffer : IReadOnlyList<Vector2>
{
    private readonly Vector2[] points;
    private int head;
    private int count;

    /// <summary>建一个缓冲。</summary>
    /// <param name="capacity">最多留几个点，至少 2（一个点连不成线段）。</param>
    public MonoTrailBuffer(int capacity)
    {
        if (capacity < 2)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "轨迹缓冲至少要 2 个点。");

        points = new Vector2[capacity];
    }

    /// <summary>最多留几个点。</summary>
    public int Capacity => points.Length;

    /// <summary>现在有几个点。</summary>
    public int Count => count;

    /// <summary>最新的那个点；还没有点时是 <see cref="Vector2.Zero"/>。</summary>
    public Vector2 Head => count == 0 ? Vector2.Zero : points[(head - 1 + points.Length) % points.Length];

    /// <summary>按"尾巴在前"取点：0 最旧，<c>Count-1</c> 最新。</summary>
    /// <param name="index">下标。</param>
    public Vector2 this[int index]
    {
        get
        {
            if ((uint)index >= (uint)count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "轨迹点下标越界。");

            int start = (head - count + points.Length) % points.Length;
            return points[(start + index) % points.Length];
        }
    }

    /// <summary>清空。</summary>
    public void Clear()
    {
        head = 0;
        count = 0;
    }

    /// <summary>记一个点；满了就挤掉最旧的。</summary>
    /// <param name="point">轨迹点。</param>
    public void Add(Vector2 point)
    {
        points[head] = point;
        head = (head + 1) % points.Length;
        if (count < points.Length)
            count++;
    }

    /// <summary>
    /// 距离最新那个点不足 <paramref name="minDistance"/> 就不记。
    /// <para>实体走得慢时，逐帧记点会把同一处堆满、让条带在那一小段里反复叠加。给一个最小间距就不堆了。</para>
    /// </summary>
    /// <param name="point">候选点。</param>
    /// <param name="minDistance">最小间距（像素）；非正数表示不设门槛。</param>
    /// <returns>确实记下了才返回 <see langword="true"/>。</returns>
    public bool TryAdd(Vector2 point, float minDistance)
    {
        if (count > 0 && minDistance > 0f)
        {
            Vector2 delta = point - Head;
            if (delta.LengthSquared() < minDistance * minDistance)
                return false;
        }

        Add(point);
        return true;
    }

    /// <summary>按"尾巴在前"拷进目标；返回拷了几个。</summary>
    /// <param name="destination">目标跨度。</param>
    public int CopyTo(Span<Vector2> destination)
    {
        int copied = Math.Min(count, destination.Length);
        int start = (head - count + points.Length) % points.Length;

        for (int i = 0; i < copied; i++)
            destination[i] = points[(start + i) % points.Length];

        return copied;
    }

    /// <inheritdoc/>
    public IEnumerator<Vector2> GetEnumerator()
    {
        for (int i = 0; i < count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
