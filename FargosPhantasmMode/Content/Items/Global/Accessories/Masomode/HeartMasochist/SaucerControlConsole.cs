using FargowiltasSouls.Content.Items.Accessories.Masomode;
using Terraria;
using Terraria.ID;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.HeartMasochist
{
    public class SaucerControlConsoleOverride : PModeGlobalMasoItem<SaucerControlConsole>
    {
        public override void SafeUpdateInPack(Item item, Player player)
        {
            player.buffImmune[BuffID.VortexDebuff] = true;
        }
    }
}
