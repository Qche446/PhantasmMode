using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using Terraria;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Terra
{
    public class Terra : PModeGlobalEnchant<TerraForce>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (player.HasEffect<TerraLightningEffect>() && modPlayer.TerraProcCD > 0)
                modPlayer.TerraProcCD--;
        }
    }
}
