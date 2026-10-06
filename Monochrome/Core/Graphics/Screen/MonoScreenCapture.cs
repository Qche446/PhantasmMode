using Monochrome.Core.Graphics.PostProcessing;
using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Graphics.RenderTargets;
using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.Screen;

/// <summary>
/// 屏幕后处理的一步到位入口：捕获 → 过链 → 回贴。给一块已经画好内容的渲染目标，结果覆盖同一块目标。
/// <para>
/// <b>为什么原地处理是安全的</b>：GPU 不允许一块纹理同时当渲染目标和采样源，所以这里先把内容拷进池里
/// 租来的 <c>ping</c>，中间若干 pass 在 ping/pong 之间倒，最后再把结果写回原目标。每一步的读写目标都不同，
/// 因此不会形成反馈回路。给这块目标供给内容的通常是
/// <see cref="MonoScreenCaptureSystem"/>，它把链挂到了原版唯一合适的插入点上。
/// </para>
/// <para>
/// <b>批次要求</b>：这些方法自己 <c>Begin</c>/<c>End</c>，所以调用点上不能有开着的 <see cref="SpriteBatch"/>。
/// 原版 <c>EndCapture</c> 自己就是无条件 <c>Main.spriteBatch.Begin(...)</c> 的，因此"能钩 EndCapture
/// 的地方"天然满足这个前提。
/// </para>
/// </summary>
public static class MonoScreenCapture
{
    /// <summary>
    /// 当前被绑定为绘制目标的渲染目标；绑定的是<b>后备缓冲</b>时返回 <see langword="null"/>。
    /// <para>
    /// <b>为什么后备缓冲返回 null 而不是它的句柄</b>：D3D11 不允许把后备缓冲当纹理采样，
    /// 所以"捕获当前屏幕"这件事在后备缓冲上根本做不到。要拿得到内容，就必须先让原版把世界
    /// 画进一块真正的渲染目标——那正是 <see cref="MonoScreenCaptureSystem"/> 做的事。
    /// </para>
    /// </summary>
    public static RenderTarget2D? BoundTarget
    {
        get
        {
            if (Main.dedServ)
                return null;
            RenderTargetBinding[] bindings = Main.instance.GraphicsDevice.GetRenderTargets();
            return bindings.Length > 0 && bindings[0].RenderTarget is RenderTarget2D target ? target : null;
        }
    }

    /// <summary>
    /// 把 <paramref name="scene"/> 里的内容原地过一遍 <paramref name="pipeline"/>：捕获 → 过链 → 回贴。
    /// </summary>
    /// <param name="pipeline">要应用的链。链里一个启用的 pass 都没有时直接返回，<b>连一次拷贝都不做</b>。</param>
    /// <param name="scene">已经画好内容的渲染目标；结果会覆盖它。</param>
    /// <param name="spriteBatch">用于拷贝的批次；为 null 时用 <c>Main.spriteBatch</c>。</param>
    /// <returns>确实做了处理才返回 true；被跳过（服务器 / 空链 / 参数无效 / 池不可用）返回 false。</returns>
    public static bool Apply(MonoPostFxPipeline pipeline, RenderTarget2D scene, SpriteBatch? spriteBatch = null)
    {
        if (Main.dedServ || pipeline is null || scene is null || scene.IsDisposed)
            return false;

        // 空链什么都不做：既省下一次全屏拷贝，也避免"申请了捕获却什么都没改"的白付开销。
        if (!pipeline.IsActive)
            return false;

        SpriteBatch batch = spriteBatch ?? Main.spriteBatch;

        using MonoRenderTargetPool.Lease? first = MonoRenderTargetPool.Rent(scene.Width, scene.Height);
        using MonoRenderTargetPool.Lease? second = MonoRenderTargetPool.Rent(scene.Width, scene.Height);
        if (first is null || second is null)
            return false;

        RenderTarget2D ping = first.Target;
        RenderTarget2D pong = second.Target;

        MonoPostFxPipeline.CopyTo(scene, ping, batch);

        // 直接读 Passes 而不用 pipeline.Apply：这里已经借好了两块中间目标，
        // 再让 Apply 自己借两块就是白借（每帧两次渲染目标分配）。
        IReadOnlyList<IMonoPostFxPass> passes = pipeline.Passes;
        for (int i = 0; i < passes.Count; i++)
        {
            IMonoPostFxPass pass = passes[i];
            if (!pass.Enabled)
                continue;
            pass.Apply(ping, pong, batch);
            (ping, pong) = (pong, ping);
        }

        MonoPostFxPipeline.CopyTo(ping, scene, batch);
        return true;
    }

    /// <summary>
    /// 捕获<b>当前绑定的渲染目标</b>并原地过链。绑定的是后备缓冲时返回 false（见 <see cref="BoundTarget"/>）。
    /// </summary>
    /// <param name="pipeline">要应用的链。</param>
    /// <param name="spriteBatch">用于拷贝的批次；为 null 时用 <c>Main.spriteBatch</c>。</param>
    /// <returns>确实做了处理才返回 true。</returns>
    public static bool ApplyToBoundTarget(MonoPostFxPipeline pipeline, SpriteBatch? spriteBatch = null)
        => BoundTarget is RenderTarget2D bound && Apply(pipeline, bound, spriteBatch);

    /// <summary>
    /// 把一张贴图按 1:1 铺满当前绑定的渲染目标——屏幕后处理 pass 的出口。走图元路径
    /// （<see cref="MonoPrimitiveRenderer.RenderQuad"/>，采样槽 s1），它的顶点签名与参数语义
    /// 已被图元、融合球、粒子共用并上机验证过。
    /// <para>
    /// 投影与四边形都按 <paramref name="destination"/> 的尺寸算，所以半分辨率目标也是精确 1:1 的。
    /// </para>
    /// </summary>
    /// <param name="source">要铺满目标的贴图。</param>
    /// <param name="destination">当前已绑定的目标（只用它的尺寸）。</param>
    /// <param name="shader">用来绘制的着色器（应当采样 <c>register(s1)</c>）。</param>
    /// <param name="blendState">混合状态；为 null 时用 <see cref="BlendState.Opaque"/>。</param>
    public static void DrawFullscreen(Texture2D source, RenderTarget2D destination, MonoShader shader, BlendState? blendState = null)
    {
        if (Main.dedServ || source is null || destination is null || shader is null || shader.IsDisposed)
            return;

        Vector2 center = destination.Size() * 0.5f;
        MonoPrimitiveRenderer.RenderQuad(source, center, Vector2.One, 0f, Color.White, shader, blendState ?? BlendState.Opaque, MonoGraphicsSpace.Screen, destination.Width, destination.Height);
    }
}
