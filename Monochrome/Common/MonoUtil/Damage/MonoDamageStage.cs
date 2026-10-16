namespace Monochrome.Common.MonoUtil.Damage;

/// <summary>
/// 伤害流水线的段号。它同时是分类标签与遍历顺序。
/// <para>
/// 它<b>不</b>规定每段里数值怎么算。第 <see cref="PercentMul"/> 段叫"独立乘区"，库只保证它排在
/// <see cref="PercentAdd"/> 之后，既不检查规则往里写的是乘法还是加法，也不规定哪个来源算加算、哪个算乘算。
/// 定的是位置，与算法无关。
/// </para>
/// <para>
/// 段号是分类与诊断用的标签，<b>不构成顺序约束</b>：各段写的是 <c>HitModifiers</c> 上互不相交的字段，
/// 真正把它们合起来的是原版 <c>GetDamage</c>，而加法与乘法各自可交换、取最小与取最大都幂等。
/// 库唯一固定下来的是"夹取在收尾做"，那由 <c>MonoDamage.Run</c> 在规则跑完之后无条件调 <c>Clamp()</c>
/// 保证，与规则挂在哪个段没有关系。
/// </para>
/// </summary>
public enum MonoDamageStage
{
    /// <summary>源伤缩放。作用在基础伤害上的乘区，对应原版 <c>HitModifiers.SourceDamage</c>。</summary>
    Source = 0,

    /// <summary>固定加伤。加在基础伤害上、不进暴击与防御的那部分。</summary>
    Flat = 1,

    /// <summary>百分比加伤。同一个加法池，多个来源先求和再作用一次。</summary>
    PercentAdd = 2,

    /// <summary>独立乘区。每个来源各自相乘。减伤与增伤都建议写在这里，<see cref="Clamp"/> 段才看得见它们。</summary>
    PercentMul = 3,

    /// <summary>暴击。暴击倍率、强制暴击与禁用暴击。</summary>
    Crit = 4,

    /// <summary>防御。护甲的固定值与倍率。</summary>
    Defense = 5,

    /// <summary>减伤。按条件削减最终伤害的来源。</summary>
    Reduction = 6,

    /// <summary>穿透。按比例或固定值无视防御与减伤。</summary>
    Penetration = 7,

    /// <summary>免疫。把这次命中整体作废，闪避与免疫判定写在这里。</summary>
    Immunity = 8,

    /// <summary>上下限。把前面各段攒出来的乘区夹一次：总减伤封顶与伤害下限在这一段。</summary>
    Clamp = 9,

    /// <summary>收尾。需要看到夹过之后的乘区的规则（记录、上报）放在这里。</summary>
    Post = 10,
}

/// <summary>段号的中文名，给诊断输出用。</summary>
public static class MonoDamageStageExtensions
{
    /// <summary>取段号的中文名。认不出的值回落到编号。</summary>
    /// <param name="stage">段号。</param>
    public static string Describe(this MonoDamageStage stage) => stage switch
    {
        MonoDamageStage.Source => "源伤缩放",
        MonoDamageStage.Flat => "固定加伤",
        MonoDamageStage.PercentAdd => "百分比加伤",
        MonoDamageStage.PercentMul => "独立乘区",
        MonoDamageStage.Crit => "暴击",
        MonoDamageStage.Defense => "防御",
        MonoDamageStage.Reduction => "减伤",
        MonoDamageStage.Penetration => "穿透",
        MonoDamageStage.Immunity => "免疫",
        MonoDamageStage.Clamp => "上下限",
        MonoDamageStage.Post => "收尾",
        _ => $"段 {(int)stage}",
    };
}
