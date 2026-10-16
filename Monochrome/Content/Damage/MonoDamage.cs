using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage;

/// <summary>
/// 伤害规则表的对消费者入口：登记规则、冻结、跑一趟、看追踪。
/// <para>
/// <b>它不改原版的伤害公式。</b>规则跑完之后，这里只把累加器里的数写回 <c>HitModifiers</c> / <c>HurtModifiers</c>
/// 已有的字段，最终那个数仍然由原版的 <c>GetDamage</c> 算。所以第三方模组直接改这些字段的效果照旧生效，
/// 两边写的是同一组字段，效果叠加。
/// </para>
/// <para>
/// <b>规则表什么时候冻结。</b>显式调 <see cref="Freeze"/> 最稳妥；不调的话第一次命中时自动冻结，
/// 那之后再 <see cref="Register"/> 的规则会被拒绝并计数（<see cref="MonoDamageChain.LateRegistrationRejected"/>）。
/// 之所以不在服务装配完成时就冻结，是因为消费者可能在更晚的 <c>PostSetupContent</c> 里才登记规则。
/// </para>
/// <para>
/// <b>累加器与直通的分工。</b>伤害数值一律走累加器（减伤、增伤、上下限），只有这样才能在
/// <see cref="MonoDamageStage.Clamp"/> 段夹一次；击退、命中方向这类非伤害字段消费者直接改原版字段即可。
/// 别人直接写进 <c>FinalDamage</c> 的乘区不在封顶计算里——库看不见它们，这条边界写在
/// <c>Content/Damage/README.md</c>。
/// </para>
/// </summary>
public static class MonoDamage
{
    private static readonly MonoDamageChain chain = new();
    private static readonly MonoDamageTrace trace = new();

    /// <summary>规则表本体。命令面与诊断看它。</summary>
    public static MonoDamageChain Chain => chain;

    /// <summary>过程记录。默认关闭，开启后每次命中都会重记一遍。</summary>
    public static MonoDamageTrace Trace => trace;

    /// <summary>规则表是否已经冻结。</summary>
    public static bool IsFrozen => chain.IsFrozen;

    /// <summary>登记一条规则。在消费者的 <c>Mod.Load</c> 里调。</summary>
    /// <param name="rule">要登记的规则。</param>
    /// <returns>登记成功返回 true；表已冻结时返回 false。</returns>
    public static bool Register(MonoDamageRule rule) => chain.Register(rule);

    /// <summary>冻结规则表。冻结之后不再接受登记，求值顺序也就此定下来。</summary>
    public static void Freeze() => chain.Freeze();

    /// <summary>
    /// 移除一条规则，返回是否确实移除了。
    /// <para>
    /// 这是给"撤掉别人的规则"留的口子：附属模组想拿掉某条减伤规则时用它。表已冻结时立即重新编译，
    /// 冻结状态保持不变，所以移除之后再 <see cref="Register"/> 仍然会被拒。
    /// </para>
    /// </summary>
    /// <param name="rule">要移除的规则实例。</param>
    public static bool Unregister(MonoDamageRule rule) => chain.Unregister(rule);

    /// <summary>移除某一类型（含子类）的全部规则，返回移除的条数。</summary>
    /// <typeparam name="T">规则类型。</typeparam>
    public static int UnregisterAll<T>() where T : MonoDamageRule => chain.UnregisterAll<T>();

    /// <summary>移除所有满足条件的规则，返回移除的条数。</summary>
    /// <param name="match">条件。</param>
    public static int UnregisterAll(Predicate<MonoDamageRule> match) => chain.UnregisterAll(match);

    /// <summary>
    /// 跑一趟规则表并把结果写回 NPC 的命中修正。两条入口钩子（按物品、按弹幕）都调它。
    /// <para>
    /// 公开它是为了留一条路：如果消费者需要覆盖"没有来源"的伤害路径（NPC 打 NPC、原版
    /// <c>StrikeNPC</c> 的旧路径），可以自己在 <c>ModifyIncomingHit</c> 里构造一条来源不明的
    /// <see cref="MonoDamageInfo"/> 并调它。那样会与本库的两条钩子同时生效，需要消费者自己保证不重复施加。
    /// </para>
    /// </summary>
    /// <param name="info">这次命中。</param>
    /// <param name="modifiers">原版的命中修正，按引用写回。</param>
    public static void Run(in MonoDamageInfo info, ref NPC.HitModifiers modifiers)
    {
        EnsureFrozen();
        trace.BeginHit();

        MonoDamageValues values = MonoDamageValues.Neutral();
        chain.Evaluate(in info, ref values, trace);
        values.Clamp();
        CommitToNpc(in values, ref modifiers);
    }

    /// <summary>跑一趟规则表并把结果写回玩家的受击修正。</summary>
    /// <param name="info">这次命中。</param>
    /// <param name="modifiers">原版的受击修正，按引用写回。</param>
    public static void Run(in MonoDamageInfo info, ref Player.HurtModifiers modifiers)
    {
        EnsureFrozen();
        trace.BeginHit();

        MonoDamageValues values = MonoDamageValues.Neutral();
        chain.Evaluate(in info, ref values, trace);
        values.Clamp();
        CommitToPlayer(in values, ref modifiers);
    }

    /// <summary>规则表的文本快照，给命令面用。</summary>
    public static string Describe() => chain.Describe();

    /// <summary>清掉全部静态状态。由 <see cref="MonoDamageSystem"/> 在退场时调用。</summary>
    internal static void Reset()
    {
        chain.Clear();
        trace.Clear();
        trace.Enabled = false;
    }

    private static void EnsureFrozen()
    {
        if (!chain.IsFrozen)
            chain.Freeze();
    }

    /// <summary>
    /// 把累加器写回 NPC 的命中修正。每个字段只在偏离中性值时写，避免制造无意义的分支与浮点噪声。
    /// <para>
    /// <see cref="MonoDamageValues.PercentAdd"/> 走 <c>FinalDamage</c> 的加法池（<c>+=</c>），
    /// <see cref="MonoDamageValues.DamageMul"/> 走它的乘法池（<c>*=</c>）——这正是原版
    /// <c>StatModifier</c> 区分加法与独立乘区的地方。
    /// </para>
    /// </summary>
    private static void CommitToNpc(in MonoDamageValues values, ref NPC.HitModifiers modifiers)
    {
        if (values.SourceMul != 1f)
            modifiers.SourceDamage *= values.SourceMul;

        if (values.FlatBonus != 0f)
            modifiers.FlatBonusDamage += values.FlatBonus;

        if (values.PercentAdd != 0f)
            modifiers.FinalDamage += values.PercentAdd;

        if (values.DamageMul != 1f)
            modifiers.FinalDamage *= values.DamageMul;

        if (values.DefenseFlat != 0f)
            modifiers.Defense.Flat += values.DefenseFlat;

        if (values.DefenseMul != 1f)
            modifiers.Defense *= values.DefenseMul;

        if (values.ArmorPenetration != 0f)
            modifiers.ArmorPenetration += values.ArmorPenetration;

        if (values.ScalingArmorPenetration != 0f)
            modifiers.ScalingArmorPenetration += values.ScalingArmorPenetration;

        if (values.MaxDamage < int.MaxValue)
            modifiers.SetMaxDamage(values.MaxDamage);

        if (values.Nullified)
            modifiers.SetMaxDamage(1);
    }

    /// <summary>
    /// 把累加器写回玩家的受击修正。
    /// <para>
    /// 与 NPC 侧的差别来自原版结构本身：玩家侧没有 <c>FlatBonusDamage</c>，也没有可改的防御字段
    /// （防御是 <c>Player.statDefense</c>，由消费者自己改）。所以固定加伤落到 <c>FinalDamage.Base</c>
    /// （无视护甲但仍受缩放减伤影响），而 <see cref="MonoDamageValues.DefenseFlat"/> 与
    /// <see cref="MonoDamageValues.DefenseMul"/> 在玩家侧没有落点，会被忽略。
    /// </para>
    /// <para>
    /// 作废在玩家侧用原版的 <c>Cancel()</c>，比压到 1 点更贴近"闪避"。它不会自动给无敌帧，
    /// 需要无敌帧的消费者要自己管。
    /// </para>
    /// </summary>
    private static void CommitToPlayer(in MonoDamageValues values, ref Player.HurtModifiers modifiers)
    {
        if (values.SourceMul != 1f)
            modifiers.SourceDamage *= values.SourceMul;

        if (values.FlatBonus != 0f)
            modifiers.FinalDamage.Base += values.FlatBonus;

        if (values.PercentAdd != 0f)
            modifiers.FinalDamage += values.PercentAdd;

        if (values.DamageMul != 1f)
            modifiers.FinalDamage *= values.DamageMul;

        if (values.ArmorPenetration != 0f)
            modifiers.ArmorPenetration += values.ArmorPenetration;

        if (values.ScalingArmorPenetration != 0f)
            modifiers.ScalingArmorPenetration += values.ScalingArmorPenetration;

        if (values.MaxDamage < int.MaxValue)
            modifiers.SetMaxDamage(values.MaxDamage);

        if (values.Nullified)
            modifiers.Cancel();
    }
}
