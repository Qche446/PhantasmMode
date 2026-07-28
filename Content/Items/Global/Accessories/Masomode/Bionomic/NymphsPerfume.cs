using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using Terraria;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Bionomic
{
    public class NymphsPerfumeOverride : PModeGlobalMasoItem<NymphsPerfume>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.FargoSouls().NymphsPerfumeCD -= player.FargoSouls().MasochistSoul ? 10 : 1;
        }
    }
}
