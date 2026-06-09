using FargowiltasSouls.Content.Buffs.Masomode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs.Global
{
    public class PungentGazeBuffOverride : GlobalBuff
    {
        public bool AppliesToEntity(int buffType)
        {
            return buffType == ModContent.BuffType<PungentGazeBuff>();
        }
        public override void Update(int type, Player player, ref int buffIndex)
        {
            AppliesToEntity(type);
            if (type == ModContent.BuffType<PungentGazeBuff>())
            {
                player.GetModPlayer<PungentGazeBuffPlayer>().Gazed = true;
                player.endurance -= 0.15f;
            }
        }
    }
    public class PungentGazeBuffPlayer : ModPlayer
    {
        public bool Gazed = false;
        public int aimedCD = 90;
        public override void ResetEffects()
        {
            Gazed = false;
        }
        public override void PostUpdateBuffs()
        {
            if (aimedCD < 90)
                aimedCD++;
        }
    }
}
