using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage.Rules;

/// <summary>
/// 按伤害标签乘一个系数，对应蓝图减伤层的 D2 与伤害标签一节。独立乘区段。
/// <para>
/// 系数小于 1 是抗性，大于 1 是弱点——两者是同一件事的两头，所以只有一个类。
/// 复数个标签各自一条规则时，它们的效果是<b>相乘</b>的（每个来源一个独立乘区），
/// 想让它们相加就得自己写规则把系数合起来，库不替你选。
/// </para>
/// <para>
/// 命中的标签由 <c>MonoProjTags</c> / <c>MonoItemTags</c> 在发射时挂上；没挂标签的命中不会触发它。
/// </para>
/// </summary>
public sealed class MonoElementMultiplierRule : MonoPredicatedRule
{
    private readonly ulong tag;
    private readonly float multiplier;

    /// <summary>建一条元素倍率规则。</summary>
    /// <param name="tag">触发用的标签掩码，可以同时写多个标签。</param>
    /// <param name="multiplier">命中这些标签时乘的系数。小于 1 是抗性，大于 1 是弱点。</param>
    /// <param name="when">额外条件。</param>
    /// <param name="targetType">只管这一类目标；<c>-1</c> 表示所有目标。</param>
    public MonoElementMultiplierRule(ulong tag, float multiplier, Func<MonoDamageInfo, bool>? when = null, int targetType = -1)
        : base(targetType, when)
    {
        this.tag = tag;
        this.multiplier = multiplier;
    }

    /// <inheritdoc/>
    public override string Name => $"元素倍率({MonoDamageTag.Describe(tag)}×{multiplier:0.###})";

    /// <inheritdoc/>
    public override MonoDamageStage Stage => MonoDamageStage.PercentMul;

    /// <inheritdoc/>
    public override bool AppliesTo(in MonoDamageInfo info) => base.AppliesTo(info) && info.HasTag(tag);

    /// <inheritdoc/>
    public override void Apply(in MonoDamageInfo info, ref MonoDamageValues values) => values.DamageMul *= multiplier;
}
