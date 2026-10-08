namespace Monochrome.Content.Worms;

/// <summary>
/// 从 <c>Main.npc</c> 读快照的来源。做成 <c>struct</c> 是为了让 <see cref="MonoWormChain"/> 的泛型实参不装箱。
/// </summary>
internal readonly struct NpcWormNodes : IMonoWormNodeSource
{
    /// <inheritdoc/>
    public bool TryRead(int whoAmI, out MonoWormNode node)
    {
        if ((uint)whoAmI >= (uint)Main.npc.Length)
        {
            node = MonoWormNode.None;
            return false;
        }

        node = MonoWormNav.Snapshot(Main.npc[whoAmI]);
        return true;
    }
}

/// <summary>
/// 蠕虫段链的导航：找头、取相邻段、数段、校验连接。
/// <para>
/// 全部是静态查询，不持有状态，也不改写任何 NPC。链布局由调用方给出（见 <see cref="MonoWormLayout"/>），
/// 段的位置怎么算由调用方决定（见 <see cref="MonoWormFollow"/>）。这两个问题库都不接管。
/// </para>
/// <para>
/// 用法是每帧在体节的 AI 里按需调用，例如：
/// <code>
/// if (MonoWormNav.ValidateAhead(npc, MonoWormLayout.Vanilla, out MonoWormLinkError reason))
///     MonoWormFollow.Snake(ahead.Center, aheadAhead.Center, npc.Center, 40f, 1f, spring, out Vector2 center, out float rotation);
/// </code>
/// </para>
/// </summary>
public static class MonoWormNav
{
    private static readonly NpcWormNodes Nodes = default;

    /// <summary>读一只 NPC 的段链快照。</summary>
    /// <param name="npc">要读的 NPC。</param>
    public static MonoWormNode Snapshot(NPC npc)
        => new(npc.whoAmI, npc.active, npc.type, npc.aiStyle, npc.realLife,
            npc.ai[0], npc.ai[1], npc.ai[2], npc.ai[3]);

    /// <summary>沿"前一段"一路走到头。</summary>
    /// <param name="segment">这条虫上的任意一段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="head">走到的那一段。</param>
    /// <returns>断链、绕环、越界都返回 <see langword="false"/>。</returns>
    public static bool TryFindHead(NPC segment, in MonoWormLayout layout, out NPC? head)
    {
        head = null;

        if (segment is null)
            return false;

        if (!MonoWormChain.TryFindHead(Nodes, segment.whoAmI, layout, Main.maxNPCs, out MonoWormNode node))
            return false;

        head = Main.npc[node.WhoAmI];
        return true;
    }

    /// <summary>取前一段。</summary>
    /// <param name="segment">当前段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="ahead">取到的前一段。</param>
    public static bool TryGetAhead(NPC segment, in MonoWormLayout layout, out NPC? ahead)
    {
        ahead = null;

        if (segment is null)
            return false;

        if (!MonoWormChain.TryGetAhead(Nodes, segment.whoAmI, layout, out MonoWormNode node))
            return false;

        ahead = Main.npc[node.WhoAmI];
        return true;
    }

    /// <summary>取后一段。</summary>
    /// <param name="segment">当前段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="behind">取到的后一段。</param>
    public static bool TryGetBehind(NPC segment, in MonoWormLayout layout, out NPC? behind)
    {
        behind = null;

        if (segment is null)
            return false;

        if (!MonoWormChain.TryGetBehind(Nodes, segment.whoAmI, layout, out MonoWormNode node))
            return false;

        behind = Main.npc[node.WhoAmI];
        return true;
    }

    /// <summary>这一段是不是头。</summary>
    /// <param name="segment">要问的那一段。</param>
    /// <param name="layout">链布局。</param>
    public static bool IsHead(NPC segment, in MonoWormLayout layout)
        => segment is not null && layout.IsHead(Snapshot(segment));

    /// <summary>这一段是不是尾。身后没有有效的一段时也算尾。</summary>
    /// <param name="segment">要问的那一段。</param>
    /// <param name="layout">链布局。</param>
    public static bool IsTail(NPC segment, in MonoWormLayout layout)
    {
        if (segment is null)
            return false;

        if (layout.IsTail(Snapshot(segment)))
            return true;

        if (layout.BehindSlot < 0)
            return false;

        return !MonoWormChain.ValidateBehind(Nodes, segment.whoAmI, layout, out _);
    }

    /// <summary>这一段是第几段，头是 0。找不到头时返回 -1。</summary>
    /// <param name="segment">要问的那一段。</param>
    /// <param name="layout">链布局。</param>
    public static int IndexFromHead(NPC segment, in MonoWormLayout layout)
        => segment is null ? -1 : MonoWormChain.IndexFromHead(Nodes, segment.whoAmI, layout, Main.maxNPCs);

    /// <summary>数这条虫有多少段。找不到头时返回 0。</summary>
    /// <param name="segment">这条虫上的任意一段。</param>
    /// <param name="layout">链布局。</param>
    public static int Count(NPC segment, in MonoWormLayout layout)
        => segment is null ? 0 : MonoWormChain.Count(Nodes, segment.whoAmI, layout, Main.maxNPCs);

    /// <summary>把这条虫的段按头到尾收进 <paramref name="into"/>，已有的内容不动。返回追加的段数。</summary>
    /// <param name="segment">这条虫上的任意一段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="into">接收 NPC 的列表。</param>
    public static int Walk(NPC segment, in MonoWormLayout layout, List<NPC> into)
    {
        ArgumentNullException.ThrowIfNull(into);

        if (segment is null)
            return 0;

        int cap = Main.maxNPCs;
        Span<int> indices = stackalloc int[cap];
        int count = MonoWormChain.CollectIndices(Nodes, segment.whoAmI, layout, indices, cap);

        for (int i = 0; i < count; i++)
            into.Add(Main.npc[indices[i]]);

        return count;
    }

    /// <summary>
    /// 一次走链把整条虫的概况拿全，比分别调 <see cref="TryFindHead"/> / <see cref="IndexFromHead"/> / <see cref="Count"/> 省事。
    /// 返回的下标用 <c>Main.npc[index]</c> 取 NPC。
    /// </summary>
    /// <param name="segment">这条虫上的任意一段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="info">走链成功时填好的概况。</param>
    public static bool Describe(NPC segment, in MonoWormLayout layout, out MonoWormChainInfo info)
    {
        info = default;

        if (segment is null)
            return false;

        int cap = Main.maxNPCs;
        Span<int> indices = stackalloc int[cap];
        return MonoWormChain.WalkInfo(Nodes, segment.whoAmI, layout, indices, cap, out info);
    }

    /// <summary>校验"前一段"这一侧能不能用。体节在跟随之前用它确认链没断。</summary>
    /// <param name="segment">要校验的那一段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="reason">不能用的原因。</param>
    public static bool ValidateAhead(NPC segment, in MonoWormLayout layout, out MonoWormLinkError reason)
    {
        if (segment is null)
        {
            reason = MonoWormLinkError.OutOfRange;
            return false;
        }

        return MonoWormChain.ValidateAhead(Nodes, segment.whoAmI, layout, out reason);
    }

    /// <summary>校验"后一段"这一侧能不能用。原版世界吞噬怪就是用它在头部发现身后断链。</summary>
    /// <param name="segment">要校验的那一段。</param>
    /// <param name="layout">链布局。</param>
    /// <param name="reason">不能用的原因。</param>
    public static bool ValidateBehind(NPC segment, in MonoWormLayout layout, out MonoWormLinkError reason)
    {
        if (segment is null)
        {
            reason = MonoWormLinkError.OutOfRange;
            return false;
        }

        return MonoWormChain.ValidateBehind(Nodes, segment.whoAmI, layout, out reason);
    }
}
