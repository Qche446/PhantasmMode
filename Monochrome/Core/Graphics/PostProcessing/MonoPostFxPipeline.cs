using Monochrome.Core.Graphics.RenderTargets;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// 可复用的 ping-pong 后处理链：一串屏幕空间 pass 加上主目标之间的摆渡。
/// <para>
/// 用<b>两块</b>中间渲染目标来回倒，避免每个 pass 都从池里借新的。
/// </para>
/// <para>
/// <b>它自己不做"捕获"和"回贴"</b>——那是 <c>MonoScreenCapture</c> 的活。
/// 本类只负责"把给定的一块内容过一遍链，结果放到另一块"，因此它的输入输出都是显式参数，
/// 可以作用在任何渲染目标上（自定义 RT、世界捕获目标、UI 目标都行）。
/// </para>
/// <para>
/// pass 按 <see cref="IMonoPostFxPass.Order"/> 升序执行；没有启用中的 pass 时退化为一次整幅拷贝。
/// </para>
/// </summary>
public sealed class MonoPostFxPipeline
{
    private readonly List<IMonoPostFxPass> passes = [];

    /// <summary><see cref="Apply"/> 的暂存表，避免每帧分配一个数组。</summary>
    private readonly List<IMonoPostFxPass> enabledBuffer = [];

    private bool warnedSameTarget;

    /// <summary>当前已加入的 pass（按 Order 升序）。</summary>
    public IReadOnlyList<IMonoPostFxPass> Passes => passes;

    /// <summary>
    /// 这条链现在<b>是否会产生效果</b>：至少有一个 pass 处于启用状态。
    /// <para>
    /// <c>MonoScreenCapture</c> 用它来决定"要不要为这一帧申请屏幕捕获"——
    /// 全是禁用 pass 的链不该让整个游戏多付一次全屏复制与一次渲染目标切换。
    /// </para>
    /// </summary>
    public bool IsActive
    {
        get
        {
            for (int i = 0; i < passes.Count; i++)
            {
                if (passes[i].Enabled)
                    return true;
            }
            return false;
        }
    }

    /// <summary>加入一个 pass 并重新排序。重复加入同一个实例会被忽略。</summary>
    /// <param name="pass">要加入的 pass。</param>
    public MonoPostFxPipeline Add(IMonoPostFxPass pass)
    {
        if (pass is not null && !passes.Contains(pass))
            passes.Add(pass);
        passes.Sort((a, b) => a.Order.CompareTo(b.Order));
        return this;
    }

    /// <summary>移除一个 pass。</summary>
    /// <param name="pass">要移除的 pass。</param>
    /// <returns>确实移除了才返回 true。</returns>
    public bool Remove(IMonoPostFxPass pass) => passes.Remove(pass);

    /// <summary>
    /// 把 <paramref name="source"/> 依次过一遍启用中的 pass，然后把结果写进 <paramref name="destination"/>。
    /// <para>
    /// 专用服务器上静默跳过（没有 <c>GraphicsDevice</c>）。绘制路径统一约定：缺设备就什么都不做，
    /// 而不是抛异常——与 <c>MonoPrimitiveRenderer</c>、粒子系统一致；只有"索取资源"的成员才抛。
    /// </para>
    /// </summary>
    /// <param name="source">输入渲染目标（调用方已画好场景）。</param>
    /// <param name="destination">输出渲染目标。<b>不能和 <paramref name="source"/> 是同一块</b>。</param>
    /// <param name="spriteBatch">用于整幅拷贝的批次。</param>
    public void Apply(RenderTarget2D source, RenderTarget2D destination, SpriteBatch spriteBatch)
    {
        // 必须最先判断：下面每一句都要解引用 Main.instance.GraphicsDevice。
        if (Main.dedServ)
            return;

        // 同一块目标既当输入又当输出时，FNA 会要求同时把它绑成渲染目标又当纹理采样——
        // 那是未定义行为（实测是黑屏或报错），而且**不会**自己报错到调用点上。
        // 这是调用方的编程错误，不是"少画一帧"，所以给一次明确日志而不是静默出画面问题。
        if (ReferenceEquals(source, destination))
        {
            if (!warnedSameTarget)
            {
                warnedSameTarget = true;
                Monochrome.Instance?.Logger.Warn("MonoPostFxPipeline.Apply 的 source 与 destination 是同一块渲染目标，已跳过；请用 MonoScreenCapture.Apply 做原地后处理。");
            }
            return;
        }

        enabledBuffer.Clear();
        for (int i = 0; i < passes.Count; i++)
        {
            if (passes[i].Enabled)
                enabledBuffer.Add(passes[i]);
        }

        // 没有启用中的 pass → 退化为一次整幅拷贝，不借中间目标（见类型说明）。
        if (enabledBuffer.Count == 0)
        {
            CopyTo(source, destination, spriteBatch);
            return;
        }

        using MonoRenderTargetPool.Lease? first = MonoRenderTargetPool.Rent(source.Width, source.Height);
        using MonoRenderTargetPool.Lease? second = MonoRenderTargetPool.Rent(source.Width, source.Height);
        // 上面已挡住 dedServ，而 dedServ 正是 Rent 唯一返回 null 的场景；这里的判空是按可空返回签名兜底，
        // 也保证将来 Rent 的语义变化不会变成 NRE。
        if (first is null || second is null)
            return;

        RenderTarget2D ping = first.Target;
        RenderTarget2D pong = second.Target;
        CopyTo(source, ping, spriteBatch);
        for (int i = 0; i < enabledBuffer.Count; i++)
        {
            enabledBuffer[i].Apply(ping, pong, spriteBatch);
            (ping, pong) = (pong, ping);
        }
        CopyTo(ping, destination, spriteBatch);
    }

    /// <summary>
    /// 整幅拷贝，并恢复调用前的渲染目标绑定。
    /// <para>
    /// <b>调用前必须没有开着的批次</b>（它自己 <c>Begin</c>/<c>End</c>），
    /// 且 <paramref name="destination"/> 与 <paramref name="source"/> 不能是同一块。
    /// </para>
    /// <para>
    /// 采样用 <see cref="SamplerState.LinearClamp"/> 并与原版
    /// <c>FilterManager.EndCapture</c> 的写法一致；尺寸相同时是逐像素精确的。
    /// </para>
    /// </summary>
    /// <param name="source">来源。</param>
    /// <param name="destination">目标。</param>
    /// <param name="spriteBatch">用于绘制的批次。</param>
    internal static void CopyTo(Texture2D source, RenderTarget2D destination, SpriteBatch spriteBatch)
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;
        RenderTargetBinding[] oldTargets = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(destination);
            device.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            spriteBatch.Draw(source, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
        finally
        {
            device.SetRenderTargets(oldTargets);
        }
    }
}
