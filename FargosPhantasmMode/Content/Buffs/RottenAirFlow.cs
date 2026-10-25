using FargosPhantasmMode.Common;

namespace FargosPhantasmMode.Content.Buffs;

public class RottenAirFlow : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
        Main.debuff[Type] = true;
    }
    public override void Update(Player player, ref int buffIndex)
    {
        player.moveSpeed += 0.2f;
        player.runAcceleration *= 1.2f;
        player.maxRunSpeed *= 1.2f;
        player.endurance -= 0.1f;
        
        player.Phantasm().RottenAirFlow = true;
    }
}
