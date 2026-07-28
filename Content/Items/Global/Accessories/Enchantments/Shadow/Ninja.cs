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
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Shadow
{
    public class Ninja : PModeGlobalEnchant<NinjaEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<NinjaAttackSpeedEffect>(item);
        }
    }
    public class NinjaAttackSpeedEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<ShadowHeader>();
        public override int ToggleItemType => ModContent.ItemType<NinjaEnchant>();
        public override void PostUpdateEquips(Player player)
        {
            bool hasForce = Main.LocalPlayer.ForceEffect<NinjaAttackSpeedEffect>();
            player.GetArmorPenetration(DamageClass.Generic) += hasForce ? 40f : 15f;
            player.GetDamage(DamageClass.Generic) *= hasForce ? 0.5f : 0.6f;
            player.FargoSouls().AttackSpeed *= hasForce ? 3f : 2f;
        }
    }
}
