using FargosPhantasmMode.Content.Buffs.Global;
using Terraria;
using Terraria.ModLoader;

namespace FargosPhantasmMode.Content.Buffs
{
    public class FractureBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<PModeBuffPlayer>().Fracture = true;
            player.extraFall -= 15;
            player.maxFallSpeed *= 5f;
            player.moveSpeed -= 0.05f;
            player.endurance -= 0.05f;
        }
    }
}
