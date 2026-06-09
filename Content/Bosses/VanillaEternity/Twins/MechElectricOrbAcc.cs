using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls.Content.Projectiles.Masomode;
using System.Threading;
using FargowiltasSouls.Core.Systems;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Twins
{
    public class MechElectricOrbAcc : MechElectricOrb
    {
        float timer = 0;
        public override string Texture => "FargowiltasSouls/Content/Projectiles/Masomode/MechElectricOrb";
        public override void AI()
        {
            base.AI();
            if (++timer < 75) //straight accel
                Projectile.velocity *= WorldSavingSystem.MasochistModeReal ? 1.06f : 1.05f;
        }
    }
}
