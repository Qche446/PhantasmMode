using FargosPhantasmMode.Global;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using Monochrome.Common.MonoUtil;

namespace FargosPhantasmMode.Content.Bosses.Coffin;

public class P_CursedSpirit : PModeNPCBehaviour
{
    public override int NPCType => ModContent.NPCType<CursedSpirit>();
    public static bool TryApplyRelease(int spiritIndex, int victimWhoAmI)
    {
        if (spiritIndex >= Main.maxNPCs || victimWhoAmI >= Main.maxPlayers)
            return false;

        NPC npc = Main.npc[spiritIndex];
        if (!npc.active || npc.ModNPC is not CursedSpirit cursedSpirit)
            return false;

        Player victim = Main.player[victimWhoAmI];
        cursedSpirit.BittenPlayer = -1;
        cursedSpirit.BiteTimer = -90;
        npc.velocity = -MonoUtil.SafeDirectionTo(npc, victim.Center) * 12f;
        victim.immune = true;
        victim.immuneTime = Math.Max(victim.immuneTime, 30);
        victim.hurtCooldowns[0] = Math.Max(victim.hurtCooldowns[0], 30);
        victim.hurtCooldowns[1] = Math.Max(victim.hurtCooldowns[1], 30);
        npc.netUpdate = true;
        cursedSpirit.Timer = 0f;
        cursedSpirit.AI3 = 0f;
        return true;
    }
}
