using System;
using Terraria;
using static FargosPhantasmMode.Content.Bossbar.BossBarRegistry;

namespace FargosPhantasmMode.Content.Bossbar
{
    public class BossBarData(int whoAmI, int type)
    {
        public int NPCWhoAmI = whoAmI;
        public int NPCType = type;
        public NPC GetNPC()
        {
            if (NPCWhoAmI >= 0 && NPCWhoAmI < Main.npc.Length)
            {
                return Main.npc[NPCWhoAmI];
            }
            return null;
        }
    }
    public class BossBarConfig(DrawBossBarMethod drawbossBarMethod, bool hasShield = false, Func<NPC, int> shield = null, Func<NPC, int> maxShield = null)
    {
        public DrawBossBarMethod drawBossBarMethod = drawbossBarMethod;
        public bool HasShield = hasShield;
        public Func<NPC, int> Shield = shield;
        public Func<NPC, int> MaxShield = maxShield;
    }
}
