using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage.Rules;

/// <summary>
/// 破甲与穿透，对应蓝图减伤层的 D9。穿透段。
/// <para>
/// 两种形态对应原版自己的两个字段：<c>scaling</c> 是按比例无视防御
/// （落到 <c>ScalingArmorPenetration</c>，<c>1</c> 表示完全无视），<c>flat</c> 是固定值穿透
/// （落到 <c>ArmorPenetration</c>）。原版把比例穿透做成加法池，多条规则相加后一次性作用。
/// </para>
/// <para>
/// <b>穿透必须在场。</b>没有它，减伤系统会不可逆地压掉所有"破防"类玩法：消费者再也拿不出一条
/// 能穿过前面那些减伤的路径。
/// </para>
/// </summary>
public sealed class MonoPenetrationRule : MonoPredicatedRule
{
    private readonly float scaling;
    private readonly float flat;

    /// <summary>建一条穿透规则。</summary>
    /// <param name="scaling">按比例无视防御，夹进 <c>[0, 1]</c>。</param>
    /// <param name="flat">固定值穿透。</param>
    /// <param name="when">额外条件。</param>
    /// <param name="targetType">只管这一类目标；<c>-1</c> 表示所有目标。</param>
    public MonoPenetrationRule(float scaling, float flat = 0f,
        Func<MonoDamageInfo, bool>? when = null, int targetType = -1)
        : base(targetType, when)
    {
        this.scaling = Math.Clamp(scaling, 0f, 1f);
        this.flat = Math.Max(flat, 0f);
    }

    /// <inheritdoc/>
    public override string Name => $"穿透(比例 {scaling:0.###}, 固定 {flat:0.###})";

    /// <inheritdoc/>
    public override MonoDamageStage Stage => MonoDamageStage.Penetration;

    /// <inheritdoc/>
    public override void Apply(in MonoDamageInfo info, ref MonoDamageValues values)
    {
        values.ScalingArmorPenetration += scaling;
        values.ArmorPenetration += flat;
    }
}
