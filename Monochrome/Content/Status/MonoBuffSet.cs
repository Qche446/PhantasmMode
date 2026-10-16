namespace Monochrome.Content.Status;

/// <summary>
/// 把一个原版 buff 类型映射到槽位。不是本系统的状态时返回 -1。
/// <para>游戏侧的实现是「<c>ModContent.GetModBuff(type) as MonoBuff</c> 取其 <see cref="MonoBuff.Slot"/>」，
/// 离线用例里传自己的映射即可。</para>
/// </summary>
/// <param name="buffType">原版 buff 类型 id。</param>
/// <returns>槽位；不属于本系统时返回 -1。</returns>
public delegate int MonoBuffSlotResolver(int buffType);

/// <summary>
/// 一个实体（玩家或 NPC）身上的状态槽表。
/// <para>
/// 第 i 格固定属于第 i 个注册的状态（<see cref="MonoBuff.Slot"/>）。每 tick 第一次访问时
/// <see cref="Refresh"/> 扫一遍原版 <c>buffType</c> 数组，把「谁活着、在原版数组里排第几」写进槽表；
/// 同一 tick 再扫会被挡掉，因此 <see cref="JustApplied"/> / <see cref="JustRemoved"/> 只反映跨 tick 的变化。
/// </para>
/// <para>
/// <b>投影会断档</b>：实体本身就会跳过投影，多人客户端上不在已加载区块的 NPC 就是如此。
/// 所以两次投影间隔超过一 tick 时，槽表按「中间那段没看见」处理——旧数据全部丢掉，当前还在的状态
/// 当作刚施加，离开的状态不补发 <see cref="MonoBuff.OnRemoved(NPC)"/>。
/// </para>
/// <para>
/// <b>「有没有这个状态」的真相源始终是原版 buff 数组</b>：右键取消、<c>ClearBuff</c>、死亡重置、
/// 多人同步都只动那一份数据，槽表只做它的投影。原版数组每个 tick 都会递减时长并把到期的删掉，
/// 所以槽表要在每个 tick 重新投影一次。
/// </para>
/// <para>
/// 这个类不引用任何 Terraria 类型，能直接进离线验收台。
/// </para>
/// </summary>
public sealed class MonoBuffSet
{
    /// <summary>本 tick 的占用集合。</summary>
    private readonly bool[] current;

    /// <summary>上一 tick 的占用集合，用来算进入 / 离开。</summary>
    private readonly bool[] previous;

    /// <summary>各槽位在当前原版数组里的下标，未占用时为 -1。</summary>
    private readonly int[] buffIndices;

    /// <summary>各槽位的消费者数据，第一次取用时才建。</summary>
    private readonly MonoBuffSlotData?[] data;

    /// <summary>各槽位最近一次写进包里的 <c>Ai</c>；没发过是 null。用来判断这一格有没有变。</summary>
    private readonly float[]?[] sentAi;

    /// <summary>已经投影过的 tick；用来挡掉同一 tick 的重复刷新。</summary>
    private long tick = long.MinValue;

    /// <summary>建一张槽表。</summary>
    /// <param name="slotCount">注册的状态数量，也就是槽位总数。</param>
    public MonoBuffSet(int slotCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slotCount);

        current = new bool[slotCount];
        previous = new bool[slotCount];
        buffIndices = new int[slotCount];
        data = new MonoBuffSlotData?[slotCount];
        sentAi = new float[]?[slotCount];
        Array.Fill(buffIndices, -1);
    }

    /// <summary>槽位总数。</summary>
    public int SlotCount => current.Length;

    /// <summary>本 tick 活跃的状态数。</summary>
    public int ActiveCount { get; private set; }

    /// <summary>上一次投影用的 tick。</summary>
    public long Tick => tick;

    /// <summary>
    /// 按实体当前的原版 buff 数组重新投影槽表，并算出这一 tick 的进入 / 离开。
    /// </summary>
    /// <param name="buffTypes">实体的 <c>buffType</c> 数组；空闲格子的值是什么都行。</param>
    /// <param name="resolve">buff 类型到槽位的映射。</param>
    /// <param name="tick">当前 tick；与上一次相同时整个调用被挡掉。</param>
    public void Refresh(ReadOnlySpan<int> buffTypes, MonoBuffSlotResolver resolve, long tick)
    {
        ArgumentNullException.ThrowIfNull(resolve);

        if (tick == this.tick)
            return;

        // 断档：上一次投影不在上一 tick。实体会跳过投影——多人客户端上，不在已加载区块的 NPC
        // 在 NPCLoader.ResetEffects 之前就 return 了（NPC.cs:88381-88389）。跨过这段空档的
        // "消失又出现"会被整段吞掉，所以旧数据一律作废，当前还在的按刚施加处理。
        if (this.tick != long.MinValue && tick != this.tick + 1)
        {
            Array.Clear(current);
            Array.Clear(previous);
            Array.Clear(data);
            Array.Clear(sentAi);
            ActiveCount = 0;
        }

        Array.Copy(current, previous, current.Length);
        Array.Clear(current);
        Array.Fill(buffIndices, -1);
        ActiveCount = 0;

        for (int i = 0; i < buffTypes.Length; i++)
        {
            int slot = resolve(buffTypes[i]);
            if (!InRange(slot) || current[slot])
                continue;

            current[slot] = true;
            buffIndices[slot] = i;
            ActiveCount++;
        }

        this.tick = tick;
    }

    /// <summary>本 tick 这个槽位活着吗。</summary>
    /// <param name="slot">槽位。</param>
    public bool Has(int slot) => InRange(slot) && current[slot];

    /// <summary>这个槽位是在本 tick 出现的吗。</summary>
    /// <param name="slot">槽位。</param>
    public bool JustApplied(int slot) => InRange(slot) && current[slot] && !previous[slot];

    /// <summary>这个槽位是在本 tick 消失的吗。</summary>
    /// <param name="slot">槽位。</param>
    public bool JustRemoved(int slot) => InRange(slot) && !current[slot] && previous[slot];

    /// <summary>这个槽位对应的原版 buff 下标；没占用时是 -1。</summary>
    /// <param name="slot">槽位。</param>
    public int BuffIndex(int slot) => InRange(slot) ? buffIndices[slot] : -1;

    /// <summary>取这个槽位的消费者数据；还没建过时是 <see langword="null"/>。</summary>
    /// <param name="slot">槽位。</param>
    public MonoBuffSlotData? Data(int slot) => InRange(slot) ? data[slot] : null;

    /// <summary>取这个槽位的消费者数据，没有就按 <paramref name="aiSize"/> 建一份。</summary>
    /// <param name="slot">槽位。</param>
    /// <param name="aiSize">参数数组长度，取 <see cref="MonoBuff.AiSize"/>。</param>
    public MonoBuffSlotData GetOrCreateData(int slot, int aiSize)
    {
        if (!InRange(slot))
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "槽位超出范围。");

        return data[slot] ??= new MonoBuffSlotData(aiSize);
    }

    /// <summary>丢掉这个槽位的消费者数据。数据里的内容会跟着一起没。</summary>
    /// <param name="slot">槽位。</param>
    public void Release(int slot)
    {
        if (!InRange(slot))
            return;

        data[slot] = null;
        sentAi[slot] = null;
    }

    /// <summary>把整张表恢复到刚建好的样子，包括清掉全部消费者数据。</summary>
    public void Clear()
    {
        Array.Clear(current);
        Array.Clear(previous);
        Array.Fill(buffIndices, -1);
        Array.Clear(data);
        Array.Clear(sentAi);
        ActiveCount = 0;
        tick = long.MinValue;
    }

    /// <summary>
    /// 这个槽位的 <c>Ai</c> 与最近一次写进包里的值不一样吗。没有数据时是 <see langword="false"/>。
    /// </summary>
    /// <param name="slot">槽位。</param>
    public bool AiChanged(int slot)
    {
        if (!InRange(slot) || data[slot] is not { } slotData)
            return false;

        float[]? sent = sentAi[slot];
        if (sent is null || sent.Length != slotData.Ai.Length)
            return true;

        for (int i = 0; i < sent.Length; i++)
        {
            if (sent[i] != slotData.Ai[i])
                return true;
        }

        return false;
    }

    /// <summary>把这一格当前的 <c>Ai</c> 记成"已经写进包里了"，<see cref="AiChanged"/> 随之回到 <see langword="false"/>。</summary>
    /// <param name="slot">槽位。</param>
    public void AcceptAi(int slot)
    {
        if (!InRange(slot))
            return;

        if (data[slot] is not { } slotData)
        {
            sentAi[slot] = null;
            return;
        }

        float[] sent = sentAi[slot] is { } existing && existing.Length == slotData.Ai.Length
            ? existing
            : sentAi[slot] = new float[slotData.Ai.Length];

        Array.Copy(slotData.Ai, sent, sent.Length);
    }

    /// <summary>有没有哪一格"在场的、该同步的"槽位的 <c>Ai</c> 变了。实体要不要发一次同步看它。</summary>
    /// <param name="syncOf">按槽位问该不该同步（游戏侧是 <see cref="MonoBuff.SyncAi"/>）。</param>
    public bool AnyAiDirty(Func<int, bool> syncOf)
    {
        ArgumentNullException.ThrowIfNull(syncOf);

        for (int slot = 0; slot < current.Length; slot++)
        {
            if (current[slot] && syncOf(slot) && AiChanged(slot))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 把该同步的槽位的 <c>Ai</c> 写进包：先一个字节的槽位数，然后每项"槽位号、数组长度、各值"。
    /// <para>
    /// <paramref name="all"/> 为 <see langword="false"/> 时只写值变过的（平时用的就是它）；
    /// 为 <see langword="true"/> 时把在场的都写一遍，用于新玩家加入世界那种全量补齐。
    /// </para>
    /// <para>写出去的格子会按"已发送"记账，于是同一份值不会被重复发。</para>
    /// </summary>
    /// <param name="syncOf">按槽位问该不该同步。</param>
    /// <param name="all">是否连没变过的也写。</param>
    /// <param name="writer">包体。</param>
    /// <returns>写进去的槽位数。</returns>
    public int WriteSync(Func<int, bool> syncOf, bool all, BinaryWriter writer)
    {
        ArgumentNullException.ThrowIfNull(syncOf);
        ArgumentNullException.ThrowIfNull(writer);

        int count = 0;
        for (int slot = 0; slot < current.Length; slot++)
        {
            if (Syncable(slot, syncOf, all))
                count++;
        }

        writer.Write((byte)count);

        for (int slot = 0; slot < current.Length; slot++)
        {
            if (!Syncable(slot, syncOf, all))
                continue;

            float[] ai = data[slot]!.Ai;
            writer.Write((byte)slot);
            writer.Write((byte)ai.Length);
            for (int i = 0; i < ai.Length; i++)
                writer.Write(ai[i]);

            AcceptAi(slot);
        }

        return count;
    }

    /// <summary>
    /// 从包里读回一批 <c>Ai</c> 并落进槽位。落不过去的格子（槽位越界、值比本地数组多）只把字节读掉，
    /// 不落值——少读一个字节会让后面全部错位。
    /// </summary>
    /// <param name="reader">包体。</param>
    public void ReadSync(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        int count = reader.ReadByte();
        for (int i = 0; i < count; i++)
        {
            int slot = reader.ReadByte();
            int size = reader.ReadByte();

            MonoBuffSlotData? data = InRange(slot) ? GetOrCreateData(slot, size) : null;
            for (int k = 0; k < size; k++)
            {
                float value = reader.ReadSingle();
                if (data is not null && k < data.Ai.Length)
                    data.Ai[k] = value;
            }

            if (InRange(slot))
                AcceptAi(slot);
        }
    }

    /// <summary>这一格要不要进包：在场的、有数据的、声明了要同步的，并且（全量 或 值变过）。</summary>
    private bool Syncable(int slot, Func<int, bool> syncOf, bool all)
        => current[slot] && data[slot] is not null && syncOf(slot) && (all || AiChanged(slot));

    /// <summary>槽位落在这张表里吗。</summary>
    private bool InRange(int slot) => (uint)slot < (uint)current.Length;
}
