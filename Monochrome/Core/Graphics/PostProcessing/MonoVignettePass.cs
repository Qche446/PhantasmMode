using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// 暗角（Vignette）：把画面四周压暗，把视线收拢到中心。
/// <para>着色器：<c>Assets/Effects/Screen/Vignette.fx</c>，注册名 <c>Monochrome.Vignette</c>。</para>
/// <para>默认 <c>Order</c> 是 60：暗角应当压在其它特效<b>之上</b>，否则先压暗再染色会让暗部被提亮回来。</para>
/// </summary>
public sealed class MonoVignettePass : MonoShaderPass
{
    /// <summary>构造一个暗角 pass。</summary>
    /// <param name="order">执行顺序。</param>
    public MonoVignettePass(int order = 60) : base("Monochrome.Vignette", order)
    {
    }

    /// <summary>暗角强度：0 = 关，1 = 四角完全变成 <see cref="Color"/>。默认 0.65。</summary>
    public float Strength { get; set; } = 0.65f;

    /// <summary>过渡宽度：越小边角压得越硬。最小按 0.01 处理（0 会让 <c>smoothstep</c> 退化）。默认 0.55。</summary>
    public float Softness { get; set; } = 0.55f;

    /// <summary>四角压到的颜色。默认黑；用深蓝/深紫可以做出"暗部偏色"的胶片感。</summary>
    public Color Color { get; set; } = Color.Black;

    /// <inheritdoc/>
    protected override void ApplyParameters(MonoShader shader)
        => shader.SetParameter("vignetteStrength", Math.Max(0f, Strength))
                 .SetParameter("vignetteSoftness", MathHelper.Clamp(Softness, 0.01f, 1f))
                 .SetParameter("vignetteColor", Color);
}
