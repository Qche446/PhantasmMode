using Terraria.UI;

namespace Monochrome.Core.UI;

/// <summary>
/// 画一行模组条目时能拿到的全部信息。回调只读，改不了行本身的状态。
/// <para>
/// <see cref="Outer"/> 与 <see cref="Inner"/> 都是<b>UI 单位</b>：批次由 tModLoader 用 <c>Main.UIScaleMatrix</c> 开好，
/// 回调里直接按这两个矩形画即可，不要自己乘 <c>Main.UIScale</c>，也不要在回调里 <c>Begin</c>/<c>End</c>。
/// </para>
/// </summary>
public readonly struct MonoRowSkinContext
{
    internal MonoRowSkinContext(SpriteBatch batch, UIElement row, string modName, CalculatedStyle outer, CalculatedStyle inner)
    {
        Batch = batch;
        Row = row;
        ModName = modName;
        Outer = outer;
        Inner = inner;
    }

    /// <summary>原版正在用的批次。<b>已经 Begin 过</b>，回调只往里画。</summary>
    public SpriteBatch Batch { get; }

    /// <summary>这一行的 <see cref="UIElement"/>。想在行里加自己的元素就调它的 <c>Append</c>。</summary>
    public UIElement Row { get; }

    /// <summary>这一行属于哪个模组，即 <c>Mod.Name</c>。</summary>
    public string ModName { get; }

    /// <summary>整行的矩形（含内边距），原点已在 UI 空间。</summary>
    public CalculatedStyle Outer { get; }

    /// <summary>去掉内边距后的矩形，图标与名字都画在这里。</summary>
    public CalculatedStyle Inner { get; }

    /// <summary>鼠标是否停在这一行上。原版用它换边框与底色，皮肤通常也跟着变。</summary>
    public bool Hovered => Row.IsMouseHovering;
    /// <summary>
    /// 拆开的简便方法
    /// </summary>
    public void Deconstruct(out SpriteBatch batch, out UIElement row, out string modName, out CalculatedStyle outer, out CalculatedStyle inter)
    {
        batch = Batch;
        row = Row;
        modName = ModName;
        outer = Outer;
        inter = Inner;
    }
}
