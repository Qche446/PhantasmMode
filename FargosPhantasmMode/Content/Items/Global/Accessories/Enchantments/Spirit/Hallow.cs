using FargosPhantasmMode.Common;
using Fargowiltas.Items;
using FargowiltasSouls.Content.Buffs.Souls;
using FargowiltasSouls;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;
using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls.Core.Toggler;
using FargowiltasSouls.Core.Toggler.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Luminance.Common.Utilities;
using FargosPhantasmMode.Content.Buffs.Global;
using FargosPhantasmMode.Content.Buffs;

namespace FargosPhantasmMode.Content.Items.Global.Accessories.Enchantments.Spirit
{
    public class Hallow : PModeGlobalEnchant<HallowEnchant>
    {
        public override void Load()
        {
            //PhanUtil.AddHooks(ModContent.GetInstance<FargoGlobalItem>().OnConsumeItem, OnConsumeItemFixed);
        }
        /*
        private static void OnConsumeItemFixed(Action<FargoGlobalItem, Item, Player> orig, FargoGlobalItem self, Item item, Player player)
        {
            FargoSoulsPlayer modPlayer = player.FargoSouls();

            if (item.healLife > 0 && item.potion && (player.HasBuff(BuffID.PotionSickness)))
            {
                if (player.HasEffect<ShroomiteHealEffect>())
                {
                    if (item.type == ItemID.Mushroom)
                    {
                        player.AddBuff(ModContent.BuffType<MushroomPowerBuff>(), Utilities.SecondsToFrames(20f));
                    }
                }
                if (player.HasEffect<HallowEffect>())
                {
                    int hallowIndex = ModContent.GetInstance<HallowEffect>().Index;
                    // Hallow needs to disabled so it doesn't set GetHealLife to 0
                    player.AccessoryEffects().ActiveEffects[hallowIndex] = false;
                    float mult = modPlayer.ForceEffect<HallowEnchant>() ? 1.7f : 1.4f;
                    modPlayer.HallowHealTotal = player.GetHealLife(item) * mult;
                    modPlayer.HallowHealTime = 600;
                    player.AccessoryEffects().ActiveEffects[hallowIndex] = true;
                    HallowEffect.HealRepel(player);
                }
                
                modPlayer.StatLifePrevious += modPlayer.GetHealMultiplier(item.healLife);
            }
        }
        */
        public override void SafeUpdateAccessory(Item item, Player player, bool hideVisual)
        {
            player.AddEffect<HallowFlameEffect>(item);
        }
    }
    public class HallowFlameEffect : AccessoryEffect
    {
        public override Header ToggleHeader => Header.GetHeader<SpiritHeader>();
        public override int ToggleItemType => ModContent.ItemType<HallowEnchant>();
        public override void PostUpdateEquips(Player player)
        {
            player.GetModPlayer<PModeBuffPlayer>().MaxHallowLevel = player.ForceEffect<HallowFlameEffect>() ? 15 : 10;
        }
        public override void ModifyHitNPCBoth(Player player, NPC npc, ref NPC.HitModifiers modifiers, DamageClass damageClass)
        {
            var pp = player.GetModPlayer<PModeBuffPlayer>();
            if (player.GetModPlayer<PModeBuffPlayer>().HallowFlame)
            {
                npc.AddBuff(ModContent.BuffType<HallowFlameBuff>(), 480);
                npc.GetGlobalNPC<PModeGlobalBuffNPC>().HallowFlameLevel = pp.HallowFlameLevel;
            }
        }
    }
}
