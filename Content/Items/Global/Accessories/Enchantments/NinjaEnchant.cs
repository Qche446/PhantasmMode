using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
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
