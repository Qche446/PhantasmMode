using FargosPhantasmMode.Global;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.Kingslime
{
    public class P_KingSlime : PModeNPCBehaviour
    {
        public override int NPCType => NPCID.KingSlime;
        public override GlobalNPC NewInstance(NPC target) => null;
        public override void OnFirstTick(NPC npc) => npc.GetGlobalNPC<KingSlime>().RunEmodeAI = false;
        public override void SetDefaults(NPC npc)
        {
            if (!Main.getGoodWorld)
                npc.lifeMax = (int)(1.2f * npc.lifeMax);
        }

    }
}
