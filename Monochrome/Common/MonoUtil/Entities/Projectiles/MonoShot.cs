using Terraria.DataStructures;

namespace Monochrome.Common.MonoUtil;

/// <summary>
/// 弹幕发射的收口层。三种排布各一个函数：整圈铺满、沿一段弧铺开、以某个方向为中心随机散布。
/// <para>
/// 用它的收益有两处。发射循环只写一遍，角度算式与 <c>ai0/ai1/ai2</c> 的位置参数不再各写各的；
/// 权威端门统一放在函数内部，多人下客户端调用不会生成弹幕。
/// </para>
/// <para>
/// 它只负责生成。命中、绘制、存活由弹幕自身与消费者决定；想自己写循环时继续直连
/// <c>Projectile.NewProjectile</c>。
/// </para>
/// <para>
/// 范围之外：弹幕模板与外观、时序编排（延迟 / 序列 / 循环）、尘埃与表现环、绕过
/// <c>Projectile</c> 的弹幕池。这几样各自单独决定，见蓝图 §7.5。
/// </para>
/// <para>
/// 原版的活动弹幕上限是 <c>Main.maxProjectiles</c>（1000，只读）。满员时新生成的弹幕会顶掉
/// <c>timeLeft</c> 最小的一颗，置 <c>Projectile.netImportant</c> 可以免于被顶掉。
/// </para>
/// </summary>
public static class MonoShot
{
    /// <summary>
    /// 以 <paramref name="center"/> 为心均匀铺满整圈。第 0 颗取 <paramref name="rotation"/>，
    /// 其余按 2π / 数量 递推。
    /// </summary>
    /// <param name="source">生成来源，一般传 <c>npc.GetSource_FromThis()</c>。</param>
    /// <param name="center">圆心，世界坐标。</param>
    /// <param name="count">数量。为 0 或负数时不生成。</param>
    /// <param name="type">弹幕类型。</param>
    /// <param name="damage">伤害。需要按世界难度换算时用 <see cref="ScaledDamage"/>。</param>
    /// <param name="speed">初速大小。</param>
    /// <param name="rotation">第 0 颗的角度（弧度）。</param>
    /// <param name="knockBack">击退。</param>
    /// <param name="owner">弹幕归属的玩家编号。留 <c>-1</c> 由 <c>NewProjectile</c> 取本地玩家。</param>
    /// <param name="ai0">写进 <c>Projectile.ai[0]</c> 的值。</param>
    /// <param name="ai1">写进 <c>Projectile.ai[1]</c> 的值。</param>
    /// <param name="ai2">写进 <c>Projectile.ai[2]</c> 的值。</param>
    /// <param name="gated">为 <c>true</c> 时本函数自己判权威端；调用方已经判过门就传 <c>false</c>。</param>
    /// <returns>实际生成的弹幕数量，被门挡住时为 0。</returns>
    public static int Ring(IEntitySource source, Vector2 center, int count, int type, int damage, float speed,
        float rotation = 0f, float knockBack = 0f, int owner = -1,
        float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, bool gated = true)
    {
        if (!PassGate(gated) || count <= 0)
            return 0;

        for (int i = 0; i < count; i++)
        {
            float angle = rotation + MathHelper.TwoPi * i / count;
            Projectile.NewProjectile(source, center, Vector2.UnitX.RotatedBy(angle) * speed, type, damage, knockBack, owner, ai0, ai1, ai2);
        }

        return count;
    }

    /// <summary>来源为 NPC 的 <c>Ring</c> 便捷重载，内部取 <c>npc.GetSource_FromThis()</c>。</summary>
    public static int Ring(NPC npc, Vector2 center, int count, int type, int damage, float speed,
        float rotation = 0f, float knockBack = 0f, int owner = -1,
        float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, bool gated = true)
        => Ring(npc.GetSource_FromThis(), center, count, type, damage, speed, rotation, knockBack, owner, ai0, ai1, ai2, gated);

    /// <summary>
    /// 从 <paramref name="from"/> 到 <paramref name="to"/> 均匀铺开一段弧，两端都含在内。
    /// </summary>
    /// <param name="source">生成来源，一般传 <c>npc.GetSource_FromThis()</c>。</param>
    /// <param name="center">发射点，世界坐标。</param>
    /// <param name="count">数量。为 1 时全部落在 <paramref name="from"/>。</param>
    /// <param name="type">弹幕类型。</param>
    /// <param name="damage">伤害。需要按世界难度换算时用 <see cref="ScaledDamage"/>。</param>
    /// <param name="speed">初速大小。</param>
    /// <param name="from">起始角度（弧度）。</param>
    /// <param name="to">结束角度（弧度）。</param>
    /// <param name="knockBack">击退。</param>
    /// <param name="owner">弹幕归属的玩家编号。留 <c>-1</c> 由 <c>NewProjectile</c> 取本地玩家。</param>
    /// <param name="ai0">写进 <c>Projectile.ai[0]</c> 的值。</param>
    /// <param name="ai1">写进 <c>Projectile.ai[1]</c> 的值。</param>
    /// <param name="ai2">写进 <c>Projectile.ai[2]</c> 的值。</param>
    /// <param name="gated">为 <c>true</c> 时本函数自己判权威端；调用方已经判过门就传 <c>false</c>。</param>
    /// <returns>实际生成的弹幕数量，被门挡住时为 0。</returns>
    public static int Arc(IEntitySource source, Vector2 center, int count, int type, int damage, float speed,
        float from, float to, float knockBack = 0f, int owner = -1,
        float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, bool gated = true)
    {
        if (!PassGate(gated) || count <= 0)
            return 0;

        float step = count > 1 ? (to - from) / (count - 1) : 0f;
        for (int i = 0; i < count; i++)
        {
            float angle = from + step * i;
            Projectile.NewProjectile(source, center, Vector2.UnitX.RotatedBy(angle) * speed, type, damage, knockBack, owner, ai0, ai1, ai2);
        }

        return count;
    }

    /// <summary>来源为 NPC 的 <c>Arc</c> 便捷重载，内部取 <c>npc.GetSource_FromThis()</c>。</summary>
    public static int Arc(NPC npc, Vector2 center, int count, int type, int damage, float speed,
        float from, float to, float knockBack = 0f, int owner = -1,
        float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, bool gated = true)
        => Arc(npc.GetSource_FromThis(), center, count, type, damage, speed, from, to, knockBack, owner, ai0, ai1, ai2, gated);

    /// <summary>
    /// 以 <paramref name="direction"/> 为中心随机散布，每颗的角度偏移独立取自
    /// <c>[-spread, spread]</c>。速度大小沿用 <paramref name="direction"/> 的长度。
    /// </summary>
    /// <param name="source">生成来源，一般传 <c>npc.GetSource_FromThis()</c>。</param>
    /// <param name="center">发射点，世界坐标。</param>
    /// <param name="count">数量。为 0 或负数时不生成。</param>
    /// <param name="type">弹幕类型。</param>
    /// <param name="damage">伤害。需要按世界难度换算时用 <see cref="ScaledDamage"/>。</param>
    /// <param name="direction">中心方向，长度即初速。</param>
    /// <param name="spread">单侧最大偏角（弧度）。</param>
    /// <param name="knockBack">击退。</param>
    /// <param name="owner">弹幕归属的玩家编号。留 <c>-1</c> 由 <c>NewProjectile</c> 取本地玩家。</param>
    /// <param name="ai0">写进 <c>Projectile.ai[0]</c> 的值。</param>
    /// <param name="ai1">写进 <c>Projectile.ai[1]</c> 的值。</param>
    /// <param name="ai2">写进 <c>Projectile.ai[2]</c> 的值。</param>
    /// <param name="gated">为 <c>true</c> 时本函数自己判权威端；调用方已经判过门就传 <c>false</c>。</param>
    /// <returns>实际生成的弹幕数量，被门挡住时为 0。</returns>
    public static int Spread(IEntitySource source, Vector2 center, int count, int type, int damage, Vector2 direction,
        float spread, float knockBack = 0f, int owner = -1,
        float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, bool gated = true)
    {
        if (!PassGate(gated) || count <= 0)
            return 0;

        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = direction.RotatedBy(Main.rand.NextFloat(-spread, spread));
            Projectile.NewProjectile(source, center, velocity, type, damage, knockBack, owner, ai0, ai1, ai2);
        }

        return count;
    }

    /// <summary>来源为 NPC 的 <c>Spread</c> 便捷重载，内部取 <c>npc.GetSource_FromThis()</c>。</summary>
    public static int Spread(NPC npc, Vector2 center, int count, int type, int damage, Vector2 direction,
        float spread, float knockBack = 0f, int owner = -1,
        float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, bool gated = true)
        => Spread(npc.GetSource_FromThis(), center, count, type, damage, direction, spread, knockBack, owner, ai0, ai1, ai2, gated);

    /// <summary>
    /// 当前世界给敌对弹幕的伤害倍率。旅途模式取难度滑杆，其余取 <c>EnemyDamageMultiplier</c>。
    /// </summary>
    public static float ProjWorldDamage => Main.GameModeInfo.IsJourneyMode
        ? CreativePowerManager.Instance.GetPower<CreativePowers.DifficultySliderPower>().StrengthMultiplierToGiveNPCs
        : Main.GameModeInfo.EnemyDamageMultiplier;

    /// <summary>
    /// 把 NPC 的接触伤害换算成弹幕伤害。世界难度越高，同一份接触伤害换出的弹幕伤害越低。
    /// </summary>
    /// <param name="npcDamage">来源 NPC 的伤害。</param>
    /// <param name="modifier">在这套换算之上再乘的系数，用来区分强弹与弱弹。</param>
    /// <param name="npcDamageCalculationsOffset">换算下限。世界倍率低于它时按它算，防止低难度下弹幕伤害过高。</param>
    public static int ScaledDamage(int npcDamage, float modifier = 1f, int npcDamageCalculationsOffset = 2)
    {
        const float inherentHostileProjMultiplier = 2f;
        float worldDamage = ProjWorldDamage;
        return (int)(modifier * npcDamage / inherentHostileProjMultiplier / Math.Max(npcDamageCalculationsOffset, worldDamage));
    }

    private static bool PassGate(bool gated) => !gated || Main.netMode != NetmodeID.MultiplayerClient;
}
