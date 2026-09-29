using System;
using System.Collections.Generic;
using Terraria;

namespace FargosPhantasmMode.Common
{
    /// <summary>
    /// 直接使用委托来实现状态机的接口，避免了使用枚举和switch语句的复杂性。每个状态都是一个方法，可以直接赋值给AIState属性，从而改变NPC的行为。
    /// 可以通过PhaseList来定义不同阶段的状态列表，每个阶段可以有多个状态，NPC会按顺序执行这些状态。
    /// 默认情况下会将NPC的ai[0]作为PhaseIndex(每个Phase即委托数组的index)，localAI[3]作为PhaseListIndex(即阶段数)
    /// </summary>
    public interface IDelegateStateMachine
    {
        public delegate void AIMethod(NPC npc, Player player);
        public AIMethod AIState { get; set; }
        public List<List<AIMethod>> PhaseList { get; }
        public static void Initialize(NPC npc)
        {
            npc.ai[1] = npc.ai[2] = npc.ai[3] = 0;
        }
        public static void ChooseAttack(NPC npc, IDelegateStateMachine self, bool initialize = true)
        {
            if (initialize)
                Initialize(npc);
            int PhaseIndex = Convert.ToInt32(npc.ai[0]);
            int PhaseListIndex = Convert.ToInt32(npc.localAI[3]);
            List<AIMethod> Phase = self.PhaseList[PhaseListIndex];
            if (PhaseIndex > Phase.Count - 1)
                PhaseIndex = 0;
            self.AIState = Phase[PhaseIndex];
            PhaseIndex++;
            npc.ai[0] = PhaseIndex;
            npc.netUpdate = true;
        }
        public static void NextPhase(NPC npc, IDelegateStateMachine self)
        {
            Initialize(npc);
            npc.ai[0] = 0;
            npc.localAI[3]++;
            if (npc.localAI[3] > self.PhaseList.Count - 1)
                throw new Exception("阶段ai数组越界，你神了");
        }
        public static void GotoAttack(NPC npc, IDelegateStateMachine self, AIMethod method, bool initialize = true)
        {
            npc.ai[0] = self.PhaseList[Convert.ToInt32(npc.localAI[3])].IndexOf(method);
            ChooseAttack(npc, self, initialize);
        }
        public static void GotoAttack(NPC npc, IDelegateStateMachine self, int goalIndex, bool initialize = true)
        {
            npc.ai[0] = goalIndex;
            ChooseAttack(npc, self, initialize);
        }
    }
}
