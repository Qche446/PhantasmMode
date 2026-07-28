using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Chalice
{
    public class MagicalBulbOverride : PModeGlobalMasoItem<MagicalBulb>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<IvyVenomAttackEffect>(item);
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
