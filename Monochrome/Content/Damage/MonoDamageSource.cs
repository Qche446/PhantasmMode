using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage;

/// <summary>
/// 从 <see cref="MonoDamageInfo"/> 里取出目标或来源实体。
/// <para>
/// 规则的条件判断几乎都要问"目标是不是这只 boss""来源是哪颗弹幕"，而编号越界与
/// "目标是玩家"这两种情况每次都得判。这几个助手把它们收在一处，规则里就只剩业务条件。
/// </para>
/// <para>
/// 取不出实体时一律返回 <c>false</c>，此时 <c>out</c> 参数是 <c>null</c>——调用方不要直接解引用。
/// 它们读 <c>Main.npc</c> / <c>Main.projectile</c>，所以只在本库的游戏侧可用。
/// </para>
/// </summary>
public static class MonoDamageSource
{
    /// <summary>取目标 NPC。目标是玩家、编号越界、或那只 NPC 不活跃时返回 false。</summary>
    /// <param name="info">这次命中。</param>
    /// <param name="npc">取到的 NPC。</param>
    public static bool TryTargetNpc(in MonoDamageInfo info, out NPC? npc)
    {
        npc = null;

        if (info.AgainstPlayer)
            return false;

        if ((uint)info.TargetIndex >= (uint)Main.npc.Length)
            return false;

        NPC candidate = Main.npc[info.TargetIndex];
        if (!candidate.active)
            return false;

        npc = candidate;
        return true;
    }

    /// <summary>取来源弹幕。来源不是弹幕、编号越界、或那颗弹幕不活跃时返回 false。</summary>
    /// <param name="info">这次命中。</param>
    /// <param name="projectile">取到的弹幕。</param>
    public static bool TrySourceProjectile(in MonoDamageInfo info, out Projectile? projectile)
    {
        projectile = null;

        if (info.SourceKind != MonoDamageSourceKind.Projectile)
            return false;

        if ((uint)info.SourceIndex >= (uint)Main.projectile.Length)
            return false;

        Projectile candidate = Main.projectile[info.SourceIndex];
        if (!candidate.active)
            return false;

        projectile = candidate;
        return true;
    }

    /// <summary>取来源 NPC。来源不是 NPC、编号越界、或那只 NPC 不活跃时返回 false。</summary>
    /// <param name="info">这次命中。</param>
    /// <param name="npc">取到的 NPC。</param>
    public static bool TrySourceNpc(in MonoDamageInfo info, out NPC? npc)
    {
        npc = null;

        if (info.SourceKind != MonoDamageSourceKind.Npc)
            return false;

        if ((uint)info.SourceIndex >= (uint)Main.npc.Length)
            return false;

        NPC candidate = Main.npc[info.SourceIndex];
        if (!candidate.active)
            return false;

        npc = candidate;
        return true;
    }
}
