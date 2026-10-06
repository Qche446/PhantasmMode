using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// 噪点（Grain）：叠加胶片颗粒。
/// <para>着色器：<c>Assets/Effects/Screen/Grain.fx</c>，注册名 <c>Monochrome.Grain</c>。</para>
/// <para><b>噪点的相位来自 <c>globalTime</c>，按 30fps 量化</b>（<c>floor(globalTime * 30)</c>）。
/// 不量化的话每秒 60 次变化会让颗粒"沸腾"得刺眼；量化到 30 之后仍像胶片。</para>
/// <para>默认 <c>Order</c> 是 50：噪点要加在最后几步，否则会被后面的模糊抹平。</para>
/// </summary>
public sealed class MonoGrainPass : MonoShaderPass
{
    /// <summary>构造一个噪点 pass。</summary>
    /// <param name="order">执行顺序。</param>
    public MonoGrainPass(int order = 50) : base("Monochrome.Grain", order)
    {
    }

    /// <summary>叠加幅度。默认 0.08（"有质感但不脏"）。超过 0.2 就很吵了。</summary>
    public float Strength { get; set; } = 0.08f;

    /// <summary>噪声格子密度：1 = 全屏一个格子（看不出噪点），600 左右是细密颗粒。默认 600。</summary>
    public float Scale { get; set; } = 600f;

    /// <summary>true = 单色噪点（像胶片，推荐）；false = 三通道独立（彩色噪点，更脏）。默认 true。</summary>
    public bool Monochrome { get; set; } = true;

    /// <inheritdoc/>
    protected override void ApplyParameters(MonoShader shader)
        => shader.SetParameter("grainStrength", Math.Max(0f, Strength))
                 .SetParameter("grainScale", Math.Max(1f, Scale))
                 .SetParameter("grainMonochrome", Monochrome ? 1f : 0f);
}
