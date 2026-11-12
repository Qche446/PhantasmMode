namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>Monochrome ping-pong 后处理链里的一个屏幕空间 pass。</summary>
public interface IMonoPostFxPass
{
    /// <summary>执行顺序，越小越先执行。</summary>
    int Order { get; }

    /// <summary>
    /// 是否启用。为 false 时 <see cref="MonoPostFxPipeline"/> 会跳过它且不消耗一块中间目标。
    /// <para>可写：开关是运行期的事（诊断命令、配置项），不该逼调用方重建整条链。</para>
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// 把 <paramref name="source"/> 处理进 <paramref name="destination"/>。
    /// <para><b>实现方约定：</b>自己 <c>SetRenderTarget</c> 到 <paramref name="destination"/>，画完把
    /// 渲染目标恢复回调用前的绑定；批次由实现方自己 <c>Begin</c>/<c>End</c>。</para>
    /// </summary>
    /// <param name="source">输入渲染目标。</param>
    /// <param name="destination">输出渲染目标。</param>
    /// <param name="spriteBatch">可用于整幅拷贝的批次。</param>
    void Apply(RenderTarget2D source, RenderTarget2D destination, SpriteBatch spriteBatch);
}
