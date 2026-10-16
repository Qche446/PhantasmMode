using Monochrome.Core.Graphics.Particles;
using XnaColor = Microsoft.Xna.Framework.Color;

using FargowiltasSouls;
namespace FargosPhantasmMode.Common.Particles
{
    /// <summary>
    /// 膨胀光斑：从 <paramref name="startScale"/> 平滑放大到 <paramref name="endScale"/>，同时淡出。
    /// 行为与 FSS 的同名粒子一致，包括"颜色 alpha 清零、只靠贴图与混合出形状"这一点。
    /// </summary>
    public class FargoBloomParticle : MonoParticle
    {
        private readonly Vector2 startScale;
        private readonly Vector2 endScale;
        private readonly XnaColor bloomColor;
        private readonly bool useBloom;

        public override Texture2D Texture => FargoParticleAtlas.Texture;
        public override Rectangle? Frame => FargoParticleAtlas.Frame("Bloom");

        /// <summary>贴图按 100×100 设计，所以绘制缩放是 <see cref="MonoParticle.Scale"/> 的三成。</summary>
        private Vector2 DrawScale => Scale * 0.3f;

        public FargoBloomParticle(Vector2 position, Vector2 velocity, XnaColor drawColor, Vector2 startScale, Vector2 endScale, int lifetime, bool useExtraBloom = false, XnaColor? extraBloomColor = null)
        {
            Position = position;
            Velocity = velocity;
            Color = drawColor;
            Scale = this.startScale = startScale;
            this.endScale = endScale;
            Lifetime = lifetime;
            useBloom = useExtraBloom;
            bloomColor = extraBloomColor ?? XnaColor.White;
        }

        public override void Update()
        {
            Opacity = MathHelper.Lerp(1f, 0f, FargoSoulsUtil.SineInOut(LifetimeRatio));
            Scale = Vector2.Lerp(startScale, endScale, FargoSoulsUtil.SineInOut(LifetimeRatio));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture is null)
                return;

            Vector2 screen = Position - Main.screenPosition;
            spriteBatch.Draw(Texture, screen, Frame, (Color with { A = 0 }) * Opacity, Rotation, Vector2.Zero, DrawScale, SpriteEffects.None, 0f);

            if (useBloom)
                spriteBatch.Draw(Texture, screen, Frame, (bloomColor with { A = 0 }) * 0.4f * Opacity, Rotation, Vector2.Zero, DrawScale * 0.66f, SpriteEffects.None, 0f);
        }
    }
}
