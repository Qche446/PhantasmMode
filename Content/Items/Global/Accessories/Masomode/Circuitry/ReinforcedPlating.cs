using FargosPhantasmMode.Content.Buffs;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Masomode.Circuitry
{
    public class ReinforcedPlatingOverride : PModeGlobalMasoItem<ReinforcedPlating>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<ReinforcedPlatingNanoErosionEffect>(item);
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
