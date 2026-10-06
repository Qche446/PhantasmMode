namespace Monochrome.Core.Graphics.RenderTargets;

/// <summary>
/// 在 tModLoader 的绘制循环上统一维护所有 <see cref="MonoManagedRenderTarget"/>：
/// 跟随分辨率重建、偶尔刷新闲置计数、到期回收显存。
/// <para>
/// 它把两件只有在这里才拿得到的信息接了起来：<c>Main.OnPreDraw</c>（每帧一次的时间点）与
/// <c>Main.SetDisplayMode</c>（分辨率真的变了的那一刻）。
/// </para>
/// </summary>
public sealed class MonoRenderTargetManager : ModSystem
{
    private static readonly List<MonoManagedRenderTarget> targets = new();

    /// <summary>本实例是否真的挂上了钩子；用它而不是用 <see cref="Main.dedServ"/> 判断该不该退订。</summary>
    private bool hooked;

    /// <summary>
    /// 每帧在 <c>Main.OnPreDraw</c> 时机触发，<b>触发前后的渲染目标绑定会被自动保存/恢复</b>。
    /// 这是"往受管渲染目标里画东西"的推荐时机。
    /// </summary>
    public static event Action? RenderTargetUpdateLoop;

    /// <summary>当前登记的全部受管渲染目标。</summary>
    public static IReadOnlyList<MonoManagedRenderTarget> ManagedTargets => targets;

    /// <summary>允许自动回收的目标在多少帧未使用后被释放。默认 600 帧（10 秒）。</summary>
    public static int FramesUntilCollection { get; set; } = 60 * 10;

    /// <summary>登记一个受管目标。由 <see cref="MonoManagedRenderTarget"/> 的构造函数调用，重复登记会被忽略。</summary>
    /// <param name="target">要登记的目标。</param>
    internal static void Register(MonoManagedRenderTarget target)
    {
        if (!targets.Contains(target))
            targets.Add(target);
    }

    /// <summary>
    /// 从登记表摘除一个受管目标，此后它不再参与分辨率重建与闲置回收。
    /// 由 <see cref="MonoManagedRenderTarget.Retire"/> 调用。
    /// <para>
    /// <b>不要</b>在 <see cref="MonoManagedRenderTarget.Dispose"/> 里摘除：本类会在闲置回收时主动
    /// <c>Dispose</c> 别人的目标（见 <see cref="HandleTargetUpdateLoop"/>），若摘除就会把"被自动回收"
    /// 变成"永久退管"——目标永远不再随分辨率重建，且没有任何异常或日志。
    /// </para>
    /// </summary>
    /// <param name="target">要摘除的目标。</param>
    internal static void Unregister(MonoManagedRenderTarget target) => targets.Remove(target);

    /// <summary>挂上绘制循环与分辨率变化钩子（仅客户端）。</summary>
    public override void OnModLoad()
    {
        // 专用服务器没有 GraphicsDevice：Main.OnPreDraw 根本不会被触发（它只在 Terraria\Main.cs:59541 的
        // if (!dedServ) 分支里被调用），而 HandleTargetUpdateLoop 第一句就要解引用 GraphicsDevice。
        // 与其挂一个永不触发、一旦触发就 NRE 的钩子，不如根本不挂。
        if (Main.dedServ)
            return;

        Main.OnPreDraw += HandleTargetUpdateLoop;
        On_Main.SetDisplayMode += ResizeScreenSizedTargets;
        hooked = true;
    }

    /// <summary>摘掉钩子、释放全部受管目标并清空登记表。</summary>
    public override void OnModUnload()
    {
        if (hooked)
        {
            Main.OnPreDraw -= HandleTargetUpdateLoop;
            On_Main.SetDisplayMode -= ResizeScreenSizedTargets;
            hooked = false;
        }

        // 卸载不在主线程上：ModLoader.BeginLoad 用 Task.Run 起任务（ModLoader.cs:157），最终由
        // Mod.UnloadContent → SystemLoader.OnModUnload 回调到这里（Mod.cs:536）；而 RenderTarget2D 的释放
        // 会碰 FNA3D，此时主线程正并行绘制"加载模组"进度界面。所以登记表立刻清空（万一有漏网的
        // OnPreDraw 也已无物可遍历），显存释放推到主线程做。
        List<MonoManagedRenderTarget> pending = [.. targets];
        targets.Clear();
        RenderTargetUpdateLoop = null;

        if (Main.dedServ)
        {
            // 服务器上既没有图形设备、也没有主线程动作循环在跑，直接释放（此时通常根本没有目标）。
            foreach (MonoManagedRenderTarget target in pending)
                target.Dispose();
            return;
        }

        Main.QueueMainThreadAction(() =>
        {
            foreach (MonoManagedRenderTarget target in pending)
                target.Dispose();
        });
    }

    private static void HandleTargetUpdateLoop(GameTime gameTime)
    {
        RenderTargetBinding[] oldTargets = Main.instance.GraphicsDevice.GetRenderTargets();
        try
        {
            RenderTargetUpdateLoop?.Invoke();
        }
        finally
        {
            Main.instance.GraphicsDevice.SetRenderTargets(oldTargets);
        }
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            MonoManagedRenderTarget target = targets[i];
            if (target.IsDisposed)
                continue;
            if (!target.SubjectToGarbageCollection || target.IsUninitialized)
                continue;
            target.TimeSinceLastUsage++;
            if (target.TimeSinceLastUsage >= FramesUntilCollection)
                target.Dispose();
        }
    }

    private static void ResizeScreenSizedTargets(On_Main.orig_SetDisplayMode orig, int width, int height, bool fullscreen)
    {
        int oldWidth = Main.screenWidth;
        int oldHeight = Main.screenHeight;
        orig(width, height, fullscreen);
        if (oldWidth == Main.screenWidth && oldHeight == Main.screenHeight)
            return;
        foreach (MonoManagedRenderTarget target in targets)
        {
            if (!target.ResetOnResize || target.WaitingForFirstInitialization || target.IsDisposed)
                continue;
            MonoManagedRenderTarget captured = target;
            Main.QueueMainThreadAction(() => captured.Recreate(Main.screenWidth, Main.screenHeight));
        }
    }
}
