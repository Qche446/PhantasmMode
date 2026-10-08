namespace Monochrome.Content.Worms;

/// <summary>
/// 按下标读一段虫体快照的来源。
/// <para>
/// 走链只通过它取数据，于是找头、断链、绕环、越界这些分支都能脱离游戏运行时验证；
/// 游戏侧的适配器在 <see cref="MonoWormNav"/> 里。
/// </para>
/// </summary>
public interface IMonoWormNodeSource
{
    /// <summary>读第 <paramref name="whoAmI"/> 段的快照。</summary>
    /// <param name="whoAmI">段的下标。</param>
    /// <param name="node">读到的快照。</param>
    /// <returns>这个下标上没有东西时返回 <see langword="false"/>（负数与越界都要算没有）。</returns>
    bool TryRead(int whoAmI, out MonoWormNode node);
}

/// <summary>一段虫体的某一侧连接为什么不能用。</summary>
public enum MonoWormLinkError
{
    /// <summary>连接可用。</summary>
    None = 0,

    /// <summary>这一段本来就没有这一侧：头没有前一段，尾没有后一段。</summary>
    NoLink,

    /// <summary>布局里没有这一侧的槽位。</summary>
    SlotMissing,

    /// <summary>槽里的下标上没有有效的段。</summary>
    OutOfRange,

    /// <summary>槽指向自己。</summary>
    SelfReference,

    /// <summary>目标段已经不在了。</summary>
    Inactive,

    /// <summary>目标段和这一段不属于同一条虫。</summary>
    DifferentWorm,

    /// <summary>目标段的反向指针没有指回来。</summary>
    NoBackReference,
}

/// <summary>一次走链得到的整条虫概况。所有字段都是 NPC 下标，读数用 <c>Main.npc[index]</c>。</summary>
public readonly struct MonoWormChainInfo
{
    /// <summary>建一份概况。</summary>
    /// <param name="headIndex">头的位置。</param>
    /// <param name="index">被问的那一段是第几段，头是 0。</param>
    /// <param name="count">整条虫的段数。</param>
    /// <param name="aheadIndex">前一段的位置；被问的那一段是头时是 -1。</param>
    /// <param name="behindIndex">后一段的位置；被问的那一段是尾时是 -1。</param>
    public MonoWormChainInfo(int headIndex, int index, int count, int aheadIndex, int behindIndex)
    {
        HeadIndex = headIndex;
        Index = index;
        Count = count;
        AheadIndex = aheadIndex;
        BehindIndex = behindIndex;
    }

    /// <summary>头的位置。</summary>
    public int HeadIndex { get; }

    /// <summary>被问的那一段是第几段，头是 0。</summary>
    public int Index { get; }

    /// <summary>整条虫的段数。</summary>
    public int Count { get; }

    /// <summary>前一段的位置；被问的那一段是头时是 -1。</summary>
    public int AheadIndex { get; }

    /// <summary>后一段的位置；被问的那一段是尾时是 -1。</summary>
    public int BehindIndex { get; }

    /// <summary>走链有没有成功。</summary>
    public bool Valid => Count > 0;
}

/// <summary>
/// 段链的走查：找头、取相邻段、数段、校验连接。
/// <para>
/// 只依赖 <see cref="IMonoWormNodeSource"/> 与 <see cref="MonoWormLayout"/>，不碰游戏运行时；
/// 步数上限 <c>stepCap</c> 由调用方给出，游戏侧传 <c>Main.maxNPCs</c>。
/// 上限同时兜住了绕环：链一旦成环，走到上限就返回失败。
/// </para>
/// <para>
/// 所有查询都只读，不写任何字段。段的位置怎么算交给 <see cref="MonoWormFollow"/>。
/// </para>
/// </summary>
public static class MonoWormChain
{
    /// <summary>沿"前一段"一路走到头。</summary>
    /// <typeparam name="TSource">节点来源类型。</typeparam>
    /// <param name="source">节点来源。</param>
    /// <param name="from">从哪一段开始走。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="stepCap">步数上限。</param>
    /// <param name="head">走到的那一段。</param>
    /// <returns>走到头才返回 <see langword="true"/>；断链、绕环、越界都返回 <see langword="false"/>。</returns>
    public static bool TryFindHead<TSource>(TSource source, int from, in MonoWormLayout layout, int stepCap,
        out MonoWormNode head)
        where TSource : struct, IMonoWormNodeSource
        => WalkToHead(source, from, layout, stepCap, out head, out _);

    /// <summary>取前一段。</summary>
    /// <typeparam name="TSource">节点来源类型。</typeparam>
    /// <param name="source">节点来源。</param>
    /// <param name="from">当前段的下标。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="ahead">取到的前一段。</param>
    /// <returns>这一段是头、布局没有这一格、或目标不可用时返回 <see langword="false"/>。</returns>
    public static bool TryGetAhead<TSource>(TSource source, int from, in MonoWormLayout layout, out MonoWormNode ahead)
        where TSource : struct, IMonoWormNodeSource
    {
        ahead = MonoWormNode.None;

        if (!layout.IsComplete || !source.TryRead(from, out MonoWormNode node) || layout.IsHead(node))
            return false;

        int index = layout.AheadOf(node);
        if (index == from || index < 0 || !source.TryRead(index, out ahead))
            return false;

        if (!ahead.Active || !layout.SameWorm(node, ahead))
        {
            ahead = MonoWormNode.None;
            return false;
        }

        return true;
    }

    /// <summary>取后一段。</summary>
    /// <typeparam name="TSource">节点来源类型。</typeparam>
    /// <param name="source">节点来源。</param>
    /// <param name="from">当前段的下标。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="behind">取到的后一段。</param>
    /// <returns>这一段是尾、布局没有这一格、或目标不可用时返回 <see langword="false"/>。</returns>
    public static bool TryGetBehind<TSource>(TSource source, int from, in MonoWormLayout layout, out MonoWormNode behind)
        where TSource : struct, IMonoWormNodeSource
    {
        behind = MonoWormNode.None;

        if (!layout.IsComplete || !source.TryRead(from, out MonoWormNode node) || layout.IsTail(node))
            return false;

        int index = layout.BehindOf(node);
        if (index == from || index < 0 || !source.TryRead(index, out behind))
            return false;

        if (!behind.Active || !layout.SameWorm(node, behind))
        {
            behind = MonoWormNode.None;
            return false;
        }

        return true;
    }

    /// <summary>这一段是第几段，头是 0。找不到头时返回 -1。</summary>
    /// <typeparam name="TSource">节点来源类型。</typeparam>
    /// <param name="source">节点来源。</param>
    /// <param name="from">要问的那一段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="stepCap">步数上限。</param>
    public static int IndexFromHead<TSource>(TSource source, int from, in MonoWormLayout layout, int stepCap)
        where TSource : struct, IMonoWormNodeSource
    {
        if (!layout.IsComplete)
            return -1;

        // 布局自己存了段序就不用走链。
        if (layout.IndexSlot >= 0 && source.TryRead(from, out MonoWormNode node))
            return layout.IndexInWorm(node);

        return WalkToHead(source, from, layout, stepCap, out _, out int steps) ? steps : -1;    }

    /// <summary>数这条虫有多少段。找不到头时返回 0。</summary>
    /// <typeparam name="TSource">节点来源类型。</typeparam>
    /// <param name="source">节点来源。</param>
    /// <param name="from">这条虫上的任意一段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="stepCap">步数上限。</param>
    public static int Count<TSource>(TSource source, int from, in MonoWormLayout layout, int stepCap)
        where TSource : struct, IMonoWormNodeSource
    {
        if (!WalkToHead(source, from, layout, stepCap, out MonoWormNode head, out _))
            return 0;

        if (layout.BehindSlot < 0)
            return 1;

        int count = 1;
        MonoWormNode current = head;
        for (int step = 0; step < stepCap; step++)
        {
            int index = layout.BehindOf(current);
            if (index == current.WhoAmI || index < 0 || !source.TryRead(index, out MonoWormNode next))
                break;

            if (!next.Active || !layout.SameWorm(current, next))
                break;

            count++;
            current = next;
        }

        return count;
    }

    /// <summary>把这条虫的段按下标收起来，头在前。返回真正写进去的段数。</summary>
    /// <typeparam name="TSource">节点来源类型。</typeparam>
    /// <param name="source">节点来源。</param>
    /// <param name="from">这条虫上的任意一段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="into">接收下标的目标。装不下时只写到装得下的部分。</param>
    /// <param name="stepCap">步数上限。</param>
    public static int CollectIndices<TSource>(TSource source, int from, in MonoWormLayout layout, Span<int> into, int stepCap)
        where TSource : struct, IMonoWormNodeSource
    {
        if (into.Length == 0 || !WalkToHead(source, from, layout, stepCap, out MonoWormNode head, out _))
            return 0;

        int count = 0;
        into[count++] = head.WhoAmI;

        if (layout.BehindSlot < 0)
            return count;

        MonoWormNode current = head;
        for (int step = 0; step < stepCap && count < into.Length; step++)
        {
            int index = layout.BehindOf(current);
            if (index == current.WhoAmI || index < 0 || !source.TryRead(index, out MonoWormNode next))
                break;

            if (!next.Active || !layout.SameWorm(current, next))
                break;

            into[count++] = next.WhoAmI;
            current = next;
        }

        return count;
    }

    /// <summary>一次走链把整条虫的概况拿全：头、段序、段数、前后段。</summary>
    /// <typeparam name="TSource">节点来源类型。</typeparam>
    /// <param name="source">节点来源。</param>
    /// <param name="from">这条虫上的任意一段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="into">走链用的中转缓冲，长度不小于段数。</param>
    /// <param name="stepCap">步数上限。</param>
    /// <param name="info">走链成功时填好的概况。</param>
    public static bool WalkInfo<TSource>(TSource source, int from, in MonoWormLayout layout, Span<int> into, int stepCap,
        out MonoWormChainInfo info)
        where TSource : struct, IMonoWormNodeSource
    {
        info = default;

        int count = CollectIndices(source, from, layout, into, stepCap);
        if (count == 0)
            return false;

        int index = -1;
        for (int i = 0; i < count; i++)
        {
            if (into[i] == from)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return false;

        info = new MonoWormChainInfo(
            into[0],
            index,
            count,
            index > 0 ? into[index - 1] : -1,
            index < count - 1 ? into[index + 1] : -1);
        return true;
    }

    /// <summary>校验"前一段"这一侧能不能用。</summary>
    /// <typeparam name="TSource">节点来源类型。</typeparam>
    /// <param name="source">节点来源。</param>
    /// <param name="from">要校验的那一段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="reason">不能用的原因。</param>
    public static bool ValidateAhead<TSource>(TSource source, int from, in MonoWormLayout layout, out MonoWormLinkError reason)
        where TSource : struct, IMonoWormNodeSource
        => Validate(source, from, layout, towardHead: true, out reason);

    /// <summary>校验"后一段"这一侧能不能用。</summary>
    /// <typeparam name="TSource">节点来源类型。</typeparam>
    /// <param name="source">节点来源。</param>
    /// <param name="from">要校验的那一段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="reason">不能用的原因。</param>
    public static bool ValidateBehind<TSource>(TSource source, int from, in MonoWormLayout layout, out MonoWormLinkError reason)
        where TSource : struct, IMonoWormNodeSource
        => Validate(source, from, layout, towardHead: false, out reason);

    /// <summary>沿"前一段"走到头，顺带带回走了几步（也就是起始段的段序）。</summary>
    private static bool WalkToHead<TSource>(TSource source, int from, in MonoWormLayout layout, int stepCap,
        out MonoWormNode head, out int steps)
        where TSource : struct, IMonoWormNodeSource
    {
        head = MonoWormNode.None;
        steps = -1;

        if (from < 0 || !layout.IsComplete || stepCap <= 0 || !source.TryRead(from, out MonoWormNode node))
            return false;

        for (int step = 0; step < stepCap; step++)
        {
            if (layout.IsHead(node))
            {
                head = node;
                steps = step;
                return true;
            }

            int index = layout.AheadOf(node);
            if (index == node.WhoAmI || index < 0 || !source.TryRead(index, out MonoWormNode next))
                return false;

            if (!next.Active || !layout.SameWorm(node, next))
                return false;

            node = next;
        }

        return false;
    }

    /// <summary>两个方向的连接校验共用一套判据。</summary>
    private static bool Validate<TSource>(TSource source, int from, in MonoWormLayout layout, bool towardHead,
        out MonoWormLinkError reason)
        where TSource : struct, IMonoWormNodeSource
    {
        reason = MonoWormLinkError.None;

        if (!layout.IsComplete)
        {
            reason = MonoWormLinkError.SlotMissing;
            return false;
        }

        if (!source.TryRead(from, out MonoWormNode node))
        {
            reason = MonoWormLinkError.OutOfRange;
            return false;
        }

        if (towardHead ? layout.IsHead(node) : layout.IsTail(node))
        {
            reason = MonoWormLinkError.NoLink;
            return false;
        }

        int index = towardHead ? layout.AheadOf(node) : layout.BehindOf(node);
        if (index == from)
        {
            reason = MonoWormLinkError.SelfReference;
            return false;
        }

        if (!source.TryRead(index, out MonoWormNode target))
        {
            reason = MonoWormLinkError.OutOfRange;
            return false;
        }

        if (!target.Active)
        {
            reason = MonoWormLinkError.Inactive;
            return false;
        }

        if (!layout.SameWorm(node, target))
        {
            reason = MonoWormLinkError.DifferentWorm;
            return false;
        }

        // 反向指针：布局里有对面那一格时交叉验证，原版世界吞噬怪就是靠它发现断链的。
        if ((towardHead ? layout.BehindSlot : layout.AheadSlot) >= 0)
        {
            int back = towardHead ? layout.BehindOf(target) : layout.AheadOf(target);
            if (back != from)
            {
                reason = MonoWormLinkError.NoBackReference;
                return false;
            }
        }

        return true;
    }
}
