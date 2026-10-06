namespace Monochrome.Core.Graphics.Screen;

/// <summary>
/// 相机跟随：平滑跟随 + 死区 + 前瞻。默认关闭，用 <see cref="FollowPlayer"/> 或 <see cref="Follow"/> 开启。
/// <para>
/// <b>它只替换"跟随"这一份，其余原样保留。</b>原版 <c>Main.DoDraw_UpdateCameraPosition</c>
/// （<c>Main.cs:60440</c>，从 <c>59593</c> 调用）先<b>无条件重写</b> <c>screenPosition</c> 为"玩家锚点居中"
/// （<c>60470-60471</c>），之后才依次叠加 Boss 平移（<c>60749</c>）、原版相机修饰器（<c>60763</c>）、
/// 模组钩子（<c>60765</c>，本类在这里）、取整（<c>60766</c>）与世界边界钳制（<c>60768</c>）。
/// 因为第一步是重写而不是累加，本类可以算出
/// <c>extras = screenPosition − 原版跟随基准</c>，它正好等于"平移 + 相机修饰器 + 其它模组加的东西"，
/// 于是只把跟随那一份换掉：<c>screenPosition = 焦点 − 屏幕中心 + extras</c>。平移、相机修饰器、
/// 其它模组与震动的贡献都不会被吃掉，也不会累加漂移。边界钳制与整像素取整同样由原版替你完成
/// （它们排在本类的钩子之后）。
/// </para>
/// <para>
/// <b>漂移守卫</b>：上面的基准公式抄自反编译源码，tML 改了写法时 <c>extras</c> 会变成一个荒谬的值，
/// 这时本类自动退出跟随并记一次日志。<b>速度自适应缩放没有实现</b>，因为
/// <c>PlayerInput.SetZoom_World()</c> 在 <c>Main.cs:60230</c> 才跑，会把 zoom 重新推一遍，
/// 在这里写等于没写。细节见图形文档 §5.10。
/// </para>
/// </summary>
public sealed class MonoCameraFollow : ModSystem
{
    /// <summary>原版跟随基准里"玩家脚底往上抬"的像素数（<c>Main.cs:60448</c> 的 <c>num = 21</c>）。</summary>
    private const float FeetLift = 21f;

    /// <summary>漂移守卫的倍数：extras 超过"三倍屏幕对角线"就认为是公式过期了。</summary>
    private const float DriftLimitScreens = 3f;

    private static Vector2 focus;
    private static Vector2 lookAhead;
    private static Vector2 previousTarget;
    private static bool hasPreviousTarget;
    private static bool followPlayer;
    private static bool needsRecenter;
    private static bool disengaged;

    /// <summary>是否正在跟随。<b>默认 false</b>——不开启时本类一帧都不碰相机。</summary>
    public static bool Following { get; set; }

    /// <summary>要保持在屏幕中心（更准确地说：保持在死区中心）的世界坐标点。点跟随模式下由调用方每帧更新。</summary>
    public static Vector2 Target { get; set; }

    /// <summary>
    /// 死区尺寸（世界单位）：目标停留在这个矩形内时相机<b>完全不动</b>。
    /// <para>世界单位 = 像素（zoom = 1 时）。默认 <c>(80, 56)</c>——比玩家还小一点，于是走动时相机几乎不动，
    /// 只有冲刺/飞行才会跟。想要"跟得更紧"就把两个分量调小（例如 <c>(24, 16)</c>）。</para>
    /// </summary>
    public static Vector2 DeadZone { get; set; } = new(80f, 56f);

    /// <summary>
    /// 每帧追上"超出死区的那部分"的比例，<c>(0,1]</c>。越小越"黏"、相机越滞后。
    /// <para>这是指数收敛：稳定后的滞后量 ≈ <c>超出量 × (1 - Smoothing) / Smoothing</c>。
    /// 默认 0.12（约滞后 7 倍超出量）。</para>
    /// </summary>
    public static float Smoothing { get; set; } = 0.12f;

    /// <summary>前瞻时长（秒）：按目标速度提前把相机推出去。0 = 关闭。默认 0.12。</summary>
    public static float LookAhead { get; set; } = 0.12f;

    /// <summary>前瞻偏移的像素上限，避免高速时把画面推飞。默认 48。</summary>
    public static float MaxLookAhead { get; set; } = 48f;

    /// <summary>前瞻偏移自身的平滑比例，避免速度突变时相机抽一下。默认 0.12。</summary>
    public static float LookAheadSmoothing { get; set; } = 0.12f;

    /// <summary>当前相机焦点（屏幕中心对应的世界点），诊断用。</summary>
    public static Vector2 Focus => focus;

    /// <summary>当前前瞻偏移，诊断用。</summary>
    public static Vector2 LookAheadOffset => lookAhead;

    /// <summary>是否已经因为漂移守卫退出跟随（诊断用）。</summary>
    public static bool Disengaged => disengaged;

    /// <summary>
    /// 开始跟随玩家。焦点<b>瞬移</b>到玩家当前的锚点，因此开启的那一帧画面不会跳。
    /// </summary>
    public static void FollowPlayer()
    {
        followPlayer = true;
        Following = true;
        Enter();
    }

    /// <summary>
    /// 开始跟随一个世界坐标点。
    /// <para><b>首帧会瞬移过去</b>（焦点直接设成它）。这是刻意的：否则相机会从上一个世界、
    /// 上一个目标的位置一路飞过来。想要平滑的"移过去"，请先 <see cref="FollowPlayer"/> 再用
    /// <see cref="Target"/> 每帧推——死区与平滑会自然把它带过去。</para>
    /// </summary>
    /// <param name="worldPoint">要保持在屏幕中心的点。</param>
    public static void Follow(Vector2 worldPoint)
    {
        followPlayer = false;
        Target = worldPoint;
        Following = true;
        Enter();
    }

    /// <summary>停止跟随。相机立刻回到原版行为（原版下一帧就会重写 screenPosition）。</summary>
    public static void Stop()
    {
        Following = false;
        followPlayer = false;
        lookAhead = Vector2.Zero;
        hasPreviousTarget = false;
        needsRecenter = false;
    }

    /// <summary>
    /// 每帧把焦点往"目标 + 前瞻"收敛，然后<b>只替换原版跟随基准</b>那部分。
    /// </summary>
    public override void ModifyScreenPosition()
    {
        if (Main.dedServ || !Following || disengaged || Main.gameMenu)
            return;

        Player player = Main.LocalPlayer;
        if (player is null || !player.active)
            return;

        Vector2 target = followPlayer ? PlayerAnchor(player) : Target;

        if (needsRecenter)
        {
            // 首帧：直接把焦点放上去，并让前瞻从零开始。这一帧算出来的 screenPosition 就等于原版值。
            focus = target;
            lookAhead = Vector2.Zero;
            previousTarget = target;
            hasPreviousTarget = true;
            needsRecenter = false;
        }
        else if (hasPreviousTarget)
        {
            // 前瞻：目标的每帧位移 × 60 当作"每秒速度"，再乘前瞻时长。
            // 用位移而不是 player.velocity，是为了让"跟随任意世界坐标点"也自动有前瞻。
            Vector2 wanted = (target - previousTarget) * 60f * LookAhead;
            float limit = Math.Max(0f, MaxLookAhead);
            if (wanted.LengthSquared() > limit * limit && wanted != Vector2.Zero)
                wanted = Vector2.Normalize(wanted) * limit;
            lookAhead = Vector2.Lerp(lookAhead, wanted, MathHelper.Clamp(LookAheadSmoothing, 0.01f, 1f));
            previousTarget = target;
        }

        // 死区：只在"目标 + 前瞻"跑出以焦点为中心的矩形时才动，而且只动超出部分的一个比例。
        Vector2 desired = target + lookAhead;
        Vector2 delta = desired - focus;
        Vector2 half = Vector2.Max(DeadZone, Vector2.Zero) * 0.5f;
        Vector2 overflow = Vector2.Zero;
        if (Math.Abs(delta.X) > half.X)
            overflow.X = delta.X - Math.Sign(delta.X) * half.X;
        if (Math.Abs(delta.Y) > half.Y)
            overflow.Y = delta.Y - Math.Sign(delta.Y) * half.Y;
        if (overflow != Vector2.Zero)
            focus += overflow * MathHelper.Clamp(Smoothing, 0.001f, 1f);

        Vector2 vanillaBase = VanillaFollowBase(player);
        Vector2 extras = Main.screenPosition - vanillaBase;
        if (!float.IsFinite(extras.X) || !float.IsFinite(extras.Y) || extras.LengthSquared() > DriftLimitSquared())
        {
            disengaged = true;
            Monochrome.Instance?.Logger.Warn(
                "MonoCameraFollow：无法从 screenPosition 里分离出原版跟随基准（偏差 " +
                $"{extras.Length():F0} 像素，超过三倍屏幕对角线），已自动停止跟随。" +
                "这通常意味着 tML 改动了 Main.DoDraw_UpdateCameraPosition 的写法，本类里的公式需要跟着更新。");
            return;
        }

        // 屏幕中心同样按原版的**整数除法**算，免得跟基准差半个像素。
        Main.screenPosition = focus - new Vector2(Main.screenWidth / 2, Main.screenHeight / 2) + extras;
    }

    /// <inheritdoc/>
    public override void Unload() => Stop();

    /// <summary>世界卸载时退出跟随：焦点是跨帧状态，不能带到下一个世界去。</summary>
    public override void OnWorldUnload() => Stop();

    /// <summary>逐字复刻 <c>Main.cs:60470-60471</c> 的跟随基准。</summary>
    /// <param name="player">局部玩家。</param>
    private static Vector2 VanillaFollowBase(Player player)
    {
        // 原版那两行里的 vector2 就是 (1,1)：Main.cs:60442-60443 定义 vector = Vector3.One、
        // vector2 = Vector3.One / vector，两者到 60470 之前都没被改过。
        // screenWidth / 2 是**整数除法**，照抄。
        return new Vector2(
            player.position.X + player.width * 0.5f - Main.screenWidth / 2 + Main.cameraX,
            player.position.Y + player.height - FeetLift - Main.screenHeight / 2 + player.gfxOffY);
    }

    /// <summary>原版会把"玩家的这个点"放在屏幕中心。</summary>
    /// <param name="player">局部玩家。</param>
    private static Vector2 PlayerAnchor(Player player)
        => VanillaFollowBase(player) + new Vector2(Main.screenWidth / 2, Main.screenHeight / 2);

    private static float DriftLimitSquared()
    {
        float diagonal = new Vector2(Main.screenWidth, Main.screenHeight).Length() * DriftLimitScreens;
        return diagonal * diagonal;
    }

    private static void Enter()
    {
        needsRecenter = true;
        disengaged = false;
        hasPreviousTarget = false;
        lookAhead = Vector2.Zero;
    }
}
