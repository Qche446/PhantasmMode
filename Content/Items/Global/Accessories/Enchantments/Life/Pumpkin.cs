using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Life
{
    public class Pumpkin : PModeGlobalEnchant<PumpkinEnchant>
    {
        public override void SafeUpdateInPack(Item item, Player player)
        {
            player.AddEffect<PumpkinFeedEffect>(item);
            //Main.NewText(player.buffImmune[BuffID.WellFed3]);
        }
    }
    public class PumpkinFeedEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<LifeHeader>();
        public override int ToggleItemType => ModContent.ItemType<PumpkinEnchant>();

        public static List<int> PumpkinBuff => [BuffID.Regeneration, BuffID.Ironskin, BuffID.Swiftness, 
            BuffID.Wrath, BuffID.Rage,BuffID.Endurance,BuffID.Warmth,
            BuffID.Lucky, BuffID.WellFed3,
            BuffID.Sharpened, BuffID.Sunflower,BuffID.Campfire, BuffID.SugarRush];
        public override void PostUpdateEquips(Player player)
        {
            if (!player.HasEffect<PumpkinFeedEffect>())
                return;
            foreach (int type in PumpkinBuff)
            {
                player.AddBuff(type, 2);
            }
        }
    }
}
