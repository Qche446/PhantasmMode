using FargowiltasSouls.Core.ModPlayers;
using FargowiltasSouls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Items.Accessories.Enchantments;
using FargowiltasSouls.Core.AccessoryEffectSystem;

namespace FargosPhantasmMode.Content.Buffs.Global
{
    public class GlobalBuffNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public bool IvyVenom = false;
        public bool Neurotoxin = false;
        public bool Hypothermia = false;
        public override void ResetEffects(NPC npc)
        {
            IvyVenom = false;
            Neurotoxin = false;
            Hypothermia = false;
        }
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            Player py = Main.player[Main.myPlayer];
            FargoSoulsPlayer fp= py.FargoSouls();
            FargoSoulsGlobalNPC fgn = ModContent.GetInstance<FargoSoulsGlobalNPC>();
            float dotMultiplier = DoTMultiplier(npc, py);
            if (IvyVenom)//15dps
            {
                if (npc.lifeRegen > 0)
                    npc.lifeRegen = 0;
                
                if (damage < 15)
                    damage = 15;

                npc.lifeRegen -= (int)(30 * dotMultiplier);

            }
            if (Neurotoxin)//160dps
            {
                if (npc.lifeRegen > 0)
                    npc.lifeRegen = 0;

                if (damage < 80)
                    damage = 80;

                npc.lifeRegen -= (int)(320 * dotMultiplier);
            }
            if (Hypothermia)//200dps
            {
                if (npc.lifeRegen > 0)
                    npc.lifeRegen = 0;

                if (damage < 50)
                    damage = 50;

                npc.lifeRegen -= (int)(400 * dotMultiplier);
            }
            if (npc.HasBuff(ModContent.BuffType<OceanicMaulBuff>()))//+100dps
            {
                if (npc.lifeRegen > 0)
                    npc.lifeRegen = 0;

                if (damage < 50)
                    damage = 50;

                npc.lifeRegen -= (int)(200 * dotMultiplier);
            }
        }
        public static float DoTMultiplier(NPC npc, Player player)
        {
            float multiplier = 1;
            bool hasNanoErosion = npc.HasBuff(ModContent.BuffType<NanoErosionBuff>());
            bool hasHypothermia = npc.GetGlobalNPC<GlobalBuffNPC>().Hypothermia;
            if (player.HasEffect<OrichalcumEffect>())
                multiplier += OrichalcumEffect.OriDotModifier(npc, player.FargoSouls()) - 1;

            if (npc.FargoSouls().MagicalCurse)
            {
                if (hasNanoErosion || hasHypothermia)
                {
                    multiplier *= 2;
                }
                else
                {
                    multiplier += 1;
                }
            }
            if (npc.daybreak && multiplier > 1 && (!hasNanoErosion) && (!hasHypothermia))
                multiplier -= (multiplier - 1) / 2;
            multiplier *= hasNanoErosion ? 1.2f : 1f;
            multiplier *= hasHypothermia ? 1.05f : 1;
            return multiplier;
        }
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.GetGlobalNPC<FargoSoulsGlobalNPC>().OceanicMaul)
                modifiers.ArmorPenetration += 20;
            if (Hypothermia)
            {
                modifiers.FinalDamage *= 1.05f;
            }
        }
    }
}
