using FargowiltasSouls.Content.Items.Accessories.Masomode;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Bionomic
{
    public class MysticSkullOverride : PModeGlobalMasoItem<MysticSkull>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Magic) += 0.15f;
        }
    }
}
