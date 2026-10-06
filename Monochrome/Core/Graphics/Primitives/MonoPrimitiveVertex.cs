namespace Monochrome.Core.Graphics.Primitives;

/// <summary>
/// Monochrome 图元网格使用的顶点：屏幕位置 + 颜色 + 一个三分量的附加数据位。
/// <para>
/// <b>GPU 不会画圆、也不会画线，它只画三角形</b>，所以这里的每个形状都要先被拆成顶点
/// （三角形的角）与索引（哪几个角拼成一个三角形）。顶点里的颜色会被 GPU 在三角形内部
/// 逐像素线性插值——这就是"渐变"为什么免费：只要给两个角写不同的颜色，中间是算出来的，
/// 不需要逐像素计算。
/// </para>
/// </summary>
/// <param name="position">屏幕坐标（写入前已按坐标空间减去 <c>Main.screenPosition</c>）。</param>
/// <param name="color">顶点颜色，三角形内部由 GPU 线性插值。</param>
/// <param name="textureCoordinate">
/// 附加数据位：<c>X</c>/<c>Y</c> 是归一化参数，<c>Z</c> 是像素跨度。详见
/// <see cref="TextureCoordinate"/>。
/// </param>
public readonly struct MonoPrimitiveVertex(Vector2 position, Color color, Vector3 textureCoordinate) : IVertexType
{
    /// <summary>屏幕坐标。写入前已按坐标空间减去 <c>Main.screenPosition</c>，见 <see cref="MonoGraphicsSpace"/>。</summary>
    public readonly Vector2 Position = position;

    /// <summary>顶点颜色。三角形内部由 GPU 线性插值，因此两端写不同颜色即得渐变。</summary>
    public readonly Color Color = color;

    /// <summary>
    /// 附加数据位。没有贴图被采样，这三个分量是留给图元着色器的自由数据：
    /// <b>X</b> = 沿形状的长度/角度参数 <c>t ∈ [0,1]</c>；<b>Y</b> = 截面坐标 <c>[0,1]</c>，
    /// <c>0</c> / <c>1</c> 是截面两侧、<c>0.5</c> 恒为正中；<b>Z</b> = <c>Y</c> 从 0 走到 1 所跨越的
    /// 真实像素距离（拖尾是条带全宽、圆环是环厚、圆是半径）。于是 <c>Y * Z</c> 就是"距截面 0 侧的像素数"。
    /// <para>
    /// 需要 Z 是因为归一化的 Y 只知道"在截面的百分之几"，不知道这一段实际多宽——想让柔边恒定 3 像素、
    /// 想让虚线在像素上等长，就必须拿到真实宽度。Luminance 的 <c>VertexPosition2DColorTexture</c>
    /// 出于同样理由把宽度放在 Z，两者布局一致。
    /// </para>
    /// </summary>
    public readonly Vector3 TextureCoordinate = textureCoordinate;

    /// <inheritdoc/>
    public VertexDeclaration VertexDeclaration => Declaration;

    /// <summary>
    /// FNA 的顶点布局描述：位置 8 字节（偏移 0）+ 颜色 4 字节（偏移 8）+ 附加 12 字节（偏移 12），
    /// 合计步长 24 字节。
    /// </summary>
    public static readonly VertexDeclaration Declaration = new(
    [
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
    ]);
}
