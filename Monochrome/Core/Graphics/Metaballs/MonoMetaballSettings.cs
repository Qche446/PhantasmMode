namespace Monochrome.Core.Graphics.Metaballs;

/// <summary>
/// GPU 融合场的全局参数。<b>所有层共用一套</b>（逐层参数留给后续）。
/// <para>
/// 这些值同时喂给两个着色器：跑在
/// <c>Assets/Effects/Metaballs/MetaballField.fx</c> 里的累积 pass 用 <see cref="InfluencePower"/>，
/// 跑在 <c>MetaballComposite.fx</c> 里的合成 pass 用 <see cref="Threshold"/> 及其后几项。
/// </para>
/// </summary>
public sealed record MonoMetaballSettings
{
    /// <summary>
    /// 融合积极性：两颗球多早开始搭桥连成一体。默认 0.5。
    /// <para>
    /// 它不改变球的可见大小——提交时支撑半径会被放大到 <c>Radius / (1 - 阈值)</c>
    /// （见 <see cref="SupportRadius"/>），等值面因此恰好落在 <c>Radius</c> 上。阈值只决定场有多"胖"。
    /// 融合距离（中心距 ≲ 这个值就连起来，可见半径记作 R）：<c>0.5</c> → <c>3R</c>、
    /// <c>0.25</c> → <c>2.33R</c>、<c>0.1</c> → <c>2.11R</c>、<c>0.02</c> → <c>2.04R</c>（接近"贴到才连"）。
    /// </para>
    /// </summary>
    public float Threshold { get; set; } = 0.5f;

    /// <summary>
    /// 由<b>可见半径</b>换算<b>支撑半径</b>：<c>visible / (1 - 阈值)</c>。
    /// <para>
    /// 线性影响力下等值面落在 <c>d = (1 - 阈值) · 支撑半径</c>，所以把支撑半径放大
    /// <c>1 / (1 - 阈值)</c> 倍，就能让"看到的半径"恰好等于传进来的半径。
    /// <b>累积与 CPU 采样都只走这一个换算，别在别处重复算。</b>
    /// </para>
    /// <para>
    /// 阈值被夹在 <c>[0, 0.9]</c>，所以支撑半径最多放大 10 倍——防止有人把阈值设成 0.99 把场撑爆。
    /// </para>
    /// </summary>
    /// <param name="visibleRadius">想要的可见半径。</param>
    public float SupportRadius(float visibleRadius)
    {
        float threshold = Math.Clamp(Threshold, 0f, 0.9f);
        return visibleRadius / Math.Max(0.1f, 1f - threshold);
    }

    /// <summary>阈值过渡宽度，决定边缘软硬（<c>smoothstep</c> 的宽度）。默认 0.08。</summary>
    public float Softness { get; set; } = 0.08f;

    /// <summary>边缘带宽度（也在阈值域里）。0 表示不画边缘带。默认 0.1。</summary>
    public float EdgeWidth { get; set; } = 0.1f;

    /// <summary>边缘带颜色；其 <see cref="Color.A"/> 是边缘带的不透明程度，0 等于关闭边缘。</summary>
    public Color EdgeColor { get; set; } = Color.White;

    /// <summary>
    /// 影响力函数的手感：<c>pow(1 - d/r, power)</c>。
    /// <c>1</c> = 线性（蓝图 §11.5 的 Influence1，与 CPU 侧 <see cref="MonoMetaball.Influence"/> 完全同式）、
    /// <c>2</c>/<c>3</c> = 更黏。
    /// </summary>
    public float InfluencePower { get; set; } = 1f;

    /// <summary>每颗球扇形的切片数。越大边界越圆，但每颗球都是独立一次绘制。默认 24。</summary>
    public int Segments { get; set; } = 24;

    /// <summary>
    /// 累积场的分辨率除数：2 = 半分辨率（默认）。
    /// 阈值场本身是低频的，半分辨率几乎看不出差别，而累积成本按面积降 4 倍。
    /// 改动后要等下一次分辨率变化或重新创建该层的目标才会生效。
    /// </summary>
    public int ResolutionDivisor { get; set; } = 2;

    /// <summary>合成时的整体不透明度。默认 1。</summary>
    public float Opacity { get; set; } = 1f;
}
