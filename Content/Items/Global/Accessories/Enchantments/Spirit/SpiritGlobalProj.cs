using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit
{
    public class SpiritGlobalProj : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public override GlobalProjectile NewInstance(Projectile target)
        {
            return PModeWorldSavingSystem.PhantasmMode ? base.NewInstance(target) : null;
        }
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            Player player = Main.player[projectile.owner];
            if (player == null || !player.HasEffect<TikiMinLimitEffect>())
                return;
            if (projectile.aiStyle == ProjAIStyleID.Whip)
            {
                float MinLimit = player.ForceEffect<TikiMinLimitEffect>() ? 2.4f : 2f;
                float num = projectile.WhipSettings.RangeMultiplier * player.whipRangeMultiplier;
                if (num < MinLimit)
                {
                    projectile.WhipSettings.RangeMultiplier = MinLimit / player.whipRangeMultiplier;
                }
            }
        }
    }
}
