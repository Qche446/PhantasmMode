using System.Text;

namespace Monochrome.Common.MonoUtil.Damage;

/// <summary>伤害来源的种类。</summary>
public enum MonoDamageSourceKind
{
    /// <summary>没识别出来源。</summary>
    Unknown = 0,

    /// <summary>玩家。</summary>
    Player,

    /// <summary>NPC。</summary>
    Npc,

    /// <summary>弹幕。</summary>
    Projectile,

    /// <summary>环境，例如机关、坠落、溺水。</summary>
    Environment,
}

/// <summary>
/// 一次命中在规则眼里长什么样。<b>只读</b>：所有字段都是值类型与 <c>readonly</c>，
/// 规则只能看它，改不了它——要改就改 <see cref="MonoDamageValues"/>。
/// <para>
/// 它不带实体引用（没有 <c>NPC</c> / <c>Projectile</c> 字段）。带上引用就等于让规则能读世界，
/// 于是伤害结算变成"看当前帧的世界状态"，多人下每个客户端算出来的数就可能不一样。目标一律用编号与类型表示，
/// 真要读实体由调用方在钩子里自己取（那一层知道自己在哪个端）。
/// </para>
/// </summary>
public readonly struct MonoDamageInfo
{
    /// <summary>来源种类。</summary>
    public readonly MonoDamageSourceKind SourceKind;

    /// <summary>来源编号（<c>whoAmI</c> 或 <c>Projectile.owner</c> 这类）。用不上时给 <c>-1</c>。</summary>
    public readonly int SourceIndex;

    /// <summary>目标的实体编号。</summary>
    public readonly int TargetIndex;

    /// <summary>目标的类型。规则按目标类型预编译时就靠它；未知给 <c>-1</c>。</summary>
    public readonly int TargetType;

    /// <summary>这次命中带的伤害标签掩码，见 <see cref="MonoDamageTag"/>。没有标签就是 0。</summary>
    public readonly ulong Tags;

    /// <summary>
    /// 这次命中是否暴击。
    /// <para>
    /// <b>现阶段两条入口都填不出它。</b>暴击是 <c>ToHitInfo</c> 的实参，而原版 <c>HitModifiers</c> 上只有私有的
    /// <c>_critOverride</c>；钩子跑到的时候这个值还没被决定。字段留着是给将来某个"能在结算前拿到暴击"的入口
    /// 留位置，规则不要依赖它。
    /// </para>
    /// </summary>
    public readonly bool Crit;

    /// <summary>击退方向，<c>1</c> 或 <c>-1</c>。</summary>
    public readonly int HitDirection;

    /// <summary>目标是否为玩家。玩家侧与 NPC 侧共用同一套规则表，靠这个字段区分。</summary>
    public readonly bool AgainstPlayer;

    /// <summary>建一条命中信息。</summary>
    /// <param name="sourceKind">来源种类。</param>
    /// <param name="sourceIndex">来源编号。</param>
    /// <param name="targetIndex">目标编号。</param>
    /// <param name="targetType">目标类型。</param>
    /// <param name="tags">伤害标签掩码。</param>
    /// <param name="crit">是否暴击。</param>
    /// <param name="hitDirection">击退方向。</param>
    /// <param name="againstPlayer">目标是否为玩家。</param>
    public MonoDamageInfo(MonoDamageSourceKind sourceKind = MonoDamageSourceKind.Unknown,
        int sourceIndex = -1, int targetIndex = -1, int targetType = -1, ulong tags = 0UL,
        bool crit = false, int hitDirection = 0, bool againstPlayer = false)
    {
        SourceKind = sourceKind;
        SourceIndex = sourceIndex;
        TargetIndex = targetIndex;
        TargetType = targetType;
        Tags = tags;
        Crit = crit;
        HitDirection = hitDirection;
        AgainstPlayer = againstPlayer;
    }

    /// <summary>这次命中是否带某个标签。</summary>
    /// <param name="tag">单个标签的掩码。</param>
    public bool HasTag(ulong tag) => (Tags & tag) != 0UL;

    /// <summary>诊断输出。会分配，只在命令与追踪里用。</summary>
    public string Describe()
    {
        StringBuilder output = new();
        output.Append("命中 ").Append(SourceKind).Append('#').Append(SourceIndex)
              .Append(" → ").Append(AgainstPlayer ? "玩家" : "NPC").Append('#').Append(TargetIndex)
              .Append("（类型 ").Append(TargetType).Append('）')
              .Append("，标签 ").Append(MonoDamageTag.Describe(Tags));
        if (Crit)
            output.Append("，暴击");
        return output.ToString();
    }
}
