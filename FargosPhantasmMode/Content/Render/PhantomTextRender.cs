using System.Collections.Generic;
using Monochrome.Core.Graphics;
using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Graphics.Text;
using Monochrome.Core.Graphics.Trails;
using ReLogic.Graphics;
using Terraria.GameContent;

namespace FargosPhantasmMode.Content.Render;
public static class PhantomTextRender
{
    //青
    public static readonly Color Cyan = new(96, 238, 255);
    
    //蓝
    public static readonly Color Blue = new(78, 96, 255);

    // 时间参数，单位秒。
    private const float PulsePeriod = 2.6f;

    // 闪现事件：编号 k 的事件在 FlashStart(k) 开始、持续 FlashDuration(k) 秒。
    // 间隔 = FlashPeriod ± 抖动，所以节奏不再等距；抖动不到半个周期，保证编号单调、不会乱序。
    private const float FlashPeriod = 1.5f;
    private const float FlashJitter = 0.48f;
    private const float FlashMinDuration = 0.09f;
    private const float FlashMaxDuration = 0.26f;
    private const int FlashProbeRange = 2;
    private const int MaxActiveFlashes = 2;

    //单轮错位的最大距离
    private const float JumpAmplitude = 2.6f;

    // 各层强度。
    private const float WhiteLift = 0.35f;
    private const int GlowLayers = 3;
    private const float GlowOffset = 1.6f;
    private const float GlowOpacity = 0.24f;
    private const int MaxGhostsDrawn = 4;
    private const float GhostOpacity = 0.55f;

    /// <summary>每一轮幻影的数量分档：小于这个值是 1 枚，再小于下一档是 2 枚，其余 3 枚。</summary>
    private const float GhostPairChance = 0.45f;
    private const float GhostTripleChance = 0.78f;

    // 丝带参数。
    private const int RibbonCount = 3;
    private const int RibbonPoints = 28;
    private const float RibbonThickness = 7f;
    private const float RibbonAmplitude = 2.2f;
    private const float RibbonSpread = 4.5f;
    private const float RibbonOpacity = 0.55f;
    private const float RibbonDrift = 0.35f;
    private const float RibbonWaves = 1.6f;

    private static readonly List<Vector2> ribbonScratch = [];

    private static readonly int[] activeFlashes = new int[MaxActiveFlashes];

    private static readonly Func<float, float> RibbonWidth = static t => RibbonThickness * MathF.Sin(t * MathF.PI);
    public static bool Draw(DrawableTooltipLine line, Color color1, Color color2)
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

        double seconds = Main.GlobalTimeWrappedHourly;
        Color mood = Color.Lerp(color1, color2, Pulse(seconds));
        int flashCount = CollectActiveFlashes(seconds, activeFlashes);
        Vector2 jump = CombinedJump(flashCount);

        SpriteBatch spriteBatch = Main.spriteBatch;

        //丝带绘制
        spriteBatch.End();
        DrawRibbons(at.X - 10, at.X + size.X + 42, at.Y + size.Y + 1.5f, mood, seconds);

        Vector2 textoffset = new(10, 0);
        Vector2 baseOffset = 1.5f * jump + 0.8f * textoffset;
        //光晕与幻影
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, Main.UIScaleMatrix);
        DrawGlow(spriteBatch, line.Text, at + baseOffset - 0.5f * jump, textStyle, mood, seconds);
        DrawGhosts(spriteBatch, line.Text, at, baseOffset, seconds, flashCount, textStyle, mood);

        //稳定字形
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);
        textStyle.Color = Color.Lerp(mood, Color.White, WhiteLift);
        MonoText.Draw(spriteBatch, line.Text, at + textoffset, textStyle);

        return true;
    }

    /// <summary>沿文字下沿铺几条横向正弦丝带，相位随时间缓慢漂移。</summary>
    /// <param name="left">文字左边界。</param>
    /// <param name="right">文字右边界。</param>
    /// <param name="bottom">丝带基准线（文字下沿略往下一点）。</param>
    /// <param name="mood">当前脉动到的那一档颜色。</param>
    /// <param name="seconds">动画时间（秒）。</param>
    private static void DrawRibbons(float left, float right, float bottom, Color mood, double seconds)
    {
        if (RibbonCount <= 0 || right - left < 2f)
            return;

        var shader = MonoTrail.ResolveShader();
        var texture = shader is null ? null : MonoTrailTextures.Streak;

        for (int i = 0; i < RibbonCount; i++)
        {
            FillRibbon(left, right, bottom - 15, i, seconds);
            if (ribbonScratch.Count < 2)
                continue;

            Color color = mood * (RibbonOpacity * (1f - i * 0.25f));

            MonoPrim.RenderTrail(
                ribbonScratch,
                new MonoPrimitiveSettings(
                    RibbonWidth,
                    _ => color,
                    OffsetFunction: null,
                    Smoothen: true,
                    Shader: shader,
                    BlendState: BlendState.Additive,
                    Space: MonoGraphicsSpace.Screen,
                    Texture: texture,
                    Sampler: SamplerState.LinearWrap));
        }
    }

    /// <summary>算出第 <paramref name="index"/> 条丝带的路径点，写进 <see cref="ribbonScratch"/>。</summary>
    private static void FillRibbon(float left, float right, float bottom, int index, double seconds)
    {
        ribbonScratch.Clear();

        float middle = (RibbonCount - 1) * 0.5f;
        float spread = (index - middle) * RibbonSpread;
        float phase = index * 2.1f;
        float drift = (float)(seconds * RibbonDrift) * MathHelper.TwoPi;

        for (int i = 0; i < RibbonPoints; i++)
        {
            float t = i / (float)(RibbonPoints - 1);
            float x = MathHelper.Lerp(left, right, t);
            float wave = MathF.Sin(t * MathHelper.TwoPi * RibbonWaves + drift + phase);
            ribbonScratch.Add(new Vector2(x, bottom + spread + wave * RibbonAmplitude));
        }
    }

    /// <summary>在同一段文字上绕一圈微偏移画几遍，读出"字在发光"的底噪。</summary>
    private static void DrawGlow(SpriteBatch spriteBatch, string text, Vector2 at, in MonoTextStyle baseStyle,
        Color mood, double seconds)
    {
        MonoTextStyle glow = baseStyle;
        glow.Outline = false;
        glow.Shadow = false;
        glow.Color = mood * GlowOpacity * 4f;

        for (int i = 0; i < GlowLayers; i++)
        {
            float angle = (float)(seconds * 0.9) + i * MathHelper.TwoPi / GlowLayers;
            Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * GlowOffset * 2f;
            MonoText.Draw(spriteBatch, text, at + offset, glow);
        }
    }

    /// <summary>
    /// 闪现窗口内画幻影副本：每一轮的方向、数量与时长都不同，两轮挨得近时会同时出现。
    /// </summary>
    /// <param name="spriteBatch">已经开了加法批次的批次。</param>
    /// <param name="text">文字。</param>
    /// <param name="at">文字左上角。</param>
    /// <param name="baseOffset">幻影的公共基准偏移（半截错位加右移量）。</param>
    /// <param name="seconds">动画时间（秒）。</param>
    /// <param name="flashCount">本帧活跃的闪现轮数。</param>
    /// <param name="baseStyle">稳定字形的样式，这里只改颜色与描边。</param>
    /// <param name="mood">当前脉动到的那一档颜色。</param>
    private static void DrawGhosts(SpriteBatch spriteBatch, string text, Vector2 at, Vector2 baseOffset,
        double seconds, int flashCount, in MonoTextStyle baseStyle, Color mood)
    {
        if (flashCount <= 0)
            return;

        MonoTextStyle ghost = baseStyle;
        ghost.Outline = false;
        ghost.Shadow = false;

        int drawn = 0;
        for (int i = 0; i < flashCount && drawn < MaxGhostsDrawn; i++)
        {
            int index = activeFlashes[i];
            float envelope = FlashEnvelope(seconds, index);
            if (envelope <= 0f)
                continue;

            int count = Math.Min(GhostsFor(index), MaxGhostsDrawn - drawn);
            for (int g = 0; g < count; g++)
            {
                float spread = 1.8f + 0.6f * g;
                ghost.Color = mood * (GhostOpacity * envelope * (1f - 0.2f * g)) * 2f;
                MonoText.Draw(spriteBatch, text, at + (baseOffset + FlashOffset(index, g + 1)) * spread, ghost);
                drawn++;
            }
        }
    }

    /// <summary>颜色脉动，返回值落在 <c>[0,1]</c>：0 是青、1 是蓝。</summary>
    private static float Pulse(double seconds)
    {
        float t = (float)(seconds % PulsePeriod) / PulsePeriod;
        return 0.5f + 0.5f * MathF.Sin(t * MathHelper.TwoPi);
    }

    /// <summary>
    /// 收集此刻活跃的闪现事件编号，返回条数（最多 <see cref="MaxActiveFlashes"/> 条）。
    /// <para>抖动幅度不到半个周期，所以活跃编号一定落在"按平均周期算出的编号"附近
    /// ±<see cref="FlashProbeRange"/> 之内，不必从头扫。</para>
    /// </summary>
    private static int CollectActiveFlashes(double seconds, int[] destination)
    {
        int center = (int)(seconds / FlashPeriod);
        int count = 0;

        for (int k = center - FlashProbeRange; k <= center + FlashProbeRange; k++)
        {
            if (count >= destination.Length)
                break;

            if (k < 0)
                continue;

            float age = (float)seconds - FlashStart(k);
            if (age > 0f && age < FlashDuration(k))
                destination[count++] = k;
        }

        return count;
    }

    /// <summary>事件编号 k 的起始时刻：平均周期加上一个不到半周期的抖动，间隔因此长短不一。</summary>
    private static float FlashStart(int index)
        => index * FlashPeriod + (Hash01(index * 5 + 1) - 0.5f) * (2f * FlashJitter * FlashPeriod);

    /// <summary>事件编号 k 的持续时长，短促与略长的混着来。</summary>
    private static float FlashDuration(int index)
        => MathHelper.Lerp(FlashMinDuration, FlashMaxDuration, Hash01(index * 5 + 2));

    /// <summary>事件编号 k 在 <paramref name="seconds"/> 处的包络；不在窗口内时为 0。</summary>
    private static float FlashEnvelope(double seconds, int index)
    {
        float duration = FlashDuration(index);
        float age = (float)seconds - FlashStart(index);
        return age <= 0f || age >= duration ? 0f : MathF.Sin(age / duration * MathF.PI);
    }

    /// <summary>事件编号 k 的第 <paramref name="salt"/> 枚偏移量，方向与距离都从编号推出来。</summary>
    private static Vector2 FlashOffset(int index, int salt)
    {
        float angle = Hash01(index * 13 + salt * 7 + 5) * MathHelper.TwoPi;
        float distance = (0.45f + 0.55f * Hash01(index * 13 + salt * 7 + 6)) * JumpAmplitude;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
    }

    /// <summary>事件编号 k 这一轮同时出几枚幻影：一枚是常态，偶尔两枚，少数时候三枚。</summary>
    private static int GhostsFor(int index)
    {
        float roll = Hash01(index * 13 + 41);
        if (roll < GhostPairChance)
            return 1;
        return roll < GhostTripleChance ? 2 : 3;
    }

    /// <summary>活跃轮次的错位合成，长度夹在 <see cref="JumpAmplitude"/> 之内。</summary>
    private static Vector2 CombinedJump(int flashCount)
    {
        Vector2 total = Vector2.Zero;
        for (int i = 0; i < flashCount; i++)
            total += FlashOffset(activeFlashes[i], 0);

        return total.LengthSquared() > JumpAmplitude * JumpAmplitude
            ? Vector2.Normalize(total) * JumpAmplitude
            : total;
    }

    /// <summary>由整数取一个确定的 <c>[0,1)</c>，让每一轮闪现的方向与幅度都不同、又不依赖任何状态。</summary>
    private static float Hash01(int value)
    {
        uint x = (uint)value * 747796405u + 2891336453u;
        uint word = ((x >> (int)((x >> 28) + 4)) ^ x) * 277803737u;
        word = (word >> 22) ^ word;
        return (word & 0xFFFFFF) / 16777216f;
    }
}
