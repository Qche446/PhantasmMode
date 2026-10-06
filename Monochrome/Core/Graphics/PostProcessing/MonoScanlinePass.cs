using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// 扫描线（Scanline）：CRT 味道的横向暗线，可选滚动。
/// <para>着色器：<c>Assets/Effects/Screen/Scanline.fx</c>，注册名 <c>Monochrome.Scanline</c>。</para>
/// <para><b>数量是按屏幕高度的比例给的，不是固定像素</b>：所以分辨率或窗口大小变化时暗线的"粗细比例"
/// 不会跳变。<see cref="Count"/> 用半个屏幕高度（例如 1080 就填 540）最接近真实 CRT。</para>
/// <para>默认 <c>Order</c> 是 40。</para>
/// </summary>
public sealed class MonoScanlinePass : MonoShaderPass
{
    /// <summary>构造一个扫描线 pass。</summary>
    /// <param name="order">执行顺序。</param>
    public MonoScanlinePass(int order = 40) : base("Monochrome.Scanline", order)
    {
    }

    /// <summary>暗线强度 <c>[0,1]</c>。默认 0.25；0.5 以上会明显吃掉亮度。</summary>
    public float Strength { get; set; } = 0.25f;

    /// <summary>屏幕里有多少条暗线。默认 540。</summary>
    public float Count { get; set; } = 540f;

    /// <summary>滚动速度（条/秒）。0 = 静止。默认 0。</summary>
    public float Speed { get; set; }

    /// <inheritdoc/>
    protected override void ApplyParameters(MonoShader shader)
        => shader.SetParameter("scanlineStrength", MathHelper.Clamp(Strength, 0f, 1f))
                 .SetParameter("scanlineCount", Math.Max(1f, Count))
                 .SetParameter("scanlineSpeed", Speed);
}
