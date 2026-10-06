namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// 无着色器的整幅染色 pass：把输入按 <see cref="Tint"/> 乘一遍。
/// <para>
/// 用途是<b>测试后处理链本身</b>（验证中间目标、ping-pong 顺序、状态恢复是否正确）以及在
/// 着色器缺失时提供一个可用的降级效果，不是用来做正式特效的——正式特效请写
/// <c>.fx</c> + <see cref="Shaders.MonoShader"/>。
/// </para>
/// </summary>
public sealed class MonoTintPass : IMonoPostFxPass
{
    /// <inheritdoc/>
    public int Order { get; }

    /// <inheritdoc/>
    public bool Enabled { get; set; } = true;

    /// <summary>染色乘数；<see cref="Color.White"/> 表示不变。</summary>
    public Color Tint { get; set; }

    /// <summary>构造一个染色 pass。</summary>
    /// <param name="tint">染色乘数。</param>
    /// <param name="order">执行顺序。</param>
    public MonoTintPass(Color tint, int order = 0)
    {
        Tint = tint;
        Order = order;
    }

    /// <inheritdoc/>
    public void Apply(RenderTarget2D source, RenderTarget2D destination, SpriteBatch spriteBatch)
    {
        // 这个 pass 是公开类型，可能被直接调用而不经过 MonoPostFxPipeline，所以自己也要挡住。
        if (Main.dedServ)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        RenderTargetBinding[] oldTargets = device.GetRenderTargets();
        try
        {
            device.SetRenderTarget(destination);
            device.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
            spriteBatch.Draw(source, Vector2.Zero, Tint);
            spriteBatch.End();
        }
        finally
        {
            device.SetRenderTargets(oldTargets);
        }
    }
}
