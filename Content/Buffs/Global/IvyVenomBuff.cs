using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Core.Systems;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs.Global
{
    public class IvyVenomBuffOverride : GlobalBuff
    {
        public bool AppliesToEntity(int buffType)
        {
            return buffType == ModContent.BuffType<IvyVenomBuff>();
        }
        public override void Update(int type, NPC npc, ref int buffIndex)
        {
            AppliesToEntity(type);
            if (type == ModContent.BuffType<IvyVenomBuff>() && WorldSavingSystem.masochistModeReal)
            {
                if (npc.buffTime[buffIndex] > 1200)
                {
                    npc.AddBuff(ModContent.BuffType<NeurotoxinBuff>(), npc.buffTime[buffIndex]);
                    npc.buffTime[buffIndex] = 1;
                    if (npc.whoAmI == Main.myPlayer)
                    {
                        Main.NewText(Language.GetTextValue("Mods." + base.Mod.Name + ".Buffs.IvyVenomBuff.Transform"), 175, 75);
                    }
                }
                npc.GetGlobalNPC<GlobalBuffNPC>().IvyVenom = true;
            }
        }
        public override bool ReApply(int type, NPC npc, int time, int buffIndex)
        {
            AppliesToEntity(type);
            if (type == ModContent.BuffType<IvyVenomBuff>() && WorldSavingSystem.masochistModeReal)
                npc.buffTime[buffIndex] += time;
            return false;
        }
    }
}
