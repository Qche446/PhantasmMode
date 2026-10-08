namespace Monochrome.Content.Worms;

/// <summary>
/// 头部移动的算法，输入输出只有 <see cref="Vector2"/> 与标量。
/// <para>
/// 五个走法：<see cref="Chase"/> 常规追击、<see cref="Steer"/> 带最大转向率的追踪、<see cref="Orbit"/> 环绕、
/// <see cref="Hover"/> 驻留、<see cref="Facing"/> 头部朝向。库只算速度与朝向，写回
/// <c>npc.velocity</c> / <c>npc.rotation</c> 由调用方完成。
/// </para>
/// <para>
/// 退化输入（目标与当前位置重合、半径或速率为零、速度为零）都给出确定结果，不产生 NaN。
/// </para>
/// </summary>
public static class MonoWormMove
{
    /// <summary><see cref="Chase"/> 里反向加速的倍率。</summary>
    public const float ReverseBoost = 2f;

    /// <summary>
    /// 常规追击：逐轴朝目标点加速，速度封顶在 <paramref name="maxSpeed"/>，某根轴与目标反向时加速度放大 <see cref="ReverseBoost"/> 倍。
    /// <para>
    /// 对应原版与毁灭者那种「朝玩家慢慢提速」的手感，不关心当前朝向。
    /// </para>
    /// </summary>
    /// <param name="position">当前位置。</param>
    /// <param name="velocity">当前速度。</param>
    /// <param name="target">要追的目标点。</param>
    /// <param name="maxSpeed">速度上限。</param>
    /// <param name="accel">每 tick 的加速度。</param>
    /// <param name="result">算出的速度。</param>
    public static void Chase(Vector2 position, Vector2 velocity, Vector2 target, float maxSpeed, float accel,
        out Vector2 result)
    {
        Vector2 desired = MonoUtil.SafeNormalize(target - position, Vector2.Zero) * maxSpeed;
        result = new Vector2(
            Approach(velocity.X, desired.X, accel),
            Approach(velocity.Y, desired.Y, accel));
    }

    /// <summary>
    /// 带最大转向率的追踪：速度方向每 tick 最多转 <paramref name="turnRate"/> 弧度，速率朝 <paramref name="maxSpeed"/> 插值。
    /// <para>
    /// 对应哈迪斯那种「先摆头再加速」的手感：转弯能力有上限，速率是渐进的。
    /// </para>
    /// </summary>
    /// <param name="position">当前位置。</param>
    /// <param name="velocity">当前速度。</param>
    /// <param name="target">要追的目标点。</param>
    /// <param name="maxSpeed">目标速率。</param>
    /// <param name="turnRate">每 tick 允许转过的最大弧度。</param>
    /// <param name="speedLerp">速率朝 <paramref name="maxSpeed"/> 靠拢的比例。</param>
    /// <param name="result">算出的速度。</param>
    public static void Steer(Vector2 position, Vector2 velocity, Vector2 target, float maxSpeed, float turnRate,
        float speedLerp, out Vector2 result)
    {
        float current = MathF.Atan2(velocity.Y, velocity.X);

        Vector2 offset = target - position;
        float ideal = offset.LengthSquared() <= MonoUtil.EpsilonSqr ? current : MathF.Atan2(offset.Y, offset.X);

        float turn = MathHelper.WrapAngle(ideal - current);
        float angle = current + MathHelper.Clamp(turn, -turnRate, turnRate);
        float speed = MathHelper.Lerp(velocity.Length(), maxSpeed, MathHelper.Clamp(speedLerp, 0f, 1f));

        result = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
    }

    /// <summary>
    /// 环绕：保持速率，每 tick 把速度方向朝 <paramref name="direction"/> 一侧转 <c>speed / radius</c> 弧度。
    /// <para>
    /// 速度方向自己会稳定在半径 <paramref name="radius"/> 的圆上，因此不需要圆心与当前位置。
    /// </para>
    /// </summary>
    /// <param name="velocity">当前速度，用来定初始方向。</param>
    /// <param name="speed">环绕速率。</param>
    /// <param name="radius">环绕半径。</param>
    /// <param name="direction">旋转方向，正数逆时针、负数顺时针。</param>
    /// <param name="result">算出的速度。</param>
    public static void Orbit(Vector2 velocity, float speed, float radius, int direction, out Vector2 result)
    {
        Vector2 heading = MonoUtil.SafeNormalize(velocity, Vector2.UnitX) * speed;

        if (radius <= MonoUtil.Epsilon)
        {
            result = heading;
            return;
        }

        int sign = direction >= 0 ? 1 : -1;
        result = heading + MonoUtil.Perpendicular(heading) * sign * (speed / radius);
    }

    /// <summary>
    /// 驻留：朝停靠点插值过去，越接近越慢。
    /// <para>
    /// 停在某个点附近、保持朝向、等下一个动作时用它。
    /// </para>
    /// </summary>
    /// <param name="position">当前位置。</param>
    /// <param name="velocity">当前速度。</param>
    /// <param name="destination">停靠点。</param>
    /// <param name="speed">靠拢时的速率。</param>
    /// <param name="lerp">每 tick 的插值比例。</param>
    /// <param name="result">算出的速度。</param>
    public static void Hover(Vector2 position, Vector2 velocity, Vector2 destination, float speed, float lerp,
        out Vector2 result)
    {
        Vector2 desired = MonoUtil.SafeNormalize(destination - position, Vector2.Zero) * speed;
        result = Vector2.Lerp(velocity, desired, MathHelper.Clamp(lerp, 0f, 1f));
    }

    /// <summary>
    /// 头部朝向：速度方向再转 90 度。原版与两份消费者的贴图都是朝上的，写 <c>npc.rotation</c> 时用它。
    /// </summary>
    /// <param name="velocity">当前速度。</param>
    public static float Facing(Vector2 velocity) => MathF.Atan2(velocity.Y, velocity.X) + MathHelper.PiOver2;

    /// <summary>朝目标值靠过去：正常时每 tick 走一个加速度，反向时放大，已经超速则直接压回目标值。</summary>
    private static float Approach(float current, float target, float accel)
    {
        if (current != 0f && target != 0f
            && MathF.Sign(current) == MathF.Sign(target) && MathF.Abs(current) > MathF.Abs(target))
        {
            return target;
        }

        float step = accel;
        if (current != 0f && target != 0f && MathF.Sign(current) != MathF.Sign(target))
            step *= ReverseBoost;

        return current < target ? MathF.Min(current + step, target) : MathF.Max(current - step, target);
    }
}
