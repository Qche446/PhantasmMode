using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Life
{
    public class Spider : PModeGlobalEnchant<SpiderEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<SpiderEffect>().PostUpdateEquips, SpiderFixed);
        }
        private static void SpiderFixed(Action<SpiderEffect, Player> orig, SpiderEffect self, Player player)
        {
            if (self.HasEffectEnchant(player) || PModeChangeApply)
                player.FargoSouls().MinionCrits = true;
            player.GetCritChance(DamageClass.Generic) += 10;
            if (player.FargoSouls().ForceEffect(ModContent.ItemType<SpiderEnchant>()))
                player.GetCritChance(DamageClass.Generic) += 15;
        }
    }
}
