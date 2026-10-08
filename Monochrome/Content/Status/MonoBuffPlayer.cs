using Terraria.DataStructures;

namespace Monochrome.Content.Status;

/// <summary>
/// 玩家侧的状态宿主。
/// <para>
/// 它持有一张 <see cref="MonoBuffSet"/>，把 <see cref="MonoBuff"/> 里那批原本属于
/// <see cref="ModPlayer"/> 的钩子按槽位派发下去。派发只在状态活着时发生，身上没有状态时
/// 每个钩子只是一次遍历槽表的开销。
/// </para>
/// <para>
/// 槽表在每个 tick 的 <see cref="ResetEffects"/> 里投影一次，反映的是那一刻玩家身上的原版 buff。
/// 同一 tick 中途施加或移除的状态要到下一个 tick 才可见，<see cref="MonoBuff.OnApplied(Player)"/> 与
/// <see cref="MonoBuff.OnRemoved(Player)"/> 也跟着落在这个节拍上。
/// </para>
/// </summary>
public sealed class MonoBuffPlayer : ModPlayer
{
    private MonoBuffSet? buffs;

    /// <summary>这个玩家的状态槽表。第一次取用时按已注册的状态数建好。</summary>
    public MonoBuffSet Buffs => buffs ??= new MonoBuffSet(MonoBuffSystem.SlotCount);

    /// <inheritdoc/>
    public override void ResetEffects()
    {
        Refresh();

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            MonoBuff state = MonoBuffSystem.Instances[slot];

            if (set.JustApplied(slot))
            {
                MonoBuffSlotData data = set.GetOrCreateData(slot, state.AiSize);
                data.Custom = state.CreateState();
                state.OnApplied(Player);
            }
            else if (set.JustRemoved(slot))
            {
                state.OnRemoved(Player);
                set.Release(slot);
            }
        }

        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].ResetEffects(Player);
        }
    }

    /// <inheritdoc/>
    public override void PostUpdateEquips()
    {
        Refresh();

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].PostUpdateEquips(Player);
        }
    }

    /// <inheritdoc/>
    public override void UpdateBadLifeRegen()
    {
        Refresh();

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].UpdateBadLifeRegen(Player);
        }
    }

    /// <inheritdoc/>
    public override void OnHurt(Player.HurtInfo info)
    {
        Refresh();

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].OnHurt(Player, info);
        }
    }

    /// <inheritdoc/>
    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
    {
        Refresh();

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].DrawEffects(Player, drawInfo, ref r, ref g, ref b, ref a, ref fullBright);
        }
    }

    /// <summary>把槽表投影到玩家当前的原版 buff 数组；同一 tick 里再调是空操作。</summary>
    private void Refresh() => Buffs.Refresh(Player.buffType, MonoBuffSystem.Resolve, Main.GameUpdateCount);
}
