using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage.Rules;

/// <summary>
/// 单次伤害上限，对应蓝图减伤层的 D6。上下限段。
/// <para>
/// 它落到原版的 <c>HitModifiers.SetMaxDamage</c>：原版规定多个来源同时设上限时<b>取最小</b>，
/// 所以"防一击秒杀"与"防秒杀连段"都只是往这个通道里塞一个数，不需要谁去协调。
/// </para>
/// <para>
/// 上限是<b>绝对值</b>（点）。原版的 clamp 下界是 1，所以写 0 没有意义，
/// 构造时会抬到 1。
/// </para>
/// </summary>
public sealed class MonoBurstGuardRule : MonoPredicatedRule
{
    private readonly int maxPerHit;

    /// <summary>建一条单次上限规则。</summary>
    /// <param name="maxPerHit">单次命中的伤害上限（点）。</param>
    /// <param name="when">额外条件。</param>
    /// <param name="targetType">只管这一类目标；<c>-1</c> 表示所有目标。</param>
    public MonoBurstGuardRule(int maxPerHit, Func<MonoDamageInfo, bool>? when = null, int targetType = -1)
        : base(targetType, when)
        => this.maxPerHit = Math.Max(maxPerHit, 1);

    /// <inheritdoc/>
    public override string Name => $"单次上限({maxPerHit})";

    /// <inheritdoc/>
    public override MonoDamageStage Stage => MonoDamageStage.Clamp;

    /// <inheritdoc/>
    public override void Apply(in MonoDamageInfo info, ref MonoDamageValues values)
        => values.MaxDamage = Math.Min(values.MaxDamage, maxPerHit);
}
