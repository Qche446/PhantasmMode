namespace Monochrome.Content.Worms;

/// <summary>
/// 一段虫体上与段链有关的字段快照。
/// <para>
/// 布局判定与走链都只读这个结构，因此它们能脱离 <c>NPC</c> 单独验证；
/// 从 NPC 读快照这一步由游戏侧的导航层负责。
/// </para>
/// </summary>
public readonly struct MonoWormNode
{
    private readonly float ai0;
    private readonly float ai1;
    private readonly float ai2;
    private readonly float ai3;

    /// <summary>代表"没有这一段"的空快照，<see cref="Exists"/> 为 <see langword="false"/>。</summary>
    public static readonly MonoWormNode None = new(-1, false, 0, 0, -1, 0f, 0f, 0f, 0f);

    /// <summary>建一个快照。</summary>
    /// <param name="whoAmI">NPC 下标。</param>
    /// <param name="active">这一段是否还活着。</param>
    /// <param name="type">NPC 类型。</param>
    /// <param name="aiStyle">AI 风格。</param>
    /// <param name="realLife">共享血量时的头部下标，不适用时传 -1。</param>
    /// <param name="ai0">第 0 个 ai 槽的值。</param>
    /// <param name="ai1">第 1 个 ai 槽的值。</param>
    /// <param name="ai2">第 2 个 ai 槽的值。</param>
    /// <param name="ai3">第 3 个 ai 槽的值。</param>
    public MonoWormNode(int whoAmI, bool active, int type, int aiStyle, int realLife,
        float ai0, float ai1, float ai2, float ai3)
    {
        WhoAmI = whoAmI;
        Active = active;
        Type = type;
        AiStyle = aiStyle;
        RealLife = realLife;
        this.ai0 = ai0;
        this.ai1 = ai1;
        this.ai2 = ai2;
        this.ai3 = ai3;
    }

    /// <summary>NPC 下标。</summary>
    public int WhoAmI { get; }

    /// <summary>这一段是否还活着。</summary>
    public bool Active { get; }

    /// <summary>NPC 类型。</summary>
    public int Type { get; }

    /// <summary>AI 风格。原版用它判定两段是否属于同一条虫。</summary>
    public int AiStyle { get; }

    /// <summary>共享血量时指向头部的下标；不适用时为 -1。</summary>
    public int RealLife { get; }

    /// <summary>这个快照是否对应一段真实存在的虫体。</summary>
    public bool Exists => WhoAmI >= 0;

    /// <summary>取第 <paramref name="slot"/> 个 ai 槽；槽位不在 0–3 之内时返回 0。</summary>
    /// <param name="slot">ai 槽位。</param>
    public float Ai(int slot) => slot switch
    {
        0 => ai0,
        1 => ai1,
        2 => ai2,
        3 => ai3,
        _ => 0f,
    };
}

/// <summary>
/// 一条蠕虫的段链写在哪些 ai 槽里。
/// <para>
/// 段链的写法在各家实现之间并不统一：原版（世界吞噬怪、毁灭者）用 <c>ai[1]</c> 指向前一段、<c>ai[0]</c> 指向后一段；
/// 哈迪斯用 <c>ai[2]</c> 存前一段下标、<c>ai[3]</c> 存段序，并把 <c>ai[1]</c> 当尾标记。库只认这个结构，
/// 调用方按自己那套填，或直接用 <see cref="Vanilla"/>。
/// </para>
/// <para>
/// 布局只描述"字段在哪一格"，不描述段的位置怎么算——跟随交给 <see cref="MonoWormFollow"/>、头部移动交给 <see cref="MonoWormMove"/>。
/// </para>
/// <para>
/// <see cref="AheadSlot"/> 与 <see cref="BehindSlot"/> <b>必须都填</b>：找头靠前者，收整条虫与反向指针校验靠后者。
/// <see cref="IndexSlot"/> 与 <see cref="TailFlagSlot"/> 可选，填了就能省掉一次走链。
/// </para>
/// </summary>
public readonly struct MonoWormLayout
{
    /// <summary>
    /// 原版约定：<c>ai[1]</c> 指前一段（朝头）、<c>ai[0]</c> 指后一段（朝尾）；
    /// 头是 <c>ai[1] &lt;= 0</c> 的那一段，尾是 <c>ai[0] &lt;= 0</c> 的那一段，两者 <see cref="MonoWormNode.AiStyle"/> 相同才算一伙。
    /// </summary>
    public static MonoWormLayout Vanilla { get; } = new()
    {
        AheadSlot = 1,
        BehindSlot = 0,
        HeadIsAheadless = true,
        RequiresSameAiStyle = true,
    };

    /// <summary>建一个未配置的布局：四个槽位都是 -1。用对象初始化器填需要的那几格。</summary>
    public MonoWormLayout()
    {
        AheadSlot = -1;
        BehindSlot = -1;
        IndexSlot = -1;
        TailFlagSlot = -1;
    }

    /// <summary>存"前一段下标"的 ai 槽；-1 表示不存。</summary>
    public int AheadSlot { get; init; }

    /// <summary>存"后一段下标"的 ai 槽；-1 表示不存。</summary>
    public int BehindSlot { get; init; }

    /// <summary>存"段序"的 ai 槽；-1 表示不存。段序 0 是头。</summary>
    public int IndexSlot { get; init; }

    /// <summary>存"是不是尾"这个标记的 ai 槽；-1 表示不存。</summary>
    public int TailFlagSlot { get; init; }

    /// <summary><see cref="TailFlagSlot"/> 里的值等于它时，这一段算尾。</summary>
    public float TailFlag { get; init; }

    /// <summary>为真时，"前一段"槽读出来小于等于 0 的那一段就是头。</summary>
    public bool HeadIsAheadless { get; init; }

    /// <summary>为真时，只有 <see cref="MonoWormNode.AiStyle"/> 相同才算同一条虫。</summary>
    public bool RequiresSameAiStyle { get; init; }

    /// <summary>两个方向都填了才算可用。</summary>
    public bool IsComplete => AheadSlot >= 0 && BehindSlot >= 0;

    /// <summary>检查布局是否可用。</summary>
    /// <param name="reason">不可用的原因；可用时是 <see cref="MonoWormLinkError.None"/>。</param>
    /// <returns>两个方向都填了才返回 <see langword="true"/>。</returns>
    public bool Validate(out MonoWormLinkError reason)
    {
        if (!IsComplete)
        {
            reason = MonoWormLinkError.SlotMissing;
            return false;
        }

        reason = MonoWormLinkError.None;
        return true;
    }

    /// <summary>取前一段的下标；布局没这一格时返回 -1。</summary>
    /// <param name="node">当前段的快照。</param>
    public int AheadOf(in MonoWormNode node) => AheadSlot < 0 ? -1 : (int)node.Ai(AheadSlot);

    /// <summary>取后一段的下标；布局没这一格时返回 -1。</summary>
    /// <param name="node">当前段的快照。</param>
    public int BehindOf(in MonoWormNode node) => BehindSlot < 0 ? -1 : (int)node.Ai(BehindSlot);

    /// <summary>取段序；布局没这一格时返回 -1。</summary>
    /// <param name="node">当前段的快照。</param>
    public int IndexInWorm(in MonoWormNode node) => IndexSlot < 0 ? -1 : (int)node.Ai(IndexSlot);

    /// <summary>按布局判定这一段是不是头。</summary>
    /// <param name="node">要判定的快照。</param>
    public bool IsHead(in MonoWormNode node)
    {
        if (!node.Exists)
            return false;

        if (IndexSlot >= 0 && (int)node.Ai(IndexSlot) == 0)
            return true;

        return HeadIsAheadless && AheadSlot >= 0 && (int)node.Ai(AheadSlot) <= 0;
    }

    /// <summary>按布局判定这一段是不是尾。</summary>
    /// <param name="node">要判定的快照。</param>
    public bool IsTail(in MonoWormNode node)
    {
        if (!node.Exists)
            return false;

        if (TailFlagSlot >= 0)
            return node.Ai(TailFlagSlot) == TailFlag;

        return BehindSlot >= 0 && (int)node.Ai(BehindSlot) <= 0;
    }

    /// <summary>按布局判定两段是否属于同一条虫。布局没要求校验 <see cref="MonoWormNode.AiStyle"/> 时恒为真。</summary>
    /// <param name="a">第一段的快照。</param>
    /// <param name="b">第二段的快照。</param>
    public bool SameWorm(in MonoWormNode a, in MonoWormNode b)
        => !RequiresSameAiStyle || a.AiStyle == b.AiStyle;
}
