using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class QueenStingerOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<QueenStinger>() && WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.QueenStinger"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (item.type == ModContent.ItemType<QueenStinger>() && WorldSavingSystem.masochistModeReal)
            {
                player.honey = true;
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
        public override void UpdateVanity(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<QueenStinger>() && WorldSavingSystem.masochistModeReal)
            {
                player.honey = true;
            }
            base.UpdateVanity(item, player);
        }
        public override void UpdateInventory(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<QueenStinger>() && WorldSavingSystem.masochistModeReal)
            {
                player.honey = true;
            }
            base.UpdateInventory(item, player);
        }
    }
}
