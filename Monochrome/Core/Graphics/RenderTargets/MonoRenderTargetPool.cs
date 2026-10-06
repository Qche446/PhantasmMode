namespace Monochrome.Core.Graphics.RenderTargets;

/// <summary>
/// 按 (宽, 高, 表面格式, 深度格式) 分组的渲染目标池。借出 <see cref="Lease"/>，<c>Dispose</c> 即归还。
/// <para>
/// <b>为什么要池化：</b>每次 <c>new RenderTarget2D</c> 都会真的分配一块显存并重建后备缓冲，
/// 在后处理链这种"每帧都要两块中间目标"的场景里是纯粹的浪费。蓝图 §11.3 明确禁止随手 new。
/// </para>
/// <para>
/// <b>与 <see cref="MonoManagedRenderTarget"/> 的分工：</b>池适合"帧内短租"（用完立刻还），
/// 受管目标适合"跨帧长期持有 + 跟随分辨率"。两者不要混用同一块目标。
/// </para>
/// </summary>
public sealed class MonoRenderTargetPool : ModSystem
{
    private static readonly Dictionary<PoolKey, Stack<RenderTarget2D>> pool = [];

    /// <summary>
    /// 从池中借一个渲染目标；<c>Dispose</c> 即归还。
    /// </summary>
    /// <param name="width">宽度，最小按 1 处理。</param>
    /// <param name="height">高度，最小按 1 处理。</param>
    /// <param name="format">表面格式，默认 <see cref="SurfaceFormat.Color"/>。</param>
    /// <param name="depth">深度格式，默认不带深度。</param>
    /// <returns>
    /// 租约；<b>专用服务器上没有图形设备，返回 <see langword="null"/></b>。
    /// 调用方必须判空——<c>using</c> 对 null 是安全的，但 <c>lease.Target</c> 不是。
    /// </returns>
    public static Lease? Rent(int width, int height, SurfaceFormat format = SurfaceFormat.Color, DepthFormat depth = DepthFormat.None)
    {
        if (Main.dedServ)
            return null;

        width = Math.Max(1, width);
        height = Math.Max(1, height);
        PoolKey key = new(width, height, format, depth);
        if (!pool.TryGetValue(key, out Stack<RenderTarget2D>? targets))
            pool[key] = targets = new();

        RenderTarget2D? target = null;
        while (targets.Count > 0 && target is null)
        {
            RenderTarget2D candidate = targets.Pop();
            if (!candidate.IsDisposed)
                target = candidate;
            else
                candidate.Dispose();
        }
        target ??= new RenderTarget2D(Main.instance.GraphicsDevice, width, height, false, format, depth);
        return new Lease(key, target);
    }

    /// <inheritdoc/>
    public override void Unload()
    {
        foreach (Stack<RenderTarget2D> targets in pool.Values)
            while (targets.Count > 0)
                targets.Pop().Dispose();
        pool.Clear();
    }

    /// <summary>一次借用。释放即把渲染目标推回对应分组。</summary>
    public sealed class Lease : IDisposable
    {
        private readonly PoolKey key;

        /// <summary>借出的渲染目标。只有在 <see cref="Rent"/> 返回 null 时才不存在，因此这里非 null。</summary>
        public RenderTarget2D Target { get; }

        private bool returned;

        internal Lease(PoolKey key, RenderTarget2D target)
        {
            this.key = key;
            Target = target;
        }

        /// <summary>归还渲染目标。重复调用、目标已释放、池已清空都安全地什么都不做。</summary>
        public void Dispose()
        {
            if (returned || Target.IsDisposed || !pool.TryGetValue(key, out Stack<RenderTarget2D>? targets))
                return;
            returned = true;
            targets.Push(Target);
        }
    }

    /// <summary>池分组键：尺寸与格式完全一致才复用。</summary>
    internal readonly record struct PoolKey(int Width, int Height, SurfaceFormat Format, DepthFormat Depth);
}
