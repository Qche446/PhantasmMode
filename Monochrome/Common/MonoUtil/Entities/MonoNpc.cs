namespace Monochrome.Common.MonoUtil;

/// <summary>
/// NPC 侧的取巧扩展。
/// </summary>
public static class MonoNpc
{
    /// <summary>
    /// 重新选一次目标玩家并返回。等价于原版模板里的
    /// <c>TargetClosest(); if (!HasValidTarget) 脱战;</c>，只是把脱战换成返回 <c>null</c>。
    /// </summary>
    /// <param name="npc">要取目标的 NPC。</param>
    /// <param name="faceTarget">是否顺带把 <c>npc.direction</c> 翻向目标。</param>
    /// <returns>有效目标；没有有效目标时为 <c>null</c>。</returns>
    public static Player? Target(NPC npc, bool faceTarget = true)
    {
        npc.TargetClosest(faceTarget);
        return npc.HasValidTarget ? Main.player[npc.target] : null;
    }

    /// <summary>
    /// 沿用当前目标，失效时才重选。目标活着且未走远时不会换人，适合锁住一个玩家的 Boss。
    /// </summary>
    /// <param name="npc">要取目标的 NPC。</param>
    /// <param name="faceTarget">重选时是否顺带把 <c>npc.direction</c> 翻向目标。</param>
    /// <returns>有效目标；没有有效目标时为 <c>null</c>。</returns>
    public static Player? KeepTarget(NPC npc, bool faceTarget = true)
    {
        if (!npc.HasValidTarget)
            npc.TargetClosest(faceTarget);

        return npc.HasValidTarget ? Main.player[npc.target] : null;
    }
}
