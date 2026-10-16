using System.Text;

namespace Monochrome.Common.MonoUtil.Damage;

/// <summary>
/// 伤害流水线的过程记录：每条规则改了多少，以及原版最后算出来的数是多少。
/// <para>
/// <b>它回答的是什么。</b>只有最终值时，一次伤害的来路无法排查。追踪把它拆成
/// "谁参与了、各自把哪个字段从多少改到多少"，最后补上原版自己结算出来的数。它<b>不</b>复算原版
/// <c>GetDamage</c> 内部的中间值——那条路要重写整个伤害公式，代价与收益不成比例（见
/// <c>Content/Damage/README.md</c> 的边界一节）。
/// </para>
/// <para>
/// <b>零成本关闭。</b><see cref="Enabled"/> 为假时 <see cref="BeginHit"/> / <see cref="Add"/> 直接返回，
/// 规则表那边也只在追踪开启时才去取改动前的快照。常规游玩路径上它只是一次布尔判断。
/// </para>
/// <para>
/// <b>满了覆盖最旧的。</b>和 <c>MonoRingBuffer</c> 同一套约定。这里没有直接复用它，是因为它在
/// <c>Monochrome.Core</c>，而 <c>Common/MonoUtil</c> 在依赖方向的下面，引用不到。
/// </para>
/// </summary>
public sealed class MonoDamageTrace
{
    /// <summary>默认容量。一次命中的规则条数通常远小于它。</summary>
    public const int DefaultCapacity = 64;

    /// <summary>一条记录：某条规则把累加器从什么改成了什么。</summary>
    public readonly struct Entry
    {
        /// <summary>改动的规则。存引用，因为取名字要走 <c>GetType().Name</c>，那是一次分配。</summary>
        public readonly MonoDamageRule Rule;

        /// <summary>规则所属的段号。</summary>
        public readonly MonoDamageStage Stage;

        /// <summary>规则跑之前的累加器快照。</summary>
        public readonly MonoDamageValues Before;

        /// <summary>规则跑之后的累加器快照。</summary>
        public readonly MonoDamageValues After;

        internal Entry(MonoDamageRule rule, MonoDamageStage stage, in MonoDamageValues before, in MonoDamageValues after)
        {
            Rule = rule;
            Stage = stage;
            Before = before;
            After = after;
        }
    }

    private readonly Entry[] entries;
    private int head;
    private int count;

    /// <summary>建一份追踪记录。</summary>
    /// <param name="capacity">容量，至少 1。满了覆盖最旧的一条。</param>
    public MonoDamageTrace(int capacity = DefaultCapacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "追踪容量必须 ≥ 1。");

        entries = new Entry[capacity];
    }

    /// <summary>是否在记录。默认关闭。</summary>
    public bool Enabled { get; set; }

    /// <summary>容量。</summary>
    public int Capacity => entries.Length;

    /// <summary>当前记录条数（≤ <see cref="Capacity"/>）。</summary>
    public int Count => count;

    /// <summary>因为超容量而被覆盖掉的条数。</summary>
    public int Overwritten { get; private set; }

    /// <summary>累计记录过多少次命中。</summary>
    public long HitCount { get; private set; }

    /// <summary>原版结算出来的最终伤害；<c>-1</c> 表示还没记录。</summary>
    public int ResolvedDamage { get; private set; } = -1;

    /// <summary>
    /// 开始一次新的记录：清空上一趟并计数。<b>未开启时什么都不做。</b>
    /// <para>
    /// 每次命中都清，是因为"最近这一下经历了什么"才是排查时真正要看的东西。
    /// 要留下多趟记录，请在调用方另存一份 <see cref="Entry"/> 快照。
    /// </para>
    /// </summary>
    public void BeginHit()
    {
        if (!Enabled)
            return;

        HitCount++;
        head = 0;
        count = 0;
        ResolvedDamage = -1;
    }

    /// <summary>
    /// 记一条规则改动。未开启时直接返回。
    /// </summary>
    /// <param name="rule">改动的规则。</param>
    /// <param name="stage">规则所属的段号。</param>
    /// <param name="before">改动前的累加器。</param>
    /// <param name="after">改动后的累加器。</param>
    public void Add(MonoDamageRule rule, MonoDamageStage stage, in MonoDamageValues before, in MonoDamageValues after)
    {
        if (!Enabled)
            return;

        entries[head] = new Entry(rule, stage, in before, in after);
        head = (head + 1) % entries.Length;
        if (count < entries.Length)
            count++;
        else
            Overwritten++;
    }

    /// <summary>记录原版算出来的最终伤害。未开启时直接返回。</summary>
    /// <param name="damage">最终伤害。</param>
    public void RecordResolved(int damage)
    {
        if (Enabled)
            ResolvedDamage = damage;
    }

    /// <summary>按时间顺序取第 <paramref name="i"/> 条（0 = 最旧的）。越界返回 <c>default</c>。</summary>
    /// <param name="i">从最旧算起的序号。</param>
    public Entry this[int i]
    {
        get
        {
            if ((uint)i >= (uint)count)
                return default;
            int start = count == entries.Length ? head : 0;
            return entries[(start + i) % entries.Length];
        }
    }

    /// <summary>清空记录，不改 <see cref="Enabled"/>。</summary>
    public void Clear()
    {
        head = 0;
        count = 0;
        ResolvedDamage = -1;
    }

    /// <summary>把记录渲染成多行文本。会分配，只在命令里用。</summary>
    public string Describe()
    {
        if (!Enabled)
            return "伤害追踪未开启。";

        if (count == 0)
            return "这次命中没有规则改过数值。";

        StringBuilder output = new();
        output.Append("伤害追踪：").Append(count).Append(" 条规则改过数值");
        if (Overwritten > 0)
            output.Append("（超容量覆盖了 ").Append(Overwritten).Append(" 条）");
        output.Append('。');

        for (int i = 0; i < count; i++)
        {
            Entry entry = this[i];
            output.Append("\n  ").Append(entry.Stage.Describe()).Append("  ").Append(entry.Rule.Name).Append("：");
            AppendDiff(output, in entry.Before, in entry.After);
        }

        output.Append("\n  原版结算：");
        output.Append(ResolvedDamage >= 0 ? $"{ResolvedDamage} 点伤害" : "本次没有记录到（命中没走到结算就被取消了）");
        return output.ToString();
    }

    /// <summary>把两个快照之间的差异拼进输出。没变的字段不出现。</summary>
    private static void AppendDiff(StringBuilder output, in MonoDamageValues before, in MonoDamageValues after)
    {
        int start = output.Length;

        AppendFloat(output, "源伤乘区", before.SourceMul, after.SourceMul);
        AppendFloat(output, "固定加伤", before.FlatBonus, after.FlatBonus);
        AppendFloat(output, "百分比加伤", before.PercentAdd, after.PercentAdd);
        AppendFloat(output, "伤害乘区", before.DamageMul, after.DamageMul);
        AppendFloat(output, "加算减伤", before.ReductionAdd, after.ReductionAdd);
        AppendFloat(output, "减伤缩放", before.ReductionScale, after.ReductionScale);
        AppendFloat(output, "防御固定值", before.DefenseFlat, after.DefenseFlat);
        AppendFloat(output, "防御倍率", before.DefenseMul, after.DefenseMul);
        AppendFloat(output, "护甲穿透", before.ArmorPenetration, after.ArmorPenetration);
        AppendFloat(output, "护甲穿透比例", before.ScalingArmorPenetration, after.ScalingArmorPenetration);
        AppendFloat(output, "乘区下限", before.MinRatio, after.MinRatio);
        AppendFloat(output, "乘区上限", before.MaxRatio, after.MaxRatio);
        AppendInt(output, "伤害上限", before.MaxDamage, after.MaxDamage);

        if (before.Nullified != after.Nullified)
            output.Append("作废 ").Append(before.Nullified).Append(" → ").Append(after.Nullified).Append('；');

        if (output.Length == start)
            output.Append("（没改动）");
    }

    /// <summary>一个字段变了就写一条"名字 旧值 → 新值"。</summary>
    private static void AppendFloat(StringBuilder output, string label, float before, float after)
    {
        if (before.Equals(after))
            return;

        output.Append(label).Append(' ')
              .Append(before.ToString("0.####")).Append(" → ").Append(after.ToString("0.####")).Append('；');
    }

    /// <summary>整数字段变了就写一条"名字 旧值 → 新值"。</summary>
    private static void AppendInt(StringBuilder output, string label, int before, int after)
    {
        if (before == after)
            return;

        output.Append(label).Append(' ').Append(before).Append(" → ").Append(after).Append('；');
    }
}
