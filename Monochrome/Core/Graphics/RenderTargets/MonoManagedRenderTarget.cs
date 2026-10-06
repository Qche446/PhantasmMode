using System.Diagnostics;

namespace Monochrome.Core.Graphics.RenderTargets;

/// <summary>
/// 受管渲染目标：首次访问时才创建，分辨率变化时自动重建，长期未使用时允许自动释放显存。
/// 构造时会把自己登记到 <see cref="MonoRenderTargetManager"/>，由后者挂在绘制循环上统一维护。
/// <para>
/// <see cref="Target"/> 的 getter 有副作用：它把 <see cref="TimeSinceLastUsage"/> 归零
/// （<see cref="Width"/> / <see cref="Height"/> 也会），所以别把它放进每帧轮询的循环里，
/// 否则空闲回收永远不会触发。
/// </para>
/// <para>
/// <see cref="Dispose"/> 不是终态：它只丢掉显存里那块纹理，之后访问 <see cref="Target"/> 会按需重建。
/// 这是为了让 wrapper 比底层目标活得久——它是唯一持有重建委托的东西。要永久注销请用
/// <see cref="Retire"/>，那才会释放显存并把自己从登记表摘掉。
/// </para>
/// </summary>
[DebuggerDisplay("{Width}x{Height}, Uninitialized={IsUninitialized}, Idle={TimeSinceLastUsage}")]
public sealed class MonoManagedRenderTarget : IDisposable
{
    private readonly Func<int, int, RenderTarget2D> initializer;
    private RenderTarget2D? target;

    /// <summary>是否还没经历过第一次创建（此时 <see cref="ResetOnResize"/> 不会触发重建）。</summary>
    public bool WaitingForFirstInitialization { get; private set; } = true;

    /// <summary>屏幕分辨率变化时是否自动重建。</summary>
    public bool ResetOnResize { get; }

    /// <summary>是否允许在长期未使用后被自动释放以回收显存。</summary> 
    public bool SubjectToGarbageCollection { get; }

    /// <summary>
    /// 底层渲染目标当前是否处于已释放状态。
    /// <para>
    /// 这<b>不是终态</b>：只要还没 <see cref="Retire"/>，访问 <see cref="Target"/> 就会重建并把这里重新置为 <see langword="false"/>。
    /// </para>
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// 是否已被 <see cref="Retire"/> 永久注销（终态）。
    /// 此后 <see cref="Target"/> 与 <see cref="Recreate"/> 都会抛 <see cref="ObjectDisposedException"/>，也不会再参与分辨率重建与闲置回收。
    /// </summary>
    public bool IsRetired { get; private set; }

    /// <summary>
    /// 当前宿主是否具备图形设备（即 <c>!Main.dedServ</c>）。
    /// <para>
    /// 专用服务器上本类型的全部图形成员都会抛 <see cref="InvalidOperationException"/>，
    /// 调用方应当用这个属性<b>提前分支</b>，而不是去 catch：<c>if (!MonoManagedRenderTarget.IsAvailable) return;</c>
    /// </para>
    /// </summary>
    public static bool IsAvailable => !Main.dedServ;

    /// <summary>距最后一次使用经过的帧数，由 <see cref="MonoRenderTargetManager"/> 递增。</summary>
    public int TimeSinceLastUsage { get; internal set; }

    /// <summary>底层渲染目标是否不存在（尚未创建或已被释放）。</summary>
    public bool IsUninitialized => target is null || target.IsDisposed;

    /// <summary>
    /// 底层渲染目标，必要时惰性创建。
    /// <para>
    /// 初始化委托返回 null 时抛 <see cref="InvalidOperationException"/>——与其让下游拿去绘制、
    /// 在 <c>SetRenderTarget(null)</c> 处失败得更难查，不如在这里就报错。
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">初始化委托返回了 null；或在专用服务器上被访问。</exception>
    /// <exception cref="ObjectDisposedException">已被 <see cref="Retire"/> 永久注销。</exception>
    public RenderTarget2D Target
    {
        get
        {
            if (IsRetired)
                throw new ObjectDisposedException(nameof(MonoManagedRenderTarget), "该受管渲染目标已被 Retire 永久注销，不能再访问 Target。");
            ThrowIfNoGraphicsDevice();
            TimeSinceLastUsage = 0;
            RenderTarget2D? current = target;
            if (current is null || current.IsDisposed)
            {
                current = initializer(Main.screenWidth, Main.screenHeight)
                    ?? throw new InvalidOperationException("MonoManagedRenderTarget 的初始化委托返回了 null。");
                target = current;
                WaitingForFirstInitialization = false;
                IsDisposed = false;
            }
            return current;
        }
    }

    /// <summary>当前渲染目标宽度（会刷新空闲计时器，见类型说明）。</summary>
    public int Width => Target.Width;

    /// <summary>当前渲染目标高度（会刷新空闲计时器，见类型说明）。</summary>
    public int Height => Target.Height;

    /// <summary>构造一个受管渲染目标，并立即登记到 <see cref="MonoRenderTargetManager"/>（专用服务器上不登记）。</summary>
    /// <param name="initializer">按宽高创建渲染目标。</param>
    /// <param name="resetOnResize">分辨率变化时是否重建。</param>
    /// <param name="subjectToGarbageCollection">长期未使用时是否允许自动释放。</param>
    /// <exception cref="ArgumentNullException"><paramref name="initializer"/> 为 null。</exception>
    public MonoManagedRenderTarget(Func<int, int, RenderTarget2D> initializer, bool resetOnResize = true, bool subjectToGarbageCollection = true)
    {
        this.initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
        ResetOnResize = resetOnResize;
        SubjectToGarbageCollection = subjectToGarbageCollection;

        // 服务器上不会用到它，登记也是空转（管理器连钩子都没挂），干脆不登记：登记表就不必在服务器上增长。
        // 也正因为如此，构造在 dedServ 上<b>不抛异常</b>——mod 里"两边都建一份、只有客户端用"的写法不该让服务器崩。
        if (IsAvailable)
            MonoRenderTargetManager.Register(this);
    }

    /// <summary>默认的"跟随屏幕"创建方式：<see cref="SurfaceFormat.Color"/>、无深度、无 MipMap。</summary>
    /// <param name="width">宽度。</param>
    /// <param name="height">高度。</param>
    /// <exception cref="InvalidOperationException">在专用服务器上被调用。</exception>
    public static RenderTarget2D CreateScreenSizedTarget(int width, int height)
    {
        ThrowIfNoGraphicsDevice();
        return new(Main.instance.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
    }

    /// <summary>丢弃当前渲染目标并按新尺寸重建。</summary>
    /// <param name="width">新宽度。</param>
    /// <param name="height">新高度。</param>
    /// <exception cref="InvalidOperationException">在专用服务器上被调用。</exception>
    /// <exception cref="ObjectDisposedException">已被 <see cref="Retire"/> 永久注销。</exception>
    public void Recreate(int width, int height)
    {
        if (IsRetired)
            throw new ObjectDisposedException(nameof(MonoManagedRenderTarget), "该受管渲染目标已被 Retire 永久注销，不能再重建。");
        ThrowIfNoGraphicsDevice();
        target?.Dispose();
        target = initializer(width, height);
        WaitingForFirstInitialization = false;
        IsDisposed = false;
        TimeSinceLastUsage = 0;
    }

    /// <summary>把图形设备的当前渲染目标切换到自己，可选地先清屏。</summary>
    /// <param name="clearColor">清屏颜色；为 null 时不清屏。</param>
    /// <exception cref="InvalidOperationException">在专用服务器上被调用。</exception>
    public void SwapToRenderTarget(Color? clearColor = null)
    {
        ThrowIfNoGraphicsDevice();
        Main.instance.GraphicsDevice.SetRenderTarget(Target);
        if (clearColor.HasValue)
            Main.instance.GraphicsDevice.Clear(clearColor.Value);
    }

    /// <summary>把另一个渲染目标的内容整幅拷进来（先把目标清成透明，再以 <see cref="BlendState.Opaque"/> 画满）。</summary>
    /// <param name="source">来源渲染目标。</param>
    /// <exception cref="InvalidOperationException">在专用服务器上被调用。</exception>
    public void CopyContentsFrom(RenderTarget2D source)
    {
        ThrowIfNoGraphicsDevice();
        RenderTargetBinding[] oldTargets = Main.instance.GraphicsDevice.GetRenderTargets();
        try
        {
            Main.instance.GraphicsDevice.SetRenderTarget(Target);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            Main.spriteBatch.Draw(source, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
        finally
        {
            Main.instance.GraphicsDevice.SetRenderTargets(oldTargets);
        }
    }

    /// <summary>隐式转换到底层渲染目标，便于直接传给需要 <see cref="RenderTarget2D"/> 的 API。</summary>
    /// <param name="managed">受管渲染目标。</param>
    public static implicit operator RenderTarget2D(MonoManagedRenderTarget managed) => managed.Target;

    /// <summary>
    /// <b>只释放底层渲染目标</b>，不注销自己——这不是本对象的终态，之后访问 <see cref="Target"/> 会按需重建（见类型说明）。
    /// 需要永久注销请用 <see cref="Retire"/>。重复调用安全。
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        target?.Dispose();
        target = null;
        TimeSinceLastUsage = 0;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 永久注销：释放显存里的纹理，<b>并</b>把自己从 <see cref="MonoRenderTargetManager"/> 的登记表里摘掉。
    /// 此后 <see cref="IsRetired"/> 为 <see langword="true"/>，访问 <see cref="Target"/> 或调用 <see cref="Recreate"/> 会抛
    /// <see cref="ObjectDisposedException"/>，也不会再参与分辨率重建与闲置回收。
    /// <para>
    /// 适合"动态创建、用完就丢"的目标（例如每个世界、每个 Boss 各一份），避免登记表只增不减。
    /// 重复调用安全；被闲置回收后再调用也安全。
    /// </para>
    /// </summary>
    public void Retire()
    {
        if (IsRetired)
            return;
        IsRetired = true;
        Dispose();
        MonoRenderTargetManager.Unregister(this);
    }

    /// <summary>
    /// 专用服务器上没有 <c>GraphicsDevice</c>，任何触碰设备的路径都必须在这里失败。
    /// <para>
    /// 不这样做的后果不是"少画一帧"，而是在 <c>Main.instance.GraphicsDevice</c> 上抛 <see cref="NullReferenceException"/>——
    /// 堆栈只会指向 FNA 的某个成员，读不出"这是服务器"这个真正的原因。
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">当前宿主是专用服务器。</exception>
    private static void ThrowIfNoGraphicsDevice()
    {
        if (Main.dedServ)
            throw new InvalidOperationException("专用服务器上没有 GraphicsDevice（Main.dedServ 为 true），受管渲染目标不可用；请用 MonoManagedRenderTarget.IsAvailable 提前分支。");
    }
}
