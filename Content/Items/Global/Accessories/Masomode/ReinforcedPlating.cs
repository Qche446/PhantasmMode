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
using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode
{
    public class ReinforcedPlatingOverride : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ModContent.ItemType<ReinforcedPlating>();
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Masomode.ReinforcedPlating"))
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
                player.AddEffect<ReinforcedPlatingNanoErosionEffect>(item);
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class ReinforcedPlatingNanoErosionEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<DubiousHeader>();
        public override int ToggleItemType => ModContent.ItemType<ReinforcedPlating>();
        public override void ModifyHitByNPC(Player player, NPC npc, ref Player.HurtModifiers modifiers)
        {
            npc.AddBuff(ModContent.BuffType<NanoErosionBuff>(), 300);
            base.ModifyHitByNPC(player, npc, ref modifiers);
        }
        public override void ModifyHitByProjectile(Player player, Projectile projectile, ref Player.HurtModifiers modifiers)
        {
            if (projectile.hostile && projectile.GetSourceNPC() != null)
            {
                NPC ownerNPC = projectile.GetSourceNPC();
                if (ownerNPC.active)
                {
                    ownerNPC.AddBuff(ModContent.BuffType<NanoErosionBuff>(), 300);
                }
            }
            base.ModifyHitByProjectile(player, projectile, ref modifiers);
        }
    }
}
