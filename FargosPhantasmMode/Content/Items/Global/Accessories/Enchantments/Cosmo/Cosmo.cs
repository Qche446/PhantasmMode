using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using Terraria;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Cosmo
{
    public class Cosmo : PModeGlobalEnchant<CosmoForce>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<CosmoMoonEnhanceEffect>(item);
        }
    }
    public class CosmoMoonEnhanceEffect : AccessoryEffect
    {
        public override Header ToggleHeader => null;
    }
}
