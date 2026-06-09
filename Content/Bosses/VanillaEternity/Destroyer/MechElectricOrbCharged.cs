using FargowiltasSouls.Content.Projectiles.Masomode;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria;
using System;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Destroyer
{
    public class MechElectricOrbCharged : MechElectricOrb
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Masomode/MechElectricOrb";
        float Timer = 0;
        public override void AI()
        {
            base.AI();
            if (++Timer >= 20)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<MechElectricOrbCharged>() && Main.projectile[i] != Projectile)
                    {
                        Vector2 Vec = Projectile.Center - Main.projectile[i].Center;
                        float distance = Vec.Length();
                        Projectile.velocity += Math.Sign(Projectile.ai[1]) * Main.projectile[i].ai[1] * Vec * (25000f / (distance * distance * distance) - 300000f / (distance * distance * distance * distance));
                    }
                }
                float speed = Projectile.velocity.Length();
                Vector2 vel = Vector2.Normalize(Projectile.velocity);
                if (speed >= 20)
                {
                    Projectile.velocity = 20 * vel;
                }
                
            }
        }

    }
}
