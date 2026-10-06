using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// 可分离高斯模糊的一个方向。着色器 <c>Assets/Effects/Screen/Blur.fx</c>，注册名 <c>Monochrome.Blur</c>。
/// <para>
/// 一条模糊链要横纵两个实例，而且必须相邻（用 <see cref="CreateChain"/> 或
/// <c>MonoScreenFx.AddTo(pipeline, "blur", order)</c>）：只跑一个方向是拖影，中间插进别的 pass 则
/// 两者都不成立。5 抽头 (1,4,6,4,1)/16，单次总半径约 <c>2 × Radius</c> 像素；要更宽就叠更多对，
/// 把 <see cref="Radius"/> 调过 2 像素会因采样不足出现块状条纹。
/// </para>
/// </summary>
public sealed class MonoBlurPass : MonoShaderPass
{
    /// <summary>构造一个单方向模糊 pass。</summary>
    /// <param name="horizontal">true = 横向，false = 纵向。</param>
    /// <param name="order">执行顺序。</param>
    public MonoBlurPass(bool horizontal, int order = 10) : base("Monochrome.Blur", order)
    {
        Horizontal = horizontal;
    }

    /// <summary>本实例负责的方向。</summary>
    public bool Horizontal { get; }

    /// <summary>单步半径（像素）。总模糊半径约为它的两倍。默认 3。</summary>
    public float Radius { get; set; } = 3f;

    /// <summary>
    /// 造出一对（横向 + 纵向）相邻的模糊 pass。
    /// <para>两个实例各自持有独立的 <see cref="Radius"/>，所以想改半径要<b>两个都改</b>。
    /// 这是刻意的：横纵半径不同在数学上就是各向异性模糊，有时正是想要的（横向运动模糊）。
    /// </para>
    /// </summary>
    /// <param name="order">横向的 order；纵向会用 <c>order + 1</c>。</param>
    /// <param name="radius">两个方向的半径。</param>
    /// <returns>横、纵两个 pass。</returns>
    public static (MonoBlurPass Horizontal, MonoBlurPass Vertical) CreateChain(int order = 10, float radius = 3f)
        => (new MonoBlurPass(true, order) { Radius = radius }, new MonoBlurPass(false, order + 1) { Radius = radius });

    /// <inheritdoc/>
    protected override void ApplyParameters(MonoShader shader)
        => shader.SetParameter("blurDirection", Horizontal ? Vector2.UnitX : Vector2.UnitY)
                 .SetParameter("blurRadius", Math.Max(0f, Radius));
}
