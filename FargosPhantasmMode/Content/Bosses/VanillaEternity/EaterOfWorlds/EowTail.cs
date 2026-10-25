using FargosPhantasmMode.Global;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using Monochrome.Content.Worms;
namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EaterOfWorlds;

public class P_EowTail : PModeNPCBehaviour
{
    private readonly MonoWormLayout layout = MonoWormLayout.Vanilla;
    public override int NPCType => NPCID.EaterofWorldsTail;
    public override void OnFirstTick(NPC npc) => npc.GetGlobalNPC<EaterofWorldsSegment>().RunEmodeAI = false;
    public override bool SafePreAI(NPC npc)
    {
        EowChain.ManageAlpha(npc);
        if (EowChain.UpdateLinks(npc, layout))
            return false;
        NPC ahead = Main.npc[(int)npc.ai[1]];
        int index = MonoWormNav.IndexFromHead(npc, layout);
        if (index == 1)
        {
            MonoWormFollow.FixedSpacing(ahead.Center, ahead.rotation, npc.width, ahead.velocity,
                out Vector2 center, out float rotation);
            npc.Center = center;
            npc.rotation = rotation;
        }
        else if (MonoWormNav.TryGetAhead(ahead, layout, out NPC ahead2) && ahead2 is not null)
        {
            MonoWormFollow.Snake(ahead.Center, ahead2.Center, npc.Center, npc.width, 0.5f, 0f,
                out Vector2 center, out float rotation);
            npc.Center = center;
            npc.rotation = rotation;
        }

        EowVanilla.DigDust(npc);
        return false;
    }
}
