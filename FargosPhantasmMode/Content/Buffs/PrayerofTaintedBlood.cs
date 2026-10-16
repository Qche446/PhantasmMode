
using FargosPhantasmMode.Common;

namespace FargosPhantasmMode.Content.Buffs
{
    public class PrayerofTaintedBlood : ModBuff
    {
        public override void SetStaticDefaults()
        {
            //Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.Phantasm().PrayerofTaintedBlood = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.Phantasm().PrayerofTaintedBlood = true;
        }
    }
}
