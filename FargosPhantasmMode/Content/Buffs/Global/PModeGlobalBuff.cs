using FargosPhantasmMode.Core.Systems;
using FargowiltasSouls.Content.Buffs;
using FargowiltasSouls.Content.Buffs.Masomode;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs.Global
{
    public class PModeGlobalBuff : GlobalBuff
    {
        public static bool PModeChangeApply => PModeWorldSavingSystem.PhantasmMode;
        public override void Update(int type, NPC npc, ref int buffIndex)
        {
            if (!PModeChangeApply)
                return;
            //常春藤毒素
            if (type == ModContent.BuffType<IvyVenomBuff>())
            {
                if (npc.buffTime[buffIndex] > 1200)
                {
                    npc.AddBuff(ModContent.BuffType<NeurotoxinBuff>(), npc.buffTime[buffIndex]);
                    npc.buffTime[buffIndex] = 1;
                }
                npc.GetGlobalNPC<PModeGlobalBuffNPC>().IvyVenom = true;
            }
            //神经毒素
            if (type == ModContent.BuffType<NeurotoxinBuff>())
            {
                npc.GetGlobalNPC<PModeGlobalBuffNPC>().Neurotoxin = true;
            }
        }
        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (!PModeChangeApply)
                return;
            if (type == ModContent.BuffType<GladiatorSpiritBuff>())
            {
                player.statDefense += 15;
                player.endurance += 0.15f;
            }
            //尖刻注视
            if (type == ModContent.BuffType<PungentGazeBuff>())
            {
                player.endurance -= 0.15f;
            }
            if (type == BuffID.RapidHealing)
            {
                player.statDefense += 5;
            }
        }
        public override bool ReApply(int type, NPC npc, int time, int buffIndex)
        {
            if (!PModeChangeApply)
                return false;
            if (type == ModContent.BuffType<IvyVenomBuff>())
            {
                npc.buffTime[buffIndex] += time;
                return false;
            }
            return false;
        }
    }
}
