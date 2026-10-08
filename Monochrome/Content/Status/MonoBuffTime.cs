namespace Monochrome.Content.Status;

/// <summary>
/// 状态时长的算术。
/// <para>
/// 「当前剩余时长 + 一个增量，再夹进 [0, max]」这件事单独拎出来，好在离线验收台里把边界钉住。
/// 增量用 <see cref="int"/>，中间按 64 位算，避免极值下溢出。
/// </para>
/// </summary>
public static class MonoBuffTime
{
    /// <summary>在当前剩余时长上叠加一个增量，结果夹在 <c>[0, max]</c>。</summary>
    /// <param name="current">当前剩余 tick。</param>
    /// <param name="delta">增量，可以是负数。</param>
    /// <param name="max">上限；小于 0 时按 0 处理。</param>
    /// <returns>应当写回的剩余 tick。</returns>
    public static int Add(int current, int delta, int max = int.MaxValue)
    {
        long limit = max < 0 ? 0 : max;
        long value = current + (long)delta;

        if (value < 0)
            value = 0;
        else if (value > limit)
            value = limit;

        return (int)value;
    }
}
