using FargosPhantasmMode.Core.Systems;

namespace FargosPhantasmMode.Content.NPCs.PGlobalNPC
{
    public partial class PmodeGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public static bool PMode => PModeWorldSavingSystem.PhantasmMode;
        public override void Load()
        {
            AddBuffHook();
        }
        public override void ResetEffects(NPC npc)
        {
            BuffReset(npc);
        }
        public override void AI(NPC npc)
        {
            GetElement(npc);
        }
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            ApplyBuffLifeRegen(npc, ref damage);
        }
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            DrawBuffEffect(npc, ref drawColor);
        }
        public override void OnKill(NPC npc)
        {
            BuffOnkill(npc);
        }
    }
}
