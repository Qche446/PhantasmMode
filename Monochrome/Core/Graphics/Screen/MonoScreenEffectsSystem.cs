namespace Monochrome.Core.Graphics.Screen;

/// <summary>
/// 相机安全的屏幕震动，以及真正生效的世界时间缩放与命中定格。全部只在客户端生效，且是叠加式的。
/// <para>
/// 震动用 trauma 模型：振幅是强度的平方（参考 Celeste），多个来源可以在同一帧叠加而不互相压制。
/// </para>
/// <para>
/// 时间缩放走原版现成的闸门——<c>Main.cs:16938</c> 的
/// <c>if (ShouldUpdateEntities()) { DoUpdateInWorld(...); }</c>，本类 detour 它，在"这一帧世界不该推进"
/// 时返回 false。于是 NPC / 玩家 / 弹幕 / 图格 / 时间 / 粒子都不更新，而输入（<c>16820</c>）、绘制、
/// 界面动画照常，因为它们都在闸门之外。
/// </para>
/// <para>
/// 两条硬约束：定格计数不能放在 <c>PostUpdateEverything</c>（<c>17660</c>）里递减，那个钩子在
/// <c>DoUpdateInWorld</c> 内部，世界一停就永不执行，计数永不归零等于永久冻结，所以走
/// <c>PreUpdateEntities</c>（<c>16908</c>）；闸门 hook 里只读一个预先算好的字段，不在里面改状态，
/// 因为那个方法可能被别处调用。
/// </para>
/// </summary>
public sealed class MonoScreenEffectsSystem : ModSystem
{
    private sealed class Shake
    {
        public float Trauma;
        public float Decay;
        public float Radius;
        public Vector2 Center;
        public bool HasCenter;
        public int Seed;
    }

    private static readonly List<Shake> shakes = [];
    private static int hitstopFrames;
    private static bool skipWorldUpdate;
    private static float skipAccumulator;

    /// <summary>是否处于命中定格中。</summary>
    public static bool IsHitstopActive => hitstopFrames > 0;

    /// <summary>本帧是否跳过了世界更新（诊断用）。</summary>
    public static bool IsWorldUpdateSkipped => skipWorldUpdate;

    /// <summary>
    /// 世界时间倍率，<b>会真正生效</b>：<c>1</c> = 正常、<c>0.5</c> = 半速、<c>0</c> = 完全停住。
    /// <para>
    /// 实现方式是"按比例跳过世界更新帧"，因为原版更新是逐帧的、没有 dt 可以缩放。
    /// 所以非整数倍率长时间运行会有轻微顿挫（例如 0.7 是"每 10 帧跳 3 帧"）。
    /// </para>
    /// </summary>
    public static float WorldTimeScale { get; set; } = 1f;

    /// <summary>
    /// 触发一次屏幕震动。
    /// </summary>
    /// <param name="strength">
    /// 初始 trauma，<c>[0,1]</c>；实际振幅是它的<b>平方</b>乘以 18 像素。
    /// </param>
    /// <param name="decay">每帧衰减量，最小按 0.001 处理。</param>
    /// <param name="center">衰减参考中心（世界坐标）；配合 <paramref name="radius"/> 做距离衰减用。</param>
    /// <param name="radius">距离衰减半径；为 0 表示全场等效、不做距离衰减。</param>
    public static void StartShake(float strength, float decay = 0.08f, Vector2? center = null, float radius = 0f)
    {
        if (Main.dedServ || strength <= 0f)
            return;
        shakes.Add(new Shake { Trauma = MathHelper.Clamp(strength, 0f, 1f), Decay = Math.Max(0.001f, decay), Center = center ?? Vector2.Zero, HasCenter = center.HasValue, Radius = radius, Seed = Main.rand.Next() });
    }

    /// <summary>
    /// 请求命中定格若干帧：这段时间内<b>世界完全不推进</b>，而输入、绘制、界面照常。
    /// <para>多次请求取<b>较大者</b>，不会累加（避免连击把定格堆到几秒）。</para>
    /// </summary>
    /// <param name="frames">帧数。</param>
    public static void StartHitstop(int frames) => hitstopFrames = Math.Max(hitstopFrames, Math.Max(0, frames));

    /// <summary>
    /// 每帧在实体更新之前决定"这一帧世界要不要推进"，并递减定格计数。
    /// <b>必须在世界更新之外做</b>——见类型说明的纪律 1。
    /// </summary>
    public override void PreUpdateEntities()
    {
        if (Main.dedServ)
            return;

        if (hitstopFrames > 0)
        {
            hitstopFrames--;
            skipWorldUpdate = true;
            skipAccumulator = 0f;
            return;
        }

        float scale = MathHelper.Clamp(WorldTimeScale, 0f, 1f);
        if (scale >= 1f)
        {
            skipWorldUpdate = false;
            skipAccumulator = 0f;
            return;
        }

        // 按比例跳帧：平均下来世界的推进速度就是 scale（0.5 → 每两帧跑一帧）。
        skipAccumulator += 1f - scale;
        if (skipAccumulator >= 1f)
        {
            skipAccumulator -= 1f;
            skipWorldUpdate = true;
        }
        else
        {
            skipWorldUpdate = false;
        }
    }

    /// <summary>
    /// 世界更新闸门的 detour。只读 <see cref="skipWorldUpdate"/>，
    /// 于是"跳过世界更新"仍然尊重原版自己的判断（例如世界还没准备好时本来就该返回 false）。
    /// </summary>
    private static bool ShouldUpdateEntitiesHook(On_Main.orig_ShouldUpdateEntities orig, Main self)
        => !skipWorldUpdate && orig(self);

    /// <summary>把震动位移加到相机上，并推进 trauma 衰减与回收。</summary>
    public override void ModifyScreenPosition()
    {
        if (Main.dedServ)
            return;
        for (int i = shakes.Count - 1; i >= 0; i--)
        {
            Shake shake = shakes[i];
            float trauma = shake.Trauma;
            if (shake.HasCenter && shake.Radius > 0f)
            {
                float distance = Vector2.Distance(Main.LocalPlayer.Center, shake.Center);
                trauma *= MathHelper.Clamp(1f - distance / shake.Radius, 0f, 1f);
            }
            float amplitude = trauma * trauma * 18f;
            float time = Main.GameUpdateCount * 0.37f + shake.Seed;
            Main.screenPosition += new Vector2(MathF.Sin(time * 1.71f), MathF.Cos(time * 1.23f)) * amplitude;
            shake.Trauma = Math.Max(0f, shake.Trauma - shake.Decay);
            if (shake.Trauma <= 0f)
                shakes.RemoveAt(i);
        }
    }

    /// <summary>挂上世界更新闸门（客户端）。</summary>
    public override void Load()
    {
        if (!Main.dedServ)
            On_Main.ShouldUpdateEntities += ShouldUpdateEntitiesHook;
    }

    /// <summary>摘掉闸门并清空状态。</summary>
    public override void Unload()
    {
        if (!Main.dedServ)
            On_Main.ShouldUpdateEntities -= ShouldUpdateEntitiesHook;
        shakes.Clear();
        hitstopFrames = 0;
        skipWorldUpdate = false;
        skipAccumulator = 0f;
        WorldTimeScale = 1f;
    }

    /// <summary>世界卸载时清空震动、定格与时间倍率。</summary>
    public override void OnWorldUnload()
    {
        shakes.Clear();
        hitstopFrames = 0;
        skipWorldUpdate = false;
        skipAccumulator = 0f;
        WorldTimeScale = 1f;
    }
}
