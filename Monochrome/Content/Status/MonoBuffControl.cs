namespace Monochrome.Content.Status;

/// <summary>
/// 原版 buff 数组的操控台。
/// <para>
/// 原版给的手段很窄：<c>AddBuff</c> 施加或刷新，而且必经 <c>ReApply</c> 与 <c>buffImmune</c>；
/// <c>ClearBuff</c> 只有玩家有，NPC 侧得自己拿下标 <c>DelBuff</c>；想设定或叠加时长只能自己找下标读写
/// <c>buffTime</c>。这里补齐几样常用的，Player 与 NPC 用同一套签名。
/// </para>
/// <para>
/// 每个方法内部都重新 <c>FindBuffIndex</c>，不吃缓存下标——<c>DelBuff</c> 会把数组往下压实，
/// 移除之后旧下标就指到别的地方了。要拿下标请当场查，别跨调用留着。
/// </para>
/// <para>
/// 这些方法只在本端那份数组上做事，不判断权威端。多人下该由哪一端施加、要不要补同步，仍由调用方决定。
/// </para>
/// </summary>
public static class MonoBuffControl
{
    /// <summary>取玩家的剩余时长；没有这个状态时返回 -1。</summary>
    /// <param name="player">玩家。</param>
    /// <param name="type">buff 类型。</param>
    public static int TimeLeft(Player player, int type)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (type <= 0)
            return -1;

        int index = player.FindBuffIndex(type);
        return index < 0 ? -1 : player.buffTime[index];
    }

    /// <summary>取 NPC 的剩余时长；没有这个状态时返回 -1。</summary>
    /// <param name="npc">NPC。</param>
    /// <param name="type">buff 类型。</param>
    public static int TimeLeft(NPC npc, int type)
    {
        ArgumentNullException.ThrowIfNull(npc);

        if (type <= 0)
            return -1;

        int index = npc.FindBuffIndex(type);
        return index < 0 ? -1 : npc.buffTime[index];
    }

    /// <summary>把玩家的剩余时长直接写成 <paramref name="time"/>，不经过 <c>ReApply</c>；没有这个状态时是空操作。</summary>
    /// <param name="player">玩家。</param>
    /// <param name="type">buff 类型。</param>
    /// <param name="time">要写入的剩余 tick，小于 0 时按 0 处理。</param>
    public static void SetTime(Player player, int type, int time)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (type <= 0)
            return;

        int index = player.FindBuffIndex(type);
        if (index >= 0)
            player.buffTime[index] = time < 0 ? 0 : time;
    }

    /// <summary>把 NPC 的剩余时长直接写成 <paramref name="time"/>，不经过 <c>ReApply</c>；没有这个状态时是空操作。</summary>
    /// <param name="npc">NPC。</param>
    /// <param name="type">buff 类型。</param>
    /// <param name="time">要写入的剩余 tick，小于 0 时按 0 处理。</param>
    public static void SetTime(NPC npc, int type, int time)
    {
        ArgumentNullException.ThrowIfNull(npc);

        if (type <= 0)
            return;

        int index = npc.FindBuffIndex(type);
        if (index >= 0)
            npc.buffTime[index] = time < 0 ? 0 : time;
    }

    /// <summary>在玩家的剩余时长上叠加 <paramref name="delta"/>，结果夹在 <c>[0, max]</c>。</summary>
    /// <param name="player">玩家。</param>
    /// <param name="type">buff 类型。</param>
    /// <param name="delta">增量，可以是负数。</param>
    /// <param name="max">上限。</param>
    /// <returns>写入后的剩余 tick；没有这个状态时返回 -1。</returns>
    public static int AddTime(Player player, int type, int delta, int max = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (type <= 0)
            return -1;

        int index = player.FindBuffIndex(type);
        if (index < 0)
            return -1;

        return player.buffTime[index] = MonoBuffTime.Add(player.buffTime[index], delta, max);
    }

    /// <summary>在 NPC 的剩余时长上叠加 <paramref name="delta"/>，结果夹在 <c>[0, max]</c>。</summary>
    /// <param name="npc">NPC。</param>
    /// <param name="type">buff 类型。</param>
    /// <param name="delta">增量，可以是负数。</param>
    /// <param name="max">上限。</param>
    /// <returns>写入后的剩余 tick；没有这个状态时返回 -1。</returns>
    public static int AddTime(NPC npc, int type, int delta, int max = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(npc);

        if (type <= 0)
            return -1;

        int index = npc.FindBuffIndex(type);
        if (index < 0)
            return -1;

        return npc.buffTime[index] = MonoBuffTime.Add(npc.buffTime[index], delta, max);
    }

    /// <summary>给玩家施加或刷新一个状态。</summary>
    /// <param name="player">玩家。</param>
    /// <param name="type">buff 类型。</param>
    /// <param name="time">时长。</param>
    /// <param name="quiet">是否不弹提示。</param>
    /// <returns>施加之后身上到底有没有它；被 <c>buffImmune</c> 挡掉时是 <see langword="false"/>。</returns>
    public static bool Give(Player player, int type, int time, bool quiet = true)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (type <= 0)
            return false;

        player.AddBuff(type, time, quiet);
        return player.FindBuffIndex(type) >= 0;
    }

    /// <summary>给 NPC 施加或刷新一个状态。</summary>
    /// <param name="npc">NPC。</param>
    /// <param name="type">buff 类型。</param>
    /// <param name="time">时长。</param>
    /// <param name="quiet">是否不弹提示。</param>
    /// <returns>施加之后身上到底有没有它；被 <c>buffImmune</c> 挡掉时是 <see langword="false"/>。</returns>
    public static bool Give(NPC npc, int type, int time, bool quiet = true)
    {
        ArgumentNullException.ThrowIfNull(npc);

        if (type <= 0)
            return false;

        npc.AddBuff(type, time, quiet);
        return npc.FindBuffIndex(type) >= 0;
    }

    /// <summary>从玩家身上移除这个状态。</summary>
    /// <param name="player">玩家。</param>
    /// <param name="type">buff 类型。</param>
    /// <returns>真的移除了才返回 <see langword="true"/>。</returns>
    public static bool Take(Player player, int type)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (type <= 0)
            return false;

        int index = player.FindBuffIndex(type);
        if (index < 0)
            return false;

        player.DelBuff(index);
        return true;
    }

    /// <summary>从 NPC 身上移除这个状态。原版 NPC 没有 <c>ClearBuff</c>，这里是那条路的统一入口。</summary>
    /// <param name="npc">NPC。</param>
    /// <param name="type">buff 类型。</param>
    /// <returns>真的移除了才返回 <see langword="true"/>。</returns>
    public static bool Take(NPC npc, int type)
    {
        ArgumentNullException.ThrowIfNull(npc);

        if (type <= 0)
            return false;

        int index = npc.FindBuffIndex(type);
        if (index < 0)
            return false;

        npc.DelBuff(index);
        return true;
    }

    /// <summary>把玩家身上的某个状态换成另一个：先移除来源，再施加目标。</summary>
    /// <param name="player">玩家。</param>
    /// <param name="from">要被换掉的状态；与目标相同时只做刷新。</param>
    /// <param name="to">换上的状态。</param>
    /// <param name="time">目标状态的时长。</param>
    /// <param name="quiet">是否不弹提示。</param>
    /// <returns>目标状态最终在不在身上。</returns>
    public static bool Replace(Player player, int from, int to, int time, bool quiet = true)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (to <= 0)
            return false;

        if (from > 0 && from != to)
            Take(player, from);

        return Give(player, to, time, quiet);
    }

    /// <summary>把 NPC 身上的某个状态换成另一个：先移除来源，再施加目标。</summary>
    /// <param name="npc">NPC。</param>
    /// <param name="from">要被换掉的状态；与目标相同时只做刷新。</param>
    /// <param name="to">换上的状态。</param>
    /// <param name="time">目标状态的时长。</param>
    /// <param name="quiet">是否不弹提示。</param>
    /// <returns>目标状态最终在不在身上。</returns>
    public static bool Replace(NPC npc, int from, int to, int time, bool quiet = true)
    {
        ArgumentNullException.ThrowIfNull(npc);

        if (to <= 0)
            return false;

        if (from > 0 && from != to)
            Take(npc, from);

        return Give(npc, to, time, quiet);
    }
}
