using FargosPhantasmMode.Content.Buffs.Global;
using FargowiltasSouls;
using FargowiltasSouls.Content.Buffs.Masomode;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs
{
    public class HallowFlameBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var pp = player.GetModPlayer<PModeBuffPlayer>();
            player.buffImmune[BuffID.PotionSickness] = true;
            player.potionDelay = 0;
            if (player.FargoSouls().HallowHealTime > 0)
                player.FargoSouls().HallowHealTime = 0;

            pp.HallowFlame = true;
            player.statDefense -= pp.HallowFlameLevel * 4;
            player.statLifeMax2 = (int)((1f - 0.05f * pp.HallowFlameLevel) * player.statLifeMax2);

            player.endurance -= pp.HallowFlameLevel * 0.02f;
            player.resistCold = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            var pn = npc.GetGlobalNPC<PModeGlobalBuffNPC>();
            pn.HallowFlame = true;
        }
        public override bool ReApply(Player player, int time, int buffIndex)
        {
            if (player.buffTime[buffIndex] > time)
            {
                player.buffTime[buffIndex] += time / 2;
            }
            else 
            {
                player.buffTime[buffIndex] = player.buffTime[buffIndex] / 2 + time;
                //player.buffTime[buffIndex] += time;
            }
            player.GetModPlayer<PModeBuffPlayer>().HallowFlameLevel++;
            return true;
        }
    }
}
