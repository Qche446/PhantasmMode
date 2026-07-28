using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Earth
{
    public class Mythril : PModeGlobalEnchant<MythrilEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            var fp = player.FargoSouls();
            if (!player.HasEffect<MythrilEffect>())
                return;
            if (fp.WeaponUseTimer > 0)
            {

            }
            else
            {
                if (fp.MythrilDelay > 0)
                    fp.MythrilDelay--;
                else
                {
                    fp.MythrilTimer += 0.25f;
                }
            }
        }
    }
}
