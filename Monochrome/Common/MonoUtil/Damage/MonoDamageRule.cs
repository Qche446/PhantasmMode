namespace Monochrome.Common.MonoUtil.Damage;

/// <summary>
/// 一条伤害规则。消费者的减伤、增伤、元素抗性、破甲都写成它的子类，再交给
/// <see cref="MonoDamageChain"/> 排队。
/// <para>
/// <b>它只声明位置与条件，不管顺序。</b>顺序由 <see cref="Stage"/> 与 <see cref="Order"/> 决定，
/// 规则自己不需要知道别人跑在哪一步——这是这套设计里唯一需要遵守的约定。
/// </para>
/// <para>
/// <b>它是无状态的。</b>同一条规则会被反复调用（每次命中一次），要存"这个 boss 现在处于第几阶段"这类信息，
/// 存到目标实体上，或者存到规则自身但按目标编号索引——不要存成"最后一次结算"那样的可变字段，
/// 那在多目标同帧命中时会串味。库不强制这一点，但把状态写在规则里是这类系统最常见的一类错。
/// </para>
/// </summary>
public abstract class MonoDamageRule
{
    /// <summary>这条规则属于哪一段。段号只决定先后，见 <see cref="MonoDamageStage"/>。</summary>
    public abstract MonoDamageStage Stage { get; }

    /// <summary>同段内的次序，<b>越小越先跑</b>。默认 0，即按注册顺序。</summary>
    public virtual int Order => 0;

    /// <summary>
    /// 只管这一类目标；<c>-1</c> 表示所有目标（默认）。
    /// <para>
    /// 填入具体类型能让规则表按目标类型预编译分片：一个只对某个 boss 生效的规则，不必在别的 NPC 身上
    /// 被挨个问一遍 <see cref="AppliesTo"/>。分片是按类型惰性建的，所以类型不写死在这里也不会退化成全表扫描。
    /// </para>
    /// </summary>
    public virtual int TargetType => -1;

    /// <summary>诊断里显示的名字。默认用类型名。</summary>
    public virtual string Name => GetType().Name;

    /// <summary>
    /// 轻量条件：为假时 <see cref="Apply"/> 不会被调用。
    /// <para>
    /// 它存在的意义是让"这条规则这次参不参与"变成可枚举的一步。判据写在这里还是写在
    /// <see cref="Apply"/> 第一行，行为一样，但写在这里的诊断输出会明确区分"没参与"和"参与了但没改动"。
    /// </para>
    /// </summary>
    /// <param name="info">这次命中。</param>
    public virtual bool AppliesTo(in MonoDamageInfo info) => true;

    /// <summary>
    /// 结算。改 <paramref name="values"/> 里对应段该改的字段。
    /// </summary>
    /// <param name="info">这次命中，只读。</param>
    /// <param name="values">累加器，按引用改。</param>
    public abstract void Apply(in MonoDamageInfo info, ref MonoDamageValues values);
}
