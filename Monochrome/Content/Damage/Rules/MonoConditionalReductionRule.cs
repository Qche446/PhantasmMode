using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage.Rules;

/// <summary>
/// 条件式百分比减伤，对应蓝图减伤层的 D4。减伤段。
/// <para>
/// "条件"完全由 <c>when</c> 决定：阶段、血线、是否被控、弹幕类型，消费者想判什么都行。
/// 多个来源同时成立时它们<b>相加</b>（都写进加算减伤池），所以减伤会很快积到很高，
/// 配一条 <see cref="MonoAbsorbCapRule"/> 几乎是必须的。
/// </para>
/// <para>
/// <c>reduction</c> 会被夹进 <c>[0, 1]</c>。负数等于给敌人回血，按零处理。
/// 想要增伤请用 <see cref="MonoElementMultiplierRule"/> 或自己写规则。
/// </para>
/// </summary>
public sealed class MonoConditionalReductionRule : MonoPredicatedRule
{
    private readonly float reduction;

    /// <summary>建一条条件减伤规则。</summary>
    /// <param name="reduction">减伤比例，<c>0.4</c> 表示少打四成。</param>
    /// <param name="when">触发条件。</param>
    /// <param name="targetType">只管这一类目标；<c>-1</c> 表示所有目标。</param>
    public MonoConditionalReductionRule(float reduction, Func<MonoDamageInfo, bool>? when = null, int targetType = -1)
        : base(targetType, when)
        => this.reduction = Math.Clamp(reduction, 0f, 1f);

    /// <inheritdoc/>
    public override string Name => $"条件减伤({reduction:0.###})";

    /// <inheritdoc/>
    public override MonoDamageStage Stage => MonoDamageStage.Reduction;

    /// <inheritdoc/>
    public override void Apply(in MonoDamageInfo info, ref MonoDamageValues values) => values.ReductionAdd += reduction;
}
