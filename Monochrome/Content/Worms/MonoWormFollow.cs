namespace Monochrome.Content.Worms;

/// <summary>
/// 体节跟随的算法，输入输出只有 <see cref="Vector2"/> 与标量。
/// <para>
/// 调用方在体节的 AI 里算出位置与朝向，再写回 <c>npc.Center</c> 与 <c>npc.rotation</c>；
/// 库不接管这一步，也不参与原版跟随物理。
/// </para>
/// <para>
/// 退化输入（两段重合、零间距、方向未知）都返回一个确定的结果，不产生 NaN。
/// </para>
/// </summary>
public static class MonoWormFollow
{
    /// <summary><see cref="Snake"/> 里蛇形偏移的默认缩放系数。</summary>
    public const float SnakeBearingScale = 0.055f;

    /// <summary>
    /// 定距跟随：把体节摆在"前一段"后方 <paramref name="spacing"/> 处，朝向沿用前一段。
    /// <para>
    /// 适合紧挨着头的第一节，也适合不想要弯曲的直链。
    /// </para>
    /// </summary>
    /// <param name="aheadCenter">前一段的中心。</param>
    /// <param name="aheadRotation">前一段的朝向。</param>
    /// <param name="spacing">两段之间的距离。</param>
    /// <param name="aheadVelocity">前一段的速度，作为位置补偿加进去；不需要时传 <see cref="Vector2.Zero"/>。</param>
    /// <param name="center">算出的中心。</param>
    /// <param name="rotation">算出的朝向。</param>
    public static void FixedSpacing(Vector2 aheadCenter, float aheadRotation, float spacing, Vector2 aheadVelocity,
        out Vector2 center, out float rotation)
    {
        rotation = aheadRotation;

        // 只用到 FNA 的三角函数：这两个原语在离线验收台里也要能跑，不能依赖 Terraria 的 Vector2 扩展。
        float backAngle = rotation - MathHelper.PiOver2;
        Vector2 back = new(MathF.Cos(backAngle), MathF.Sin(backAngle));
        center = aheadCenter - back * spacing + aheadVelocity;
    }

    /// <summary>
    /// 蛇形跟随：与前一段保持固定距离，用再前一段的方向做弯曲，并叠加一个弹簧偏移。
    /// <para>
    /// 弹簧偏移由调用方自己推进（每帧累加速度、再乘衰减），传进来的值只影响这一帧的横向偏移量。
    /// </para>
    /// </summary>
    /// <param name="aheadCenter">前一段的中心。</param>
    /// <param name="ahead2Center">再前一段的中心，用来判断链的走向。</param>
    /// <param name="currentCenter">这一段当前的中心，决定它往哪一侧偏。</param>
    /// <param name="spacing">两段之间的距离。</param>
    /// <param name="strength">弯曲强度，通常由头部按当前动作给出。</param>
    /// <param name="springOffset">横向弹簧偏移，单位与 <paramref name="spacing"/> 相同。</param>
    /// <param name="center">算出的中心。</param>
    /// <param name="rotation">算出的朝向。</param>
    public static void Snake(Vector2 aheadCenter, Vector2 ahead2Center, Vector2 currentCenter,
        float spacing, float strength, float springOffset, out Vector2 center, out float rotation)
    {
        Vector2 forward = MonoUtil.SafeNormalize(aheadCenter - ahead2Center, Vector2.Zero);
        Vector2 toSelf = MonoUtil.SafeNormalize(currentCenter - aheadCenter, Vector2.Zero);

        Vector2 bearing = (aheadCenter - ahead2Center) * (strength * SnakeBearingScale);
        bearing += MonoUtil.Perpendicular(toSelf) * springOffset;

        Vector2 direction = MonoUtil.SafeNormalize(toSelf + bearing, Vector2.Zero);
        if (direction == Vector2.Zero)
        {
            // 这一段正好压在前一段上：沿链的走向往后摆；链走向也未知时给一个固定方向。
            direction = forward == Vector2.Zero ? -Vector2.UnitY : -forward;
        }

        center = aheadCenter + direction * spacing;

        // 朝向取"从这一段指向前一段"的方向；两段重合时退回链的走向。
        Vector2 axis = MonoUtil.SafeNormalize(aheadCenter - center, forward);
        rotation = (axis == Vector2.Zero ? 0f : MathF.Atan2(axis.Y, axis.X)) + MathHelper.PiOver2;
    }
}
