using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage.Rules;

/// <summary>
/// 总减伤封顶，对应蓝图减伤层的 D7 与 D8。上下限段。
/// <para>
/// <b>D7 与 D8 在这里是同一个通道。</b>"总减伤最多到 90%"与"伤害不低于原始的一半"说的都是
/// <see cref="MonoDamageValues.DamageMul"/> 的下界：前者是 <c>0.1</c>，后者是 <c>0.5</c>。
/// 蓝图把它们列成两层是因为它设想的是重算整条管线；在薄层里最终那个数由原版算，库能保证的只有比例下界，
/// <b>绝对点数下限（"至少打出 5 点"）在原版字段上无处安放</b>，所以没有第二个类。
/// </para>
/// <para>
/// 它必须存在：加算池里多个减伤来源相加会很快积到接近 100%，玩家遇到的就是"打不动"。多个封顶规则同时命中时取最松的那个
/// （下界取最大）。
/// </para>
/// </summary>
public sealed class MonoAbsorbCapRule : MonoPredicatedRule
{
    private readonly float absorbCap;

    /// <summary>建一条总减伤封顶规则。</summary>
    /// <param name="absorbCap">减伤比例的上限，<c>0.9</c> 表示"最多减到只剩一成"。</param>
    /// <param name="when">额外条件。</param>
    /// <param name="targetType">只管这一类目标；<c>-1</c> 表示所有目标。</param>
    public MonoAbsorbCapRule(float absorbCap, Func<MonoDamageInfo, bool>? when = null, int targetType = -1)
        : base(targetType, when)
        => this.absorbCap = Math.Clamp(absorbCap, 0f, 1f);

    /// <inheritdoc/>
    public override string Name => $"减伤封顶({absorbCap:0.###})";

    /// <inheritdoc/>
    public override MonoDamageStage Stage => MonoDamageStage.Clamp;

    /// <inheritdoc/>
    public override void Apply(in MonoDamageInfo info, ref MonoDamageValues values)
        => values.MinRatio = Math.Max(values.MinRatio, 1f - absorbCap);
}
