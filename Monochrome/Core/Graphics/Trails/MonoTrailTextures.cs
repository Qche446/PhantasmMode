namespace Monochrome.Core.Graphics.Trails;

/// <summary>
/// 拖尾用的贴图。<b>与粒子那套一样，不用外部素材，加载时按程序生成</b>。
/// <para>
/// <b>方向。</b>在 <c>StreakTrail.fx</c> 的采样里，贴图的 <b>X 轴对应拖尾的长度</b>、
/// <b>Y 轴对应拖尾的截面</b>。所以"顺着拖尾走的光"必须沿贴图的 X 轴延伸，画成竖条就成了横跨拖尾的横杠。
/// </para>
/// <para>
/// <b>尺度。</b>着色器把贴图的<b>整幅高度铺满条带</b>（<c>uSectionTiles = 1</c>），所以这里的纵向剖面
/// 就是条带上看到的截面形状；横向的细颗粒沿长度滚动，读起来是"细碎的光在流动"。
/// 只画一条低频正弦的话，看到的就是一段没来由的明暗渐变——条带本身很窄，低频内容在它上面读不出形状。
/// </para>
/// <para>
/// 参考素材：FargowiltasSouls 的 <c>Assets/ExtraTextures/Trails/FadedStreak.png</c>（移植时对着它看的）。
/// </para>
/// </summary>
public static class MonoTrailTextures
{
    /// <summary>条纹贴图的边长（像素）。必须是 2 的幂——D3D9 的 wrap 寻址只对 2 的幂可靠。</summary>
    public const int StreakSize = 128;

    private static Texture2D? streak;

    /// <summary>程序生成的渐隐条纹：只有红色通道有内容，着色器读的就是它。</summary>
    public static Texture2D Streak => streak ??= BuildStreak();

    /// <summary>贴图建好了吗。</summary>
    public static bool IsBuilt => streak is not null;

    /// <summary>丢掉生成的贴图。由拖尾系统的卸载钩子调用，释放会排到主线程。</summary>
    public static void Dispose()
    {
        // 先把实例取出来再排队：排队期间若又访问了 Streak，字段已经是新的那份了。
        Texture2D? texture = streak;
        streak = null;
        MonoGraphicsDisposal.OnMainThread(texture);
    }

    private static Texture2D BuildStreak()
    {
        Color[] pixels = new Color[StreakSize * StreakSize];

        for (int y = 0; y < StreakSize; y++)
        {
            // v ∈ [-0.5, 0.5]，0 是截面中线。着色器把整幅高度铺满条带，所以这个剖面就是条带的截面形状。
            float v = (y + 0.5f) / StreakSize - 0.5f;

            // 截面剖面：中线 1、两侧 0 的平滑包络。
            float profile = MathF.Pow(Math.Clamp(1f - MathF.Abs(v) * 2f, 0f, 1f), 1.2f);

            for (int x = 0; x < StreakSize; x++)
            {
                float u = (x + 0.5f) / StreakSize;

                // 横向颗粒：几条整数周期的正弦叠出不规则的高频纹理。它沿条带长度滚动，
                // 读起来是"细碎的光在流动"——只放一个低频正弦的话，看到的就是一段没来由的明暗渐变。
                // 整数周期保证左右首尾相接，平铺无缝。
                float grain = 0.5f + 0.5f * (
                    0.5f * MathF.Sin(u * MathHelper.TwoPi * 7f) +
                    0.3f * MathF.Sin(u * MathHelper.TwoPi * 11f + 1.3f) +
                    0.2f * MathF.Sin(u * MathHelper.TwoPi * 3f + 0.7f));

                byte level = (byte)(Math.Clamp(profile * (0.55f + 0.45f * grain), 0f, 1f) * 255f);
                pixels[y * StreakSize + x] = new Color(level, level, level, (byte)255);
            }
        }

        Texture2D texture = new(Main.instance.GraphicsDevice, StreakSize, StreakSize);
        texture.SetData(pixels);
        return texture;
    }
}
