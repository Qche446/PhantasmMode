using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using FargosPhantasmMode.Content.Buffs.Global;

namespace FargosPhantasmMode.Content.Buffs
{
    public class HypothermiaBuff : ModBuff
    {
        public override string Texture => "FargosPhantasmMode/Content/Buffs/PlaceholderDebuff";
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
        }
        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<GlobalBuffNPC>().Hypothermia = true;
        }
    }
    
}
