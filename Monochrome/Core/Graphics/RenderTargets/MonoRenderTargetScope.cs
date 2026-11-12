namespace Monochrome.Core.Graphics.RenderTargets;

/// <summary>
/// 一次渲染目标切换的作用域：进入时记下当前绑定并切过去，退出时切回来。
/// <para>
/// <b>它保护的是后备缓冲。</b>FNA 的 <c>GraphicsDevice.SetRenderTargets</c> 在切入
/// <c>DiscardContents</c> 的目标时必定先清一次；后备缓冲的 usage 取
/// <c>PresentationParameters.RenderTargetUsage</c>，默认同样是 <c>DiscardContents</c>。
/// 于是「切走再切回屏幕」会把已经画好的画面抹掉——本作用域在绑后备缓冲时把那项设置临时改成
/// <see cref="RenderTargetUsage.PreserveContents"/>，绑完立刻还原，只影响这一次绑定。
/// </para>
/// <para>
/// <b>它管不了渲染目标自己的 usage。</b><see cref="RenderTarget2D.RenderTargetUsage"/> 的 setter 不是 public，
/// 想要保留内容只能在八参构造时指定。往 <c>DiscardContents</c> 的目标里画之前，绑定同样会清掉它——
/// 这一点与作用域无关，作用域只是让清空变得可预期。
/// </para>
/// <para>
/// 用法：<c>using (MonoRenderTargetScope.Bind(target, Color.Transparent)) { ... }</c>，
/// 或 <c>using (lease.Scope()) { ... }</c>；<c>Bind(null)</c> 表示绑后备缓冲。
/// </para>
/// </summary>
public readonly struct MonoRenderTargetScope : IDisposable
{
    private readonly GraphicsDevice? device;
    private readonly RenderTargetBinding[]? previous;

    private MonoRenderTargetScope(GraphicsDevice device, RenderTargetBinding[] previous)
    {
        this.device = device;
        this.previous = previous;
    }

    /// <summary>切到 <paramref name="target"/>；<paramref name="target"/> 为 null 表示后备缓冲。</summary>
    /// <param name="target">要绑定的渲染目标。</param>
    /// <param name="clearColor">切完之后要清屏就给颜色；为 null 时不清（目标仍可能被 FNA 自己清掉，见类型说明）。</param>
    /// <returns>专用服务器上返回一个什么都不做的作用域。</returns>
    public static MonoRenderTargetScope Bind(RenderTarget2D? target, Color? clearColor = null)
    {
        if (Main.dedServ)
            return default;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        RenderTargetBinding[] previous = device.GetRenderTargets();
        SetTarget(device, target);
        if (clearColor.HasValue && target is not null)
            device.Clear(clearColor.Value);
        return new MonoRenderTargetScope(device, previous);
    }

    /// <summary>把绑定恢复成进入时的样子；绑回后备缓冲时不会把它清掉。</summary>
    public void Dispose()
    {
        if (device is null || previous is null)
            return;

        if (previous.Length == 0)
            SetTarget(device, null);
        else
            device.SetRenderTargets(previous);
    }

    /// <summary>绑后备缓冲时临时把 usage 调成保留内容，绑完还原。</summary>
    private static void SetTarget(GraphicsDevice device, RenderTarget2D? target)
    {
        if (target is not null)
        {
            device.SetRenderTarget(target);
            return;
        }

        PresentationParameters presentation = device.PresentationParameters;
        RenderTargetUsage previousUsage = presentation.RenderTargetUsage;
        presentation.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        try
        {
            device.SetRenderTarget(null);
        }
        finally
        {
            presentation.RenderTargetUsage = previousUsage;
        }
    }
}

/// <summary><see cref="MonoRenderTargetScope"/> 的糖：在渲染目标或池租约上直接开作用域。</summary>
public static class MonoRenderTargetScopeExtensions
{
    /// <summary>在这个渲染目标上开一个作用域。</summary>
    public static MonoRenderTargetScope Scope(this RenderTarget2D target, Color? clearColor = null)
        => MonoRenderTargetScope.Bind(target, clearColor);

    /// <summary>在这个池租约上开一个作用域；租约为 null 时作用域什么都不做。</summary>
    public static MonoRenderTargetScope Scope(this MonoRenderTargetPool.Lease? lease, Color? clearColor = null)
        => lease is null ? default : MonoRenderTargetScope.Bind(lease.Target, clearColor);
}
