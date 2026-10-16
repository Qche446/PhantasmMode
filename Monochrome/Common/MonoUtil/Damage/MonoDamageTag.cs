using System.Text;

namespace Monochrome.Common.MonoUtil.Damage;

/// <summary>
/// 伤害标签：用位掩码表示"这次命中带哪些属性"。八种内置标签占了低八位，其余位由消费者登记后使用。
/// <para>
/// <b>不用固定 <c>[Flags] enum</c> 的理由。</b>固定枚举意味着标签种类被库锁死，第三方想加"虚空"
/// 这类自己的元素时改不动库里的类型。这里的做法与 <c>MonoBuff</c> 的槽位分配一致：库按登记顺序把位分出去，
/// 消费者不写位号。代价是位号要等登记之后才知道，不能写死在静态字段的初始化里。
/// </para>
/// <para>
/// 标签与 <c>DamageClass</c> 是两回事：<c>DamageClass</c> 说的是"什么武器类型"（近战 / 远程 / 魔法 / 召唤），
/// 标签说的是"什么属性"。两者服务不同的平衡维度，不要合并。
/// </para>
/// <para>
/// 用位掩码是为了让"同时是火和物理"这种复合标签能被表达出来。
/// </para>
/// <para>
/// <b>写入只发生在加载期</b>（登记标签），运行期只读，所以没有加锁。
/// </para>
/// </summary>
public static class MonoDamageTag
{
    /// <summary>没有标签。与任何掩码按位与都是 0。</summary>
    public const ulong None = 0UL;

    /// <summary>物理。</summary>
    public const ulong Physical = 1UL << 0;

    /// <summary>火。</summary>
    public const ulong Fire = 1UL << 1;

    /// <summary>冰。</summary>
    public const ulong Ice = 1UL << 2;

    /// <summary>雷。</summary>
    public const ulong Lightning = 1UL << 3;

    /// <summary>毒。</summary>
    public const ulong Poison = 1UL << 4;

    /// <summary>暗。</summary>
    public const ulong Shadow = 1UL << 5;

    /// <summary>圣。</summary>
    public const ulong Holy = 1UL << 6;

    /// <summary>真实伤害。用它标记"不参与任何抗性减免"的命中。</summary>
    public const ulong True = 1UL << 7;

    /// <summary>内置标签的数量，也就是第一个可登记位。</summary>
    public const int BuiltInCount = 8;

    /// <summary>位掩码的总位数。</summary>
    public const int BitCapacity = 64;

    private static readonly List<string> names =
    [
        nameof(Physical), nameof(Fire), nameof(Ice), nameof(Lightning),
        nameof(Poison), nameof(Shadow), nameof(Holy), nameof(True),
    ];

    /// <summary>已登记的标签总数（含内置的八种）。</summary>
    public static int RegisteredCount => names.Count;

    /// <summary>已登记的标签名，下标即位号。诊断用。</summary>
    public static IReadOnlyList<string> Names => names;

    /// <summary>
    /// 登记一个标签，返回它的位号。同名重复登记是幂等的，返回同一个位号。
    /// </summary>
    /// <param name="name">标签名。一般直接用标签类的名字。</param>
    /// <returns>位号。</returns>
    /// <exception cref="ArgumentException">名字为空或全是空白。</exception>
    /// <exception cref="InvalidOperationException">64 位已经用满。</exception>
    public static int Register(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("标签名不能为空。", nameof(name));

        int existing = names.IndexOf(name);
        if (existing >= 0)
            return existing;

        if (names.Count >= BitCapacity)
            throw new InvalidOperationException($"{BitCapacity} 位标签位已经用满，登记 {name} 失败。");

        names.Add(name);
        return names.Count - 1;
    }

    /// <summary>按名字取掩码。没登记过的名字返回 false。</summary>
    /// <param name="name">标签名。</param>
    /// <param name="mask">对应的掩码；失败时为 <see cref="None"/>。</param>
    public static bool TryGetMask(string name, out ulong mask)
    {
        int index = name is null ? -1 : names.IndexOf(name);
        mask = index < 0 ? None : 1UL << index;
        return index >= 0;
    }

    /// <summary>按名字取掩码。</summary>
    /// <param name="name">标签名。</param>
    /// <exception cref="ArgumentException">这个名字没有登记过。</exception>
    public static ulong MaskOf(string name)
    {
        if (!TryGetMask(name, out ulong mask))
            throw new ArgumentException($"标签 {name} 没有登记过。消费者标签要先调 MonoDamageTag.Register。", nameof(name));
        return mask;
    }

    /// <summary>掩码里是否含某个标签。</summary>
    /// <param name="mask">要检查的掩码。</param>
    /// <param name="tag">单个标签的掩码。</param>
    public static bool Has(ulong mask, ulong tag) => (mask & tag) != 0UL;

    /// <summary>把掩码还原成名字，用 <c>|</c> 连接。<see cref="None"/> 输出 <c>None</c>。</summary>
    /// <param name="mask">要还原的掩码。</param>
    public static string Describe(ulong mask)
    {
        if (mask == None)
            return nameof(None);

        StringBuilder output = new();
        for (int bit = 0; bit < BitCapacity; bit++)
        {
            if ((mask & (1UL << bit)) == 0UL)
                continue;

            if (output.Length > 0)
                output.Append('|');

            // 位已经置上却没有名字，说明是手工拼出来的掩码或者登记表被清过，把位号原样报出来。
            output.Append(bit < names.Count ? names[bit] : $"#{bit}");
        }

        return output.ToString();
    }

    /// <summary>
    /// 清掉消费者登记的标签，只留内置八种。
    /// 给离线验收台与热重载用——正常流程里不需要复位。
    /// </summary>
    public static void Reset() => names.RemoveRange(BuiltInCount, names.Count - BuiltInCount);
}
