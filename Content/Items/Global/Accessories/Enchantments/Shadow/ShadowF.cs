using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Terraria;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow
{
    public class ShadowF : PModeGlobalEnchant<ShadowForce>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            var fp = player.FargoSouls();
            fp.AncientShadowEnchantActive = true;
            player.AddEffect<DarkArtistMinion>(item);
            player.AddEffect<DarkArtistEffect>(item);
            player.AddEffect<NinjaAttackSpeedEffect>(item);
        }
    }
}
