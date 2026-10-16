namespace Monochrome.Core.Graphics.Particles.Builtin;

/// <summary>
/// 软光点：加法混合的一团光，越老越小、越淡。适合做"发光点 / 能量球 / 命中闪"。
/// <para>尺寸用像素给（<see cref="StartSize"/> → <see cref="EndSize"/>），不必换算成 64 像素贴图的倍数。</para>
/// </summary>
public class MonoGlow : MonoParticle
{
    /// <inheritdoc/>
    public override Texture2D Texture => MonoParticleTextures.Soft;

    /// <inheritdoc/>
    public override BlendState BlendState => BlendState.Additive;

    /// <summary>出生时的直径（像素）。</summary>
    public float StartSize = 40f;

    /// <summary>消失时的直径（像素）。</summary>
    public float EndSize = 8f;

    /// <summary>出生时的完全不透明度。</summary>
    public float MaxOpacity = 1f;

    /// <inheritdoc/>
    public override void Update()
    {
        float alive = 1f - LifetimeRatio;
        Opacity = MaxOpacity * alive;
        Scale = Vector2.One * (MathHelper.Lerp(EndSize, StartSize, alive) / MonoParticleTextures.SoftSize);
    }
}
