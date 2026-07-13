using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class SecurityWalletOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<SecurityWallet>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.SecurityWallet"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    /*
    public class PickCoinEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<BionomicHeader>();
        public override int ToggleItemType => ModContent.ItemType<SecurityWallet>();
        public override void PostUpdateEquips(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {

            }
        }
    }
    */
}
