namespace Monochrome.Core.Graphics.RenderTargets;

/// <summary>
/// <see cref="MonoManagedRenderTarget"/> 的兼容包装：构造签名收 <see cref="GraphicsDevice"/>，
/// 可以隐式转换回 <see cref="RenderTarget2D"/>。
/// <para>
/// 新代码直接用 <see cref="MonoManagedRenderTarget"/> 或 <see cref="MonoRt"/> 即可；
/// 这个类型保留是为了兼容按 Luminance 的 <c>ManagedRenderTarget(GraphicsDevice, ...)</c>
/// 形状写下来的调用点。
/// </para>
/// </summary>
public sealed class MonoRenderTarget : IDisposable
{
    private readonly MonoManagedRenderTarget managed;

    /// <summary>
    /// 底层渲染目标当前是否已释放。
    /// <b>不是终态</b>：访问 <see cref="Target"/> 会按需重建并把它重新置为 <see langword="false"/>。
    /// </summary>
    public bool IsDisposed => managed.IsDisposed;

    /// <summary>是否已被 <see cref="Retire"/> 永久注销（终态）。</summary>
    public bool IsRetired => managed.IsRetired;

    /// <summary>
    /// 当前宿主是否具备图形设备（即 <c>!Main.dedServ</c>）。
    /// 为 <see langword="false"/> 时全部图形路径都会抛 <see cref="InvalidOperationException"/>，调用方应当提前分支。
    /// </summary>
    public static bool IsAvailable => MonoManagedRenderTarget.IsAvailable;

    /// <summary>屏幕分辨率变化时是否自动重建。</summary>
    public bool ResetOnResize => managed.ResetOnResize;

    /// <summary>底层渲染目标是否尚不存在（未创建或已释放）。</summary>
    public bool IsUninitialized => managed.IsUninitialized;

    /// <summary>距最后一次使用的帧数。</summary>
    public int TimeSinceLastUsage => managed.TimeSinceLastUsage;

    /// <summary>底层渲染目标，必要时惰性创建。</summary>
    public RenderTarget2D Target => managed.Target;

    /// <summary>当前宽度。</summary>
    public int Width => managed.Width;

    /// <summary>当前高度。</summary>
    public int Height => managed.Height;

    /// <summary>构造一个受管渲染目标。</summary>
    /// <param name="factory">按图形设备与宽高创建渲染目标。</param>
    /// <param name="resetOnResize">分辨率变化时是否重建。</param>
    /// <param name="subjectToGarbageCollection">长期未使用时是否允许自动释放。</param>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> 为 null。</exception>
    public MonoRenderTarget(Func<GraphicsDevice, int, int, RenderTarget2D> factory, bool resetOnResize = true, bool subjectToGarbageCollection = true)
    {
        if (factory is null)
            throw new ArgumentNullException(nameof(factory));
        managed = new MonoManagedRenderTarget((width, height) => factory(Main.instance.GraphicsDevice, width, height), resetOnResize, subjectToGarbageCollection);
    }

    /// <summary>按新尺寸重建。</summary>
    /// <param name="width">新宽度。</param>
    /// <param name="height">新高度。</param>
    public void Reset(int width, int height) => managed.Recreate(width, height);

    /// <summary>把图形设备的当前渲染目标切换到自己，可选地先清屏。</summary>
    /// <param name="clearColor">清屏颜色；为 null 时不清屏。</param>
    public void SwapToRenderTarget(Color? clearColor = null) => managed.SwapToRenderTarget(clearColor);

    /// <summary>把另一个渲染目标的内容整幅拷进来。</summary>
    /// <param name="source">来源渲染目标。</param>
    public void CopyContentsFrom(RenderTarget2D source) => managed.CopyContentsFrom(source);

    /// <summary>
    /// 只释放底层渲染目标，不注销自己；之后访问 <see cref="Target"/> 会按需重建。
    /// 永久注销请用 <see cref="Retire"/>。重复调用安全。
    /// </summary>
    public void Dispose() => managed.Dispose();

    /// <summary>永久注销：释放显存并把自己从受管登记表中摘掉，此后访问 <see cref="Target"/> 会抛 <see cref="ObjectDisposedException"/>。</summary>
    public void Retire() => managed.Retire();

    /// <summary>隐式转换到底层渲染目标。</summary>
    /// <param name="target">受管渲染目标。</param>
    public static implicit operator RenderTarget2D(MonoRenderTarget target) => target.Target;
}
