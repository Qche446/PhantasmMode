using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage.Rules;

/// <summary>
/// 血线曲线减伤，对应蓝图减伤层的 D5。减伤段。
/// <para>
/// 公式是 <c>dr = maxReduction × (当前血量 / 满血)^exponent</c>：满血时减伤最多，血越少减伤越接近 0。
/// <c>exponent</c> 控制曲线形状，<c>1</c> 是线性，越大越集中在满血附近。
/// </para>
/// <para>
/// 它要读目标的血量，所以只对 NPC 生效（<c>info.AgainstPlayer</c> 为真时直接不适用）。
/// 血量是各端同步的数据，攻击方读到的是它那一刻的值，这一点与原版其它按血量判定的逻辑一致。
/// </para>
/// </summary>
public sealed class MonoHpRatioReductionRule : MonoPredicatedRule
{
    private readonly float maxReduction;
    private readonly float exponent;

    /// <summary>建一条血线曲线减伤规则。</summary>
    /// <param name="maxReduction">满血时的减伤比例。</param>
    /// <param name="exponent">曲线指数，<c>1</c> 为线性。</param>
    /// <param name="when">额外条件。</param>
    /// <param name="targetType">只管这一类目标；<c>-1</c> 表示所有目标。</param>
    public MonoHpRatioReductionRule(float maxReduction, float exponent = 1f,
        Func<MonoDamageInfo, bool>? when = null, int targetType = -1)
        : base(targetType, when)
    {
        this.maxReduction = Math.Clamp(maxReduction, 0f, 1f);
        this.exponent = Math.Max(exponent, 0f);
    }

    /// <inheritdoc/>
    public override string Name => $"血线减伤(上限 {maxReduction:0.###}, k={exponent:0.###})";

    /// <inheritdoc/>
    public override MonoDamageStage Stage => MonoDamageStage.Reduction;

    /// <inheritdoc/>
    public override bool AppliesTo(in MonoDamageInfo info) => base.AppliesTo(info) && !info.AgainstPlayer;

    /// <inheritdoc/>
    public override void Apply(in MonoDamageInfo info, ref MonoDamageValues values)
    {
        if ((uint)info.TargetIndex >= (uint)Main.npc.Length)
            return;

        NPC npc = Main.npc[info.TargetIndex];
        if (!npc.active || npc.lifeMax <= 0)
            return;

        float ratio = Math.Clamp((float)npc.life / npc.lifeMax, 0f, 1f);
        values.ReductionAdd += maxReduction * MathF.Pow(ratio, exponent);
    }
}
