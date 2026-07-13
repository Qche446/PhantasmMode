using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs
{
    public class MadMoonBuff : ModBuff
    {
        public override string Texture => "FargosPhantasmMode/Content/Buffs/PlaceholderDebuff";
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            base.Update(player, ref buffIndex);
            player.GetModPlayer<MadMoonPlayer>().MadMoon = true;
        }
    }
    public class MadMoonPlayer : ModPlayer
    {
        public bool MadMoon = false;
        public int MadMoonTimer = 0;
        public override void ResetEffects()
        {
            MadMoon = false;
        }
        public override void PostUpdateBuffs()
        {
            if (MadMoon)
            {
                MadMoonTimer++;
            }
            else
            {
                MadMoonTimer = 0;
            }
        }
    }
}
