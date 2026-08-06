using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Content.Projectiles.Souls;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow
{
    public class ShadowGlobalProj : GlobalProjectile
    {
        public override GlobalProjectile NewInstance(Projectile target) => PModeWorldSavingSystem.PhantasmMode ? base.NewInstance(target) : null;
        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            if (proj.type == ModContent.ProjectileType<MonkDashSlash>() && source is EntitySource_Parent parent && parent.Entity is Projectile)
            {
                Player py = Main.player[proj.owner];
                py.GetModPlayer<ShadowPlayer>().ActualReduceMonoCD = true;
            }
        }
    }
}
