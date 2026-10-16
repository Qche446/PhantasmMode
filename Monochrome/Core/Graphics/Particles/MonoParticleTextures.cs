namespace Monochrome.Core.Graphics.Particles;

/// <summary>
/// 内置粒子用的贴图。<b>不用外部素材，加载时按程序生成一张软圆</b>——于是这四个现成粒子在任何工程里
/// 都能直接跑，不必先准备 PNG。
/// <para>
/// 软圆的 alpha 从圆心到边缘用 smoothstep 落到 0，所以放大之后边缘也不会出现明显的方角。
/// <b>它是共享的</b>：内置粒子只读它，不要改它的像素；要自己的贴图就在派生类里覆写
/// <see cref="MonoParticle.Texture"/>。
/// </para>
/// <para>专用服务器上没有图形设备，访问会抛异常——内置粒子本身在专用服务器上不会被创建。</para>
/// </summary>
public static class MonoParticleTextures
{
    /// <summary>软圆的边长（像素）。够大才不会在放大时看出锯齿。</summary>
    public const int SoftSize = 64;

    private static Texture2D? soft;

    /// <summary>程序生成的软圆：中心不透明，边缘平滑透明。</summary>
    public static Texture2D Soft => soft ??= BuildSoft();

    /// <summary>贴图建好了吗。诊断与用例会看它。</summary>
    public static bool IsBuilt => soft is not null;

    /// <summary>丢掉生成的贴图。由粒子管理器在卸载时调用，释放会排到主线程。</summary>
    public static void Dispose()
    {
        // 先把实例取出来再排队：排队期间若又访问了 Soft，字段已经是新的那份了。
        Texture2D? texture = soft;
        soft = null;
        MonoGraphicsDisposal.OnMainThread(texture);
    }

    private static Texture2D BuildSoft()
    {
        GraphicsDevice device = Main.instance.GraphicsDevice;
        Color[] pixels = new Color[SoftSize * SoftSize];
        float half = SoftSize * 0.5f;

        for (int y = 0; y < SoftSize; y++)
        {
            for (int x = 0; x < SoftSize; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float distance = MathF.Sqrt(dx * dx + dy * dy);
                float alpha = Math.Clamp(1f - distance, 0f, 1f);

                // smoothstep：线性衰减的边缘在放大之后能看出同心圆，这样落得自然一些。
                alpha = alpha * alpha * (3f - 2f * alpha);

                // 直通 alpha（RGB 保持白）：配合 AlphaBlend 才不会把边缘压暗两次。
                pixels[y * SoftSize + x] = new Color(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        Texture2D texture = new(device, SoftSize, SoftSize);
        texture.SetData(pixels);
        return texture;
    }
}
