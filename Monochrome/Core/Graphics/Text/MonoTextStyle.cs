using ReLogic.Graphics;

namespace Monochrome.Core.Graphics.Text;

/// <summary>
/// 文字绘制的样式：字体、缩放、颜色、描边、阴影、锚点、行距。
/// <para>
/// <b>用 <c>new MonoTextStyle()</c> 建，不要用 <c>default</c></b>——<c>default</c> 会把颜色变成全透明黑，
/// 那看起来像"文字没画出来"，很难查。
/// </para>
/// <para>
/// 它是结构体，可以按 <c>in</c> 传，不必担心每次绘制都复制一份。
/// </para>
/// </summary>
public struct MonoTextStyle
{
    /// <summary>建一份默认样式：白字、黑描边、缩放 1、左上锚点。</summary>
    public MonoTextStyle() { }

    /// <summary>字体。<see langword="null"/> 时用 <c>FontAssets.MouseText</c>（界面正文字体）。</summary>
    public DynamicSpriteFont? Font;

    /// <summary>缩放。<c>1</c> 是原版界面正文的大小。</summary>
    public float Scale = 1f;

    /// <summary>文字颜色。</summary>
    public Color Color = Color.White;

    /// <summary>要不要四向描边。</summary>
    public bool Outline = true;

    /// <summary>描边颜色。<b>描边的粗细由原版固定为 2 像素，且不随缩放变化。</b></summary>
    public Color OutlineColor = Color.Black;

    /// <summary>要不要先画一层阴影。与描边可以同时开，阴影会先画。</summary>
    public bool Shadow;

    /// <summary>阴影颜色。</summary>
    public Color ShadowColor = Color.Black;

    /// <summary>阴影偏移（像素）。</summary>
    public float ShadowOffset = 2f;

    /// <summary>
    /// 锚点，两个分量都在 <c>[0,1]</c>：<c>(0,0)</c> = 文字的左上角落在给定位置上，
    /// <c>(0.5,0.5)</c> = 正中，<c>(1,1)</c> = 右下角。多行时每一行各按自己的宽度对齐。
    /// </summary>
    public Vector2 Anchor = Vector2.Zero;

    /// <summary>多行之间额外加的行距（像素，缩放之后再算）。</summary>
    public float ExtraLineSpacing;
}
