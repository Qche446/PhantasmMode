using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// 色阶（ColorGrade）：色调 / 饱和度 / 对比度 / 亮度，一次过。做"整体氛围"最省的一条。
/// <para>着色器：<c>Assets/Effects/Screen/ColorGrade.fx</c>，注册名 <c>Monochrome.ColorGrade</c>。</para>
/// <para>四个参数的默认值<b>全是恒等</b>（白 / 1 / 1 / 0），也就是说默认什么都不改——
/// 想让画面"暖一点、浓一点"就调 <see cref="Tint"/> 与 <see cref="Saturation"/>。
/// 色阶的默认值刻意不预设风格：一条"默认就把画面染色"的 pass 会让消费者莫名其妙。</para>
/// <para>默认 <c>Order</c> 是 20：色阶要在模糊之后、色差之前（先定调，再加光学瑕疵）。</para>
/// </summary>
public sealed class MonoColorGradePass : MonoShaderPass
{
    /// <summary>构造一个色阶 pass。</summary>
    /// <param name="order">执行顺序。</param>
    public MonoColorGradePass(int order = 20) : base("Monochrome.ColorGrade", order)
    {
    }

    /// <summary>乘性色调。<see cref="Color.White"/> = 不变；暖调试试 <c>new Color(255, 235, 210)</c>。</summary>
    public Color Tint { get; set; } = Color.White;

    /// <summary>1 = 不变；0 = 灰度；&gt;1 = 加饱和。默认 1。</summary>
    public float Saturation { get; set; } = 1f;

    /// <summary>围绕 0.5 中灰拉伸的对比度。1 = 不变。默认 1。</summary>
    public float Contrast { get; set; } = 1f;

    /// <summary>直接加上的亮度偏移。0 = 不变。默认 0。</summary>
    public float Brightness { get; set; }

    /// <summary>把四个参数一次性设成"给画面加一层氛围"的常用组合。</summary>
    /// <param name="tint">乘性色调。</param>
    /// <param name="saturation">饱和度。</param>
    /// <param name="contrast">对比度。</param>
    /// <param name="brightness">亮度偏移。</param>
    public MonoColorGradePass Configure(Color tint, float saturation = 1f, float contrast = 1f, float brightness = 0f)
    {
        Tint = tint;
        Saturation = saturation;
        Contrast = contrast;
        Brightness = brightness;
        return this;
    }

    /// <inheritdoc/>
    protected override void ApplyParameters(MonoShader shader)
        => shader.SetParameter("gradeTint", Tint)
                 .SetParameter("gradeSaturation", Math.Max(0f, Saturation))
                 .SetParameter("gradeContrast", Contrast)
                 .SetParameter("gradeBrightness", Brightness);
}
