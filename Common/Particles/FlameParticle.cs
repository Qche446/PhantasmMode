using FargowiltasSouls;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace FargosPhantasmMode.Common.Particles
{
    public class FlameParticle : Particle
    {
        public override string AtlasTextureName => "FargowiltasSouls.FlameParticle";
        public Color BloomColor;
        public readonly bool UseBloom;
        public int Variant;
        public override int FrameCount => 6;
        public FlameParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale, bool useBloom = true, Color? bloomColor = null)
        {
            Position = position;
            Velocity = velocity;
            DrawColor = color;
            Scale = Vector2.One * scale;
            Lifetime = lifetime;
            UseBloom = useBloom;
            bloomColor ??= Color.White;
            BloomColor = bloomColor.Value;
            Variant = Main.rand.Next(FrameCount);
        }
        public override void Update()
        {
            // Shrink, fade, and slow over time.
            Velocity *= 0.95f;
            Opacity = FargoSoulsUtil.SineInOut(1f - LifetimeRatio);
            Rotation = Velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            float opacity = Utilities.InverseLerpBump(0f, 0.02f, 0.4f, 1f, LifetimeRatio) * 0.75f;
            int width = 48;
            int height = 64;
            Rectangle frame = new(0, height * Variant, width, height);

            Vector2 origin = Vector2.Zero;
            float x = MathHelper.Clamp(frame.X + Texture.Frame.X, Texture.Frame.X, Texture.Frame.X + Texture.Frame.Width - width);
            float y = MathHelper.Clamp(frame.Y + Texture.Frame.Y, Texture.Frame.Y, Texture.Frame.Y + Texture.Frame.Height - height);
            Rectangle frameOnAtlas = new((int)x, (int)y, (int)width, (int)height);

            spriteBatch.Draw(Texture.Atlas.Texture.Value, Position - Main.screenPosition, frameOnAtlas, DrawColor * opacity, Rotation, origin, Scale * 2f, 0, 0f);
        }
    }
}
