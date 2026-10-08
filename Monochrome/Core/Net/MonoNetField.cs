namespace Monochrome.Core.Net;

/// <summary>
/// 字段的非泛型视图：批量包与诊断只需要知道它的 id、名字和怎么读写。
/// </summary>
public abstract class MonoNetFieldBase
{
    private bool dirty;
    private bool refusalLogged;

    private protected MonoNetFieldBase(ushort id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>写进包里的字段 id，由"通道名 + 字段名"算出来。</summary>
    public ushort Id { get; }

    /// <summary>字段名，同时参与 id 与字段表哈希的计算。</summary>
    public string Name { get; }

    /// <summary>所属通道。注册时由 <see cref="MonoNet"/> 填好。</summary>
    public MonoNetChannel Channel { get; internal set; } = null!;

    /// <summary>当前值是否还没发出去。</summary>
    public bool IsDirty => dirty;

    /// <summary>当前值的诊断文本。</summary>
    public abstract string DescribeValue();

    /// <summary>把当前值写进 writer，不带 id。</summary>
    /// <param name="writer">包体。</param>
    internal abstract void WriteValue(BinaryWriter writer);

    /// <summary>
    /// 从 reader 读入一个值。<b>直接落值、顺手清掉脏标记</b>——收到的是权威值，不该再发回去。
    /// </summary>
    /// <param name="reader">包体。</param>
    /// <param name="notify">
    /// 值确实不同时要不要触发 <see cref="MonoNetField{T}.OnChanged"/>。运行时下发的批量包传 true，
    /// 加入世界时那份全量传 false。
    /// </param>
    internal abstract void ReadValue(BinaryReader reader, bool notify);

    /// <summary>退回注册时的初值（换世界、卸载时用）。</summary>
    internal abstract void ResetToInitial();

    /// <summary>记一笔"这个字段有值要发"。重复标记是空操作。</summary>
    internal void MarkDirty()
    {
        if (dirty || Channel is null)
            return;

        dirty = true;
        Channel.Dirty.Add(this);
    }

    /// <summary>发完了，清掉脏标记。</summary>
    internal void ClearDirty() => dirty = false;

    /// <summary>
    /// 记一笔"非权威端想写它"。<b>只记第一条日志</b>，之后只计数——
    /// 客户端一旦有这个念头，多半是每帧都会试，逐条记会把日志刷掉。
    /// </summary>
    internal void NoteRefusedWrite()
    {
        if (refusalLogged)
            return;

        refusalLogged = true;
        MonoLog.Warn(MonoLogLevel.Warn, $"「{Channel.Name}.{Name}」是世界级字段，非权威端的写入被忽略（同类拒绝之后只计数）；世界状态由服务端决定，客户端只接收。");
    }
}

/// <summary>
/// 一个世界级字段：权威端写它，值变时随批量包下发；客户端写它会被拒绝。
/// <para>
/// 支持的值类型是 <c>bool</c> / <c>byte</c> / <c>ushort</c> / <c>int</c> / <c>float</c> / <c>double</c>。
/// 别的类型会在建字段时当场抛异常，而不是等到发包时才炸。
/// </para>
/// </summary>
/// <typeparam name="T">值类型。</typeparam>
public sealed class MonoNetField<T> : MonoNetFieldBase where T : struct, IEquatable<T>
{
    private readonly T initial;
    private readonly Action<BinaryWriter, T> write;
    private readonly Func<BinaryReader, T> read;
    private T value;

    internal MonoNetField(ushort id, string name, T initial)
        : base(id, name)
    {
        this.initial = initial;
        value = initial;

        (Action<BinaryWriter, T> writer, Func<BinaryReader, T> reader) = MonoNetCodec<T>.Resolve();
        write = writer;
        read = reader;
    }

    /// <summary>
    /// 值变化时的回调，参数是旧值与新值。
    /// <para>
    /// <b>本地写入与收到远端值都会触发</b>（后者只在值真的不同时）。写同一个值、被拒的写入、
    /// 换世界时的复位、以及加入世界时那份全量补齐都<b>不</b>触发——它们的语义都不是"发生了一次变化"。
    /// </para>
    /// <para>
    /// 它是同步调用，别在里面做重活。它与发送无关：送值出去的是脏队列，这里只是"通知本地"。
    /// </para>
    /// </summary>
    public Action<T, T>? OnChanged { get; set; }

    /// <summary>
    /// 当前值。<b>写入只在权威端生效</b>：非权威端写它会保持不变、计一次拒绝并记一条日志。
    /// 写同一个值不产生任何流量。
    /// </summary>
    public T Value
    {
        get => value;
        set
        {
            if (value.Equals(this.value))
                return;

            if (!MonoNet.Authority)
            {
                MonoNet.NoteRefusedWrite(this);
                return;
            }

            T previous = this.value;
            this.value = value;
            MarkDirty();
            OnChanged?.Invoke(previous, value);
        }
    }

    /// <inheritdoc/>
    public override string DescribeValue() => value.ToString() ?? string.Empty;

    /// <summary>
    /// 直接把值放进去，<b>不通知、也不入队</b>。
    /// <para>
    /// 给"恢复已知状态"用：读档、换世界复位。那两种情况下值不是"刚刚发生了一次变化"，
    /// 通知会凭空播一次报，入队则会在一进世界就发一轮多余的包——客户端本来就是靠加入时那份全量补上的。
    /// </para>
    /// </summary>
    /// <param name="newValue">要放进去的值。</param>
    public void SetSilently(T newValue)
    {
        value = newValue;
        ClearDirty();
    }

    internal override void ResetToInitial()
    {
        value = initial;
        ClearDirty();
    }

    internal override void WriteValue(BinaryWriter writer) => write(writer, value);

    internal override void ReadValue(BinaryReader reader, bool notify)
    {
        T incoming = read(reader);
        if (incoming.Equals(value))
        {
            ClearDirty();
            return;
        }

        T previous = value;
        value = incoming;
        ClearDirty();

        if (notify)
            OnChanged?.Invoke(previous, incoming);
    }
}

/// <summary>
/// 值类型到字节的编解码器：每种类型只解析一次，之后走缓存好的那一对委托。
/// <para>
/// 它用装箱转换，因为这几个字段值的读写频率是"一局游戏几次"级别：多一次装箱换来
/// 调用点不必手写 <c>Write</c>/<c>Read</c> 两个委托（写错一半就是两端永远读不对的 bug）。
/// </para>
/// </summary>
/// <typeparam name="T">值类型。</typeparam>
internal static class MonoNetCodec<T> where T : struct, IEquatable<T>
{
    private static Action<BinaryWriter, T>? write;
    private static Func<BinaryReader, T>? read;

    internal static (Action<BinaryWriter, T> Write, Func<BinaryReader, T> Read) Resolve()
    {
        if (write is not null && read is not null)
            return (write, read);

        if (typeof(T) == typeof(bool))
        {
            write = static (writer, value) => writer.Write((bool)(object)value);
            read = static reader => (T)(object)reader.ReadBoolean();
        }
        else if (typeof(T) == typeof(byte))
        {
            write = static (writer, value) => writer.Write((byte)(object)value);
            read = static reader => (T)(object)reader.ReadByte();
        }
        else if (typeof(T) == typeof(ushort))
        {
            write = static (writer, value) => writer.Write((ushort)(object)value);
            read = static reader => (T)(object)reader.ReadUInt16();
        }
        else if (typeof(T) == typeof(int))
        {
            write = static (writer, value) => writer.Write((int)(object)value);
            read = static reader => (T)(object)reader.ReadInt32();
        }
        else if (typeof(T) == typeof(float))
        {
            write = static (writer, value) => writer.Write((float)(object)value);
            read = static reader => (T)(object)reader.ReadSingle();
        }
        else if (typeof(T) == typeof(double))
        {
            write = static (writer, value) => writer.Write((double)(object)value);
            read = static reader => (T)(object)reader.ReadDouble();
        }
        else
        {
            throw new NotSupportedException(
                $"MonoNet 还不支持 {typeof(T).Name} 这类字段值。可用的类型是 bool / byte / ushort / int / float / double。");
        }

        return (write, read!);
    }
}
