using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Systems;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.HeartMasochist
{
    public class MutantAntibodiesOverride : PModeGlobalMasoItem<MutantAntibodies>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<OceanicMaulAttackEffect>(item);
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
