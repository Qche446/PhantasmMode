using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;


namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Earth
{
    public class EarthPlayer : ModPlayer
    {
        public static bool PModeChangeApply => PModeWorldSavingSystem.PhantasmMode;
        public float TiEnergy = 0;
        public float MaxTiEnergy = 600;
        public int TiChargeTime = 0;
        public List<Projectile> TiList => [.. Main.projectile.Where(p => p.TypeAlive(ModContent.ProjectileType<TiRitualFragmentsProj>()) && p.owner == Main.myPlayer)];
        public bool PrepareForTi => TiList.Count <= 0;
    }
}
