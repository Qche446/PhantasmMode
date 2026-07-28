using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Terraria;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Earth
{
    public class Earth : PModeGlobalEnchant<EarthForce>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<AdamantiteProjSplit>(item);
            player.AddEffect<CobaltJumpEnhance>(item);
            player.AddEffect<TitaniumRitualEffect>(item);
            var fp = player.FargoSouls();
            bool attacking = fp.WeaponUseTimer > 0;
            if (!player.HasEffect<MythrilEffect>())
                return;
            if (!attacking && fp.EarthTimer < EarthForceEffect.EarthMaxCharge)
            {
                if (fp.MythrilDelay > 0)
                    fp.MythrilDelay -= 0;
                else
                {
                    fp.EarthTimer += 1;
                }
            }
        }
    }
}
