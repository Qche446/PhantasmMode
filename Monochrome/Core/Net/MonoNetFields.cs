using Microsoft.Xna.Framework;

namespace Monochrome.Core.Net;

/// <summary>
/// 一张实体额外字段表的非泛型视图。<see cref="MonoNetFields{TEntity}"/> 是它带类型的形态。
/// <para>
/// 它与世界级字段（<see cref="MonoNetField{T}"/>）不是一回事：世界级字段有协议 id、自己要下行；
/// 实体字段只是"这只 NPC / 这颗弹幕的 extra-AI 里有哪些格子"，<b>声明顺序就是协议本身</b>。
/// 所以这里只登记名字、形状与流量，供 <c>/mononet tables</c> 对照两端的表。
/// </para>
/// </summary>
public abstract class MonoNetFieldsBase
{
    private static readonly List<MonoNetFieldsBase> all = [];
    private static long duplicateNames;

    private protected MonoNetFieldsBase(string name, string entity)
    {
        Name = name;
        Entity = entity;

        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Name != name || all[i].Entity != entity)
                continue;

            duplicateNames++;
            MonoLog.Warn(MonoLogLevel.Warn, $"MonoNet 字段表「{name}」({entity}) 被建了两次；表应当是每个类型一个静态字段，两张表会各记各的流量。");
            break;
        }

        all.Add(this);
    }

    /// <summary>表名，诊断用。建议带模组前缀。</summary>
    public string Name { get; }

    /// <summary>实体类型名（<c>NPC</c> / <c>Projectile</c>），诊断用。</summary>
    public string Entity { get; }

    /// <summary>字段个数，也就是这张表每次收发的格子数。</summary>
    public int FieldCount { get; protected set; }

    /// <summary>
    /// 形状哈希：各字段类型按声明顺序算出来的一个数，<b>与值、表名、字段名都无关</b>。
    /// 两端的形状对不上就说明表长得不一样，位置会对不上、读出来的东西会错位；
    /// <c>/mononet tables</c> 把它打出来供两端对照。
    /// </summary>
    public uint ShapeHash { get; protected set; }

    /// <summary>这张表写进包里的次数。</summary>
    public long Sends { get; protected set; }

    /// <summary>这张表写进包里的字节数（变长字段按实际长度算）。</summary>
    public long BytesOut { get; protected set; }

    /// <summary>这张表从包里读的次数。</summary>
    public long Reads { get; protected set; }

    /// <summary>这张表从包里读的字节数。</summary>
    public long BytesIn { get; protected set; }

    /// <summary>登记过的表，按创建顺序。诊断用。</summary>
    public static IReadOnlyList<MonoNetFieldsBase> Tables => all;

    /// <summary>同名同实体的表被建了两次的次数。<b>应当是 0</b>：重复建表会让诊断各记各的。</summary>
    public static long DuplicateNames => duplicateNames;

    /// <summary>字段名，按声明顺序。</summary>
    public abstract IReadOnlyList<string> FieldNames { get; }

    /// <summary>诊断输出：名字、实体、字段数、形状与流量。</summary>
    public string Describe()
        => $"「{Name}」({Entity})：{FieldCount} 个字段，形状 0x{ShapeHash:X8}，发送 {Sends} 次 / {BytesOut} 字节，接收 {Reads} 次 / {BytesIn} 字节";

    /// <summary>逐个字段列出声明顺序与类型。诊断用。</summary>
    public abstract string DescribeFields();
}

/// <summary>
/// 一份实体的额外字段清单：<b>声明一次，写与读共用同一份顺序</b>。
/// <para>
/// 它替掉手写的一对 <c>SendExtraAI</c> / <c>ReceiveExtraAI</c> 方法体。那对方法体的失败模式是
/// "两边各写一遍、慢慢漂开"：字段宽度写歪（<c>Write(float)</c> 配 <c>Read()</c>）、顺序对不上、
/// 加了字段忘了另一边——错了不报编译错，只在运行时悄悄读错值或触发 tML 的 underflow。
/// 表把顺序与宽度收成一处声明，两个方向都从它走。
/// </para>
/// <para>
/// <b>用法</b>：每个类型一个静态字段，get / set 指向那个类型的状态。
/// </para>
/// <code>
/// private static P_KingSlime Self(NPC npc) =&gt; npc.GetGlobalNPC&lt;P_KingSlime&gt;();
///
/// private static readonly MonoNetFields&lt;NPC&gt; Fields =
///     MonoNet.Fields&lt;NPC&gt;("fpm.kingSlime")
///         .Float("localAI0", n =&gt; n.localAI[0], (n, v) =&gt; n.localAI[0] = v)
///         .Bool("superSpecialJump", n =&gt; Self(n).SuperSpecialJump, (n, v) =&gt; Self(n).SuperSpecialJump = v);
///
/// public override void SendExtraAI(NPC npc, BitWriter bits, BinaryWriter w) =&gt; Fields.Write(npc, w);
/// public override void ReceiveExtraAI(NPC npc, BitReader bits, BinaryReader r) =&gt; Fields.Read(npc, r);
/// </code>
/// <para>
/// <b>边界</b>：① 字段按声明顺序写读，<b>加字段只能加在末尾</b>，改顺序等于改协议；
/// ② bool 占一个字节（走字节流），不用位流——ModNPC 侧的 <c>SendExtraAI</c> 拿不到 <c>BitWriter</c>，
/// 统一走字节才能一张 API 覆盖四类实体；③ 表不参与加入世界时那份全量，也没有脏检查，
/// 每次 <c>SendExtraAI</c> 都把全部字段写一遍（tML 没有"这次是全量"的标记，漏写会让中途加入的人拿到旧值）。
/// </para>
/// </summary>
/// <typeparam name="TEntity">实体类型（<c>NPC</c> / <c>Projectile</c>）。</typeparam>
public sealed class MonoNetFields<TEntity> : MonoNetFieldsBase
{
    private const byte TagBool = 1;
    private const byte TagByte = 2;
    private const byte TagInt = 3;
    private const byte TagFloat = 4;
    private const byte TagVector2 = 5;
    private const byte TagCustom = 6;

    /// <summary>FNV-1a 的起始值。形状哈希从它开始，逐个字段混进类型签。</summary>
    private const uint ShapeSeed = 2166136261u;

    private readonly List<string> names = [];
    private readonly List<byte> tags = [];
    private readonly List<Action<BinaryWriter, TEntity>> writers = [];
    private readonly List<Action<BinaryReader, TEntity>> readers = [];

    private MonoNetFields(string name)
        : base(name, typeof(TEntity).Name)
    {
        ShapeHash = ShapeSeed;
    }

    /// <summary>
    /// 建一张表。名字只用于诊断，建议带模组前缀（<c>fpm.kingSlime</c>）。
    /// <para>每个类型只该建一次：写成 <c>private static readonly</c>，别放进构造函数。</para>
    /// </summary>
    /// <param name="name">表名。</param>
    /// <returns>可继续追加字段的表。</returns>
    public static MonoNetFields<TEntity> For(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new MonoNetFields<TEntity>(name);
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> FieldNames => names;

    /// <summary>追加一个 <c>bool</c> 字段（占一个字节）。</summary>
    /// <param name="name">字段名，表内唯一。</param>
    /// <param name="get">取值。</param>
    /// <param name="set">落值。</param>
    public MonoNetFields<TEntity> Bool(string name, Func<TEntity, bool> get, Action<TEntity, bool> set)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        return Add(name, TagBool,
            (writer, entity) => writer.Write(get(entity)),
            (reader, entity) => set(entity, reader.ReadBoolean()));
    }

    /// <summary>追加一个 <c>byte</c> 字段。</summary>
    /// <param name="name">字段名，表内唯一。</param>
    /// <param name="get">取值。</param>
    /// <param name="set">落值。</param>
    public MonoNetFields<TEntity> Byte(string name, Func<TEntity, byte> get, Action<TEntity, byte> set)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        return Add(name, TagByte,
            (writer, entity) => writer.Write(get(entity)),
            (reader, entity) => set(entity, reader.ReadByte()));
    }

    /// <summary>追加一个 <c>int</c> 字段（固定 4 字节）。</summary>
    /// <param name="name">字段名，表内唯一。</param>
    /// <param name="get">取值。</param>
    /// <param name="set">落值。</param>
    public MonoNetFields<TEntity> Int(string name, Func<TEntity, int> get, Action<TEntity, int> set)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        return Add(name, TagInt,
            (writer, entity) => writer.Write(get(entity)),
            (reader, entity) => set(entity, reader.ReadInt32()));
    }

    /// <summary>追加一个 <c>float</c> 字段（固定 4 字节）。</summary>
    /// <param name="name">字段名，表内唯一。</param>
    /// <param name="get">取值。</param>
    /// <param name="set">落值。</param>
    public MonoNetFields<TEntity> Float(string name, Func<TEntity, float> get, Action<TEntity, float> set)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        return Add(name, TagFloat,
            (writer, entity) => writer.Write(get(entity)),
            (reader, entity) => set(entity, reader.ReadSingle()));
    }

    /// <summary>追加一个 <c>Vector2</c> 字段（两个 <c>float</c>，先 X 后 Y）。</summary>
    /// <param name="name">字段名，表内唯一。</param>
    /// <param name="get">取值。</param>
    /// <param name="set">落值。</param>
    public MonoNetFields<TEntity> Vector2(string name, Func<TEntity, Vector2> get, Action<TEntity, Vector2> set)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        return Add(name, TagVector2,
            (writer, entity) =>
            {
                Vector2 value = get(entity);
                writer.Write(value.X);
                writer.Write(value.Y);
            },
            (reader, entity) => set(entity, new Vector2(reader.ReadSingle(), reader.ReadSingle())));
    }

    /// <summary>
    /// 追加一个自己序列化的字段：变长载荷（列表、状态栈）走这里。
    /// <para>
    /// 它是出口，不是首选：这一格的读写对不对得由调用方保证。写与读紧挨着放，别拆到两个方法里去。
    /// </para>
    /// </summary>
    /// <param name="name">字段名，表内唯一。</param>
    /// <param name="write">写这一格。</param>
    /// <param name="read">读这一格。</param>
    public MonoNetFields<TEntity> Custom(string name, Action<BinaryWriter, TEntity> write, Action<BinaryReader, TEntity> read)
    {
        ArgumentNullException.ThrowIfNull(write);
        ArgumentNullException.ThrowIfNull(read);
        return Add(name, TagCustom, write, read);
    }

    /// <summary>把这张表的所有字段按<b>声明顺序</b>写进 <paramref name="writer"/>。</summary>
    /// <param name="entity">取值的实体。</param>
    /// <param name="writer">包体。</param>
    public void Write(TEntity entity, BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        long start = Position(writer.BaseStream);
        for (int i = 0; i < writers.Count; i++)
            writers[i](writer, entity);

        Sends++;
        BytesOut += Written(writer.BaseStream, start);
    }

    /// <summary>按<b>声明顺序</b>把 <paramref name="reader"/> 里的值落进实体。</summary>
    /// <param name="entity">落值的实体。</param>
    /// <param name="reader">包体。</param>
    public void Read(TEntity entity, BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        long start = Position(reader.BaseStream);
        for (int i = 0; i < readers.Count; i++)
            readers[i](reader, entity);

        Reads++;
        BytesIn += Written(reader.BaseStream, start);
    }

    /// <inheritdoc/>
    public override string DescribeFields()
    {
        System.Text.StringBuilder output = new();
        output.Append("「").Append(Name).Append("」按声明顺序：");

        for (int i = 0; i < names.Count; i++)
            output.Append("\n  #").Append(i).Append("  ").Append(names[i]).Append("  ").Append(TagName(tags[i]));

        return output.ToString();
    }

    private MonoNetFields<TEntity> Add(string name, byte tag,
        Action<BinaryWriter, TEntity> write, Action<BinaryReader, TEntity> read)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (names.Contains(name))
            throw new InvalidOperationException($"字段表「{Name}」里已经有一个叫「{name}」的字段；表内字段名要唯一，否则诊断分不清是哪一格。");

        names.Add(name);
        tags.Add(tag);
        writers.Add(write);
        readers.Add(read);

        FieldCount = names.Count;
        ShapeHash = MonoNet.Mix(ShapeHash, tag);
        return this;
    }

    private static string TagName(byte tag) => tag switch
    {
        TagBool => "bool",
        TagByte => "byte",
        TagInt => "int",
        TagFloat => "float",
        TagVector2 => "Vector2",
        _ => "Custom",
    };

    /// <summary>流可以定位时返回当前位置，否则返回 -1（统计不到字节数就不统计，不影响收发）。</summary>
    private static long Position(Stream stream) => stream.CanSeek ? stream.Position : -1;

    /// <summary>这一轮写/读掉的字节数。</summary>
    private static long Written(Stream stream, long start) => start < 0 || !stream.CanSeek ? 0 : stream.Position - start;
}
