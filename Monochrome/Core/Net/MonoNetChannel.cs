namespace Monochrome.Core.Net;

/// <summary>
/// 一条网络通道的账本：名字、稳定 id、收发条数与字节数。
/// <para>
/// id 由名字算出来（FNV-1a 折到 16 位），不按注册顺序编号。这样"某个字段注册得晚"只表现为对端
/// 遇到一个不认识的 id 并记一条日志，不会让后面所有字段的 id 整体错位、把值写进别的字段里。
/// 撞 id 会在注册时当场报错。
/// </para>
/// </summary>
public sealed class MonoNetChannel
{
    private readonly List<MonoNetFieldBase> fields = [];

    internal MonoNetChannel(ushort id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>写进包里的通道 id。0 保留给"无效"，实际分配从 1 起。</summary>
    public ushort Id { get; }

    /// <summary>通道名，同时参与 id 与字段表哈希的计算。</summary>
    public string Name { get; }

    /// <summary>发出去的包数。</summary>
    public long Sent { get; internal set; }

    /// <summary>收进来的包数。</summary>
    public long Received { get; internal set; }

    /// <summary>发出去的字节数（只统计本通道的载荷）。</summary>
    public long BytesOut { get; internal set; }

    /// <summary>收进来的字节数（只统计本通道的载荷）。</summary>
    public long BytesIn { get; internal set; }

    /// <summary>通道里的字段，按注册顺序。诊断用。</summary>
    public IReadOnlyList<MonoNetFieldBase> Fields => fields;

    /// <summary>有值还没发出去的字段。<c>MonoNet</c> 发完会清空它。</summary>
    internal List<MonoNetFieldBase> Dirty { get; } = [];

    internal void Add(MonoNetFieldBase field) => fields.Add(field);

    /// <summary>按 id 找一个字段，找不到返回 null（收端遇到不认识的 id 时走它）。</summary>
    /// <param name="id">字段 id。</param>
    public MonoNetFieldBase? Find(ushort id)
    {
        for (int i = 0; i < fields.Count; i++)
        {
            if (fields[i].Id == id)
                return fields[i];
        }
        return null;
    }

    /// <summary>诊断输出。</summary>
    public string Describe()
        => $"#{Id} 「{Name}」：{fields.Count} 个字段，发送 {Sent} 包 / {BytesOut} 字节，接收 {Received} 包 / {BytesIn} 字节";
}
