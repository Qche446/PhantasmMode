using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler.Content;
using FargowiltasSouls.Core.Toggler;
using FargosPhantasmMode.Content.Buffs;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;
using FargosPhantasmMode.Assets.ExtraTextures;
using FargosPhantasmMode.Content.Render;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class PureHeartOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<PureHeart>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.PureHeart.Base"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
                var extraLine2 = new TooltipLine(Mod, "PHAddTooltipsExtra", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.PureHeart.Extra"));
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
                    TextRender.BurnDraw(line, 0.4f, new Vector2 (0, -0.5f), Color.Gray, Color.ForestGreen, Color.IndianRed, Color.Purple);
                    
                    return false;
                }
            }
            return true;
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                player.AddEffect<GuttedHeartAura>(item);
                player.AddEffect<FlawlessEffect>(item);
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class FlawlessEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<PureHeartHeader>();
        public override int ToggleItemType => ModContent.ItemType<PureHeart>();
        
        public override void PostUpdateEquips(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                var modPlayer = player.GetModPlayer<FlawlessPlayer>();
                if (++modPlayer.FlawlessTimer >= 60 * 15)
                {
                    modPlayer.FlawlessTimer = 60 * 15;
                    player.AddBuff(ModContent.BuffType<FlawlessBuff>(), 2);
                }
            }
        }
    }
}
