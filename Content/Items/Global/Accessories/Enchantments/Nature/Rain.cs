using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Terraria;
using Terraria.ID;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Nature
{
    public class Rain : PModeGlobalEnchant<RainEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (player.HasEffect<RainFeatherfallEffect>())
                player.AddBuff(BuffID.Featherfall, 2);
        }
    }
}
