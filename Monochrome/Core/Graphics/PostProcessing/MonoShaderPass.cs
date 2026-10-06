using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Graphics.Screen;
using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// 用着色器实现的屏幕后处理 pass 的基类。子类只需实现 <see cref="ApplyParameters"/>。
/// <para>
/// 父类包办：解析着色器（缺资产就安静跳过）、绑定目标、<c>finally</c> 恢复绑定、铺满全屏、写淡出参数。
/// 着色器契约见 <c>Assets/Effects/Screen/</c> 里任一同胞的头部注释：输入在采样槽 <b>s1</b>，UV 取
/// <c>TextureCoordinates.xy</c>，结果写成 <c>lerp(原始, 结果, saturate(fxOpacity))</c>。
/// 淡出参数叫 <c>fxOpacity</c> 而不是 <c>opacity</c>，因为图元提交路径每帧都会把 <c>opacity</c> 写成 1。
/// </para>
/// </summary>
public abstract class MonoShaderPass : IMonoPostFxPass
{
    private MonoShader? shader;
    private bool warnedUnavailable;

    /// <summary>构造一个屏幕后处理 pass。</summary>
    /// <param name="shaderName">着色器的注册名（形如 <c>Monochrome.Vignette</c>）。</param>
    /// <param name="order">执行顺序，越小越先执行。</param>
    protected MonoShaderPass(string shaderName, int order)
    {
        ShaderName = shaderName;
        Order = order;
    }

    /// <summary>着色器的注册名。</summary>
    public string ShaderName { get; }

    /// <summary>
    /// 执行顺序，越小越先执行。子类都有自己的推荐默认值（见各 AutoLoad 顺序的注释）。
    /// <para><b>改完要重新 <see cref="MonoPostFxPipeline.Add"/> 一次</b>：链只在加入时按 Order 排序，
    /// 直接改这个属性不会让已经在链里的 pass 换位置。</para>
    /// </summary>
    public int Order { get; set; }

    /// <inheritdoc/>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <c>[0,1]</c> 的整体不透明度：1 = 完全生效，0 = 完全不生效（等价于"这条特效看不见"，但仍然在跑）。
    /// <para>写进着色器的 <c>fxOpacity</c>。要淡入淡出请动它，不要去改强度参数——强度参数的语义是"效果长什么样"，
    /// 不是"效果出现多少"。</para>
    /// </summary>
    public float Opacity { get; set; } = 1f;

    /// <summary>已解析到的着色器；尚未注册或已释放时为 <see langword="null"/>。</summary>
    public MonoShader? Shader => ResolveShader();

    /// <summary>着色器资产是否已经可用。</summary>
    public bool IsAvailable => ResolveShader() is not null;

    /// <summary>把本 pass 自己的参数写进着色器。每次绘制都会调用，所以这里只需写"会变的东西"（参数带缓存）。</summary>
    /// <param name="shader">已解析到的着色器。</param>
    protected abstract void ApplyParameters(MonoShader shader);

    /// <inheritdoc/>
    public void Apply(RenderTarget2D source, RenderTarget2D destination, SpriteBatch spriteBatch)
    {
        if (Main.dedServ)
            return;

        MonoShader? resolved = ResolveShader();
        // 图元渲染器没就绪（设备资源创建失败/首次初始化还没轮到）时画不出来，这时**不要**去 SetRenderTarget，
        // 否则会白白清掉目标又重新绑定一次，而且目标内容会被留成空的。
        if (resolved is null || !MonoPrimitiveRenderer.IsReady)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        RenderTargetBinding[] oldTargets = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(destination);
            // 刻意不 Clear：全屏四边形会写满 destination 的每一个像素（含 alpha），清屏是纯浪费的填充率。
            // 这一点由 MonoScreenCapture.DrawFullscreen 保证：它的投影与四边形都按 destination 的尺寸算。
            ApplyParameters(resolved);
            resolved.SetParameter("fxOpacity", MathHelper.Clamp(Opacity, 0f, 1f));
            MonoScreenCapture.DrawFullscreen(source, destination, resolved);
        }
        finally
        {
            device.SetRenderTargets(oldTargets);
        }
    }

    /// <summary>
    /// 取着色器。<b>取到就缓存；取不到返回 null 且不缓存</b>——<c>MonoShaderManager</c> 可能在这之后才注册
    /// （模组加载顺序、着色器热重载），而字典查询的成本可以忽略。
    /// </summary>
    private MonoShader? ResolveShader()
    {
        if (shader is not null && !shader.IsDisposed)
            return shader;

        shader = MonoShaderManager.TryGet(ShaderName, out MonoShader? found) ? found : null;
        if (shader is null && !warnedUnavailable)
        {
            warnedUnavailable = true;
            Monochrome.Instance?.Logger.Warn(
                $"{GetType().Name}：着色器 '{ShaderName}' 尚未注册，本 pass 会安静跳过。" +
                "检查 Assets/Effects/Screen/ 下的 .fx 是否编出了 .fxc（见 /mono shader status）。");
        }
        return shader;
    }
}
