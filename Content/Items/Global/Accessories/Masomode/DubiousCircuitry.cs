using FargosPhantasmMode.Content.Render;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class DubiousCircuitryOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<DubiousCircuitry>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.DubiousCircuitry.Base"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
                var extraLine2 = new TooltipLine(Mod, "PHAddTooltipsExtra", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.DubiousCircuitry.Extra"));
                tooltips.Add(extraLine2);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                if (line.Name == "PHAddTooltipsExtra")
                {
                    TextRender.BurnDraw(line, 0.2f, new Vector2(0.2f, 0), new Color(98, 98, 98), new Color(26, 255, 0), new Color(255, 239, 0), new Color(0, 205, 255));
                    return false;
                }
            }
            return true;
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                player.AddEffect<FusedLensMechElectricOrbEffect>(item);
                player.AddEffect<ReinforcedPlatingNanoErosionEffect>(item);
            } 
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
}
