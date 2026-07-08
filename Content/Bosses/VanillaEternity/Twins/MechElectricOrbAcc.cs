using Terraria;
using FargowiltasSouls.Content.Projectiles.Masomode;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    public class MechElectricOrbAcc : MechElectricOrb
    {
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Masomode/MechElectricOrb";
        public override void AI()
        {
            base.AI();
            if (++Projectile.ai[1] < 75) //straight accel
                Projectile.velocity *= Main.getGoodWorld ? 1.07f : 1.06f;
        }
    }
}
