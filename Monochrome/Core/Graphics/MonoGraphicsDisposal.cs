namespace Monochrome.Core.Graphics;

/// <summary>
/// 图形资源的释放入口。
/// <para>
/// FNA 的 <c>ThreadCheck</c> 要求碰图形设备的调用落在主线程，<c>Dispose</c> 也算；而 tML 卸载模组是在
/// 后台任务上跑的（<c>Mod.UnloadContent → SystemLoader.OnModUnload</c>），主线程那时正并行绘制进度界面。
/// 所以卸载钩子里不要直接 <c>Dispose</c>，交给这里排到主线程。
/// </para>
/// <para>
/// 参照的既有做法：<see cref="RenderTargets.MonoRenderTargetManager"/> 的卸载钩子、
/// <see cref="Primitives.MonoPrimitiveRenderer"/> 的排队释放。
/// </para>
/// </summary>
internal static class MonoGraphicsDisposal
{
    /// <summary>
    /// 把一次释放排到主线程。专用服务器上既没有图形设备、也没有主线程动作循环，就地释放。
    /// <para>
    /// 参数要传<b>资源实例</b>，不要传"怎么再取到它"：排队到真正执行之间，静态字段那一类持有者可能
    /// 已经重建出新的资源，届时按字段去取就会把新的那份释放掉。
    /// </para>
    /// </summary>
    /// <param name="resource">要释放的资源；为 <see langword="null"/> 时什么都不做。</param>
    public static void OnMainThread(IDisposable? resource)
    {
        if (resource is null)
            return;

        if (Main.dedServ)
        {
            resource.Dispose();
            return;
        }

        Main.QueueMainThreadAction(resource.Dispose);
    }

    /// <summary>一批资源合成一次主线程调用，省得逐个排队。</summary>
    /// <param name="resources">要释放的资源。</param>
    public static void OnMainThread(IEnumerable<IDisposable> resources)
    {
        List<IDisposable> pending = [.. resources];
        if (pending.Count == 0)
            return;

        if (Main.dedServ)
        {
            foreach (IDisposable resource in pending)
                resource.Dispose();
            return;
        }

        Main.QueueMainThreadAction(() =>
        {
            foreach (IDisposable resource in pending)
                resource.Dispose();
        });
    }
}
