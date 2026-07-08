using FargosPhantasmMode.Common.Particles;
using FargowiltasSouls;
using Luminance.Core.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu
{
    public class MoonFireProj : ModProjectile
    {
        public override string Texture => FargoSoulsUtil.EmptyTexture;
        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.scale = 1f;
            Projectile.timeLeft = 600;
            //Projectile.hide = true;
        }
        public override bool? CanDamage() => false;
        public override void AI()
        {
            Projectile.ai[0]++;
            Projectile.velocity *= 0.98f;
            //Particle p = new MoonFire(Projectile.Center, 2 * Main.rand.NextVector2Unit(), 0.9f, 30);
            //p.Spawn();
        }
    }
}
