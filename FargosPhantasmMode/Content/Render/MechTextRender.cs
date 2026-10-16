using Monochrome.Core.Graphics;
using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Graphics.Shaders;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace FargosPhantasmMode.Content.Render;
public static class MechTextRender
{
    private const string ShaderName = "FargosPhantasmMode.MechText";

    /// <summary>四边形在文字四周留的边距（UI 像素）：描边与高光都不会顶到边缘。</summary>
    private const float Padding = 14f;

    /// <summary>扫描速度：每秒走多少个行宽，0.32 约等于三秒扫过一行。</summary>
    private const float ScrollSpeed = 0.32f;

    /// <summary>四色沿横向流动的速度：每秒走几个循环。</summary>
    private const float ColorFlowSpeed = 0.12f;

    /// <summary>扫描高光的横 / 纵向半径（像素）。亮度按"到扫描线的距离"衰减，上下左右一起淡出。</summary>
    private static readonly Vector2 BandRadius = new(58f, 34f);

    /// <summary>扫描线叠在字形上的高光强度。</summary>
    private const float Highlight = 1.5f;

    private const float Erosion = 0.5f;

    private const float EdgeWidth = 0.45f;
    private const float HaloOpacity = 0.85f;

    public static readonly Color ColorRed = new(255, 0, 0);
    public static readonly Color ColorBlue = new(80, 174, 255);
    public static readonly Color ColorGreen = new(0, 255, 17);
    public static readonly Color ColorYellow = new(255, 241, 38);

    public static bool Draw(DrawableTooltipLine line, Color colorA, Color colorB, Color colorC, Color colorD)
    {
        if (Main.dedServ || string.IsNullOrEmpty(line.Text))
            return false;

        if (line.Rotation != 0f || line.Origin != Vector2.Zero)
            return false;
        if (!MonoPrimitiveRenderer.IsReady)
            return false;

        if (!MonoShaderManager.TryGet(ShaderName, out var shader) || shader is null)
            return false;

        DynamicSpriteFont font = line.Font ?? FontAssets.MouseText.Value;
        Vector2 textScale = line.BaseScale;
        Vector2 textSize = font.MeasureString(line.Text) * textScale;

        float uiScale = MathF.Max(Main.UIScaleMatrix.M11, 0.01f);
        float uiWidth = textSize.X + Padding * 2f;
        float uiHeight = textSize.Y + Padding * 2f;
        int rtWidth = Math.Max(4, (int)MathF.Ceiling(uiWidth * uiScale));
        int rtHeight = Math.Max(4, (int)MathF.Ceiling(uiHeight * uiScale));

        using var lease = MonoRt.Rent(rtWidth, rtHeight);
        if (lease is null)
            return false;

        SpriteBatch spriteBatch = Main.spriteBatch;
        GraphicsDevice device = Main.instance.GraphicsDevice;
        RenderTargetBinding[] previousTargets = device.GetRenderTargets();
        PresentationParameters presentation = device.PresentationParameters;
        RenderTargetUsage previousUsage = presentation.RenderTargetUsage;

        spriteBatch.End();

        bool bound = false;
        presentation.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        try
        {
            device.SetRenderTarget(lease.Target);
            bound = device.GetRenderTargets().Length > 0;
            if (bound)
            {
                device.Clear(Color.Transparent);
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
                    RasterizerState.CullNone, null, Matrix.CreateScale(uiScale) * Matrix.CreateTranslation(Padding, Padding, 0f));
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, line.Text, Vector2.Zero, Color.White,
                    0f, Vector2.Zero, textScale, line.MaxWidth, line.Spread);
                spriteBatch.End();
            }
        }
        finally
        {
            if (previousTargets.Length > 0 && previousTargets[0].RenderTarget is not null)
                device.SetRenderTargets(previousTargets);
            else
                device.SetRenderTarget(null);

            presentation.RenderTargetUsage = previousUsage;
        }

        if (!bound)
        {
            spriteBatch.ResetToDefaultUI(end: false);
            return false;
        }

        Vector2 glyphPixels = Vector2.Transform(new Vector2(line.X, line.Y), Main.UIScaleMatrix);
        Vector2 quadSize = new(rtWidth, rtHeight);
        Vector2 center = glyphPixels - new Vector2(Padding) + quadSize * 0.5f;

        Vector2 bandRadius = new(MathF.Min(BandRadius.X, rtWidth * 0.45f * 0.3f), MathF.Min(BandRadius.Y, rtHeight * 0.45f * 2f));
        float textPixels = textSize.X * uiScale;
        double seconds = Main.GlobalTimeWrappedHourly;

        shader.SetParameter("uColorA", colorA)
              .SetParameter("uColorB", colorB)
              .SetParameter("uColorC", colorC)
              .SetParameter("uColorD", colorD)
              .SetParameter("uColorPhase", (float)(seconds * ColorFlowSpeed % 1.0))
              .SetParameter("uTextLeft", Padding / rtWidth)
              .SetParameter("uTextRight", (Padding + textPixels) / rtWidth)
              .SetParameter("uScrollPhase", (float)(seconds * ScrollSpeed % 1.0))
              .SetParameter("uQuadSize", quadSize)
              .SetParameter("uBandRadius", bandRadius * 0.5f)
              .SetParameter("uHighlight", Highlight)
              .SetParameter("uErosion", 0)
              .SetParameter("uEdgeWidth", EdgeWidth)
              .SetParameter("uHaloOpacity", HaloOpacity);

        MonoPrim.RenderQuad(lease.Target, center, Vector2.One, 0f, Color.White, shader, BlendState.AlphaBlend,
            MonoGraphicsSpace.Screen, device.Viewport.Width, device.Viewport.Height);

        spriteBatch.ResetToDefaultUI(end: false);

        return true;
    }
}
