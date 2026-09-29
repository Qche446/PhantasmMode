using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using System;
using System.Reflection;
using Terraria.ModLoader;
using Terraria;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls;
using FargosPhantasmMode.Common;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Life
{
   public class Beetle : PModeGlobalEnchant<BeetleEnchant>
   {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<BeetleEffect>().OnHitNPCEither, BeetleFixed);
        }
        private static void BeetleFixed(Action<BeetleEffect, Player, NPC, NPC.HitInfo, DamageClass, int, Projectile, Item> orig, BeetleEffect self, Player player, NPC target, NPC.HitInfo hitInfo, DamageClass damageClass, int baseDamage, Projectile projectile, Item item)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (modPlayer.LifeForceActive && !PModeChangeApply)
                return;
            if (player.beetleOffense && damageClass != DamageClass.Melee)
            {
                player.beetleCounter += hitInfo.Damage;
            }
        }
    }
}
