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
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Bionomic
{
    public class WyvernFeatherOverride : PModeGlobalMasoItem<WyvernFeather>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.FargoSouls().WyvernBallsCD++;
            player.wingTimeMax += 60;
            if (Main.hardMode)
                player.AddEffect<ClippedWingsAttackEffect>(item);
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
