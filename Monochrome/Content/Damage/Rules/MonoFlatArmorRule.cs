using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage.Rules;

/// <summary>
/// 固定值护甲增减，对应蓝图减伤层的 D1。<see cref="MonoDamageStage.Defense"/> 段。
/// <para>
/// 正数是加护甲（更硬），负数是破甲（更脆）——原版减益"灵液"就是这一层的负值版
/// （它直接改 <c>Defense.Flat</c>）。
/// </para>
/// <para>
/// 它改的是<b>防御值</b>，所以最终削减多少伤害还取决于原版的防御换算系数与目标的穿透。
/// 想要"这一下少打 x 点"的确定效果，用 <see cref="MonoConditionalReductionRule"/> 之外的固定值手段
/// 在薄层里做不到，原因写在 <c>Content/Damage/README.md</c> 的边界一节。
/// </para>
/// </summary>
public sealed class MonoFlatArmorRule : MonoPredicatedRule
{
    private readonly float flat;

    /// <summary>建一条固定值护甲规则。</summary>
    /// <param name="flat">护甲增减值。正数加护甲，负数破甲。</param>
    /// <param name="when">额外条件。</param>
    /// <param name="targetType">只管这一类目标；<c>-1</c> 表示所有目标。</param>
    public MonoFlatArmorRule(float flat, Func<MonoDamageInfo, bool>? when = null, int targetType = -1)
        : base(targetType, when)
        => this.flat = flat;

    /// <inheritdoc/>
    public override string Name => $"固定护甲({flat:0.###})";

    /// <inheritdoc/>
    public override MonoDamageStage Stage => MonoDamageStage.Defense;

    /// <inheritdoc/>
    public override void Apply(in MonoDamageInfo info, ref MonoDamageValues values) => values.DefenseFlat += flat;
}
