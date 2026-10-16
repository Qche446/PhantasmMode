using Monochrome.Core.Graphics.Particles;
using XnaColor = Microsoft.Xna.Framework.Color;

using FargowiltasSouls;
namespace FargosPhantasmMode.Common.Particles
{
    /// <summary>
    /// 短方块火花：可以带重力，用于火花飞溅。行为与 FSS 的同名粒子一致。
    /// </summary>
    public class FargoRectangleParticle : MonoParticle
    {
        private readonly XnaColor bloomColor;
        private readonly bool useBloom;
        private readonly bool useGravity;

        public override Texture2D Texture => FargoParticleAtlas.Texture;
        public override Rectangle? Frame => FargoParticleAtlas.Frame("RectangleParticle");
        public override BlendState BlendState => BlendState.Additive;

        public FargoRectangleParticle(Vector2 worldPosition, Vector2 velocity, XnaColor drawColor, float scale, int lifetime, bool gravity = false, bool useBloom = true, XnaColor? bloomColor = null)
        {
            Position = worldPosition;
            Velocity = velocity;
            Color = drawColor;
            Scale = new Vector2(scale);
            Lifetime = lifetime;
            useGravity = gravity;
            this.useBloom = useBloom;
            this.bloomColor = bloomColor ?? XnaColor.White;
        }

        public override void Update()
        {
            if (useGravity)
                Velocity.Y += 0.33f;

            Velocity *= 0.95f;
            Scale *= 0.95f;
            Opacity = FargoSoulsUtil.SineInOut(1f - LifetimeRatio);
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture is null)
                return;

            Vector2 screen = Position - Main.screenPosition;
            Vector2 scale = new Vector2(0.5f, 1.6f) * Scale;

            spriteBatch.Draw(Texture, screen, Frame, Color, Rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(Texture, screen, Frame, Color, Rotation, Vector2.Zero, scale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);

            if (useBloom)
                spriteBatch.Draw(Texture, screen, Frame, bloomColor * 0.5f, Rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
