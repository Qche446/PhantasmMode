using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.Systems;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs.Global
{
    public class NeurotoxinBuffOverride : GlobalBuff
    {
        public bool AppliesToEntity(int buffType)
        {
            return buffType == ModContent.BuffType<NeurotoxinBuff>();
        }
        public override void Update(int type, NPC npc, ref int buffIndex)
        {
            AppliesToEntity(type);
            if (type == ModContent.BuffType<NeurotoxinBuff>() && WorldSavingSystem.masochistModeReal)
                npc.GetGlobalNPC<GlobalBuffNPC>().Neurotoxin = true;
        }
    }
}
