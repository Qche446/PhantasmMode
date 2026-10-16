namespace Monochrome.Core.Graphics.Particles.Builtin;

/// <summary>
/// 光条：把软圆沿速度方向拉长，看起来像一道拖尾。适合做飞行物尾迹、斩击余光。
/// <para>它每帧按当前速度重算朝向与长度，所以<b>速度改成 0 会缩成一小团</b>——那样最好直接 <c>Kill()</c>。</para>
/// </summary>
public class MonoStreak : MonoParticle
{
    /// <inheritdoc/>
    public override Texture2D Texture => MonoParticleTextures.Soft;

    /// <inheritdoc/>
    public override BlendState BlendState => BlendState.Additive;

    /// <summary>条的粗细（像素）。</summary>
    public float Thickness = 5f;

    /// <summary>长度 = 速度 × 这个倍数。</summary>
    public float LengthScale = 3f;

    /// <summary>长度的下限（像素），速度很小时不至于缩成一点。</summary>
    public float MinLength = 10f;

    /// <summary>每帧的速度保留比例。</summary>
    public float Drag = 0.97f;

    /// <inheritdoc/>
    public override void Update()
    {
        Velocity *= Drag;
        Opacity = 1f - LifetimeRatio;

        Rotation = Velocity.ToRotation();
        float length = MathF.Max(MinLength, Velocity.Length() * LengthScale);
        Scale = new Vector2(length / MonoParticleTextures.SoftSize, Thickness / MonoParticleTextures.SoftSize);
    }
}
