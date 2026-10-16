namespace Monochrome.Core.Graphics.Particles.Builtin;

/// <summary>
/// 烟：越老越大、越淡，边飘边往上浮，还带一点自转。用的是普通 alpha 混合（烟不是发光体）。
/// </summary>
public class MonoSmoke : MonoParticle
{
    /// <inheritdoc/>
    public override Texture2D Texture => MonoParticleTextures.Soft;

    /// <summary>出生时的直径（像素）。</summary>
    public float StartSize = 20f;

    /// <summary>消失时的直径（像素）。</summary>
    public float EndSize = 72f;

    /// <summary>每帧的速度保留比例。</summary>
    public float Drag = 0.96f;

    /// <summary>每帧加到竖直速度上的上浮加速度（负值往上）。</summary>
    public float Rise = -0.04f;

    /// <summary>出生时的最大不透明度。烟通常给 0.3–0.5。</summary>
    public float MaxOpacity = 0.45f;

    /// <inheritdoc/>
    public override void Update()
    {
        Velocity *= Drag;
        Velocity.Y += Rise;
        RotationSpeed *= 0.99f;

        Opacity = MaxOpacity * (1f - LifetimeRatio);
        Scale = Vector2.One * (MathHelper.Lerp(StartSize, EndSize, LifetimeRatio) / MonoParticleTextures.SoftSize);
    }
}
