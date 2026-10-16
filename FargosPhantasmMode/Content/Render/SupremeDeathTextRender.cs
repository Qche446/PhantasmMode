using Monochrome.Core.Graphics.Text;
using ReLogic.Graphics;
using Terraria.GameContent;

namespace FargosPhantasmMode.Content.Render;

/// <summary>
/// 至尊死亡文字：字面近黑深紫并缓慢呼吸，字外围浮着向右上漂的淡紫幻影，
/// 字底下不断渗出往右上飘的紫黑雾，文字所在的区域里周期性有一段字被血浸成暗红。
/// <para>
/// <b>给谁用。</b><c>PModeRarityGlobalItem</c> 在 <c>line.Name == "ItemName"</c> 且稀有度为
/// <see cref="SupremeDeathRarity"/> 时调 <see cref="Draw"/>；它返回 <see langword="true"/>
/// 表示这一行已经自绘，调用方应跳过原版绘制，免得两层描边叠在一起。
/// </para>
/// <para>
/// <b>它依赖什么。</b>文字走 <see cref="MonoText"/>；雾是界面粒子 <see cref="SupremeDeathMist"/>，
/// 由 <c>MonoParticleManager</c> 推进与绘制，本类只负责按节奏把新的几枚放出去。
/// <b>不碰渲染目标与自定义着色器</b>：光晕与幻影是同一行多画几遍，血浸是逐字改字色，
/// 用界面批次加 <c>Main.UIScaleMatrix</c> 就能画完，没有逐像素的活。
/// </para>
/// <para>
/// <b>清晰度怎么保证。</b>稳定字形全不透明、带纯黑描边，光晕与幻影走加法混合、低透明度只做氛围；
/// 血浸画在字形之上但不动描边，所以字始终有黑边兜着。
/// </para>
/// <para>
/// <b>节奏是无状态的。</b>血浸按"编号事件加 hash"定位，幻影副本按时间取相位，
/// 一帧里画 0 次或 2 次都不会漂；雾另有一个只用来对齐时间间隔的编号游标，
/// 它不参与绘制，同一个编号重复调用不会多放。
/// </para>
/// </summary>
public static class SupremeDeathTextRender
{
    /// <summary>物品名的稀有度色：与特效同一档深紫。</summary>
    public static readonly Color RarityColor = new(95, 52, 150);

    /// <summary>主体色的暗端：近黑深紫。</summary>
    private static readonly Color DeepViolet = new(42, 19, 64);

    /// <summary>主体色的亮端：紫罗兰。</summary>
    private static readonly Color PaleViolet = new(107, 63, 160);

    /// <summary>字形底下的亮紫光晕，把深色字形从提示框底板上分出来。</summary>
    private static readonly Color UnderGlow = new(150, 96, 226);

    /// <summary>血浸的底色：暗红。</summary>
    private static readonly Color BloodDeep = new(96, 16, 38);

    /// <summary>血浸的亮端，用来让每个字有些深浅差别。</summary>
    private static readonly Color BloodBright = new(178, 34, 52);

    /// <summary>紫黑雾的颜色。提示框底板本身是暗的，雾压到近黑就看不出来了，这里偏紫一些。</summary>
    private static readonly Color MistColor = new(118, 60, 176);

    /// <summary>幻影副本漂出的方向：右上，与紫黑雾同向。</summary>
    private static readonly Vector2 GhostDirection = Vector2.Normalize(new Vector2(0.78f, -0.63f));

    /// <summary>字面呼吸的周期（秒）。</summary>
    private const float BreathePeriod = 3.2f;

    // 底光晕：绕字微偏移画几遍。
    private const int UnderGlowLayers = 3;
    private const float UnderGlowOffset = 2f;
    private const float UnderGlowOpacity = 0.40f;

    // 幻影副本：向右上漂的循环，偏移距离再叠一层周期性缩放。
    private const int GhostCount = 2;
    private const float GhostTravel = 2.6f;
    private const float GhostDistance = 10f;
    private const float GhostPulsePeriod = 3.9f;
    private const float GhostOpacity = 0.75f;

    // 紫黑雾：放雾的间隔、初速（界面粒子按帧走）与寿命（帧）。
    private const float MistPeriod = 0.12f;
    private const int MistMaxBurst = 3;
    private const int MistCatchUpLimit = 6;
    private const float MistDriftPerFrame = 0.16f;
    private const float MistRisePerFrame = 0.22f;
    private const int MistLifeMin = 70;
    private const int MistLifeMax = 120;

    // 血浸：编号事件，持续比幻影长一个量级。
    private const float BloodPeriod = 1.6f;
    private const float BloodJitter = 0.42f;
    private const float BloodMinDuration = 0.7f;
    private const float BloodMaxDuration = 1.5f;
    private const float BloodOpacity = 0.85f;
    private const float BloodJitterAmplitude = 7f;
    private const int BloodMaxActive = 2;

    /// <summary>上一次放雾用的编号，只用来对齐时间间隔。</summary>
    private static int mistCursor = -1;

    /// <summary>
    /// 画出这一行。返回 <see langword="true"/> 表示已经自绘，调用方应跳过原版绘制。
    /// <para>专用服务器、空文本，以及这一行被设了旋转或自定义原点时返回 <see langword="false"/>——
    /// 本类按"左上角落在 (X, Y)"摆放，那两种情形不在它的假设里，交给默认绘制更稳。</para>
    /// </summary>
    /// <param name="line">要画的工具提示行。</param>
    public static bool Draw(DrawableTooltipLine line)
    {
        if (Main.dedServ || string.IsNullOrEmpty(line.Text))
            return false;

        if (line.Rotation != 0f || line.Origin != Vector2.Zero)
            return false;

        DynamicSpriteFont font = line.Font ?? FontAssets.MouseText.Value;
        float scale = line.BaseScale.Y <= 0f ? 1f : line.BaseScale.Y;

        MonoTextStyle textStyle = new()
        {
            Font = font,
            Scale = scale,
            Outline = true,
            OutlineColor = Color.Black,
            Shadow = true,
            ShadowColor = Color.Black * 0.85f,
        };

        Vector2 at = new(line.X, line.Y);
        Vector2 size = MonoText.Measure(line.Text, textStyle);
        if (size.X <= 0f || size.Y <= 0f)
            return false;

        double seconds = Main.GlobalTimeWrappedHourly;
        SpriteBatch spriteBatch = Main.spriteBatch;

        // ① 氛围层：底光晕与幻影副本走加法混合，低透明度，只做氛围。
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, Main.UIScaleMatrix);
        DrawUnderGlow(spriteBatch, line.Text, at, textStyle, seconds);
        DrawGhosts(spriteBatch, line.Text, at, textStyle, seconds);
        spriteBatch.End();

        // ② 字形层：稳定字形打底，血浸随后盖在它上面——只改字色、不动描边，所以字始终有黑边兜着。
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);
        textStyle.Color = Color.Lerp(DeepViolet, PaleViolet, Breathe(seconds)) * 1.5f;
        MonoText.Draw(spriteBatch, line.Text, at, textStyle);
        DrawBloodSoak(spriteBatch, line.Text, at, size, textStyle, font, seconds);

        //SpawnMist(at, size, seconds);

        return true;
    }

    /// <summary>主体的呼吸系数：0 是近黑深紫，1 是紫罗兰，取值落在 <c>[0.25,0.7]</c>，不会压到纯黑。</summary>
    private static float Breathe(double seconds)
    {
        float t = (float)(seconds % BreathePeriod) / BreathePeriod;
        return 0.25f + 0.45f * (0.5f + 0.5f * MathF.Sin(t * MathHelper.TwoPi));
    }

    /// <summary>在同一段文字上绕一圈微偏移画几遍，读出一层贴着字形的紫光，把深色字从底板上分出来。</summary>
    private static void DrawUnderGlow(SpriteBatch spriteBatch, string text, Vector2 at, in MonoTextStyle baseStyle,
        double seconds)
    {
        MonoTextStyle glow = baseStyle;
        glow.Outline = false;
        glow.Shadow = false;
        glow.Color = UnderGlow * UnderGlowOpacity * 2f;

        for (int i = 0; i < UnderGlowLayers; i++)
        {
            float angle = (float)(seconds * 0.7) + i * MathHelper.TwoPi / UnderGlowLayers;
            Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * UnderGlowOffset;
            MonoText.Draw(spriteBatch, text, at + offset, glow);
        }
    }

    /// <summary>
    /// 幻影副本：沿右上漂出去再从头开始，越远越淡；偏移距离再乘一层缓慢的周期性缩放，
    /// 于是副本之间的距离会一收一放，不像匀速平移。
    /// </summary>
    private static void DrawGhosts(SpriteBatch spriteBatch, string text, Vector2 at, in MonoTextStyle baseStyle,
        double seconds)
    {
        MonoTextStyle ghost = baseStyle;
        ghost.Outline = false;
        ghost.Shadow = false;

        float pulse = 0.75f + 0.35f * MathF.Sin((float)(seconds * MathHelper.TwoPi / GhostPulsePeriod));

        for (int i = 0; i < GhostCount; i++)
        {
            float span = (float)(seconds / GhostTravel) + i / (float)GhostCount;
            float phase = span - MathF.Floor(span);
            float envelope = MathF.Sin(phase * MathF.PI);
            if (envelope <= 0.02f)
                continue;

            ghost.Color = PaleViolet * (GhostOpacity * envelope) * 2f;
            MonoText.Draw(spriteBatch, text, at + GhostDirection * (phase * GhostDistance * pulse), ghost);
        }
    }

    /// <summary>
    /// 血浸：编号 k 的一段血从文字范围内的某处渗开，持续 <see cref="BloodDuration"/> 秒，淡入、保持、淡出；
    /// 整段左右快速摆动，读起来像一片在动的血而不是一张贴纸。
    /// </summary>
    private static void DrawBloodSoak(SpriteBatch spriteBatch, string text, Vector2 at, Vector2 size,
        in MonoTextStyle baseStyle, DynamicSpriteFont font, double seconds)
    {
        int center = (int)(seconds / BloodPeriod);
        int drawn = 0;

        for (int k = center - BloodMaxActive - 1; k <= center + 1; k++)
        {
            if (drawn >= BloodMaxActive)
                break;
            if (k < 0)
                continue;

            float age = (float)seconds - BloodStart(k);
            float duration = BloodDuration(k);
            if (age <= 0f || age >= duration)
                continue;

            float u = age / duration;
            float fadeIn = MathHelper.SmoothStep(0f, 1f, MathF.Min(u / 0.28f, 1f));
            float fadeOut = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((u - 0.7f) / 0.3f, 0f, 1f));
            float envelope = fadeIn * fadeOut;
            if (envelope <= 0.01f)
                continue;

            float halfWidth = size.X * (0.16f + 0.20f * Hash01(k * 7 + 7));
            float centerX = at.X + size.X * (0.18f + 0.64f * Hash01(k * 7 + 3));
            float wobble = (float)seconds * 8.5f + Hash01(k * 7 + 5) * MathHelper.TwoPi;
            centerX += MathF.Sin(wobble) * BloodJitterAmplitude;

            SoakCharacters(spriteBatch, text, at, centerX, halfWidth, BloodOpacity * envelope * 2.5f, k, font, baseStyle);
            drawn++;
        }
    }

    /// <summary>
    /// 把落在 <paramref name="centerX"/> 两侧 <paramref name="halfWidth"/> 里的字逐个浸成暗红。
    /// 包络只有横向：纵向整字铺满，所以上下不淡出，收边落在字符自身的深浅上。
    /// </summary>
    private static void SoakCharacters(SpriteBatch spriteBatch, string text, Vector2 at, float centerX, float halfWidth,
        float depth, int seed, DynamicSpriteFont font, in MonoTextStyle baseStyle)
    {
        MonoTextStyle stain = baseStyle;
        stain.Outline = false;
        stain.Shadow = false;

        float x = at.X;
        for (int i = 0; i < text.Length; i++)
        {
            string glyph = text[i].ToString();
            float width = font.MeasureString(glyph).X * stain.Scale;

            float normalized = (x + width * 0.5f - centerX) / halfWidth;
            if (normalized > -1f && normalized < 1f)
            {
                float falloff = 1f - normalized * normalized;
                int salt = seed * 131 + i * 17;
                float shade = 0.75f + 0.5f * Hash01(salt);
                stain.Color = Color.Lerp(BloodDeep, BloodBright, Hash01(salt + 3)) * (depth * falloff * falloff * shade);
                MonoText.Draw(spriteBatch, glyph, new Vector2(x, at.Y), stain);
            }

            x += width;
        }
    }
    /// <summary>编号 k 的血浸出现时刻：平均周期加一个不到半周期的抖动，所以节奏不等距、编号仍单调。</summary>
    private static float BloodStart(int index)
        => index * BloodPeriod + (Hash01(index * 11 + 1) - 0.5f) * (2f * BloodJitter * BloodPeriod);

    /// <summary>编号 k 的血浸持续多久。</summary>
    private static float BloodDuration(int index)
        => MathHelper.Lerp(BloodMinDuration, BloodMaxDuration, Hash01(index * 11 + 2));

    /// <summary>由整数取一个确定的 <c>[0,1)</c>，让每一段血浸的宽度、位置与深浅都不同、又不依赖任何状态。</summary>
    private static float Hash01(int value)
    {
        uint x = (uint)value * 747796405u + 2891336453u;
        uint word = ((x >> (int)((x >> 28) + 4)) ^ x) * 277803737u;
        word = (word >> 22) ^ word;
        return (word & 0xFFFFFF) / 16777216f;
    }
}
