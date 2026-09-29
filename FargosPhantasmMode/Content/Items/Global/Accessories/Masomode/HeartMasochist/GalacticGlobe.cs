using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using Terraria;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.HeartMasochist
{
    public class GalacticGlobeOverride : PModeGlobalMasoItem<GalacticGlobe>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.FargoSouls().WingTimeModifier += 666;
        }
    }
}
