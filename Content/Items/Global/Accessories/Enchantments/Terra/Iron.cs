using FargosPhantasmMode.Common;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using System;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Terra
{
    public class Iron : PModeGlobalEnchant<IronEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<IronPickupEffect>().PostUpdateEquips, PostUpdateEquipsF);
        }
        private static void PostUpdateEquipsF(Action<IronPickupEffect, Player> orig, IronPickupEffect self, Player player)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (modPlayer.IronReductionDuration > 0)
            {
                player.endurance += player.HasEffectEnchant<IronPickupEffect>() && player.ForceEffect<IronPickupEffect>() ? 0.35f : 0.2f;
                player.statDefense += player.HasEffectEnchant<IronPickupEffect>() && player.ForceEffect<IronPickupEffect>() ? 15 : 0;
                player.GetDamage(DamageClass.Generic) += player.HasEffectEnchant<IronPickupEffect>() && player.ForceEffect<IronPickupEffect>() ? 0.2f : 0.1f;
                modPlayer.IronReductionDuration--;
            }
        }
    }
}
