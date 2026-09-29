using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Content.Projectiles.Minions;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Will
{
    public class WillGlobalProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            if (!PModeWorldSavingSystem.PhantasmMode)
                return;
            if (proj.type == ModContent.ProjectileType<GladiatorSpirit>())
            {
                proj.extraUpdates = 1;
            }
        }
    }
}
