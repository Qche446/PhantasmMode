using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage;

/// <summary>
/// NPC 侧的规则表入口。
/// <para>
/// <b>只挂在"有来源"的两条钩子上。</b>原版的伤害流程里，<c>ModifyIncomingHit</c> 跑在
/// <c>GetIncomingStrikeModifiers</c> 内部，也就是<b>在调用方拿到 item / projectile 之前</b>——
/// 它给不出伤害标签与来源，而规则表里凡按元素或按来源判定的规则都需要这两样。所以这里不挂它，
/// 改挂 <see cref="ModifyHitByItem"/> 与 <see cref="ModifyHitByProjectile"/>：它们由
/// <c>CombinedHooks</c> 在同一个函数调用链里随后调用，且带着完整的来源。
/// </para>
/// <para>
/// <b>因此有一条边界：</b>NPC 打 NPC（<c>StrikeOtherNPC</c>）与旧版 <c>StrikeNPC</c> 路径
/// （持续伤害的每 tick 结算走这条）不经过规则表。<see cref="MonoDamage.Run(in MonoDamageInfo, ref NPC.HitModifiers)"/>
/// 是给需要覆盖那条路径的消费者留的口子。
/// </para>
/// <para>
/// <b>每个实体一份实例。</b>tML 对 <c>InstancePerEntity</c> 为真的全局 NPC 会在 NPC 生成时调
/// <c>NewInstance</c> 新建一份并绑上实体，字段因此天然从零开始、跟着这只 NPC 走，槽位被别的 NPC
/// 顶替时也不会有残留。<see cref="ElapsedTicks"/> 就是靠这个语义做出来的，不需要额外的槽位表。
/// </para>
/// </summary>
public sealed class MonoDamageNPC : GlobalNPC
{
    /// <summary>计数的饱和值。约 77 小时，足够长，也远离 <c>int</c> 溢出。</summary>
    private const int SaturationTicks = 1 << 24;

    /// <summary>这只 NPC 已经存在了多少 tick。给"看打到第几秒了"的规则读。</summary>
    public int ElapsedTicks { get; private set; }

    /// <inheritdoc/>
    public override bool InstancePerEntity => true;

    /// <inheritdoc/>
    public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
    {
        MonoDamageInfo info = new(
            sourceKind: MonoDamageSourceKind.Player,
            sourceIndex: player.whoAmI,
            targetIndex: npc.whoAmI,
            targetType: npc.type,
            tags: MonoProjTags.TagMaskOf(item),
            crit: false,
            hitDirection: modifiers.HitDirection);

        MonoDamage.Run(in info, ref modifiers);
    }

    /// <inheritdoc/>
    public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        MonoDamageInfo info = new(
            sourceKind: MonoDamageSourceKind.Projectile,
            sourceIndex: projectile.whoAmI,
            targetIndex: npc.whoAmI,
            targetType: npc.type,
            tags: MonoProjTags.TagMaskOf(projectile),
            crit: false,
            hitDirection: modifiers.HitDirection);

        MonoDamage.Run(in info, ref modifiers);
    }

    /// <summary>把原版结算出来的数补进追踪记录。它只读，不改任何东西。</summary>
    public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        => MonoDamage.Trace.RecordResolved(hit.Damage);

    /// <summary>把原版结算出来的数补进追踪记录。它只读，不改任何东西。</summary>
    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        => MonoDamage.Trace.RecordResolved(hit.Damage);

    /// <summary>
    /// 每 tick 推进 <see cref="ElapsedTicks"/>。
    /// <para>
    /// 返回值是"要不要继续跑原版的 AI"，这里必须给 <c>true</c>——返回 <c>false</c> 会让整只 NPC 停在原地。
    /// </para>
    /// </summary>
    public override bool PreAI(NPC npc)
    {
        if (ElapsedTicks < SaturationTicks)
            ElapsedTicks++;

        return true;
    }
}
