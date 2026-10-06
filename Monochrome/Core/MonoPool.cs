using System.Text;

namespace Monochrome.Core;

/// <summary>
/// 一个池槽位的句柄：<c>Index</c> 定位槽位，<c>Version</c> 证明"它还是当初那个东西"。
/// <para>
/// 这是整套句柄纪律的第一条：<b>裸下标不能当身份</b>。槽位会被复用——同一个下标下，
/// 上一帧是一个粒子，这一帧可能是一只史莱姆。只存下标的话，旧句柄会静默地指向一个毫不相干的对象，
/// 而那是最难查的一类 bug（症状离原因很远）。
/// </para>
/// <para>
/// 这与蓝图 §7.6.6 里蠕虫的 <c>MonoWormId</c>（<c>HeadIndex</c> + <c>Generation</c>）是同一个手法：
/// 凡"跨帧持有的引用"，一律用"下标 + 代号"，不要用裸下标，也不要用会被换载体的引用。
/// </para>
/// </summary>
public readonly struct MonoHandle : IEquatable<MonoHandle>
{
    /// <summary>空句柄。取 <c>Version == 0</c> 表示"什么也没指向"。</summary>
    public static readonly MonoHandle None = default;

    /// <summary>槽位下标。</summary>
    public int Index { get; }

    /// <summary>分配代号：每次分配 +1，归还时再 +1。<b>为 0 表示空句柄。</b></summary>
    public int Version { get; }

    internal MonoHandle(int index, int version)
    {
        Index = index;
        Version = version;
    }

    /// <summary>这个句柄有没有指向东西。</summary>
    public bool IsValid => Version != 0;

    /// <inheritdoc/>
    public bool Equals(MonoHandle other) => Index == other.Index && Version == other.Version;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is MonoHandle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Index, Version);

    /// <summary>== 运算符。</summary>
    public static bool operator ==(MonoHandle left, MonoHandle right) => left.Equals(right);

    /// <summary>!= 运算符。</summary>
    public static bool operator !=(MonoHandle left, MonoHandle right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => Version == 0 ? "MonoHandle(None)" : $"MonoHandle({Index}#{Version})";
}

/// <summary>
/// 结构体句柄 + 索引池（空闲链表用 <c>int[]</c> 实现，<b>不用 <c>Queue&lt;int&gt;</c> 也不装箱</b>）。
/// <para>
/// 用来替代"每帧 new 一堆小对象"：粒子、临时缓冲、事件记录这类东西都该从这里借。
/// 归还后槽位可能被立刻复用，所以<b>归还之后不要再用旧句柄</b>——<see cref="TryGet(MonoHandle, out T)"/> 会因为版本不匹配返回 false，
/// 而不是悄悄给你一个别人的槽位。
/// </para>
/// <para>
/// <b>不扩容</b>：容量在构造时定死，满了就返回 <see cref="MonoHandle.None"/>。
/// 这与蓝图 §18.1「轨迹环预分配固定容量，不动态增长」是同一条纪律——一个会在压力下扩容的池，
/// 恰恰会在最需要它的时候制造一次 GC。
/// </para>
/// </summary>
/// <typeparam name="T">槽位类型。用 <c>struct</c> 才能让"池化"真的省下 GC。</typeparam>
public sealed class MonoPool<T> where T : struct
{
    private readonly T[] items;
    private readonly int[] versions;

    /// <summary>空闲链表：<c>freeStack[0..freeCount)</c> 是可用槽位下标。</summary>
    private readonly int[] freeStack;

    private int freeCount;
    private int rentCount;      // 累计借出次数（诊断用）
    private int missCount;      // 因容量耗尽而借失败的次数

    /// <summary>建一个池。</summary>
    /// <param name="capacity">槽位数量，至少 1。</param>
    /// <param name="name">诊断里显示的名字。</param>
    public MonoPool(int capacity, string name = "")
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "池容量必须 ≥ 1。");

        items = new T[capacity];
        versions = new int[capacity];
        freeStack = new int[capacity];
        Name = name.Length > 0 ? name : typeof(T).Name;

        // 空闲链表按逆序压栈，于是第一次 Rent 拿到的是 0 号槽位（顺序可预测，方便复现 bug）。
        for (int i = 0; i < capacity; i++)
            freeStack[i] = capacity - 1 - i;
        freeCount = capacity;
    }

    /// <summary>诊断用的名字。</summary>
    public string Name { get; }

    /// <summary>槽位总数。</summary>
    public int Capacity => items.Length;

    /// <summary>当前已借出的槽位数。</summary>
    public int InUse => Capacity - freeCount;

    /// <summary>累计借出次数。</summary>
    public int RentCount => rentCount;

    /// <summary>因容量耗尽而失败的借出次数。<b>非 0 就说明容量该调大，或者有东西忘了归还。</b></summary>
    public int MissCount => missCount;

    /// <summary>
    /// 借一个槽位。借不到（池空）时返回 <see cref="MonoHandle.None"/>，并且 <paramref name="item"/> 是 <c>default</c>。
    /// <para>
    /// 返回 <c>ref T</c> 而不是对象引用：调用方拿到的是槽位本身，改它就是改池里的那一格，
    /// 不存在"我改的是副本"这种误会。
    /// </para>
    /// </summary>
    /// <param name="item">借到的槽位（按引用）。池空时是 <c>default</c>，不要用它。</param>
    /// <returns>句柄；池空时是 <see cref="MonoHandle.None"/>。</returns>
    public MonoHandle Rent(out T item)
    {
        return Rent(out item, out _);
    }

    /// <summary>
    /// 借一个槽位，并带回它的下标（把下标存进别的并行数组时用，省一次句柄解包）。
    /// </summary>
    /// <param name="item">借到的槽位（按引用）。</param>
    /// <param name="index">借到的槽位下标；失败时是 -1。</param>
    /// <returns>句柄；池空时是 <see cref="MonoHandle.None"/>。</returns>
    public MonoHandle Rent(out T item, out int index)
    {
        if (freeCount == 0)
        {
            missCount++;
            item = default;
            index = -1;
            return MonoHandle.None;
        }

        index = freeStack[--freeCount];
        rentCount++;

        // 归零：借出来的槽位必须是干净的，否则上一次的残留会变成"幽灵数据"。
        items[index] = default;

        // 版本 +1 让"上一次占用该槽位的句柄"当场失效。跳过 0（0 是空句柄的标记）。
        if (++versions[index] == 0)
            versions[index] = 1;

        item = items[index];
        return new MonoHandle(index, versions[index]);
    }

    /// <summary>按句柄取回槽位（按引用）。版本不匹配或句柄为空都返回 false。</summary>
    /// <param name="handle">句柄。</param>
    /// <param name="item">取回的槽位（按引用）。</param>
    /// <returns>句柄仍然有效才返回 true。</returns>
    public bool TryGet(MonoHandle handle, out T item)
    {
        return TryGet(handle, out item, out _);
    }

    /// <summary>按句柄取回槽位，并带回下标。</summary>
    /// <param name="handle">句柄。</param>
    /// <param name="item">取回的槽位（按引用）。</param>
    /// <param name="index">槽位下标；失败时是 -1。</param>
    /// <returns>句柄仍然有效才返回 true。</returns>
    public bool TryGet(MonoHandle handle, out T item, out int index)
    {
        index = handle.Index;
        if (!IsLive(in handle))
        {
            item = default;
            index = -1;
            return false;
        }
        item = items[index];
        return true;
    }

    /// <summary>按句柄取槽位的只读引用（<c>ref readonly</c>，不改池里的内容）。</summary>
    /// <param name="handle">句柄。</param>
    /// <param name="item">槽位的只读引用。</param>
    /// <returns>句柄仍然有效才返回 true。</returns>
    public bool TryGetReadOnly(MonoHandle handle, out T item)
    {
        bool ok = IsLive(in handle);
        item = ok ? items[handle.Index] : default;
        return ok;
    }

    /// <summary>句柄是否仍然指向一个活着的槽位（版本对得上、且没被归还）。</summary>
    /// <param name="handle">句柄。</param>
    public bool IsLive(in MonoHandle handle)
        => handle.Version != 0
           && (uint)handle.Index < (uint)items.Length
           && versions[handle.Index] == handle.Version;

    /// <summary>
    /// 取回槽位的可变引用（<c>ref</c>），<b>不检查句柄</b>。
    /// <para>
    /// 只在"我已经确认过它活着、并且每帧都要写它"的热路径上用（比如粒子系统推进位置）。
    /// 拿不准就先 <see cref="TryGet(MonoHandle, out T)"/>。
    /// </para>
    /// </summary>
    /// <param name="handle">句柄。必须是有效的。</param>
    /// <returns>槽位的引用。这个 <c>ref</c> 会直接写进池里的那一格。</returns>
    public ref T GetRef(MonoHandle handle) => ref items[handle.Index];

    /// <summary>
    /// 归还一个槽位。重复归还、归还空句柄、归还过期句柄都是<b>安全空操作</b>（返回 false），
    /// 不会污染空闲链表——双重归还会把同一个槽位塞进链表两次，那是"池逐渐吐出不存在的槽位"的经典成因。
    /// </summary>
    /// <param name="handle">要归还的句柄。</param>
    /// <returns>确实归还了一个槽位才返回 true。</returns>
    public bool Return(MonoHandle handle)
    {
        if (!IsLive(in handle))
            return false;

        int index = handle.Index;
        items[index] = default;

        // 版本再 +1：让调用方手里那份句柄立刻彻底失效。
        if (++versions[index] == 0)
            versions[index] = 1;

        freeStack[freeCount++] = index;
        return true;
    }

    /// <summary>清空全部槽位，把池恢复到刚构造的状态（<b>所有句柄一起失效</b>）。</summary>
    public void Clear()
    {
        Array.Clear(items);
        for (int i = 0; i < Capacity; i++)
        {
            freeStack[i] = Capacity - 1 - i;

            // 版本继续递增而不是归零：清空之前的句柄不该因为"池被清过"而复活。
            if (++versions[i] == 0)
                versions[i] = 1;
        }
        freeCount = Capacity;
    }

    /// <summary>诊断输出：容量、占用、历史峰值、失败次数。</summary>
    public string Describe()
        => $"池 {Name}: {InUse}/{Capacity} 占用，累计借出 {rentCount}，容量耗尽 {missCount} 次";

    /// <summary>池内元素的只读视图。<b>只给诊断用</b>——绕过句柄直接遍历会破坏版本语义。</summary>
    public ReadOnlySpan<T> RawItems => items;
}

/// <summary>
/// 定长环形缓冲。**预分配、绝不动态增长**（蓝图 §15.3）：诊断与遥测一旦会扩容，
/// 它观测的那条路径的性能特征就被自己改变了。
/// <para>
/// 满了就覆盖最旧的一条，写入永远是 O(1) 且零分配。
/// </para>
/// </summary>
/// <typeparam name="T">元素类型。</typeparam>
public sealed class MonoRingBuffer<T>
{
    private readonly T[] items;
    private int head;
    private int count;
    private long written;

    /// <summary>建一个环形缓冲。</summary>
    /// <param name="capacity">容量，至少 1。</param>
    public MonoRingBuffer(int capacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "环形缓冲容量必须 ≥ 1。");
        items = new T[capacity];
    }

    /// <summary>容量。</summary>
    public int Capacity => items.Length;

    /// <summary>当前条数（≤ <see cref="Capacity"/>）。</summary>
    public int Count => count;

    /// <summary>累计写入条数（含已被覆盖的）。</summary>
    public long Written => written;

    /// <summary>是否已经被写过至少 <see cref="Capacity"/> 次（即最旧的数据已经被覆盖）。</summary>
    public bool IsFull => count == items.Length;

    /// <summary>写一条。满了就覆盖最旧的。</summary>
    /// <param name="value">要写入的值。</param>
    public void Add(T value)
    {
        items[head] = value;
        head = (head + 1) % items.Length;
        if (count < items.Length)
            count++;
        written++;
    }

    /// <summary>按时间顺序取第 <paramref name="i"/> 条（0 = 最旧的）。越界返回 <c>default</c>。</summary>
    /// <param name="i">从最旧算起的序号。</param>
    public T this[int i]
    {
        get
        {
            if ((uint)i >= (uint)count)
                return default!;
            int start = count == items.Length ? head : 0;
            return items[(start + i) % items.Length];
        }
    }

    /// <summary>清空。<b>不擦除底层数组</b>——环形缓冲常存的是值类型快照，逐格清零纯属浪费。</summary>
    public void Clear()
    {
        head = 0;
        count = 0;
    }

    /// <summary>把内容按时间顺序复制到目标跨度（最旧的在前）。返回实际复制的条数。</summary>
    /// <param name="destination">目标跨度。</param>
    public int CopyTo(Span<T> destination)
    {
        int copied = Math.Min(count, destination.Length);
        int start = count == items.Length ? head : 0;
        for (int i = 0; i < copied; i++)
            destination[i] = items[(start + i) % items.Length];
        return copied;
    }

    /// <summary>底层数组的只读视图（诊断用）。顺序不是时间顺序。</summary>
    public ReadOnlySpan<T> Raw => items.AsSpan(0, count);

    /// <summary>把内容拼成一行，元素按 <paramref name="format"/> 格式化。<b>会分配</b>，只在命令里用。</summary>
    /// <param name="format">每个元素后面的分隔符。</param>
    public string Describe(string format = ", ")
    {
        if (count == 0)
            return "（空）";

        StringBuilder output = new(count * 8);
        int start = count == items.Length ? head : 0;
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
                output.Append(format);
            output.Append(items[(start + i) % items.Length]);
        }
        return output.ToString();
    }
}

/// <summary>
/// 一个可以在热路径上零分配地"批量登记待处理项"的定长列表。
/// <para>
/// 它存在的场景很具体：某个系统的写入时机被迫在"不能立刻处理"的位置（比如只能在渲染相位收集，
/// 但真正的处理要在更新相位做）。直接 <c>List.Add</c> 会在压力下扩容；这里满了就丢弃并计数，
/// 且<b>丢弃是安静的</b>——因为压力下的正确行为是"少做一点"而不是"崩掉"。
/// </para>
/// </summary>
/// <typeparam name="T">元素类型。</typeparam>
public sealed class MonoFrameQueue<T>
{
    private readonly T[] items;
    private int count;

    /// <summary>建一个帧内队列。</summary>
    /// <param name="capacity">容量。</param>
    /// <param name="name">诊断名。</param>
    public MonoFrameQueue(int capacity, string name = "")
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "容量必须 ≥ 1。");
        items = new T[capacity];
        Name = name.Length > 0 ? name : typeof(T).Name;
    }

    /// <summary>诊断名。</summary>
    public string Name { get; }

    /// <summary>容量。</summary>
    public int Capacity => items.Length;

    /// <summary>当前条数。</summary>
    public int Count => count;

    /// <summary>因为满而被丢弃的累计条数。<b>非 0 说明容量不够，或者忘了在帧末 <see cref="Clear"/>。</b></summary>
    public int Dropped { get; private set; }

    /// <summary>入队。满了返回 false 并计入丢弃数。</summary>
    /// <param name="value">要入队的值。</param>
    public bool Enqueue(T value)
    {
        if (count == items.Length)
        {
            Dropped++;
            return false;
        }
        items[count++] = value;
        return true;
    }

    /// <summary>取第 <paramref name="i"/> 条。</summary>
    /// <param name="i">下标。</param>
    public ref T this[int i] => ref items[i];

    /// <summary>当前内容的只读视图。</summary>
    public ReadOnlySpan<T> Items => items.AsSpan(0, count);

    /// <summary>清空。帧末一定要调——不调的话第二天它就从"零分配"变成"永远只能收满一次"。</summary>
    public void Clear() => count = 0;

    /// <summary>诊断输出。</summary>
    public string Describe() => $"帧内队列 {Name}: {count}/{Capacity}，累计丢弃 {Dropped}";
}
