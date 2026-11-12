using ReLogic.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace Monochrome.Core.UI;

/// <summary>
/// 重画一行模组条目的名字文字时能拿到的全部信息。
/// <para>
/// <see cref="Position"/> 已经算好原版的排版（行内边距、<see cref="UIText.TextOriginX"/>、
/// 以及名字过长时的等比缩小），单位是 <b>UI 单位</b>；直接把它当 <c>ChatManager.DrawColorCodedString*</c> 的起点即可。
/// 批次由 tModLoader 开好并已 <c>Begin</c>，回调里不要自己开批次。
/// </para>
/// </summary>
public readonly struct MonoRowNameContext
{
    internal MonoRowNameContext(SpriteBatch batch, UIText source, string modName, string text, DynamicSpriteFont font, Vector2 position, float scale, CalculatedStyle inner)
    {
        Batch = batch;
        Source = source;
        ModName = modName;
        Text = text;
        Font = font;
        Position = position;
        Scale = scale;
        Inner = inner;
    }

    /// <summary>原版正在用的批次，<b>已经 Begin 过</b>。</summary>
    public SpriteBatch Batch { get; }

    /// <summary>
    /// 原版的名字文字控件。只读信息用（<c>TextOriginX/Y</c>、<see cref="UIElement.IsMouseHovering"/> 等）；
    /// 它自己的颜色已被设成全透明，不要指望改它能改变可见效果。
    /// </summary>
    public UIText Source { get; }

    /// <summary>这一行属于哪个模组，即 <c>Mod.Name</c>。</summary>
    public string ModName { get; }

    /// <summary>要画的文字，取自 <see cref="Source"/>，可能带富文本标记。</summary>
    public string Text { get; }

    /// <summary><see cref="Text"/> 用的字体，即 <c>FontAssets.MouseText</c>。</summary>
    public DynamicSpriteFont Font { get; }

    /// <summary>文字左上角的 UI 坐标，已含原版的排版偏移。</summary>
    public Vector2 Position { get; }

    /// <summary>原版算出的缩放（名字太长时小于 1），<see cref="Position"/> 已按它排好。</summary>
    public float Scale { get; }

    /// <summary>名字所在的矩形（去掉内边距后）。</summary>
    public CalculatedStyle Inner { get; }

    /// <summary>鼠标是否停在这一行上。</summary>
    public bool Hovered => Source.IsMouseHovering;
}
