using Monochrome.Core.Graphics.Particles;
using XnaColor = Microsoft.Xna.Framework.Color;

using FargowiltasSouls;
namespace FargosPhantasmMode.Common.Particles
{
    /// <summary>
    /// 电火花：贴图纵向分成 5 帧，每次绘制随机取一帧，所以看着像跳动的电弧。行为与 FSS 的同名粒子一致。
    /// </summary>
    public class FargoElectricSparkParticle : MonoParticle
    {
        private const int LightningFrames = 5;

        private readonly XnaColor bloomColor;
        private readonly bool useBloom;

        public override Texture2D Texture => FargoParticleAtlas.Texture;
        public override BlendState BlendState => BlendState.Additive;

        /// <summary>整张贴图在图集里的矩形；绘制时再按每帧高度切。</summary>
        private Rectangle AtlasFrame => FargoParticleAtlas.Frame("ElectricSpark");

        public FargoElectricSparkParticle(Vector2 worldPosition, Vector2 velocity, XnaColor drawColor, float scale, int lifetime, bool useBloom = true, XnaColor? bloomColor = null)
        {
            Position = worldPosition;
            Velocity = velocity;
            Color = drawColor;
            Scale = new Vector2(scale);
            Lifetime = lifetime;
            this.useBloom = useBloom;
            this.bloomColor = bloomColor ?? XnaColor.White;
        }

        public override void Update()
        {
            Velocity *= 0.95f;
            Scale *= 0.95f;
            Opacity = FargoSoulsUtil.SineInOut(1f - LifetimeRatio);
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture is null)
                return;

            Rectangle atlasFrame = AtlasFrame;
            int frameHeight = atlasFrame.Height / LightningFrames;
            int frame = Main.rand.Next(LightningFrames);
            Rectangle lightningFrame = new(atlasFrame.X, atlasFrame.Y + frameHeight * frame, atlasFrame.Width, frameHeight);

            Vector2 screen = Position - Main.screenPosition;

            spriteBatch.Draw(Texture, screen, lightningFrame, Color * Opacity, Rotation, Vector2.Zero, Scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(Texture, screen, lightningFrame, Color * Opacity, Rotation, Vector2.Zero, Scale * new Vector2(0.45f, 1f), SpriteEffects.None, 0f);

            if (useBloom)
                spriteBatch.Draw(Texture, screen, lightningFrame, bloomColor * 0.5f * Opacity, Rotation, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
    }
}
