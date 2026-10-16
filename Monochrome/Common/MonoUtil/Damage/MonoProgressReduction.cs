namespace Monochrome.Common.MonoUtil.Damage;

/// <summary>
/// 按战斗进度追加的减伤，公式取自灾厄对夜天神用的那套动态减伤。
/// <para>
/// 判定只用两个进度量：血量比例 <c>hpRatio = life / lifeMax</c>（1 掉到 0）与时间比例
/// <c>timeRatio = 已打时长 / 理想时长</c>（0 涨到 1）。两者相加就是"进度"：正好按理想节奏打时它等于 1。
/// </para>
/// <para>
/// 进度低于 1 说明掉血快过了时间推进，此时追加减伤，追加量随差距从 0 涨到
/// <c>(1 - baseReduction) × extraScale / 2</c>；进度不低于 1 或者时间比例已经到 1（超时）时不追加。
/// 超时之后不再惩罚，是因为那条路上出现的是"打得太慢"，不是这条规则要管的事。
/// </para>
/// <para>
/// 它是纯函数：只吃四个数，不读实体、不读时钟，所以可以进离线验收台。
/// 血量与时长由 <c>Content/Damage/Rules/MonoDynamicReductionRule</c> 从游戏里取。
/// </para>
/// </summary>
public static class MonoProgressReduction
{
    /// <summary>
    /// 算出这次命中实际生效的减伤比例。
    /// </summary>
    /// <param name="baseReduction">基准减伤，也是进度达标时的减伤。</param>
    /// <param name="extraScale">额外减伤的量级系数。越大，落后时追加得越狠。</param>
    /// <param name="hpRatio">血量比例，1 为满血。</param>
    /// <param name="timeRatio">时间比例，1 为打满理想时长。</param>
    /// <param name="minReduction">减伤下限。</param>
    /// <param name="maxReduction">减伤上限。默认 1，即最多让伤害落到原版的保底值。</param>
    public static float Compute(float baseReduction, float extraScale, float hpRatio, float timeRatio,
        float minReduction = 0f, float maxReduction = 1f)
    {
        float effective = baseReduction;

        if (timeRatio < 1f)
        {
            float progress = Math.Clamp(hpRatio, 0f, 1f) + Math.Clamp(timeRatio, 0f, 1f);
            if (progress < 1f)
            {
                float limit = (1f - baseReduction) * extraScale;
                float deficit = 1f - progress;
                effective += limit - limit / (1f + deficit);
            }
        }

        return Math.Clamp(effective, minReduction, maxReduction);
    }
}
