using FargosPhantasmMode.Content.Buffs.Global;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs
{
    public class HeartBrokenBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<PModeGlobalBuffNPC>().HeartBroken = true;
            base.Update(npc, ref buffIndex);
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.statLifeMax2 -= (int)(player.statLifeMax * 0.5f);
        }
    }
    /*
    public class HeartBrokenGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        private bool hasApplied = false;
        private int originalLifeMax = 0;
        private float healthPrecentage = 0;
        public override void ResetEffects(NPC npc)
        {
            bool hasBuff = npc.GetGlobalNPC<PModeGlobalBuffNPC>().HeartBroken;
            healthPrecentage = npc.GetLifePercent();
            if (hasBuff)
            {
                if (!hasApplied)
                {
                    originalLifeMax = npc.lifeMax;
                    int reduction = (int)(npc.lifeMax * 0.15f);
                    if (reduction < 1) reduction = 1;
                    npc.lifeMax -= reduction;
                    
                    npc.life = (int)(healthPrecentage * npc.lifeMax);
                    hasApplied = true;
                }
            }
            else
            {
                if (hasApplied)
                {
                    npc.lifeMax = originalLifeMax;

                    npc.life = (int)(healthPrecentage * npc.lifeMax);

                    hasApplied = false;
                    originalLifeMax = 0;
                }
            }
        }
        public override void OnKill(NPC npc)
        {
            hasApplied = false;
            originalLifeMax = 0;
        }
    }
    */
}
