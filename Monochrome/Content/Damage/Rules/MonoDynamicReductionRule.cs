using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage.Rules;

/// <summary>
/// 按战斗进度浮动的减伤。减伤段。
/// <para>
/// 判定只用两个进度量：血量比例与时间比例（<c>已打时长 / idealKillSeconds</c>）。两者相加就是"进度"，
/// 正好按理想节奏打时等于 1；掉血快过时间推进时进度低于 1，此时追加减伤。公式与推导在
/// <see cref="MonoProgressReduction"/>，它取自灾厄对夜天神用的那套动态减伤。
/// </para>
/// <para>
/// <b>它和单次上限各管一头。</b><see cref="MonoBurstGuardRule"/> 拦某一下的数字，
/// 这条拦的是"整场打得太快"。两个一起用才不会出现"磨着不放也能很快过"的情况。
/// </para>
/// <para>
/// <b>调参看两个数。</b><c>idealKillSeconds</c> 填这场战斗该打多久，一般每个 boss 一条规则、各填各的；
/// <c>extraScale</c> 填落后时追加得有多狠，默认 10，追加上限是 <c>(1 - baseReduction) × extraScale</c> 的一半。
/// 按 FPM 的难度，这个默认值大概率要往下调，建议从 2 附近试。
/// </para>
/// <para>
/// 计时直接读 <see cref="MonoDamageNPC.ElapsedTicks"/>：那只全局 NPC 每个实体一份，<c>PreAI</c> 每 tick
/// 加一，与灾厄把 <c>killTimeTimer</c> 放在 GlobalNPC 的 PreAI 里是同一个形态。
/// </para>
/// <para>
/// 灾厄原版里还有"临时降低 boss 减伤"（<c>ApplyDRReduction</c>）与"不可削减的减伤"两个概念。
/// 这里没有做：库这一层没有对应的消费者，需要的消费者继承本类自行处理。
/// </para>
/// </summary>
public sealed class MonoDynamicReductionRule : MonoPredicatedRule
{
    private readonly float idealKillSeconds;
    private readonly float idealKillTicks;
    private readonly float baseReduction;
    private readonly float extraScale;
    private readonly float minReduction;
    private readonly float maxReduction;

    /// <summary>建一条按战斗进度浮动的减伤规则。</summary>
    /// <param name="idealKillSeconds">这场战斗的理想时长（秒）。填 0 或负数表示不启用追加减伤，只留基准值。</param>
    /// <param name="baseReduction">基准减伤，也是进度达标时的减伤。</param>
    /// <param name="extraScale">额外减伤的量级系数。越大，落后时追加得越狠。</param>
    /// <param name="minReduction">减伤下限。</param>
    /// <param name="maxReduction">减伤上限。</param>
    /// <param name="when">额外条件。</param>
    /// <param name="targetType">只管这一类目标；<c>-1</c> 表示所有目标。</param>
    public MonoDynamicReductionRule(float idealKillSeconds, float baseReduction = 0.2f, float extraScale = 10f,
        float minReduction = 0f, float maxReduction = 1f,
        Func<MonoDamageInfo, bool>? when = null, int targetType = -1)
        : base(targetType, when)
    {
        this.idealKillSeconds = idealKillSeconds > 0f ? idealKillSeconds : 0f;
        this.idealKillTicks = this.idealKillSeconds * 60f;   // 计时按 tick 累加，这里换成人读的秒
        this.baseReduction = Math.Clamp(baseReduction, 0f, 1f);
        this.extraScale = Math.Max(extraScale, 0f);
        this.minReduction = Math.Clamp(minReduction, 0f, 1f);
        this.maxReduction = Math.Clamp(Math.Max(maxReduction, this.minReduction), 0f, 1f);
    }

    /// <inheritdoc/>
    public override string Name => $"动态减伤(理想 {idealKillSeconds:0}s, 基准 {baseReduction:0.###}, 系数 {extraScale:0.###})";

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

        float hpRatio = (float)npc.life / npc.lifeMax;
        float timeRatio = idealKillTicks > 0f ? npc.GetGlobalNPC<MonoDamageNPC>().ElapsedTicks / idealKillTicks : 1f;

        float reduction = MonoProgressReduction.Compute(
            baseReduction, extraScale, hpRatio, timeRatio, minReduction, maxReduction);

        values.ReductionAdd += reduction;
    }
}
