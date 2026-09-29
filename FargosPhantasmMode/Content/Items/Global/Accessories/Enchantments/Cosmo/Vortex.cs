using FargosPhantasmMode.Content.Projectiles;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using static Terraria.ModLoader.ModContent;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Cosmo
{
    public class Vortex : PModeGlobalEnchant<VortexEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<VortexProjGravity>(item);
        }
    }
    public class VortexProjGravity : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<CosmoHeader>();
        public override int ToggleItemType => ItemType<VortexEnchant>();
        public static List<int> blacklist = [ProjectileID.Daybreak, ProjectileType<StyxGazerArmor>()];
    }
}
