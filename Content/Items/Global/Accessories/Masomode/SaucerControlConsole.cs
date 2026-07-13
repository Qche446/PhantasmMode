using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class SaucerControlConsoleOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<SaucerControlConsole>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.SaucerControlConsole"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateInventory(Item item, Player player)
        {
            if (WorldSavingSystem.masochistModeReal)
                player.buffImmune[BuffID.VortexDebuff] = true;
        }

        public override void UpdateVanity(Item item, Player player)
        {
            if (WorldSavingSystem.masochistModeReal)
                player.buffImmune[BuffID.VortexDebuff] = true;
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (WorldSavingSystem.masochistModeReal)
            {

            }
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
}
