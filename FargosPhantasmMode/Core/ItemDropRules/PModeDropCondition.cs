using FargosPhantasmMode.Core.Systems;
using Terraria.GameContent.ItemDropRules;

namespace FargosPhantasmMode.Core.ItemDropRules
{
    public class PModeDropCondition : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info) => PModeWorldSavingSystem.PhantasmMode && !info.IsInSimulation;
        public bool CanShowItemDropInUI() => PModeWorldSavingSystem.PhantasmMode;
        public string GetConditionDescription() => "In Phantasm Mode";
    }
}
