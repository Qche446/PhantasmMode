using System.Text;

namespace Monochrome.Common.MonoUtil.Damage;

/// <summary>
/// 一张伤害规则表：登记、冻结、按段序求值。
/// <para>
/// <b>定序规则固定两条。</b>先比 <see cref="MonoDamageRule.Stage"/>，段号小的先跑；段号相同的比
/// <see cref="MonoDamageRule.Order"/>；两者都相同的按登记顺序。第三条是靠 <c>OrderBy</c>/<c>ThenBy</c>
/// 的稳定性实现的，不需要额外维护序号。
/// <para>
/// 这个顺序只影响遍历与诊断输出，不改变数值结果——理由见 <see cref="MonoDamageStage"/>。
/// </para>
/// </para>
/// <para>
/// <b>冻结之后不再接受登记。</b>和 <c>MonoServiceHost</c> 对晚注册的处理一致：拒绝、计数、由调用方去记日志。
/// 库这一层不写日志——<c>Common/MonoUtil</c> 在依赖方向的下面，引用不到 <c>Core</c> 的日志。
/// </para>
/// <para>
/// <b>按目标类型惰性分片。</b>第一次遇到某个类型时，从已排好序的全量表里筛出"全局规则 + 这个类型的规则"，
/// 筛完仍是有序的（筛有序表不会打乱相对次序），缓存起来下次直接用。一张表里通常没有几个类型，
/// 所以这份缓存用一个小字典，容量随类型数长。
/// </para>
/// </summary>
public sealed class MonoDamageChain
{
    private readonly List<MonoDamageRule> rules = [];
    private readonly Dictionary<int, MonoDamageRule[]> byTargetType = [];
    private MonoDamageRule[]? frozen;

    /// <summary>规则表是否已经冻结。</summary>
    public bool IsFrozen => frozen is not null;

    /// <summary>登记的规则条数（冻结后不会再变）。</summary>
    public int Count => rules.Count;

    /// <summary>冻结之后被拒的登记次数。<b>正常应为 0</b>，非 0 说明有东西在装配完成之后还在往表里加规则。</summary>
    public int LateRegistrationRejected { get; private set; }

    /// <summary>被移除的规则条数。诊断用——规则"不见了"时先看它。</summary>
    public int Removed { get; private set; }

    /// <summary>已建立分片缓存的目标类型数。诊断用。</summary>
    public int CachedTargetTypes => byTargetType.Count;

    /// <summary>按登记顺序的规则表，只给诊断看。实际执行顺序以 <see cref="MonoDamageRule.Stage"/> 与 <see cref="MonoDamageRule.Order"/> 为准。</summary>
    public IReadOnlyList<MonoDamageRule> Rules => rules;

    /// <summary>登记一条规则。</summary>
    /// <param name="rule">要登记的规则。</param>
    /// <returns>登记成功返回 true；表已冻结时返回 false，并把 <see cref="LateRegistrationRejected"/> 加一。</returns>
    public bool Register(MonoDamageRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (frozen is not null)
        {
            LateRegistrationRejected++;
            return false;
        }

        rules.Add(rule);
        return true;
    }

    /// <summary>
    /// 冻结规则表：定序一次，之后 <see cref="Evaluate"/> 只读。可以重复调用（重新定序并清掉分片缓存）。
    /// </summary>
    public void Freeze()
    {
        frozen = [.. rules.OrderBy(static rule => (int)rule.Stage).ThenBy(static rule => rule.Order)];
        byTargetType.Clear();
    }

    /// <summary>
    /// 清空规则表并解冻，同时复位两条计数。给热重载与离线验收台用——不清的话，
    /// 热重载会把同一批规则再登记一遍。
    /// </summary>
    public void Clear()
    {
        rules.Clear();
        frozen = null;
        byTargetType.Clear();
        LateRegistrationRejected = 0;
        Removed = 0;
    }

    /// <summary>
    /// 移除一条规则。给"撤掉别人的规则"留的口子，装配期与运行期都能调。
    /// <para>
    /// 表已冻结时立即重新编译，<b>冻结状态保持不变</b>——所以移除之后再 <see cref="Register"/> 仍然会被拒，
    /// <see cref="LateRegistrationRejected"/> 的含义也不受影响。
    /// </para>
    /// </summary>
    /// <param name="rule">要移除的规则实例。</param>
    /// <returns>确实移除了返回 true；表里没有这条规则返回 false。</returns>
    public bool Unregister(MonoDamageRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (!rules.Remove(rule))
            return false;

        Removed++;
        Recompile();
        return true;
    }

    /// <summary>移除所有满足条件的规则。</summary>
    /// <param name="match">条件。</param>
    /// <returns>移除的条数。</returns>
    public int UnregisterAll(Predicate<MonoDamageRule> match)
    {
        ArgumentNullException.ThrowIfNull(match);

        int removed = rules.RemoveAll(match);
        if (removed > 0)
        {
            Removed += removed;
            Recompile();
        }

        return removed;
    }

    /// <summary>移除某一类型（含子类）的全部规则。</summary>
    /// <typeparam name="T">规则类型。</typeparam>
    /// <returns>移除的条数。</returns>
    public int UnregisterAll<T>() where T : MonoDamageRule
        => UnregisterAll(static rule => rule is T);

    /// <summary>增删之后重编译：没冻结过就什么都不做，冻结过就立刻重新定序。</summary>
    private void Recompile()
    {
        if (frozen is not null)
            Freeze();
    }

    /// <summary>
    /// 跑一趟结算：按段序把适用规则依次作用到 <paramref name="values"/> 上。
    /// </summary>
    /// <param name="info">这次命中。</param>
    /// <param name="values">累加器，按引用改。</param>
    /// <param name="trace">诊断追踪。为 <c>null</c> 或未开启时零成本，只多一次分支。</param>
    /// <exception cref="InvalidOperationException">规则表还没冻结。</exception>
    public void Evaluate(in MonoDamageInfo info, ref MonoDamageValues values, MonoDamageTrace? trace = null)
    {
        MonoDamageRule[]? chain = frozen;
        if (chain is null)
            throw new InvalidOperationException("规则表还没冻结。装配完成时调一次 MonoDamageChain.Freeze()，之后规则表只读。");

        MonoDamageRule[] ordered = RulesFor(info.TargetType);
        bool tracing = trace is not null && trace.Enabled;

        for (int i = 0; i < ordered.Length; i++)
        {
            MonoDamageRule rule = ordered[i];
            if (!rule.AppliesTo(in info))
                continue;

            if (!tracing)
            {
                rule.Apply(in info, ref values);
                continue;
            }

            MonoDamageValues before = values;
            rule.Apply(in info, ref values);
            trace!.Add(rule, rule.Stage, in before, in values);
        }
    }

    /// <summary>取某个目标类型的执行序列，第一次用时惰性建立并缓存。</summary>
    /// <param name="targetType">目标类型；<c>-1</c> 表示"只跑全局规则"。</param>
    private MonoDamageRule[] RulesFor(int targetType)
    {
        if (byTargetType.TryGetValue(targetType, out MonoDamageRule[]? cached))
            return cached;

        MonoDamageRule[] chain = frozen!;
        int count = 0;
        for (int i = 0; i < chain.Length; i++)
        {
            if (chain[i].TargetType < 0 || chain[i].TargetType == targetType)
                count++;
        }

        MonoDamageRule[] slice = new MonoDamageRule[count];
        int write = 0;
        for (int i = 0; i < chain.Length; i++)
        {
            MonoDamageRule rule = chain[i];
            if (rule.TargetType < 0 || rule.TargetType == targetType)
                slice[write++] = rule;
        }

        byTargetType[targetType] = slice;
        return slice;
    }

    /// <summary>诊断输出：按实际执行顺序列出规则。</summary>
    public string Describe()
    {
        if (rules.Count == 0)
            return "规则表是空的。";

        MonoDamageRule[] ordered = frozen ?? [.. rules];
        StringBuilder output = new();
        output.Append("规则 ").Append(rules.Count).Append(" 条")
              .Append(frozen is null ? "（未冻结，下面是登记顺序）" : "（已冻结，下面是执行顺序）")
              .Append("：");

        for (int i = 0; i < ordered.Length; i++)
        {
            MonoDamageRule rule = ordered[i];
            output.Append("\n  #").Append(i.ToString("D2"))
                  .Append("  ").Append(rule.Stage.Describe())
                  .Append("  order=").Append(rule.Order)
                  .Append("  目标=").Append(rule.TargetType < 0 ? "全部" : rule.TargetType.ToString())
                  .Append("  ").Append(rule.Name);
        }

        return output.ToString();
    }
}
