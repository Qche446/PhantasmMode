using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler.Content;
using Terraria.ID;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class MutantAntibodiesOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<MutantAntibodies>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.MutantAntibodies"))
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
                player.AddEffect<OceanicMaulAttackEffect>(item);
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class OceanicMaulAttackEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<HeartHeader>();
        public override int ToggleItemType => ModContent.ItemType<MutantAntibodies>();
        public override void PostUpdateEquips(Player player)
        {
            if (!player.FargoSouls().MasochistSoul)
            {
                player.statDefense -= 10;
                player.statLifeMax2 -= 50;
            }
            base.PostUpdateEquips(player);
        }
        public override void ModifyHitNPCBoth(Player player, NPC target, ref NPC.HitModifiers modifiers, DamageClass damageClass)
        {
            target.AddBuff(ModContent.BuffType<OceanicMaulBuff>(), 180);
            target.AddBuff(ModContent.BuffType<MutantNibbleBuff>(), 180);
            base.ModifyHitNPCBoth(player, target, ref modifiers, damageClass);
        }
    }
}
