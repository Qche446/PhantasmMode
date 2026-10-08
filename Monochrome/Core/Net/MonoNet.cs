using System.Text;

namespace Monochrome.Core.Net;

/// <summary>本端站在哪一边。<see cref="MonoNet.Side"/> 由 <see cref="MonoNetSystem"/> 每帧刷新。</summary>
public enum MonoNetSide
{
    /// <summary>单人。本机既是权威端，也没有对端可发。</summary>
    SinglePlayer,

    /// <summary>多人客户端。写世界级字段会被拒；请求走网络上行。</summary>
    Client,

    /// <summary>服务端。世界级字段由它决定；请求由它复核与执行。</summary>
    Server,
}

/// <summary>
/// 轻量的网络底座：<b>世界级字段的服务端权威下发 + 诊断</b>。
/// <para>
/// 它只解决盘点里最明确的那一类需求——"为了一个标志位发了整份世界数据"。字段注册之后，
/// 权威端写值只在<b>值变</b>时产生流量，一帧内所有变化合成一个包；客户端加入世界时由
/// <see cref="MonoNetSystem"/> 随世界数据一次性补齐。
/// </para>
/// <para>
/// 四条明确的边界：<b>①</b> 客户端写字段会被拒绝（值不变、计数、记一条日志），世界状态只由权威端决定；
/// <b>②</b> 字段 id 由名字算出来，于是"注册得晚"只会让对端报一次不认识的 id，不会让 id 整体错位；
/// <b>③</b> 收到不认识的通道或字段时<b>整包放弃</b>并计数——不知道那个值的宽度，续读只会读到垃圾；
/// <b>④</b> 实体自己的字段（<c>ai</c> / <c>localAI</c> / extra-AI）不归它管，那是 <c>netUpdate</c> 与
/// <c>SendExtraAI</c> 的事。
/// </para>
/// </summary>
public static class MonoNet
{
    private static readonly List<MonoNetChannel> allChannels = [];
    private static readonly MonoNetChannel world = NewChannel("world");
    private static readonly List<MonoNetRequestBase> allRequests = [];
    private static readonly MonoNetChannel requestChannel = NewChannel("request");

    /// <summary>
    /// 待发的请求。<b>里面存的是"把这条请求写进包"的动作，而不是包本身</b>——包要由 tML 那侧的
    /// <see cref="MonoNetSystem"/> 去借，而这一层不引用任何 tML 类型（验收台要能直接驱动它）。
    /// </summary>
    private static readonly List<Action<BinaryWriter>> outgoing = [];

    /// <summary>本端站在哪一边。由 <see cref="MonoNetSystem"/> 每帧刷新。</summary>
    public static MonoNetSide Side { get; set; } = MonoNetSide.SinglePlayer;

    /// <summary>
    /// 当前端是不是权威端（单人 或 服务端）。<b>世界级字段只有权威端能写。</b>
    /// </summary>
    public static bool Authority => Side != MonoNetSide.Client;

    /// <summary>
    /// 单机下复核回调收到的"发送者编号"。由 <see cref="MonoNetSystem"/> 每帧刷新成本地玩家号，
    /// 这样单机与多人的复核回调拿到的东西是一致的。
    /// </summary>
    public static int LocalPlayerWhoAmI { get; set; }

    /// <summary>非权威端的写入被拒的次数。</summary>
    public static long RefusedWrites { get; private set; }

    /// <summary>收到不认识的通道 id 的次数。</summary>
    public static long UnknownChannels { get; private set; }

    /// <summary>收到不认识的字段 id 的次数。</summary>
    public static long UnknownFields { get; private set; }

    /// <summary>收到不认识的<b>消息</b> id 的次数（两端 Monochrome 版本不一致）。</summary>
    public static long UnknownMessages { get; private set; }

    /// <summary>加入世界时字段表哈希对不上的次数（两端代码不是同一份）。</summary>
    public static long RegistryMismatches { get; private set; }

    /// <summary>权威端收到"只该由服务端下发"的消息的次数（有人冒充服务端发包）。</summary>
    public static long AuthorityViolations { get; private set; }

    /// <summary>发出去的请求条数。</summary>
    public static long RequestsSent { get; private set; }

    /// <summary>收下并交给复核的请求条数。</summary>
    public static long RequestsReceived { get; private set; }

    /// <summary>复核不通过（或没挂复核）而被拒绝的请求条数。</summary>
    public static long RefusedRequests { get; private set; }

    /// <summary>复核通过但没有处理者的请求条数（<c>OnServer</c> 没挂）。</summary>
    public static long UnhandledRequests { get; private set; }

    /// <summary>不认识的请求 id（两端协议不一致）。</summary>
    public static long UnknownRequests { get; private set; }

    /// <summary>请求发到了不该到的一端（客户端收到了上行请求，或服务端自己发请求）。</summary>
    public static long MisplacedRequests { get; private set; }

    /// <summary>载荷读不出来的请求条数（两端对载荷形状的理解不一致）。</summary>
    public static long BrokenRequests { get; private set; }

    /// <summary>最近一次 ping 的往返毫秒数；还没测过是 -1。</summary>
    public static double LastPingMs { get; internal set; } = -1;

    /// <summary>正在等回显的那次 ping 的本地起点（真实秒）；没有在等的是 -1。</summary>
    public static double PendingPingStart { get; internal set; } = -1;

    /// <summary>注册过的通道，按注册顺序。</summary>
    public static IReadOnlyList<MonoNetChannel> Channels => allChannels;

    /// <summary>
    /// 字段表哈希：两端一致才说明 id 对得上。<b>与注册顺序无关</b>（内部按 id 排序后累加），
    /// 所以静态初始化的先后不会让它抖动。
    /// </summary>
    public static uint RegistryHash
    {
        get
        {
            List<ushort> fieldIds = [];
            for (int c = 0; c < allChannels.Count; c++)
            {
                MonoNetChannel channel = allChannels[c];
                for (int f = 0; f < channel.Fields.Count; f++)
                    fieldIds.Add(channel.Fields[f].Id);
            }

            List<ushort> requestIds = [];
            for (int i = 0; i < allRequests.Count; i++)
                requestIds.Add(allRequests[i].Id);

            fieldIds.Sort();
            requestIds.Sort();

            uint hash = 2166136261u;
            foreach (ushort id in fieldIds)
                hash = Mix(hash, id);

            hash = Mix(hash, 0xFFFF);   // 字段段与请求段的分隔符

            foreach (ushort id in requestIds)
                hash = Mix(hash, id);

            return hash;
        }
    }

    /// <summary>
    /// 注册一个世界级字段并返回它。
    /// <para>
    /// <b>同一个名字重复调用返回同一个字段</b>——静态字段是惰性初始化的，同一个注册点被走到两次不是错误。
    /// 名字撞了 id（哈希碰撞）或同一个 id 换了名字，会在注册时当场抛异常。
    /// </para>
    /// <para>
    /// 字段必须在<b>世界数据下发之前</b>注册好。写在类的静态字段里、并让 <c>Mod.Load</c> 触发一次那个类
    /// （或者调用它的一个空方法）就够了；否则中途加入的客户端会收不到它。
    /// </para>
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="name">字段名，建议带模组前缀，例如 <c>fpm.phantasmMode</c>。</param>
    /// <param name="initial">初值，也是换世界之后回到的值。</param>
    /// <returns>字段对象。</returns>
    public static MonoNetField<T> WorldFlag<T>(string name, T initial) where T : struct, IEquatable<T>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        ushort id = FieldId(world.Name, name);
        MonoNetFieldBase? existing = world.Find(id);
        if (existing is not null)
        {
            if (existing is MonoNetField<T> typed && typed.Name == name)
                return typed;

            throw new InvalidOperationException(
                $"MonoNet 字段 id #{id} 已被「{existing.Name}」占用，不能再用给「{name}」。改一个名字即可。");
        }

        MonoNetField<T> field = new(id, name, initial) { Channel = world };
        world.Add(field);
        return field;
    }

    /// <summary>
    /// 建一张<b>实体额外字段表</b>，用来替掉手写的 <c>SendExtraAI</c> / <c>ReceiveExtraAI</c> 方法体。
    /// <para>
    /// 世界级字段（<see cref="WorldFlag{T}"/>）是"服务端决定、值变才下发"；实体字段是另一回事——
    /// 它跟着实体的 extra-AI 走，由 <c>npc.netUpdate</c> 之类触发，位置就是协议本身。
    /// 表把两个方向的顺序与宽度收成一处声明，并给 <c>/mononet tables</c> 提供对照用的形状哈希。
    /// </para>
    /// <para>每个类型只该建一次，写成 <c>private static readonly</c>；它不参与加入世界那份全量。</para>
    /// </summary>
    /// <typeparam name="TEntity">实体类型（<c>NPC</c> / <c>Projectile</c>）。</typeparam>
    /// <param name="name">表名，诊断用，建议带模组前缀。</param>
    /// <returns>可继续追加字段的表。</returns>
    public static MonoNetFields<TEntity> Fields<TEntity>(string name) => MonoNetFields<TEntity>.For(name);

    /// <summary>登记过的实体字段表，按创建顺序。诊断用。</summary>
    public static IReadOnlyList<MonoNetFieldsBase> Tables => MonoNetFieldsBase.Tables;

    /// <summary>同名同实体的表被建了两次的次数。应当是 0。</summary>
    public static long DuplicateTables => MonoNetFieldsBase.DuplicateNames;

    /// <summary>
    /// 注册一条客户端→服务端的请求并返回它。载荷的写读各给一个委托。
    /// <para>
    /// 与 <see cref="WorldFlag{T}"/> 一样，同一个名字重复调用返回同一条；id 撞了会在注册时当场抛。
    /// </para>
    /// </summary>
    /// <typeparam name="T">载荷类型。</typeparam>
    /// <param name="name">请求名，建议带模组前缀。</param>
    /// <param name="write">把载荷写进包。</param>
    /// <param name="read">从包里读回载荷。</param>
    /// <returns>请求对象。</returns>
    public static MonoNetRequest<T> Request<T>(string name, Action<BinaryWriter, T> write, Func<BinaryReader, T> read) where T : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(write);
        ArgumentNullException.ThrowIfNull(read);

        ushort id = RequestId(name);
        MonoNetRequestBase? existing = FindRequest(id);
        if (existing is not null)
        {
            if (existing is MonoNetRequest<T> typed && typed.Name == name)
                return typed;

            throw new InvalidOperationException($"MonoNet 请求 id #{id} 已被「{existing.Name}」占用，不能再用给「{name}」。改一个名字即可。");
        }

        MonoNetRequest<T> request = new(id, name, write, read);
        allRequests.Add(request);
        return request;
    }

    /// <summary>载荷是单个 <c>bool</c> / <c>byte</c> / <c>ushort</c> / <c>int</c> / <c>float</c> / <c>double</c> 时的快捷注册。</summary>
    /// <typeparam name="T">载荷类型。</typeparam>
    /// <param name="name">请求名。</param>
    /// <returns>请求对象。</returns>
    public static MonoNetRequest<T> Request<T>(string name) where T : struct, IEquatable<T>
    {
        (Action<BinaryWriter, T> write, Func<BinaryReader, T> read) = MonoNetCodec<T>.Resolve();
        return Request(name, write, read);
    }

    /// <summary>按 id 找一条请求，找不到返回 null。</summary>
    /// <param name="id">请求 id。</param>
    public static MonoNetRequestBase? FindRequest(ushort id)
    {
        for (int i = 0; i < allRequests.Count; i++)
        {
            if (allRequests[i].Id == id)
                return allRequests[i];
        }
        return null;
    }

    /// <summary>注册过的请求，按注册顺序。诊断用。</summary>
    public static IReadOnlyList<MonoNetRequestBase> Requests => allRequests;

    /// <summary>
    /// 读一批字段值并落值。载荷是 <c>通道 id</c>、<c>字段数</c>，然后每项 <c>字段 id</c> 加值。
    /// </summary>
    /// <param name="reader">包体。</param>
    /// <param name="bytes">这一包读掉的载荷字节数（统计用；放弃时是已经读掉的部分）。</param>
    /// <returns>按表读完返回 true；遇到不认识的通道或字段返回 false。</returns>
    public static bool ReadBatch(BinaryReader reader, out int bytes)
    {
        long start = reader.BaseStream.Position;

        ushort channelId = reader.ReadUInt16();
        MonoNetChannel? channel = FindChannel(channelId);
        if (channel is null)
        {
            UnknownChannels++;
            bytes = (int)(reader.BaseStream.Position - start);
            MonoLog.Warn(MonoLogLevel.Warn, $"收到不认识的通道 id #{channelId}，整包放弃。两端的 Monochrome 版本可能不是同一份。");
            return false;
        }

        int count = reader.ReadUInt16();
        for (int i = 0; i < count; i++)
        {
            ushort fieldId = reader.ReadUInt16();
            MonoNetFieldBase? field = channel.Find(fieldId);
            if (field is null)
            {
                UnknownFields++;
                bytes = (int)(reader.BaseStream.Position - start);
                MonoLog.Warn(MonoLogLevel.Warn, $"通道「{channel.Name}」里收到不认识的字段 id #{fieldId}，整包放弃。");
                return false;
            }

            field.ReadValue(reader, notify: true);
        }

        bytes = (int)(reader.BaseStream.Position - start);
        channel.Received++;
        channel.BytesIn += bytes;
        return true;
    }

    /// <summary>
    /// 读一条请求（载荷是 <c>请求 id</c> 加消费者自己的字节），交给复核与执行。
    /// <para>它只该在服务端被调用；在别处收到请求由 <see cref="MonoNetSystem"/> 挡下并计数。</para>
    /// </summary>
    /// <param name="reader">包体。</param>
    /// <param name="whoAmI">发送者编号，由传输层给出。</param>
    /// <returns>确实按表读完并处理了返回 true；不认识这条请求返回 false。</returns>
    public static bool ReadRequest(BinaryReader reader, int whoAmI)
    {
        long start = reader.BaseStream.Position;

        ushort id = reader.ReadUInt16();
        MonoNetRequestBase? request = FindRequest(id);
        if (request is null)
        {
            UnknownRequests++;
            MonoLog.Warn(MonoLogLevel.Warn, $"收到不认识的请求 id #{id}（来自 {whoAmI}）。两端的 Monochrome 或模组版本可能不是同一份。");
            return false;
        }

        try
        {
            request.ApplyFromNetwork(reader, whoAmI);
        }
        catch (Exception exception)
        {
            BrokenRequests++;
            MonoLog.Error(MonoLogLevel.Error, $"请求「{request.Name}」（来自 {whoAmI}）的载荷读不出来，整条丢弃：{exception}");
            return false;
        }

        int bytes = (int)(reader.BaseStream.Position - start);
        RequestsReceived++;
        requestChannel.Received++;
        requestChannel.BytesIn += bytes;
        return true;
    }

    /// <summary>客户端加入世界时下发全量：哈希、通道数，以及每个通道的字段与当前值。</summary>
    /// <param name="writer">包体。</param>
    public static void WriteSnapshot(BinaryWriter writer)
    {
        writer.Write(RegistryHash);
        writer.Write((ushort)allChannels.Count);

        for (int c = 0; c < allChannels.Count; c++)
        {
            MonoNetChannel channel = allChannels[c];
            writer.Write(channel.Id);
            writer.Write((ushort)channel.Fields.Count);

            for (int f = 0; f < channel.Fields.Count; f++)
            {
                writer.Write(channel.Fields[f].Id);
                channel.Fields[f].WriteValue(writer);
            }
        }
    }

    /// <summary>读回加入时的那份全量。哈希对不上时记一条警告并计数，然后照 id 把值读进来。</summary>
    /// <param name="reader">包体。</param>
    public static void ReadSnapshot(BinaryReader reader)
    {
        uint hash = reader.ReadUInt32();
        if (hash != RegistryHash)
        {
            RegistryMismatches++;
            MonoLog.Warn(MonoLogLevel.Warn, $"MonoNet 字段表哈希与下发方不一致（下发方 0x{hash:X8}，本地 0x{RegistryHash:X8}）；两端的 Monochrome 或模组版本可能不是同一份，用 /mononet stat 对比两边的清单。");
        }

        int channelCount = reader.ReadUInt16();
        for (int c = 0; c < channelCount; c++)
        {
            ushort channelId = reader.ReadUInt16();
            int fieldCount = reader.ReadUInt16();

            MonoNetChannel? channel = FindChannel(channelId);
            if (channel is null)
            {
                UnknownChannels++;
                return;
            }

            for (int f = 0; f < fieldCount; f++)
            {
                ushort fieldId = reader.ReadUInt16();
                MonoNetFieldBase? field = channel.Find(fieldId);
                if (field is null)
                {
                    UnknownFields++;
                    return;
                }

                field.ReadValue(reader, notify: false);
            }
        }
    }

    /// <summary>换世界：所有字段退回注册时的初值，清掉脏队列。<b>统计不动</b>，那是跨世界的诊断数据。</summary>
    public static void ResetForWorld()
    {
        for (int c = 0; c < allChannels.Count; c++)
        {
            MonoNetChannel channel = allChannels[c];
            for (int f = 0; f < channel.Fields.Count; f++)
                channel.Fields[f].ResetToInitial();
            channel.Dirty.Clear();
        }
    }

    /// <summary>诊断输出：通道账本、各种拒绝与未知计数、上次 ping。</summary>
    public static string Describe()
    {
        StringBuilder output = new();
        output.Append("MonoNet：").Append(allChannels.Count).Append(" 条通道，字段表哈希 0x").Append(RegistryHash.ToString("X8"))
              .Append('，').Append(Authority ? "本端是权威端" : "本端是客户端");

        for (int c = 0; c < allChannels.Count; c++)
            output.Append("\n  ").Append(allChannels[c].Describe());

        output.Append("\n  请求 ").Append(allRequests.Count).Append(" 条：发出 ").Append(RequestsSent).Append("，收到 ").Append(RequestsReceived)
              .Append("，被拒 ").Append(RefusedRequests).Append("，无处理者 ").Append(UnhandledRequests)
              .Append("，未知 id ").Append(UnknownRequests).Append("，载荷坏 ").Append(BrokenRequests)
              .Append("，位置不对 ").Append(MisplacedRequests);

        output.Append("\n  非权威端拒写 ").Append(RefusedWrites).Append(" 次，未知通道 ").Append(UnknownChannels)
              .Append(" 次，未知字段 ").Append(UnknownFields).Append(" 次，未知消息 ").Append(UnknownMessages)
              .Append(" 次，表哈希不一致 ").Append(RegistryMismatches).Append(" 次，冒充权威端 ").Append(AuthorityViolations).Append(" 次");

        long tableSends = 0;
        long tableBytes = 0;
        for (int i = 0; i < MonoNetFieldsBase.Tables.Count; i++)
        {
            tableSends += MonoNetFieldsBase.Tables[i].Sends;
            tableBytes += MonoNetFieldsBase.Tables[i].BytesOut;
        }
        output.Append("\n  实体字段表 ").Append(MonoNetFieldsBase.Tables.Count).Append(" 张：发送 ").Append(tableSends)
              .Append(" 次 / ").Append(tableBytes).Append(" 字节（/mononet tables 看逐张与逐格）");

        output.Append(LastPingMs >= 0
            ? $"\n  上次 ping 往返 {LastPingMs:F1} ms"
            : "\n  还没 ping 过（/mononet ping，仅开发期）");

        return output.ToString();
    }

    /// <summary>逐个列出字段：id、名字、当前值、有没有待发。</summary>
    public static string DescribeFields()
    {
        StringBuilder output = new();
        for (int c = 0; c < allChannels.Count; c++)
        {
            MonoNetChannel channel = allChannels[c];
            output.Append("通道 ").Append(channel.Describe());

            for (int f = 0; f < channel.Fields.Count; f++)
            {
                MonoNetFieldBase field = channel.Fields[f];
                output.Append("\n  #").Append(field.Id).Append("  ").Append(field.Name)
                      .Append("  = ").Append(field.DescribeValue())
                      .Append(field.IsDirty ? "  [待发]" : string.Empty);
            }
            output.Append('\n');
        }
        return output.ToString();
    }

    /// <summary>逐张列出实体字段表：名字、实体、字段数、形状哈希与流量，再列出各字段的声明顺序与类型。</summary>
    public static string DescribeTables()
    {
        StringBuilder output = new();
        output.Append("MonoNet 实体字段表 ").Append(MonoNetFieldsBase.Tables.Count)
              .Append(" 张（形状哈希只由名字与各字段类型算出；两端对得上，表才长得一样）");

        for (int i = 0; i < MonoNetFieldsBase.Tables.Count; i++)
        {
            MonoNetFieldsBase table = MonoNetFieldsBase.Tables[i];
            output.Append("\n  ").Append(table.Describe());
            output.Append('\n').Append(table.DescribeFields());
        }

        return output.ToString();
    }

    /// <summary>逐条列出请求：id 与名字。诊断用。</summary>
    public static string DescribeRequests()
    {
        StringBuilder output = new();
        output.Append("MonoNet 请求 ").Append(allRequests.Count).Append(" 条");

        for (int i = 0; i < allRequests.Count; i++)
            output.Append("\n  ").Append(allRequests[i].Describe());

        return output.ToString();
    }

    /// <summary>按 id 找一条通道，找不到返回 null。</summary>
    /// <param name="id">通道 id。</param>
    public static MonoNetChannel? FindChannel(ushort id)
    {
        for (int i = 0; i < allChannels.Count; i++)
        {
            if (allChannels[i].Id == id)
                return allChannels[i];
        }
        return null;
    }

    /// <summary>
    /// 取一条还有脏字段的通道（按注册顺序），没有就返回 null。
    /// <para>它是公开的，只为了让验收台能自己当 <see cref="MonoNetSystem"/> 驱动一遍。</para>
    /// </summary>
    public static MonoNetChannel? NextDirtyChannel()
    {
        for (int i = 0; i < allChannels.Count; i++)
        {
            if (allChannels[i].Dirty.Count > 0)
                return allChannels[i];
        }
        return null;
    }

    /// <summary>
    /// 把一条通道的脏字段写进包：<c>字段数</c>，然后每项 <c>字段 id</c> 加值。
    /// <para>它是公开的，只为了让验收台能自己当 <see cref="MonoNetSystem"/> 驱动一遍。</para>
    /// </summary>
    /// <param name="channel">要写的通道。</param>
    /// <param name="writer">包体。</param>
    public static void WriteDirty(MonoNetChannel channel, BinaryWriter writer)
    {
        writer.Write((ushort)channel.Dirty.Count);
        for (int i = 0; i < channel.Dirty.Count; i++)
        {
            MonoNetFieldBase field = channel.Dirty[i];
            writer.Write(field.Id);
            field.WriteValue(writer);
        }
    }

    /// <summary>
    /// 发完了：清掉这一轮的脏标记。与 <see cref="WriteDirty"/> 一样公开给验收台用。
    /// </summary>
    /// <param name="channel">刚发完的通道。</param>
    public static void CommitDirty(MonoNetChannel channel)
    {
        for (int i = 0; i < channel.Dirty.Count; i++)
            channel.Dirty[i].ClearDirty();
        channel.Dirty.Clear();
    }

    /// <summary>不发只清（单人下用：本地值已经生效，没有对端需要通知）。公开给验收台用。</summary>
    public static void DiscardDirty()
    {
        MonoNetChannel? channel;
        while ((channel = NextDirtyChannel()) is not null)
            CommitDirty(channel);
    }

    internal static void ReportSent(MonoNetChannel channel, int bytes)
    {
        channel.Sent++;
        channel.BytesOut += bytes;
    }

    /// <summary>客户端提交一条请求。单机直接在本机走一遍复核与执行。</summary>
    internal static void SendRequest<T>(MonoNetRequest<T> request, T payload) where T : struct
    {
        switch (Side)
        {
            case MonoNetSide.SinglePlayer:
                // 单机没有对端可发：本机就是服务端，直接走复核与执行，消费者不必再写单机分支。
                ApplyRequest(request, LocalPlayerWhoAmI, payload);
                return;

            case MonoNetSide.Server:
                MisplacedRequests++;
                MonoLog.Warn(MonoLogLevel.Warn, $"服务端自己发请求「{request.Name}」，已忽略：请求是客户端→服务端的上行通道。");
                return;

            default:
                // 交给 MonoNetSystem 在帧起点发出去。这里只记下"要写什么"。
                outgoing.Add(writer =>
                {
                    writer.Write(request.Id);
                    request.WritePayload(writer, payload);
                });
                return;
        }
    }

    /// <summary>
    /// 取一条待发请求的写入动作，没有就返回 null。由 <see cref="MonoNetSystem"/> 每帧排空。
    /// <para>它是公开的，只为了让验收台能自己当 <see cref="MonoNetSystem"/> 驱动一遍。</para>
    /// </summary>
    public static Action<BinaryWriter>? NextOutgoing()
    {
        if (outgoing.Count == 0)
            return null;

        Action<BinaryWriter> action = outgoing[0];
        outgoing.RemoveAt(0);
        return action;
    }

    /// <summary>复核并执行一条请求。<b>没挂复核一律拒绝</b>——忘了写校验不等于放行。</summary>
    internal static void ApplyRequest<T>(MonoNetRequest<T> request, int whoAmI, T payload) where T : struct
    {
        if (request.OnServer is null)
        {
            UnhandledRequests++;
            MonoLog.Warn(MonoLogLevel.Warn, $"请求「{request.Name}」（来自 {whoAmI}）没有处理者，已忽略。");
            return;
        }

        if (request.Validate is null)
        {
            RefusedRequests++;
            MonoLog.Warn(MonoLogLevel.Warn, $"请求「{request.Name}」（来自 {whoAmI}）没有复核回调，按拒绝处理；确实要放行就显式写 Validate = (_, _) => true。");
            return;
        }

        bool accepted;
        try
        {
            accepted = request.Validate(whoAmI, payload);
        }
        catch (Exception exception)
        {
            RefusedRequests++;
            MonoLog.Error(MonoLogLevel.Error, $"请求「{request.Name}」的复核抛异常，已拒绝：{exception}");
            return;
        }

        if (!accepted)
        {
            RefusedRequests++;
            MonoLog.Warn(MonoLogLevel.Warn, $"请求「{request.Name}」被复核拒绝（发送者 {whoAmI}）。");
            return;
        }

        try
        {
            request.OnServer(whoAmI, payload);
        }
        catch (Exception exception)
        {
            MonoLog.Error(MonoLogLevel.Error, $"请求「{request.Name}」的处理抛异常：{exception}");
        }
    }

    internal static void ReportRequestSent(int bytes)
    {
        RequestsSent++;
        requestChannel.Sent++;
        requestChannel.BytesOut += bytes;
    }

    internal static void NoteMisplacedRequest(int whoAmI)
    {
        MisplacedRequests++;
        MonoLog.Warn(MonoLogLevel.Warn, $"收到一条请求消息（来自 {whoAmI}），但请求只该发给服务端，已忽略。");
    }

    internal static void NoteRefusedWrite(MonoNetFieldBase field)
    {
        RefusedWrites++;
        field.NoteRefusedWrite();
    }

    internal static void NoteUnknownMessage(byte messageId, int whoAmI)
    {
        UnknownMessages++;
        MonoLog.Warn(MonoLogLevel.Warn, $"收到不认识的 Monochrome 消息 id #{messageId}（来自 {whoAmI}）。两端的 Monochrome 版本可能不是同一份。");
    }

    internal static void NoteAuthorityViolation(byte messageId, int whoAmI)
    {
        AuthorityViolations++;
        MonoLog.Warn(MonoLogLevel.Warn, $"权威端收到客户端 {whoAmI} 发来的消息 id #{messageId}；它只该由服务端下发，已忽略。");
    }

    private static MonoNetChannel NewChannel(string name)
    {
        ushort id = ChannelId(name);
        for (int i = 0; i < allChannels.Count; i++)
        {
            if (allChannels[i].Id == id)
                throw new InvalidOperationException($"MonoNet 通道 id #{id} 冲突：「{allChannels[i].Name}」与「{name}」。改一个通道名即可。");
        }

        MonoNetChannel channel = new(id, name);
        allChannels.Add(channel);
        return channel;
    }

    /// <summary>通道 id：名字的哈希折到 16 位。</summary>
    private static ushort ChannelId(string name) => Fold(Hash32(name));

    /// <summary>字段 id：通道名与字段名一起参与哈希，于是换通道不会撞上同一个 id。</summary>
    private static ushort FieldId(string channelName, string fieldName) => Fold(Hash32(channelName + "." + fieldName));

    /// <summary>请求 id：名字的哈希折到 16 位。请求与字段各自一张表，所以取值域相同也不会互相干扰。</summary>
    private static ushort RequestId(string name) => Fold(Hash32("request." + name));

    /// <summary>把 32 位哈希折成 16 位；0 保留给"无效"，折出 0 时换成 1。</summary>
    private static ushort Fold(uint hash)
    {
        ushort folded = (ushort)((hash ^ (hash >> 16)) & 0xFFFF);
        return folded == 0 ? (ushort)1 : folded;
    }

    private static uint Hash32(string text)
    {
        uint hash = 2166136261u;
        for (int i = 0; i < text.Length; i++)
        {
            hash ^= (byte)text[i];
            hash *= 16777619u;
            hash ^= (byte)(text[i] >> 8);
            hash *= 16777619u;
        }
        return hash;
    }

    /// <summary>把 16 位的量混进哈希。字段表拿它把每个字段的类型签揉进形状哈希。</summary>
    internal static uint Mix(uint hash, ushort value)
    {
        hash ^= (byte)value;
        hash *= 16777619u;
        hash ^= (byte)(value >> 8);
        hash *= 16777619u;
        return hash;
    }
}
