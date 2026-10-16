namespace Monochrome.Common.MonoUtil;

/// <summary>
/// 实体侧的一批小工具：方向、左右判定、类型取用等。它们都是扩展方法，所以
/// <c>npc.SafeDirectionTo(target)</c> 与 <c>MonoUtil.SafeDirectionTo(npc, target)</c> 两种写法都能用。
/// </summary>
public static partial class MonoUtil
{
    /// <summary>从实体中心指向目标点的单位矢量；与中心重合时返回 <see cref="Vector2.Zero"/>。</summary>
    /// <param name="entity">相对哪个实体。</param>
    /// <param name="destination">目标点。</param>
    public static Vector2 SafeDirectionTo(this Entity entity, Vector2 destination)
        => SafeNormalize(destination - entity.Center, Vector2.Zero);

    /// <summary>从起点指向目标点的单位矢量；两点重合时返回 <see cref="Vector2.Zero"/>。</summary>
    /// <param name="from">起点。</param>
    /// <param name="destination">目标点。</param>
    public static Vector2 SafeDirectionTo(this Vector2 from, Vector2 destination)
        => SafeNormalize(destination - from, Vector2.Zero);

    /// <summary>目标点是否落在实体中心（含）的右侧。</summary>
    /// <param name="entity">相对哪个实体。</param>
    /// <param name="destination">目标点。</param>
    public static bool OnRightSideOf(this Entity entity, Vector2 destination)
        => entity.Center.X >= destination.X;

    /// <summary>另一个实体的中心是否落在本实体中心（含）的右侧。</summary>
    /// <param name="entity">相对哪个实体。</param>
    /// <param name="other">另一个实体。</param>
    public static bool OnRightSideOf(this Entity entity, Entity other)
        => entity.Center.X >= other.Center.X;

    /// <summary>指向目标的水平方向：目标在中心左侧返回 <c>-1</c>，右侧（含正对）返回 <c>1</c>。</summary>
    /// <param name="entity">相对哪个实体。</param>
    /// <param name="destination">目标点。</param>
    public static float HorizontalDirectionTo(this Entity entity, Vector2 destination)
        => entity.Center.X >= destination.X ? -1f : 1f;

    /// <summary>鼠标上的物品；鼠标为空时退回玩家当前手持的物品。</summary>
    /// <param name="player">玩家。</param>
    public static Item HeldMouseItem(this Player player)
        => Main.mouseItem.IsAir ? player.HeldItem : Main.mouseItem;

    /// <summary>
    /// 把 NPC 取成指定的 <see cref="ModNPC"/> 类型。
    /// <para>
    /// 取不到时记一条警告，并返回该类型的默认实例（<see cref="ModContent.GetInstance{T}"/>）当替身——
    /// 它是"宁可画错也不要崩"的取舍。调用方拿到的替身字段都是默认值，不要拿它做逻辑判断。
    /// </para>
    /// </summary>
    /// <typeparam name="TNPC">期望的 ModNPC 类型。</typeparam>
    /// <param name="npc">要转换的 NPC。</param>
    public static TNPC As<TNPC>(this NPC npc) where TNPC : ModNPC
    {
        if (npc.ModNPC is TNPC casted)
            return casted;

        string actual = npc.ModNPC is null ? $"原版 NPC（type {npc.type}）" : npc.ModNPC.Name;
        Monochrome.Instance?.Logger.Warn($"{actual} 被当成了 {typeof(TNPC).Name}，已退回该类型的默认实例。");
        return ModContent.GetInstance<TNPC>();
    }

    /// <summary>
    /// 把弹幕取成指定的 <see cref="ModProjectile"/> 类型。取不到时的处理与
    /// <see cref="As{TNPC}(NPC)"/> 相同。
    /// </summary>
    /// <typeparam name="TProjectile">期望的 ModProjectile 类型。</typeparam>
    /// <param name="projectile">要转换的弹幕。</param>
    public static TProjectile As<TProjectile>(this Projectile projectile) where TProjectile : ModProjectile
    {
        if (projectile.ModProjectile is TProjectile casted)
            return casted;

        string actual = projectile.ModProjectile is null ? $"原版弹幕（type {projectile.type}）" : projectile.ModProjectile.Name;
        Monochrome.Instance?.Logger.Warn($"{actual} 被当成了 {typeof(TProjectile).Name}，已退回该类型的默认实例。");
        return ModContent.GetInstance<TProjectile>();
    }

    /// <summary>
    /// 当前是否有任意 Boss 在场（世界吞噬者的头/身/尾也算）。同一帧内只扫一次。
    /// </summary>
    public static bool AnyBosses()
    {
        if (bossCacheFrame == Main.GameUpdateCount)
            return bossCacheValue;

        bool any = false;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            bool eater = npc.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail;
            if (npc.boss || eater)
            {
                any = true;
                break;
            }
        }

        bossCacheFrame = Main.GameUpdateCount;
        bossCacheValue = any;
        return any;
    }

    private static uint bossCacheFrame = uint.MaxValue;
    private static bool bossCacheValue;

    /// <summary>反射查成员时用的通用 <see cref="BindingFlags"/>：实例 / 静态 / 公开 / 非公开全都要。</summary>
    public static readonly BindingFlags UniversalBindingFlags =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
}
