using FargowiltasSouls.Content.Items.Accessories.Masomode;
using Terraria;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Chalice
{
    public class LihzahrdTreasureBoxOverride : PModeGlobalMasoItem<LihzahrdTreasureBox>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.statDefense += 5;
        }
    }
}
