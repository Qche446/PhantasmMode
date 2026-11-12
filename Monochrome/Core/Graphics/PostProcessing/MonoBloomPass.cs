using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Graphics.RenderTargets;
using Monochrome.Core.Graphics.Screen;
using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// Bloom：亮部提取 → 两向模糊 → 加性合成，一套自带半分辨率中间目标的后处理 pass。
/// <para>
/// <b>为什么不像其它特效那样只做一个 <see cref="MonoShaderPass"/>：</b>加性合成需要"原始画面"与
/// "模糊结果"<b>同时</b>在手，而 <see cref="IMonoPostFxPass.Apply"/> 只递进来 source 与 destination，
/// 串行链上的一个 pass 拿不到两步之前的输入。所以它必须自己一次做完四步，而不是拆成四个 pass 插进链里。
/// </para>
/// <para>
/// <b>四步</b>（中间目标全部是 source 的一半尺寸，填充率省四分之三）：
/// ① 按阈值提取亮部，乘上 <see cref="Strength"/>；② <c>Monochrome.Blur</c> 横向；
/// ③ 同一份模糊着色器纵向；④ 整幅拷贝原始画面，再把模糊结果加性叠上去。
/// 阈值与强度都在第 ① 步写好，所以第 ④ 步只是一次普通加性绘制，不需要第五个着色器。
/// </para>
/// <para>
/// <b>不做 HDR。</b>8 bit 目标上阈值提取会把高光裁到 1.0，像素画风格仍然成立；
/// HDR 只在"需要区分超过 1 的亮部"时才有意义，而那要求整条链换成浮点目标。
/// </para>
/// </summary>
public sealed class MonoBloomPass : IMonoPostFxPass
{
    /// <summary>亮部提取着色器的注册名（源文件 <c>Assets/Effects/Screen/Bloom.fx</c>）。</summary>
    public const string ExtractShaderName = "Monochrome.Bloom";

    /// <summary>模糊着色器的注册名；与 <see cref="MonoBlurPass"/> 共用同一份资产与同一套参数。</summary>
    public const string BlurShaderName = "Monochrome.Blur";

    /// <summary>
    /// "只加颜色、不动 alpha"的加性混合。
    /// <para>
    /// <see cref="BlendState.Additive"/> 的颜色源因子是 <see cref="Blend.SourceAlpha"/>——源 alpha 会乘进颜色。
    /// 亮部提取的输出 alpha 恒为 0，用现成的 Additive 等于什么都没加上。这里把颜色通道的源因子换成
    /// <see cref="Blend.One"/>，alpha 通道写成"写 0、保留下层"，于是它真的只加颜色。
    /// </para>
    /// </summary>
    private static readonly BlendState AdditiveColor = new()
    {
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add,
    };

    private MonoShader? extractShader;
    private MonoShader? blurShader;
    private bool warnedUnavailable;

    /// <summary>
    /// 执行顺序。默认 5：在模糊（10）与调色（20）之前。
    /// <para>发光应当是"从原始画面里提取出来的"，先于会改写亮度的色阶；至于模糊，让一条已经含发光的
    /// 画面再被抹开，比先抹开再提取更不容易把亮点糊没。</para>
    /// </summary>
    public int Order { get; set; } = 5;

    /// <inheritdoc/>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 亮度阈值（<c>[0,1]</c>）。只有亮度超过它的像素才会发光。
    /// <para>0 = 整屏发光，那是参数用错；默认 0.62 大致是"火焰、魔法光、白字"这一档。</para>
    /// </summary>
    public float Threshold { get; set; } = 0.62f;

    /// <summary>
    /// 发光强度（加性叠上去的倍数）。0 = 退化成原画，1 = 亮部原样再叠一遍。
    /// <para>它被预乘进提取结果，所以调它不影响后面的模糊核，也不会让画面变"脏"。</para>
    /// </summary>
    public float Strength { get; set; } = 0.85f;

    /// <summary>
    /// 模糊半径（屏幕像素）。总模糊半径约为它的两倍。
    /// <para>超过 3 会开始出现块状条纹：<c>Monochrome.Blur</c> 是 5 抽头、相邻样本间隔就是这个半径，
    /// 间隔太大时采样点之间会漏空。要更宽请先在半分辨率下叠加更多对，而不是一味调大它。</para>
    /// </summary>
    public float Radius { get; set; } = 3f;

    /// <summary>两个着色器资产是否都已经可用。</summary>
    public bool IsAvailable => Has(ExtractShaderName) && Has(BlurShaderName);

    /// <summary>造一个 Bloom 并加进链里，返回它以便继续调参数。</summary>
    /// <param name="pipeline">要加入的链。</param>
    /// <param name="order">执行顺序；必须在 <c>Add</c> 之前生效，见 <see cref="Order"/>。</param>
    public static MonoBloomPass AddTo(MonoPostFxPipeline pipeline, int order = 5)
    {
        MonoBloomPass pass = new() { Order = order };
        pipeline?.Add(pass);
        return pass;
    }

    /// <inheritdoc/>
    public void Apply(RenderTarget2D source, RenderTarget2D destination, SpriteBatch spriteBatch)
    {
        // 与其它绘制路径一致：缺设备就什么都不做，不抛异常。
        if (Main.dedServ)
            return;

        MonoShader? extract = Resolve(ExtractShaderName, ref extractShader);
        MonoShader? blur = Resolve(BlurShaderName, ref blurShader);
        if (extract is null || blur is null || !MonoPrimitiveRenderer.IsReady)
            return;

        // 半分辨率：Bloom 是低频信号，几何减半看不出来，填充率只有四分之一。
        int width = Math.Max(1, source.Width / 2);
        int height = Math.Max(1, source.Height / 2);
        using MonoRenderTargetPool.Lease? extracted = MonoRenderTargetPool.Rent(width, height);
        using MonoRenderTargetPool.Lease? blurred = MonoRenderTargetPool.Rent(width, height);
        // 池只在没有图形设备时返回 null（上面已经挡掉了 dedServ）；判空是兜底，也保证将来语义变化不会变成 NRE。
        if (extracted is null || blurred is null)
            return;

        RenderTarget2D bloom = extracted.Target;
        RenderTarget2D scratch = blurred.Target;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        RenderTargetBinding[] oldTargets = device.GetRenderTargets();
        try
        {
            // ① 亮部提取：source → bloom（半分辨率）。
            device.SetRenderTarget(bloom);
            extract.SetParameter("threshold", MathHelper.Clamp(Threshold, 0f, 1f))
                   .SetParameter("strength", Math.Max(0f, Strength));
            MonoScreenCapture.DrawFullscreen(source, bloom, extract);

            // ② ③ 可分离模糊。screenSize 由提交路径写成全屏尺寸，因此 blurRadius 的量纲就是屏幕像素；
            // 中间目标是半分辨率，同样的 uv 步长对它的采样更稀，结果反而更平滑。
            blur.SetParameter("fxOpacity", 1f)
                .SetParameter("blurRadius", Math.Max(0f, Radius))
                .SetParameter("blurDirection", Vector2.UnitX);
            device.SetRenderTarget(scratch);
            MonoScreenCapture.DrawFullscreen(bloom, scratch, blur);

            blur.SetParameter("blurDirection", Vector2.UnitY);
            device.SetRenderTarget(bloom);
            MonoScreenCapture.DrawFullscreen(scratch, bloom, blur);

            // ④ 合成：destination 只绑这一次，先原地拷贝原始画面，再直接加性叠上模糊结果。
            // 这里不能用 CopyTo 之后再来一句 SetRenderTarget(destination)：CopyTo 会在 finally 里把绑定
            // 切回调用前的目标，于是那句 SetRenderTarget 是一次"重新切入"；而 destination 是池里
            // DiscardContents 的目标，FNA 每次切进它都会立刻清空，刚拷进去的原始画面会被抹掉，
            // 只剩 alpha 恒 0 的光晕，最终整幅画面在 AlphaBlend 下不可见。
            device.SetRenderTarget(destination);
            device.Clear(Color.Transparent);
            MonoPostFxPipeline.CopyToBound(source, spriteBatch);
            Vector2 center = destination.Size() * 0.5f;
            Vector2 scale = new(
                destination.Width / (float)bloom.Width,
                destination.Height / (float)bloom.Height);
            // 着色器传 null = 用图元的默认着色器（贴图乘顶点色），混合状态由这里给。
            MonoPrimitiveRenderer.RenderQuad(
                bloom, center, scale, 0f, Color.White, null, AdditiveColor,
                MonoGraphicsSpace.Screen, destination.Width, destination.Height);
        }
        finally
        {
            device.SetRenderTargets(oldTargets);
        }
    }

    /// <summary>取着色器：取到就缓存，取不到不缓存（注册可能发生在之后，热重载也会换实例）。</summary>
    private MonoShader? Resolve(string name, ref MonoShader? cache)
    {
        if (cache is not null && !cache.IsDisposed)
            return cache;

        cache = MonoShaderManager.TryGet(name, out MonoShader? found) ? found : null;
        if (cache is null && !warnedUnavailable)
        {
            warnedUnavailable = true;
            Monochrome.Instance?.Logger.Warn(
                $"{GetType().Name}：着色器 '{name}' 尚未注册，本 pass 会安静跳过。" +
                "检查 Assets/Effects/Screen/ 下的 .fx 是否编出了 .fxc（见 /monoshader status）。");
        }
        return cache;
    }

    /// <summary>只查不缓存也不报警：给 <see cref="IsAvailable"/> 这类纯查询用。</summary>
    private static bool Has(string name)
        => MonoShaderManager.TryGet(name, out MonoShader? shader) && shader is not null && !shader.IsDisposed;
}
