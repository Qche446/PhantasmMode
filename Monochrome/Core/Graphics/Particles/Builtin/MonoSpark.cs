namespace Monochrome.Core.Graphics.Particles.Builtin;

/// <summary>
/// 火花：小而亮的点，带一点重力与阻尼，越老越小。适合做火星、碎屑、弹着点。
/// </summary>
public class MonoSpark : MonoParticle
{
    /// <inheritdoc/>
    public override Texture2D Texture => MonoParticleTextures.Soft;

    /// <inheritdoc/>
    public override BlendState BlendState => BlendState.Additive;

    /// <summary>出生时的直径（像素）。</summary>
    public float StartSize = 14f;

    /// <summary>消失时的直径（像素）。</summary>
    public float EndSize = 2f;

    /// <summary>每帧加到竖直速度上的重力。</summary>
    public float Gravity = 0.12f;

    /// <summary>每帧的速度保留比例。</summary>
    public float Drag = 0.985f;

    /// <inheritdoc/>
    public override void Update()
    {
        Velocity *= Drag;
        Velocity.Y += Gravity;

        float alive = 1f - LifetimeRatio;
        Opacity = alive;
        Scale = Vector2.One * (MathHelper.Lerp(EndSize, StartSize, alive) / MonoParticleTextures.SoftSize);
    }
}
