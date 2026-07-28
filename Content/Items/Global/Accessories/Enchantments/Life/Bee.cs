using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Life
{
    public class BeeOverride : PModeGlobalEnchant<BeeEnchant>
    {
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            ArmorIDs.Wing.Sets.Stats[item.wingSlot] = modPlayer.ForceEffect(item.type) ? ArmorIDs.Wing.Sets.Stats[ArmorIDs.Wing.FishronWings] : ArmorIDs.Wing.Sets.Stats[ArmorIDs.Wing.AngelWings];
            player.wingTimeMax -= 10;
        }
        public override void VerticalWingSpeeds(Item item, Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            if (PModeChangeApply && player.FargoSouls().ForceEffect(item.type))
            {
                ascentWhenFalling = 0.62f;
                ascentWhenRising = 0.13f;
                maxCanAscendMultiplier = 1.1f;
                maxAscentMultiplier = 1.85f;
                constantAscend = 0.13f;
            }
        }

        public override void HorizontalWingSpeeds(Item item, Player player, ref float speed, ref float acceleration)
        {
            if (PModeChangeApply && player.FargoSouls().ForceEffect(item.type))
            {
                speed = 9f;
                acceleration = 0.25f;
            }
        }
    }
}
