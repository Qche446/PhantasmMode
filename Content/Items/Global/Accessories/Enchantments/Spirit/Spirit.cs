using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Terraria;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit
{
    public class Spirit : PModeGlobalEnchant<SpiritForce>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            AncientHallowEnchant.AddEffects(player, item);
            player.AddEffect<SpectreAttackEffect>(item);
            player.AddEffect<SpectreOnHitEffect>(item);
            player.AddEffect<HallowFlameEffect>(item);
            player.AddEffect<TikiMinLimitEffect>(item);
        }
    }
}
