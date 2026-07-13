using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class FrigidGemstoneOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<FrigidGemstone>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.FrigidGemstone"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (WorldSavingSystem.masochistModeReal)
                player.AddEffect<FrostBurn2AttackEffect>(item);
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class FrostBurn2AttackEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<BionomicHeader>();
        public override int ToggleItemType => ModContent.ItemType<FrigidGemstone>();
        public override void ModifyHitNPCBoth(Player player, NPC target, ref NPC.HitModifiers modifiers, DamageClass damageClass)
        {
            target.AddBuff(BuffID.Frostburn2, 180);
            base.ModifyHitNPCBoth(player, target, ref modifiers, damageClass);
        }
    }
}
