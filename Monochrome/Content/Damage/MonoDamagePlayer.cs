using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage;

/// <summary>
/// 玩家侧的规则表入口。
/// <para>
/// 原版在 <c>PlayerLoader.ModifyHurt</c> 里先按伤害来源分派 <c>ModifyHitByNPC</c> /
/// <c>ModifyHitByProjectile</c>，随后<b>无条件</b>跑一遍 <c>ModifyHurt</c>。顺序正好与 NPC 侧相反，
/// 所以这里可以做到"信息最全的那一次恰好也是唯一的那一次"：
/// </para>
/// <list type="number">
/// <item>来源能解析出实体时，在 <c>ModifyHitBy*</c> 里带着来源跑规则表，并立一个标记；</item>
/// <item>紧随其后的 <c>ModifyHurt</c> 看到标记就跳过，只把标记清掉；</item>
/// <item>没有来源（环境伤害、无实体来源的伤害）时标记是空的，规则表在 <c>ModifyHurt</c> 里以
/// "来源不明"跑一遍。</item>
/// </list>
/// <para>
/// 标记是每个玩家一份的实例字段，而 <c>ModPlayer</c> 每人一个实例、跨帧保活，正好放得下。
/// </para>
/// </summary>
public sealed class MonoDamagePlayer : ModPlayer
{
    /// <summary>本次受击是否已经在带来源的那条钩子里跑过规则表。</summary>
    private bool sourceHookHandled;

    /// <inheritdoc/>
    public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
    {
        RunWithSource(MonoDamageSourceKind.Npc, npc.whoAmI, MonoProjTags.TagMaskOf(npc), ref modifiers);
        sourceHookHandled = true;
    }

    /// <inheritdoc/>
    public override void ModifyHitByProjectile(Projectile projectile, ref Player.HurtModifiers modifiers)
    {
        RunWithSource(MonoDamageSourceKind.Projectile, projectile.whoAmI, MonoProjTags.TagMaskOf(projectile), ref modifiers);
        sourceHookHandled = true;
    }

    /// <inheritdoc/>
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (sourceHookHandled)
        {
            sourceHookHandled = false;
            return;
        }

        MonoDamageInfo info = new(
            sourceKind: MonoDamageSourceKind.Unknown,
            sourceIndex: -1,
            targetIndex: Player.whoAmI,
            targetType: -1,
            tags: 0UL,
            crit: false,
            hitDirection: modifiers.HitDirection,
            againstPlayer: true);

        MonoDamage.Run(in info, ref modifiers);
    }

    /// <summary>把原版结算出来的数补进追踪记录。它只读，不改任何东西。</summary>
    public override void OnHurt(Player.HurtInfo info) => MonoDamage.Trace.RecordResolved(info.Damage);

    /// <summary>以某个已识别的来源跑一趟规则表。</summary>
    private void RunWithSource(MonoDamageSourceKind kind, int sourceIndex, ulong tags, ref Player.HurtModifiers modifiers)
    {
        MonoDamageInfo info = new(
            sourceKind: kind,
            sourceIndex: sourceIndex,
            targetIndex: Player.whoAmI,
            targetType: -1,
            tags: tags,
            crit: false,
            hitDirection: modifiers.HitDirection,
            againstPlayer: true);

        MonoDamage.Run(in info, ref modifiers);
    }
}
