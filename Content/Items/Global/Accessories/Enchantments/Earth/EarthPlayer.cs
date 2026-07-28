using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Earth
{
    public class EarthPlayer : ModPlayer
    {
        public bool PModeChangeApply => PModeWorldSavingSystem.PhantasmMode;
        public float TiEnergy = 0;
        public float MaxTiEnergy = 600;
        public int TiChargeTime = 0;
        public List<Projectile> TiList => Main.projectile.Where(p => p.TypeAlive(ModContent.ProjectileType<TiRitualFragmentsProj>()) && p.owner == Main.myPlayer).ToList();
        public bool PrepareForTi => TiList.Count <= 0;
    }
}
