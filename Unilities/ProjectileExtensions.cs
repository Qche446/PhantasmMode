using Microsoft.Xna.Framework;
using Terraria;

namespace FargosPhantasmMode.Utilities
{
    public static class ProjectileExtensions
    {
        public static Vector2 SafeDirectionTo(this Projectile projectile, Vector2 target)
        {
            Vector2 direction = target - projectile.Center;

            if (direction == Vector2.Zero)
                return Vector2.UnitX;

            direction.Normalize();
            return direction;
        }

        public static Vector2 SafeDirectionTo(this Projectile projectile, NPC target)
            => projectile.SafeDirectionTo(target.Center);

        public static Vector2 SafeDirectionTo(this Projectile projectile, Player target)
            => projectile.SafeDirectionTo(target.Center);
    }
}