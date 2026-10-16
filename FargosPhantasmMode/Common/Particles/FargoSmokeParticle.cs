using Monochrome.Core.Graphics.Particles;
using XnaColor = Microsoft.Xna.Framework.Color;

using Monochrome.Common.MonoUtil;
namespace FargosPhantasmMode.Common.Particles
{
    /// <summary>
    /// 烟雾粒子：8 种变体 × 3 个横向阶段的小方格序列，随时间变大、变白，撞到实体方块会加速消散。
    /// 行为与 FSS 的同名粒子一致。
    /// </summary>
    public class FargoSmokeParticle : MonoParticle
    {
        private const int Variants = 8;
        private const int CellSize = 28;

        private readonly int variant;
        private readonly float scaleGrowRate;
        private readonly float rotationOffset;
        private XnaColor drawColor;

        public override Texture2D Texture => FargoParticleAtlas.Texture;
        public override BlendState BlendState => BlendState.NonPremultiplied;

        public FargoSmokeParticle(Vector2 position, Vector2 velocity, XnaColor color, int lifetime, float scale, float scaleGrowRate)
        {
            Position = position;
            Velocity = velocity;
            drawColor = color;
            Scale = Vector2.One * scale;
            variant = Main.rand.Next(Variants);
            Lifetime = lifetime;
            this.scaleGrowRate = scaleGrowRate;
            rotationOffset = -MathHelper.PiOver2 + Main.rand.NextFloatDirection() * 0.51f;
        }

        public override void Update()
        {
            Rotation = Velocity.ToRotation() + rotationOffset;
            Velocity *= 0.89f;
            Scale += Vector2.One * LifetimeRatio * scaleGrowRate;

            int area = (int)(Scale.X * 50f);
            if (Collision.SolidCollision(Position - Vector2.One * area * 0.5f, area, area))
            {
                Time += 9;
                Velocity *= 0.75f;
            }

            drawColor = XnaColor.Lerp(drawColor, XnaColor.White, 0.055f);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture is null)
                return;

            float opacity = MonoUtil.Plateau(LifetimeRatio, 0f, 0.02f, 0.4f, 1f) * 0.75f;
            int horizontalFrame = (int)MathF.Round(MathHelper.Lerp(0f, 2f, LifetimeRatio));
            Rectangle atlasFrame = FargoParticleAtlas.Frame("SmokeParticle");

            // 这一格在"图集内的这张贴图"里的位置，再按贴图自身在图集里的偏移换算成图集坐标。
            int localX = CellSize * horizontalFrame;
            int localY = CellSize * variant;
            float x = MathHelper.Clamp(localX, 0f, atlasFrame.Width - CellSize);
            float y = MathHelper.Clamp(localY, 0f, atlasFrame.Height - CellSize);
            Rectangle cell = new((int)(atlasFrame.X + x), (int)(atlasFrame.Y + y), CellSize, CellSize);

            spriteBatch.Draw(Texture, Position - Main.screenPosition, cell, drawColor * opacity, Rotation, Vector2.Zero, Scale * 2f, SpriteEffects.None, 0f);
        }
    }
}
