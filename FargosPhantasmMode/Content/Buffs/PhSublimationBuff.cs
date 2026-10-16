using FargosPhantasmMode.Common;

namespace FargosPhantasmMode.Content.Buffs
{
    public class PhSublimationBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
        }

        public override string Texture => "FargowiltasSouls/Content/Buffs/SublimationBuff";

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.Phantasm().Sublimation = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense -= 15;
            player.GetCritChance(DamageClass.Generic) -= 200;
            player.Phantasm().Sublimation = true;
        }
    }
}
