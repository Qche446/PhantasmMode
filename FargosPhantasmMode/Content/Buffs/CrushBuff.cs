using FargosPhantasmMode.Common;

namespace FargosPhantasmMode.Content.Buffs
{
    public class CrushBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            //Main.debuff[Type] = true;
            //Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.Phantasm().CrushBuff = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.DefenseEffectiveness *= 0;
            player.resistCold = false;
            player.noKnockback = false;
            player.endurance *= 0;
            player.Phantasm().CrushBuff = true;
        }
    }
}
