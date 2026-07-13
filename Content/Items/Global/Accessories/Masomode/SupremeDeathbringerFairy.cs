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
    public class SupremeDeathbringerFairyOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<SupremeDeathbringerFairy>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.SupremeDeathbringerFairy.Base"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
                var extraLine2 = new TooltipLine(Mod, "PHAddTooltipsExtra", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.SupremeDeathbringerFairy.Extra"));
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
                    TextRender.BurnDraw(line, 0.2f, new Vector2(0, -0.5f), Color.Gray, Color.Blue, Color.Aqua, Color.Purple);
                    //TextRender.FlameParticleManager.SpawnFlameParticles(line, scale: 1f, intensity: 0.55f);
                    return false;
                }
            }
            return true;
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                //躁动晶状体
                player.moveSpeed += (player.statLife <= player.statLifeMax2 / 2f) ? 0.3f : 0.1f;
                //蜂后毒刺
                player.honey = true;
                //死灵密酿
                player.AddEffect<NecroSpinSpeedEffect>(item);
                if (ModContent.GetInstance<NecroSpinSpeedEffect>().speed == 0.5f)
                    ModContent.GetInstance<NecroSpinSpeedEffect>().speed = 0.3f;
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
        public override void UpdateVanity(Item item, Player player)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                player.AddEffect<PlatformFallthroughEffect>(item);
                player.honey = true;
            }
            base.UpdateVanity(item, player);
        }
        public override void UpdateInventory(Item item, Player player)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                player.AddEffect<PlatformFallthroughEffect>(item);
                player.honey = true;
            }
            base.UpdateInventory(item, player);
        }

    }
}
