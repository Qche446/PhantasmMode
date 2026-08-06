using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.ModPlayers;
using System;
using Terraria;
using Terraria.ModLoader;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using System.Reflection;
using Microsoft.Xna.Framework;
using FargosPhantasmMode.Common;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Nature
{
    public class Crimson : PModeGlobalEnchant<CrimsonEnchant>
    {
        public override void Load()
        {
            PhanUtil.AddHooks(ModContent.GetInstance<CrimsonEffect>().OnHurt, CrimsonOnHurt);
        }
        public static void CrimsonOnHurt(Action<CrimsonEffect, Player, Player.HurtInfo> orig, CrimsonEffect self, Player player, Player.HurtInfo info)
        {
            if (!PModeChangeApply)
            {
                orig.Invoke(self, player, info);
                return;
            }
            if (!player.HasEffect<CrimsonEffect>())
                return;
            //if was already healing, stop the heal and do nothing
            if (player.HasBuff<CrimsonRegenBuff>())
            {
                player.ClearBuff(ModContent.BuffType<CrimsonRegenBuff>());
            }
            FargoSoulsPlayer modPlayer = player.FargoSouls();
            if (info.Damage < 10)
                return; 
            modPlayer.CrimsonRegenTime = 0;
            float returnHeal = 0.5f;
            modPlayer.CrimsonRegenAmount = (int)(info.Damage * returnHeal); 

            player.AddBuff(ModContent.BuffType<CrimsonRegenBuff>(),
                modPlayer.ForceEffect<CrimsonEnchant>() ? 900 : 430); 
        }
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            CrimsonRevenge(player);
        }
        public static void CrimsonRevenge(Player player)
        {
            float timeLeft = 0;
            if (player.HasBuff(ModContent.BuffType<CrimsonRegenBuff>()))
            {
                for (int i = 0; i < player.buffType.Length; i++)
                {
                    if (player.buffType[i] == ModContent.BuffType<CrimsonRegenBuff>())
                    {
                        timeLeft = player.buffTime[i];
                    }
                }
            }
            player.GetDamage(DamageClass.Generic) += 0.3f * timeLeft / 900f;
            player.endurance += 0.3f * timeLeft / 900f;
        }
    }
}
