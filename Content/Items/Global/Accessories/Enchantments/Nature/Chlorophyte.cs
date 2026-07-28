using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Nature
{
    public class Chlorophyte : PModeGlobalEnchant<ChlorophyteEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            //player.AddEffect<ChlorophyteEnhanceEffect>(item);
            player.AddEffect<JungleEnhanceEffect>(item);
        }
    }
    public class ChlorophyteEnhanceEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<NatureHeader>();
        public override int ToggleItemType => ModContent.ItemType<ChlorophyteEnchant>();
        public static List<int> ChlorophyteItem => [
            ItemID.ChlorophyteMask, ItemID.ChlorophytePlateMail, ItemID.ChlorophyteGreaves,
            ItemID.ChlorophyteWarhammer,
            ItemID.ChlorophyteClaymore,
            ];
    }
}
