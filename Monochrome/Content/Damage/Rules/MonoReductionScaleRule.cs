using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage.Rules;

/// <summary>
/// 削减目标的伤害减免，也就是把总减伤按比例削掉一截。减伤段。
/// <para>
/// 它写的是减伤缩放（dr2）：<c>factor = 0.5</c> 表示"总减伤砍一半"——目标本来四成减伤，变成两成。
/// 多个削减来源<b>相乘</b>。
/// </para>
/// <para>
/// 作用范围是规则表里的减伤，也就是 <see cref="MonoConditionalReductionRule"/>、
/// <see cref="MonoHpRatioReductionRule"/>、<see cref="MonoDynamicReductionRule"/> 写进加算池的那些。
/// <see cref="MonoElementMultiplierRule"/> 走的是伤害乘区，不受它影响。
/// </para>
/// <para>
/// 它不需要知道目标当前减伤是多少：收尾时库把加算池与缩放相乘再并入乘区，所以削减对动态的减伤
/// （血线曲线、跟随战斗进度）同样成立，消费者不必复刻那些公式。
/// </para>
/// </summary>
public sealed class MonoReductionScaleRule : MonoPredicatedRule
{
    private readonly float factor;

    /// <summary>建一条减伤削减规则。</summary>
    /// <param name="factor">减伤保留系数，夹进 <c>[0, 1]</c>。<c>0.5</c> 是砍一半，<c>0</c> 是削光。</param>
    /// <param name="when">额外条件。</param>
    /// <param name="targetType">只管这一类目标；<c>-1</c> 表示所有目标。</param>
    public MonoReductionScaleRule(float factor, Func<MonoDamageInfo, bool>? when = null, int targetType = -1)
        : base(targetType, when)
        => this.factor = Math.Clamp(factor, 0f, 1f);

    /// <inheritdoc/>
    public override string Name => $"减伤削减(×{factor:0.###})";

    /// <inheritdoc/>
    public override MonoDamageStage Stage => MonoDamageStage.Reduction;

    /// <inheritdoc/>
    public override void Apply(in MonoDamageInfo info, ref MonoDamageValues values) => values.ReductionScale *= factor;
}
