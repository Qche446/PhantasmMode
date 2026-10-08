namespace Monochrome.Content.Status;

/// <summary>
/// 状态系统的消费入口。
/// <para>
/// 用法：<c>npc.MonoBuffs().Has&lt;HallowFlameBuff&gt;()</c> 问状态在不在，
/// <c>npc.MonoBuffs().Data&lt;HallowFlameBuff&gt;()</c> 取这个实体上的伴生数据。
/// 实体上没有这个状态时 <c>Data</c> 返回 <see langword="null"/>，<c>GetOrCreateData</c> 会把它建出来。
/// </para>
/// </summary>
public static class MonoBuffAccess
{
    /// <summary>取这个玩家的状态槽表。</summary>
    /// <param name="player">玩家。</param>
    public static MonoBuffSet MonoBuffs(this Player player) => player.GetModPlayer<MonoBuffPlayer>().Buffs;

    /// <summary>取这只 NPC 的状态槽表。</summary>
    /// <param name="npc">NPC。</param>
    public static MonoBuffSet MonoBuffs(this NPC npc) => npc.GetGlobalNPC<MonoBuffNPC>().Buffs;

    /// <summary>这个实体身上有这个状态吗。</summary>
    /// <typeparam name="T">状态类型。</typeparam>
    /// <param name="set">槽表。</param>
    public static bool Has<T>(this MonoBuffSet set) where T : MonoBuff => set.Has(MonoBuffSlot<T>.Index);

    /// <summary>取这个实体身上该状态的伴生数据；没有时是 <see langword="null"/>。</summary>
    /// <typeparam name="T">状态类型。</typeparam>
    /// <param name="set">槽表。</param>
    public static MonoBuffSlotData? Data<T>(this MonoBuffSet set) where T : MonoBuff
        => set.Data(MonoBuffSlot<T>.Index);

    /// <summary>取这个实体身上该状态的伴生数据，没有就按该状态的 <see cref="MonoBuff.AiSize"/> 建一份。</summary>
    /// <typeparam name="T">状态类型。</typeparam>
    /// <param name="set">槽表。</param>
    public static MonoBuffSlotData GetOrCreateData<T>(this MonoBuffSet set) where T : MonoBuff
        => set.GetOrCreateData(MonoBuffSlot<T>.Index, ModContent.GetInstance<T>().AiSize);
}
