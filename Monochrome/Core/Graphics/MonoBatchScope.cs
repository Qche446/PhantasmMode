namespace Monochrome.Core.Graphics;

/// <summary>
/// 一次 SpriteBatch 批次的作用域：进入时 <c>Begin</c>，退出时 <c>End</c>。
/// <para>
/// 它只负责两件事：把参数写在一处，以及保证 body 抛异常时批次也会收掉。
/// <b>它不接管已经打开的批次</b>——进入时若有批次开着，<c>Begin</c> 会照旧抛
/// <see cref="InvalidOperationException"/>；要替换已开批次请用
/// <see cref="MonoSpriteBatchExtensions"/> 里的助手。
/// </para>
/// <para>
/// 不做「快照并还原上一个批次的参数」：那些字段在 FNA 里是 internal，只能靠 Publicize
/// 或反射读取，前者会把 FNA 版本钉死，后者会随字段改名静默失效。
/// </para>
/// </summary>
public readonly struct MonoBatchScope : IDisposable
{
    private readonly SpriteBatch? batch;

    private MonoBatchScope(SpriteBatch batch) => this.batch = batch;

    /// <summary>开一个批次。</summary>
    /// <param name="batch">目标批次。</param>
    /// <param name="sortMode">排序模式，默认即时（需要自定义着色器时用它）。</param>
    /// <param name="blendState">混合状态，默认不透明。</param>
    /// <param name="samplerState">采样状态，默认线性钳位（屏幕后处理的整幅拷贝用它）。</param>
    /// <param name="transform">变换矩阵，默认单位矩阵（屏幕空间）。</param>
    /// <param name="depthStencilState">深度模板状态，默认关闭。</param>
    /// <param name="rasterizerState">光栅化状态，默认不剔除。</param>
    /// <param name="effect">自定义效果，默认无。</param>
    public static MonoBatchScope Begin(
        SpriteBatch batch,
        SpriteSortMode sortMode = SpriteSortMode.Immediate,
        BlendState? blendState = null,
        SamplerState? samplerState = null,
        Matrix? transform = null,
        DepthStencilState? depthStencilState = null,
        RasterizerState? rasterizerState = null,
        Effect? effect = null)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (Main.dedServ)
            return default;

        batch.Begin(
            sortMode,
            blendState ?? BlendState.Opaque,
            samplerState ?? SamplerState.LinearClamp,
            depthStencilState ?? DepthStencilState.None,
            rasterizerState ?? RasterizerState.CullNone,
            effect,
            transform ?? Matrix.Identity);
        return new MonoBatchScope(batch);
    }

    /// <summary>屏幕空间即时批次：<see cref="SpriteSortMode.Immediate"/> + 默认采样与单位矩阵。</summary>
    public static MonoBatchScope BeginImmediate(SpriteBatch batch, BlendState? blendState = null, Matrix? transform = null)
        => Begin(batch, SpriteSortMode.Immediate, blendState, transform: transform);

    /// <summary>结束批次。</summary>
    public void Dispose() => batch?.End();
}
