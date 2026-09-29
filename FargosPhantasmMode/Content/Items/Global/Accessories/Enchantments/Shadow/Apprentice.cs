using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow
{
    public class Apprentice : PModeGlobalEnchant<ApprenticeEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.FargoSouls().DarkArtistEnchantActive = true;
            player.AddEffect<DarkArtistMinion>(item);
        }
    }
}
