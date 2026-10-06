using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// 径向色差（ChromaticAberration）：红蓝通道沿半径方向错开，边缘出现彩边。
/// <para>着色器：<c>Assets/Effects/Screen/ChromaticAberration.fx</c>，注册名 <c>Monochrome.ChromaticAberration</c>。</para>
/// <para>默认 <c>Order</c> 是 30：它要在模糊<b>之后</b>（模糊先发生，彩边才不会被抹掉）。</para>
/// <para><b>和 Luminance 的重叠</b>：Luminance 也有色差滤镜。两者可以共存，但请不要同时开启——效果会叠加成
/// 明显的"三色重影"。要跟着别人一起开关，就把本 pass 的 <see cref="MonoShaderPass.Enabled"/> 设成
/// <c>!LuminanceFilterActive</c> 之类的条件。</para>
/// </summary>
public sealed class MonoChromaticAberrationPass : MonoShaderPass
{
    /// <summary>构造一个色差 pass。</summary>
    /// <param name="order">执行顺序。</param>
    public MonoChromaticAberrationPass(int order = 30) : base("Monochrome.ChromaticAberration", order)
    {
    }

    /// <summary>
    /// 半径处的 UV 偏移量。默认 0.004（大致等于屏幕宽度的 0.4%）。
    /// <para>量纲是 <b>UV</b> 而不是像素：屏幕越大，同样的值对应的像素越多，视觉强度保持一致。</para>
    /// </summary>
    public float Strength { get; set; } = 0.004f;

    /// <summary>0 = 全屏均匀偏移（像镜头没对好焦）；1 = 只有边角才有色差。默认 1。</summary>
    public float Falloff { get; set; } = 1f;

    /// <inheritdoc/>
    protected override void ApplyParameters(MonoShader shader)
        => shader.SetParameter("aberrationStrength", Strength)
                 .SetParameter("aberrationFalloff", MathHelper.Clamp(Falloff, 0f, 1f));
}
