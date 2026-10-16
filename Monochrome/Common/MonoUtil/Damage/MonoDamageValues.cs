namespace Monochrome.Common.MonoUtil.Damage;

/// <summary>
/// 一趟伤害流水线里被规则读写的全部数值。它是库自己的累加器，规则改它，收尾时再一次性映射回
/// <c>HitModifiers</c>——伤害最后仍然由原版算，这里不重算原版的公式。
/// <para>
/// <b>这一层的用处。</b>原版的 <c>FinalDamage</c> 是个 <c>StatModifier</c>：每个来源往里塞一个互相独立的
/// 乘区，规则 A 塞 0.6、规则 B 塞 0.8，它们互相看不见，谁也算不出"乘积是多少"。于是"总减伤封顶 90%"
/// 这种话在原版字段上没法表达。累加器把乘区攒在一处，<see cref="MonoDamageStage.Clamp"/> 段才夹得动它。
/// </para>
/// <para>
/// <b>中性值不要用 <c>default</c>。</b><c>default</c> 是全零结构体，乘区会被当成 0，那就是伤害归零。
/// 一律从 <see cref="Neutral"/> 起步。
/// </para>
/// </summary>
public struct MonoDamageValues
{
    /// <summary>源伤乘区。中性值 1，映射到原版 <c>SourceDamage</c>。</summary>
    public float SourceMul;

    /// <summary>固定加伤。中性值 0，映射到原版 <c>FlatBonusDamage</c>。</summary>
    public float FlatBonus;

    /// <summary>百分比加伤（加法池）。中性值 0，映射到原版 <c>FinalDamage</c> 的加法区。</summary>
    public float PercentAdd;

    /// <summary>独立乘区。中性值 1。减伤与增伤都写这里，<see cref="MonoDamageStage.Clamp"/> 段只夹它。</summary>
    public float DamageMul;

    /// <summary>防御固定值增减。中性值 0，映射到原版 <c>Defense.Flat</c>。</summary>
    public float DefenseFlat;

    /// <summary>防御倍率。中性值 1，映射到原版 <c>Defense</c> 的乘法区。</summary>
    public float DefenseMul;

    /// <summary>护甲穿透固定值。中性值 0，映射到原版 <c>ArmorPenetration</c>。</summary>
    public float ArmorPenetration;

    /// <summary>
    /// 加算减伤（dr1），中性值 0。<b>多个来源相加</b>：写 <c>+0.1</c> 是多一成减伤，写 <c>-0.1</c> 是扣掉一成。
    /// 它不直接落到乘区上，而是与 <see cref="ReductionScale"/> 相乘后由收尾一次性并入
    /// <see cref="DamageMul"/>，公式是 <c>DamageMul *= 1 - ReductionAdd × ReductionScale</c>。
    /// </summary>
    public float ReductionAdd;

    /// <summary>
    /// 减伤缩放（dr2），中性值 1。<b>多个来源相乘</b>。写 <c>0.5</c> 表示"把总减伤砍一半"——
    /// 目标本来四成减伤就变成两成。它表达的是"削弱目标的伤害减免"这类效果。
    /// </summary>
    public float ReductionScale;

    /// <summary>
    /// 护甲穿透比例。中性值 0，映射到原版 <c>ScalingArmorPenetration</c>；<c>1</c> 表示完全无视防御。
    /// 原版把它做成<b>加法池</b>（多个来源相加后一次性作用），这里同样相加。
    /// </summary>
    public float ScalingArmorPenetration;

    /// <summary><see cref="DamageMul"/> 的下界，中性值 0。总减伤封顶就写这里。</summary>
    public float MinRatio;

    /// <summary><see cref="DamageMul"/> 的上界，中性值正无穷。想给"增伤也有个顶"就写这里。</summary>
    public float MaxRatio;

    /// <summary>
    /// 这次命中的伤害绝对上限（点）。中性值 <see cref="int.MaxValue"/>，即不设上限。
    /// 落到原版的 <c>SetMaxDamage</c>；多个来源同时设时原版自己取最小的那个。
    /// </summary>
    public int MaxDamage;

    /// <summary>这次命中是否整体作废（闪避 / 免疫）。伤害会落到原版的保底值。</summary>
    public bool Nullified;

    /// <summary>
    /// 中性值：什么规则都还没跑时的初值。每次结算都从它开始，<b>不要用 <c>default</c></b>。
    /// </summary>
    public static MonoDamageValues Neutral() => new()
    {
        SourceMul = 1f,
        DamageMul = 1f,
        DefenseMul = 1f,
        MinRatio = 0f,
        MaxRatio = float.PositiveInfinity,
        MaxDamage = int.MaxValue,
        ReductionScale = 1f,
    };

    /// <summary>
    /// 把非有限值拉回中性。NaN 与正负无穷都算非有限。
    /// <para>
    /// 它防的是"一条规则算出了 NaN，整条链后面的规则全被污染"：原版的结算最终会
    /// <c>(int)</c> 掉一个 NaN，那在 C# 里是 0 或未定义值，玩家看到的就是"伤害忽高忽低"。
    /// </para>
    /// </summary>
    public void Sanitize()
    {
        SourceMul = FiniteOr(SourceMul, 1f);
        FlatBonus = FiniteOr(FlatBonus, 0f);
        PercentAdd = FiniteOr(PercentAdd, 0f);
        DamageMul = FiniteOr(DamageMul, 1f);
        DefenseFlat = FiniteOr(DefenseFlat, 0f);
        DefenseMul = FiniteOr(DefenseMul, 1f);
        ArmorPenetration = FiniteOr(ArmorPenetration, 0f);
        ScalingArmorPenetration = FiniteOr(ScalingArmorPenetration, 0f);
        ReductionAdd = FiniteOr(ReductionAdd, 0f);
        ReductionScale = FiniteOr(ReductionScale, 1f);

        // 下界被算坏时按"没有下界"处理。按 0 处理等于取消封顶，会静默地放开无敌。
        MinRatio = FiniteOr(NonNegative(MinRatio), 0f);

        // 上界只接受有限正值，其余（NaN / 负数 / 无穷）一律当"没有上界"。
        MaxRatio = float.IsFinite(MaxRatio) && MaxRatio > 0f ? MaxRatio : float.PositiveInfinity;

        // 绝对上限最小是 1：原版的 clamp 下界也是 1，设成 0 只会让人以为能打出零伤害。
        if (MaxDamage < 1)
            MaxDamage = 1;
    }

    /// <summary>
    /// 收尾。三件事按序做：<see cref="Sanitize"/> → 把加算减伤与减伤缩放合成后并入 <see cref="DamageMul"/>
    /// → 把 <see cref="DamageMul"/> 夹进 [<see cref="MinRatio"/>, <see cref="MaxRatio"/>]。
    /// <para>
    /// 合成公式是 <c>DamageMul *= 1 - ReductionAdd × ReductionScale</c>。加算池管"多一层减伤"与"扣掉一层"，
    /// 缩放管"把总减伤按比例削掉"。乘积先夹进 <c>[0, 1]</c>，所以减伤叠过头最多把伤害压到 0，不会翻成负值。
    /// </para>
    /// <para>
    /// 上界低于下界时按"上界 = 下界"处理，不抛异常——伤害结算在一帧里可能跑很多次，
    /// 参数写反了应该是一个能被日志看见的怪数值，不该把整场战斗炸掉。
    /// </para>
    /// </summary>
    public void Clamp()
    {
        Sanitize();

        float reduction = Math.Clamp(ReductionAdd * ReductionScale, 0f, 1f);
        DamageMul *= 1f - reduction;

        float low = MinRatio;
        float high = Math.Max(MaxRatio, low);
        DamageMul = Math.Clamp(DamageMul, low, high);
    }

    private static float FiniteOr(float value, float fallback) => float.IsFinite(value) ? value : fallback;

    private static float NonNegative(float value) => value > 0f ? value : 0f;
}
