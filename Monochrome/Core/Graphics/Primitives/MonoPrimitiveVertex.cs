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
/// 附加数据位：<c>X</c> 是长度参数，<c>Y</c> 是截面坐标，<c>Z</c> 是截面半宽。详见
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
    /// <b>X</b> = 沿形状的长度/角度参数 <c>t ∈ [0,1]</c>；<b>Y</b> = 截面坐标，已按半宽偏移过；
    /// <b>Z</b> = 截面半宽（像素），截面本身已归一化的形状固定写 <c>1</c>。
    /// <para>
    /// 着色器用 <c>(Y - 0.5) / Z + 0.5</c> 把 Y 还原成 <c>[0,1]</c> 的归一化截面：条带的两侧还原成 0 / 1，
    /// 圆与四边形这类本来就归一化的还原成它自己。编码方式与 Luminance 的
    /// <c>VertexPosition2DColorTexture</c> 相同，两边的着色器可以互换。
    /// </para>
    /// <para>
    /// 两个分量由 <see cref="SegmentCoordinate"/> 与 <see cref="NormalizedCoordinate"/> 统一生成，
    /// 调用方不要手写。
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

    /// <summary>
    /// 条带类图元（拖尾、描边、虚线）的 <c>(Y, Z)</c>。<paramref name="pixelSpan"/> 是条带全宽，
    /// <paramref name="section01"/> 的 <c>0</c> / <c>1</c> 落在两侧、<c>0.5</c> 落在中线。
    /// </summary>
    public static Vector2 SegmentCoordinate(float section01, float pixelSpan)
    {
        float half = MathF.Max(pixelSpan, 1e-4f) * 0.5f;
        return new Vector2(0.5f + (section01 - 0.5f) * half, half);
    }

    /// <summary>
    /// 截面已经归一化的图元（圆、圆环、多边形填充、贴图四边形）的 <c>(Y, Z)</c>：Y 原样传出，
    /// Z 固定 1，着色器那条还原公式因此恒等于 Y。
    /// </summary>
    public static Vector2 NormalizedCoordinate(float section01) => new(section01, 1f);
}
