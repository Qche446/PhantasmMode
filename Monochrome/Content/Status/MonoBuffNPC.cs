namespace Monochrome.Content.Status;

/// <summary>
/// NPC 侧的状态宿主。每个 NPC 一份，跟着 <c>InstancePerEntity</c> 走。
/// <para>
/// 职责与 <see cref="MonoBuffPlayer"/> 相同，派发的是 <see cref="MonoBuff"/> 里那批原本属于
/// <see cref="GlobalNPC"/> 的钩子。原版 NPC 只有 5 个 buff 槽，所以这里的投射范围比玩家小。
/// </para>
/// <para>
/// 槽表在每个 tick 的 <see cref="ResetEffects"/> 里投影一次，节拍与玩家侧一致。
/// </para>
/// </summary>
public sealed class MonoBuffNPC : GlobalNPC
{
    private MonoBuffSet? buffs;

    /// <inheritdoc/>
    public override bool InstancePerEntity => true;

    /// <summary>这只 NPC 的状态槽表。第一次取用时按已注册的状态数建好。</summary>
    public MonoBuffSet Buffs => buffs ??= new MonoBuffSet(MonoBuffSystem.SlotCount);

    /// <inheritdoc/>
    public override void ResetEffects(NPC npc)
    {
        Refresh(npc);

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            MonoBuff state = MonoBuffSystem.Instances[slot];

            if (set.JustApplied(slot))
            {
                MonoBuffSlotData data = set.GetOrCreateData(slot, state.AiSize);
                data.Custom = state.CreateState();
                state.OnApplied(npc);
            }
            else if (set.JustRemoved(slot))
            {
                state.OnRemoved(npc);
                set.Release(slot);
            }
        }

        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].ResetEffects(npc);
        }
    }

    /// <inheritdoc/>
    public override void AI(NPC npc)
    {
        Refresh(npc);

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].AI(npc);
        }
    }

    /// <inheritdoc/>
    public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
        Refresh(npc);

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].UpdateLifeRegen(npc, ref damage);
        }
    }

    /// <inheritdoc/>
    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        Refresh(npc);

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].ModifyIncomingHit(npc, ref modifiers);
        }
    }

    /// <inheritdoc/>
    public override void DrawEffects(NPC npc, ref Color drawColor)
    {
        Refresh(npc);

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].DrawEffects(npc, ref drawColor);
        }
    }

    /// <inheritdoc/>
    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Refresh(npc);

        bool allow = true;
        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot) && !MonoBuffSystem.Instances[slot].PreDraw(npc, spriteBatch, screenPos, drawColor))
                allow = false;
        }

        return allow;
    }

    /// <inheritdoc/>
    public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Refresh(npc);

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].PostDraw(npc, spriteBatch, screenPos, drawColor);
        }
    }

    /// <inheritdoc/>
    public override void OnKill(NPC npc)
    {
        Refresh(npc);

        MonoBuffSet set = Buffs;
        for (int slot = 0; slot < set.SlotCount; slot++)
        {
            if (set.Has(slot))
                MonoBuffSystem.Instances[slot].OnKill(npc);
        }
    }

    /// <summary>把槽表投影到这只 NPC 当前的原版 buff 数组；同一 tick 里再调是空操作。</summary>
    private void Refresh(NPC npc) => Buffs.Refresh(npc.buffType, MonoBuffSystem.Resolve, Main.GameUpdateCount);
}
