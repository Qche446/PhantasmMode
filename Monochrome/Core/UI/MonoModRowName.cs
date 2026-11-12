using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Monochrome.Core.UI;

/// <summary>
/// 模组列表某一行的名字文字，叠在原版 <see cref="UIText"/> 之上自己画。
/// <para>
/// 原版文字是行的子元素，在 <c>UIModItem.DrawSelf</c> 之后才画，钩子里插不到它下面，
/// 所以由这一层接管可见部分：有名字回调就把它设成全透明再交给回调，
/// 没有就把颜色还回去、让原版自己画。
/// 位置每帧从原文字实时读取，tModLoader 调整它的偏移（例如出现键盘图标时右移）也能跟住。
/// </para>
/// <para>
/// 画什么完全由 <see cref="MonoRowSkins"/> 决定，本类不带任何默认样式——
/// Monochrome 自己的名字样式也是注册进去的（见 <see cref="MonoModRowStyle.DrawName"/>）。
/// </para>
/// </summary>
internal sealed class MonoModRowName : UIText
{
    private readonly UIText source;
    private readonly string modName;
    private readonly Color originalColor;

    internal MonoModRowName(UIText source, string modName) : base(source.Text)
    {
        this.source = source;
        this.modName = modName;
        originalColor = source.TextColor;
    }

    /// <summary>原版 <see cref="UIText.DrawSelf"/> 的排版算式，去掉富文本与等比缩小。</summary>
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        // 没注册、注销、或注册了但不管名字：都让原版文字照常显示。
        if (!MonoRowSkins.TryGetName(modName, out Action<MonoRowNameContext>? draw) || draw is null)
        {
            source.TextColor = originalColor;
            return;
        }

        string text = source.Text;

        if (text.Length == 0)
            return;

        DynamicSpriteFont font = FontAssets.MouseText.Value;
        Vector2 size = ChatManager.GetStringSize(font, text, Vector2.One);
        CalculatedStyle inner = source.GetInnerDimensions();

        Vector2 position = inner.Position();
        position.Y -= 2f;
        position.X += (inner.Width - size.X) * source.TextOriginX;
        position.Y += (inner.Height - size.Y) * source.TextOriginY;

        // 和原版一样：名字过长时等比缩小，不折行。
        float scale = 1f;

        if (source.DynamicallyScaleDownToWidth && size.X > inner.Width)
            scale = inner.Width / size.X;

        // 只在真的要自己画时才遮掉原文字，避免回调没画时名字整个消失。
        source.TextColor = Color.Transparent;

        MonoRowNameContext context = new(spriteBatch, source, modName, text, font, position, scale, inner);

        try
        {
            draw(context);
        }
        catch (Exception exception)
        {
            // 与底板回调同一套处理：这一行已经画坏了，停用比每帧抛一次好。
            MonoRowSkins.Disable(modName, exception);
            source.TextColor = originalColor;
        }
    }
}
