using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Pure
{
    public class GelicWingsOverride : PModeGlobalMasoItem<GelicWings>
    {

    }
    public class GelicWingSpikeEffect : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.type == ModContent.ProjectileType<GelicWingSpike>() && PModeWorldSavingSystem.PhantasmMode)
            {
                target.AddBuff(ModContent.BuffType<FlamesoftheUniverseBuff>(), 60);
                target.AddBuff(BuffID.Oiled, 240);
            }
            base.OnHitNPC(projectile, target, hit, damageDone);
        }
    }
}
