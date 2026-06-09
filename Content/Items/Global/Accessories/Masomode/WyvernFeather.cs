using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Content.Projectiles.BossWeapons;
using Terraria.ID;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.Toggler.Content;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class WyvernFeatherOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<WyvernFeather>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.WyvernFeather"))
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
            {
                player.FargoSouls().WyvernBallsCD++;
                player.AddEffect<ClippedWingsAttackEffect>(item);
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class ClippedWingsAttackEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<BionomicHeader>();
        public override int ToggleItemType => ModContent.ItemType<WyvernFeather>();
        public override void ModifyHitNPCBoth(Player player, NPC target, ref NPC.HitModifiers modifiers, DamageClass damageClass)
        {
            target.AddBuff(ModContent.BuffType<ClippedWingsBuff>(), 180);
            base.ModifyHitNPCBoth(player, target, ref modifiers, damageClass);
        }
    }
}
