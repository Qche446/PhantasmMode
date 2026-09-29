using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Terraria;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Life
{
    public class Life : PModeGlobalEnchant<LifeForce>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<CactusEffect>(item);
            BeetleEnchant.AddEffects(player, item);
        }
        public override void SafeUpdateInPack(Item item, Player player)
        {
            player.AddEffect<PumpkinFeedEffect>(item);
        }
    }
}
