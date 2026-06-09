using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler.Content;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class MagicalBulbOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<MagicalBulb>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.MagicalBulb"))
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
                player.AddEffect<IvyVenomAttackEffect>(item);
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class IvyVenomAttackEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<ChaliceHeader>();
        public override int ToggleItemType => ModContent.ItemType<MagicalBulb>();
        public override void PostUpdateEquips(Player player)
        {
            if (!player.FargoSouls().MasochistSoul)
            {
                player.lifeRegen -= 16;
                player.statLifeMax2 -= 50;
            }
            base.PostUpdateEquips(player);
        }
        public override void ModifyHitNPCBoth(Player player, NPC target, ref NPC.HitModifiers modifiers, DamageClass damageClass)
        {
            target.AddBuff(ModContent.BuffType<IvyVenomBuff>(), 15);
            base.ModifyHitNPCBoth(player, target, ref modifiers, damageClass);
        }
    }
}
