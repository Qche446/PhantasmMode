using Terraria;
using Terraria.ModLoader;
using FargowiltasSouls;
using Microsoft.Xna.Framework;
using System.Reflection;
using MonoMod.Cil;
using System;
using Mono.Cecil.Cil;
using FargowiltasSouls.Core.Systems;
using System.Collections.Generic;
using Terraria.Localization;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler.Content;
using FargowiltasSouls.Core.Toggler;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments
{
    public class NinjaEnchantOverride : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ModContent.ItemType<NinjaEnchant>() && WorldSavingSystem.masochistModeReal)
            {
                var extraLine = new TooltipLine(Mod, "PHAddTooltips", Language.GetTextValue("Mods.FargosPhantasmMode.Enchantments.NinjaEnchant"))
                {
                    OverrideColor = Color.Aqua
                };
                tooltips.Add(extraLine);
            }
            base.ModifyTooltips(item, tooltips);
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            if (item.type == ModContent.ItemType<NinjaEnchant>() && WorldSavingSystem.masochistModeReal)
            {
                player.AddEffect<NinjaAttackSpeedEffect>(item);
            }
            base.UpdateAccessory(item, player, hideVisual);
        }
    }
    public class NinjaAttackSpeedEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<ShadowHeader>();
        public override int ToggleItemType => ModContent.ItemType<NinjaEnchant>();
        public override void PostUpdateEquips(Player player)
        {
            base.PostUpdate(player);
            player.GetArmorPenetration(DamageClass.Generic) += Main.LocalPlayer.ForceEffect<NinjaAttackSpeedEffect>() ? 40 : 15f;
            player.GetDamage(DamageClass.Generic) *= Main.LocalPlayer.ForceEffect<NinjaAttackSpeedEffect>() ? 0.5f : 0.6f;
            player.FargoSouls().AttackSpeed *= Main.LocalPlayer.ForceEffect<NinjaAttackSpeedEffect>() ? 3f : 2f;
        }
    }
}
