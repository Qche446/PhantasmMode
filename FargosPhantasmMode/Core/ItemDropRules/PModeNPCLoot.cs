using FargosPhantasmMode.Content.Items.Weapons;
using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.TrojanSquirrel;
using FargowiltasSouls.Content.Items.Accessories.Forces;
using Terraria.GameContent.ItemDropRules;

namespace FargosPhantasmMode.Core.ItemDropRules
{
    public class PModeNPCLoot : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            LeadingConditionRule pmodeRule = new(new PModeDropCondition());
            switch(npc.type)
            {
                case NPCID.KingSlime:
                    break;
                case NPCID.WallofFlesh: pmodeRule.OnSuccess(FargoSoulsUtil.BossBagDropCustom(ModContent.ItemType<FleshBayonet>()));
                    break;
                case NPCID.Retinazer or NPCID.Spazmatism:
                    {
                        LeadingConditionRule noTwin = new(new Conditions.MissingTwin());
                        pmodeRule.OnSuccess(noTwin);
                        noTwin.OnSuccess(FargoSoulsUtil.BossBagDropCustom(ModContent.ItemType<Content.Items.Weapons.Monochrome>()));
                    }
                    break;

            }
            if (npc.type == ModContent.NPCType<TrojanSquirrel>())
            {
                pmodeRule.OnSuccess(ItemDropRule.OneFromOptions(1, BaseForce.EnchantsIn<TimberForce>()));
            }
            npcLoot.Add(pmodeRule);
        }
    }
}
