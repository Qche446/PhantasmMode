using FargosPhantasmMode.Common;
using FargosPhantasmMode.Content.Bossbar;
using FargosPhantasmMode.Core.Systems;
using FargosPhantasmMode.Global;
using FargowiltasSouls;
using FargowiltasSouls.Common.Utilities;
using FargowiltasSouls.Content.Bosses.Champions.Will;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.Buffs.Masomode;
using FargowiltasSouls.Content.Projectiles;
using FargowiltasSouls.Content.Projectiles.Masomode;
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.StateMachines;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EyeOfCthulhu
{
    //总的来说，这是一个下推状态机的具体实现案例，尽管有些地方逻辑有问题，但是总体比较ai流畅，可以作为范例参考
    /// <summary>
    /// 用 Luminance 的 PushdownAutomata 状态机重写的克苏鲁之眼 AI（行为逻辑与原 P_EyeOfCthulhu 一致）。
    /// 状态枚举数值与原 AIState 保持一致；原 npc.ai[1] 计时器由 EntityAIState.Time 承担，
    /// npc.ai[2] / npc.localAI[0..3] 仍作为招式参数。
    /// 注意：与 P_EyeOfCthulhu（PModeNPCBehaviour）二选一启用，避免同时运行。
    /// </summary>

    [Obsolete]
    public class L_EyeOfCthulhu : PModeNPCBehaviour
    {
        /// <summary>
        /// 状态标识，数值对应原版 AIState。
        /// </summary>
        public override GlobalNPC NewInstance(NPC target)
        {
            return null;
        }
        public enum L_EoCState
        {
            PhaseChange3rd = -3,        // P3转P4
            PhaseChange2nd = -2,        // P2转P3
            PhaseChange1st = -1,        // P1转P2
            FourCornersWait = 0,        // 四角等待
            NormalDash = 1,             // 常态三连冲
            MoonShoot = 2,              // 眼状散射月矢
            Phase2Wait = 3,             // P2P3挂机
            NormalFastDash = 4,         // P2高速冲刺
            NormalTpDash = 5,           // tp冲刺
            P3FastDash = 7,             // P3阴间冲刺
            FastTpDashs = 8,            // 高频tp冲刺
            RestraintTriangle = 9,      // 三角拘束
            RestraintSquare = 10,       // 方形拘束
            RestraintHexagon = 11,      // 六芒星拘束
            RestraintOctagonal = 12,    // 八芒星拘束
            RestraintRound = 13,        // 圆形拘束
            FaintVisibleFourRowsScythe = 14,   // 四排月镰
            FaintVisibleRoundScythe = 15,      // 圆形月镰
            ChooseNextAttack = 24,      // 选择下一招式（原版24）
            SuperTpDash = 25,           // 超级tp冲
            P3MoonShootDash = 26,       // 冲刺散射
            ChooseRandom = 27,          // 新增过渡态：选择随机招式（对应原版直接 ChooseNext）
        }

        /// <summary>
        /// 需要被注册进状态机的全部状态（初始状态 FourCornersWait 由构造函数注册，这里不重复）。
        /// </summary>
        private static readonly L_EoCState[] AllStates =
        {
            L_EoCState.PhaseChange3rd, L_EoCState.PhaseChange2nd, L_EoCState.PhaseChange1st,
            L_EoCState.NormalDash, L_EoCState.MoonShoot, L_EoCState.Phase2Wait,
            L_EoCState.NormalFastDash, L_EoCState.NormalTpDash, L_EoCState.P3FastDash,
            L_EoCState.FastTpDashs, L_EoCState.RestraintTriangle, L_EoCState.RestraintSquare,
            L_EoCState.RestraintHexagon, L_EoCState.RestraintOctagonal, L_EoCState.RestraintRound,
            L_EoCState.FaintVisibleFourRowsScythe, L_EoCState.FaintVisibleRoundScythe,
            L_EoCState.ChooseNextAttack, L_EoCState.SuperTpDash, L_EoCState.P3MoonShootDash,
            L_EoCState.ChooseRandom,
        };

        /// <summary>
        /// ChooseNext 可能选到的随机招式（8=FastTpDashs, 9~15=拘束/月镰）。
        /// 用可空类型以适配 PhanUtil.RegisterWeightedTransition 的 E?[] 参数（数组元素均为非空）。
        /// </summary>
        private static readonly L_EoCState?[] RandomAttacks =
        {
            L_EoCState.FastTpDashs,
            L_EoCState.RestraintTriangle,
            L_EoCState.RestraintSquare,
            L_EoCState.RestraintHexagon,
            L_EoCState.RestraintOctagonal,
            L_EoCState.RestraintRound,
            L_EoCState.FaintVisibleFourRowsScythe,
            L_EoCState.FaintVisibleRoundScythe,
        };

        /// <summary>状态24（P4随机）的权重数组，由 RebuildRandomWeights 在转换 condition 里每帧重建。</summary>
        private readonly float[] randomWeightsState24 = new float[RandomAttacks.Length];

        /// <summary>ChooseRandom 的权重数组，由 RebuildRandomWeights 在转换 condition 里每帧重建。</summary>
        private readonly float[] randomWeightsChooseRandom = new float[RandomAttacks.Length];

        public PushdownAutomata<EntityAIState<L_EoCState>, L_EoCState> StateMachine;

        public bool recolor = SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode;
        public bool DroppedSummon;
        public int TeleportDirection;
        public int HyperTime = 0;
        public int P3AttackChange = 0;
        public Queue<int> oldAtk = new();

        /// <summary>行为方法通过此字段访问当前 NPC（GlobalNPC 没有自带的 NPC 属性）。</summary>
        private NPC npc => FargoSoulsUtil.NPCExists(EModeGlobalNPC.eyeBoss, NPCType);
        private L_EoCState? syncedState;
        private int? syncedTime;
        public override int NPCType => NPCID.EyeofCthulhu;
        public override void SetDefaults(NPC npc)
        {
        }
        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (npc.alpha > 120)
                return false;
            return true;
        }

        public override bool CheckDead(NPC npc)
        {
            return true;
        }
        public override void OnFirstTick(NPC npc)
        {
            npc.GetGlobalNPC<EyeofCthulhu>().RunEmodeAI = false;
            InitializeStateMachine();
        }
        public sealed override bool SafePreAI(NPC npc)
        {
            PreAIInternal(npc);
            return false;
        }

        // 对应原 SafePreAI。getGoodWorld(祛华) 时会递归调用自身以加速 Boss 与弹幕更新。
        private void PreAIInternal(NPC npc)
        {
            EModeGlobalNPC.eyeBoss = npc.whoAmI;

            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            //ftw特性(祛华):根据某个神秘参数加速boss和boss弹幕更新
            if (Main.getGoodWorld)
            {
                Color light = Lighting.GetColor(npc.Center.ToTileCoordinates());
                float modifier = (light.R + light.G + light.B) / 765f;
                modifier *= (light.R + light.G + light.B) / 765f;
                modifier *= 1 - npc.alpha / 255;
                modifier *= 1 - npc.alpha / 255;
                modifier *= 0.5f * Math.Abs(Main.moonPhase - 4f) / 4f + 0.5f;
                bool RestrictedLight = modifier < 0.5f;
                int[] EoCProj = [
                    ModContent.ProjectileType<BloodScythe>(),
                    ModContent.ProjectileType<MoonScythe>(),
                    ModContent.ProjectileType<FalseEoC>(),
                    ModContent.ProjectileType<MoonBolt>(),
                    ModContent.ProjectileType<MoonlightTrail>(),
                    ModContent.ProjectileType<EoCTpTelegraph>(),
                    ModContent.ProjectileType<SuperEoCTpTelegraph>()
                ];
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = FargoSoulsUtil.ProjectileExists(i, EoCProj);
                    if (proj != null)
                    {
                        if (RestrictedLight && HyperTime == 4)
                            proj.extraUpdates = 1;
                        else
                            proj.extraUpdates = 0;
                    }
                }
                if (RestrictedLight)
                    npc.Center += 0.2f * npc.velocity;
                if (++HyperTime >= 5)
                {
                    HyperTime = 0;
                    if (RestrictedLight)
                    {
                        PreAIInternal(npc);
                    }
                }
            }

            if (npc.alpha > 50 && !Main.getGoodWorld)
                Lighting.AddLight(npc.Center, 0.75f, 1.35f, 1.5f);
            npc.dontTakeDamage = npc.alpha > 100;
            Player player = Main.player[npc.target];
            if (npc.ai[3] == 3)
                npc.AddBuff(ModContent.BuffType<CurseoftheMoonBuff>(), 5);

            PHEyeofCthulhuAI(npc, player);
        }

        // 对应原 PHEyeofCthulhuAI：AliveCheck → 状态机行为/转换 → 掉落召唤物。
        // 原 PhaseCheck 被"优先注册的阶段转换"取代，会在任意状态下打断当前攻击。
        private void PHEyeofCthulhuAI(NPC npc, Player player)
        {
            if (!AliveCheck(npc, player))
                return;
            StateMachine.PerformBehaviors();
            StateMachine.PerformStateTransitionCheck();
            EModeUtils.DropSummon(npc, "SuspiciousEye", NPC.downedBoss1, ref DroppedSummon);
        }

        #region 状态机初始化
        private void InitializeStateMachine()
        {
            StateMachine = new PushdownAutomata<EntityAIState<L_EoCState>, L_EoCState>(
                new EntityAIState<L_EoCState>(L_EoCState.FourCornersWait));

            foreach (L_EoCState s in AllStates)
                StateMachine.RegisterState(new EntityAIState<L_EoCState>(s));

            AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>
                .FillStateMachineBehaviors(StateMachine, this);

            RegisterTransitions();
            //Main.NewText("initialize");
            if (syncedState.HasValue)
            {
                StateMachine.CurrentState.Identifier = syncedState.Value;
                if (syncedTime.HasValue)
                    StateMachine.CurrentState.Time = syncedTime.Value;
            }
        }

        private void RegisterTransitions()
        {
            // 阶段转换（对应原 PhaseCheck，运行于任何状态之前，故注册在最前以打断当前攻击）
            StateMachine.ApplyToAllStatesExcept(
                s => StateMachine.RegisterTransition(s, L_EoCState.PhaseChange1st, false,
                    () => npc.life < npc.lifeMax * 0.8f && npc.ai[3] == 0,
                    PhaseChangeTo1st),
                new L_EoCState[0]);

            StateMachine.ApplyToAllStatesExcept(
                s => StateMachine.RegisterTransition(s, L_EoCState.PhaseChange2nd, false,
                    () => npc.life < npc.lifeMax * 0.5f && npc.ai[3] == 1,
                    PhaseChangeTo2nd),
                new L_EoCState[0]);

            StateMachine.ApplyToAllStatesExcept(
                s => StateMachine.RegisterTransition(s, L_EoCState.PhaseChange3rd, false,
                    () => npc.life < npc.lifeMax * 0.15f && npc.ai[3] == 2,
                    PhaseChangeTo3rd),
                new L_EoCState[0]);

            // 状态24(ChooseNextAttack)：P2/P3 → SuperTpDash；P4 → 随机招式
            StateMachine.RegisterTransition(L_EoCState.ChooseNextAttack, L_EoCState.SuperTpDash, false,
                () => State(L_EoCState.ChooseNextAttack).Time >= 1 && npc.ai[3] != 3,
                () => npc.netUpdate = true);

            // 随机招式选择：不能注册一个"目标随机"的转换——TransitionInfo.NewState 在注册后不可变，
            // 且该下推自动机检测转换时只取首个满足条件的转换。因此改用 PhanUtil.RegisterWeightedTransition：
            // 注册一个"空目标"转换，在转换回调里按权重随机选中招式并直接压栈（StateStack.Push），
            // 框架随后递归 PerformStateTransitionCheck，从选中招式继续。
            // 权重由 RebuildRandomWeights 在 condition 里每帧重建（含阶段分布与 oldAtk 去重）。
            StateMachine.RegisterWeightedTransition(
                L_EoCState.ChooseNextAttack,
                RandomAttacks,
                randomWeightsState24,
                () =>
                {
                    RebuildRandomWeights(randomWeightsState24, p4: true); // 状态24仅在P4随机：均匀选9~15（不含8）
                    return State(L_EoCState.ChooseNextAttack).Time >= 1 && npc.ai[3] == 3;
                },
                false,
                () => npc.netUpdate = true);

            StateMachine.RegisterWeightedTransition(
                L_EoCState.ChooseRandom,
                RandomAttacks,
                randomWeightsChooseRandom,
                () =>
                {
                    RebuildRandomWeights(randomWeightsChooseRandom, npc.ai[3] == 3); // P3: 50%冲刺8; P4: 均匀9~15
                    return State(L_EoCState.ChooseRandom).Time >= 1;
                },
                false,
                () => npc.netUpdate = true);

            // 转阶段状态退出
            StateMachine.RegisterTransition(L_EoCState.PhaseChange3rd, L_EoCState.ChooseNextAttack, false,
                () => State(L_EoCState.PhaseChange3rd).Time >= 60,
                () =>
                {
                    npc.defense -= 30;
                    npc.dontTakeDamage = false;
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.localAI[1] = 0;
                    npc.localAI[2] = 0;
                    npc.localAI[3] = 0;
                    npc.netUpdate = true;
                });

            StateMachine.RegisterTransition(L_EoCState.PhaseChange2nd, L_EoCState.SuperTpDash, false,
                () => State(L_EoCState.PhaseChange2nd).Time >= 120,
                () =>
                {
                    npc.dontTakeDamage = false;
                    npc.ai[2] = 0;
                    npc.netUpdate = true;
                });

            StateMachine.RegisterTransition(L_EoCState.PhaseChange1st, L_EoCState.Phase2Wait, false,
                () => State(L_EoCState.PhaseChange1st).Time >= 120,
                () =>
                {
                    npc.dontTakeDamage = false;
                    npc.ai[2] = 0;
                    npc.netUpdate = true;
                });

            // P1
            StateMachine.RegisterTransition(L_EoCState.FourCornersWait, L_EoCState.NormalDash, false,
                () => State(L_EoCState.FourCornersWait).Time > 180,
                () => npc.netUpdate = true);

            StateMachine.RegisterTransition(L_EoCState.NormalDash, L_EoCState.MoonShoot, false,
                () => npc.ai[2] >= 3,
                () => npc.ai[2] = 0);

            StateMachine.RegisterTransition(L_EoCState.MoonShoot, L_EoCState.FourCornersWait, false,
                () => State(L_EoCState.MoonShoot).Time > 90,
                () => { });

            // P2 / P3 挂机 → 下一招式
            StateMachine.RegisterTransition(L_EoCState.Phase2Wait, L_EoCState.NormalFastDash, false,
                () => State(L_EoCState.Phase2Wait).Time > 90 && npc.ai[3] < 2,
                () =>
                {
                    P3AttackChange++;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10) npc.netSpam = 10;
                });

            StateMachine.RegisterTransition(L_EoCState.Phase2Wait, L_EoCState.P3FastDash, false,
                () => State(L_EoCState.Phase2Wait).Time > 90 && npc.ai[3] >= 2 && P3AttackChange % 2 == 0,
                () =>
                {
                    P3AttackChange++;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10) npc.netSpam = 10;
                });

            StateMachine.RegisterTransition(L_EoCState.Phase2Wait, L_EoCState.P3MoonShootDash, false,
                () => State(L_EoCState.Phase2Wait).Time > 90 && npc.ai[3] >= 2 && P3AttackChange % 2 != 0,
                () =>
                {
                    P3AttackChange++;
                    npc.netUpdate = true;
                    if (npc.netSpam > 10) npc.netSpam = 10;
                });

            StateMachine.RegisterTransition(L_EoCState.NormalFastDash, L_EoCState.NormalTpDash, false,
                () => npc.ai[2] >= npc.localAI[0],
                () =>
                {
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.netUpdate = true;
                });

            StateMachine.RegisterTransition(L_EoCState.NormalTpDash, L_EoCState.Phase2Wait, false,
                () => State(L_EoCState.NormalTpDash).Time > 150,
                () =>
                {
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.localAI[1] = 0;
                    npc.localAI[2] = 0;
                    npc.netUpdate = true;
                });

            StateMachine.RegisterTransition(L_EoCState.P3FastDash, L_EoCState.ChooseRandom, false,
                () => State(L_EoCState.P3FastDash).Time > 58 && npc.ai[2] >= npc.localAI[0],
                () =>
                {
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.netUpdate = true;
                });

            StateMachine.RegisterTransition(L_EoCState.FastTpDashs, L_EoCState.ChooseRandom, false,
                () => State(L_EoCState.FastTpDashs).Time > 80 + 30 && npc.ai[2] <= 0,
                () =>
                {
                    RecordLast();
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.localAI[1] = 0;
                    npc.localAI[2] = 0;
                    npc.netUpdate = true;
                });

            // 拘束类 → 状态24
            StateMachine.RegisterTransition(L_EoCState.RestraintTriangle, L_EoCState.ChooseNextAttack, false,
                () => State(L_EoCState.RestraintTriangle).Time > 80 + 15 * 6 + 20 - (npc.ai[3] == 3 ? 10 : 0),
                RestraintExit);

            StateMachine.RegisterTransition(L_EoCState.RestraintSquare, L_EoCState.ChooseNextAttack, false,
                () => State(L_EoCState.RestraintSquare).Time > 80 + 15 * 8 + 40 - (npc.ai[3] == 3 ? 30 : 0),
                RestraintExit);

            StateMachine.RegisterTransition(L_EoCState.RestraintHexagon, L_EoCState.ChooseNextAttack, false,
                () => State(L_EoCState.RestraintHexagon).Time > 80 + 15 * 6 + 20 - (npc.ai[3] == 3 ? 10 : 0),
                RestraintExit);

            StateMachine.RegisterTransition(L_EoCState.RestraintOctagonal, L_EoCState.ChooseNextAttack, false,
                () => State(L_EoCState.RestraintOctagonal).Time > 80 + 8 * 15 + 40 - (npc.ai[3] == 3 ? 30 : 0),
                RestraintExit);

            StateMachine.RegisterTransition(L_EoCState.RestraintRound, L_EoCState.ChooseNextAttack, false,
                () => State(L_EoCState.RestraintRound).Time > 80 + 50 + 50 + 10,
                RestraintExit);

            StateMachine.RegisterTransition(L_EoCState.FaintVisibleFourRowsScythe, L_EoCState.ChooseNextAttack, false,
                () => State(L_EoCState.FaintVisibleFourRowsScythe).Time > 80 + 120 - (npc.ai[3] == 3 ? 20 : 0),
                () =>
                {
                    RecordLast();
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.localAI[1] = 0;
                    npc.localAI[2] = 0;
                    npc.localAI[3] = 0;
                    npc.netUpdate = true;
                });

            StateMachine.RegisterTransition(L_EoCState.FaintVisibleRoundScythe, L_EoCState.ChooseNextAttack, false,
                () => State(L_EoCState.FaintVisibleRoundScythe).Time > 80 + 110 + (npc.ai[3] >= 3 ? 0 : 10),
                () =>
                {
                    RecordLast();
                    npc.alpha = 0;
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.localAI[1] = 0;
                    npc.localAI[2] = 0;
                    npc.localAI[3] = 0;
                    npc.netUpdate = true;
                });

            // 超级tp冲 → P2挂机
            StateMachine.RegisterTransition(L_EoCState.SuperTpDash, L_EoCState.Phase2Wait, false,
                () => State(L_EoCState.SuperTpDash).Time > 150,
                () =>
                {
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.localAI[1] = 0;
                    npc.localAI[2] = 0;
                    npc.netUpdate = true;
                });

            // P3冲刺散射 → 随机招式
            StateMachine.RegisterTransition(L_EoCState.P3MoonShootDash, L_EoCState.ChooseRandom, false,
                () => npc.ai[2] >= npc.localAI[0],
                () =>
                {
                    npc.ai[2] = 0;
                    npc.localAI[0] = 0;
                    npc.localAI[1] = 0;
                    npc.netUpdate = true;
                });
        }

        private EntityAIState<L_EoCState> State(L_EoCState id)
        {
            return StateMachine.StateRegistry[id];
        }

        private void PhaseChangeTo1st()
        {
            npc.defense = 0;
            npc.ai[2] = 0;
            npc.ai[3] = 1;
            npc.netUpdate = true;
            if (npc.netSpam > 10) npc.netSpam = 10;
        }

        private void PhaseChangeTo2nd()
        {
            npc.ai[2] = 0;
            npc.ai[3] = 2;
            npc.netUpdate = true;
            if (npc.netSpam > 10) npc.netSpam = 10;
        }

        private void PhaseChangeTo3rd()
        {
            FargoSoulsUtil.ClearHostileProjectiles(2, npc.whoAmI);
            npc.ai[2] = 0;
            npc.ai[3] = 3;
            npc.localAI[0] = 0;
            npc.localAI[1] = 0;
            npc.localAI[2] = 0;
            npc.localAI[3] = 0;
            npc.netUpdate = true;
            if (npc.netSpam > 10) npc.netSpam = 10;
        }

        private void RestraintExit()
        {
            RecordLast();
            npc.ai[2] = 0;
            npc.localAI[0] = 0;
            npc.localAI[1] = 0;
            npc.localAI[2] = 0;
            npc.netUpdate = true;
        }
        #endregion

        #region 状态行为
        //P3转P4
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.PhaseChange3rd)]
        public void PhaseChange3rdBehavior()
        {
            var state = State(L_EoCState.PhaseChange3rd);
            Player player = Main.player[npc.target];

            npc.velocity *= 0.98f;
            npc.alpha += 4;
            npc.dontTakeDamage = true;
            RotateTowards(npc, player.Center, 0.08f);
            if (npc.alpha > 255)
                npc.alpha = 255;
            if (++state.Time >= 60)
            {
                SoundEngine.PlaySound(SoundID.Roar, npc.HasValidTarget ? Main.player[npc.target].Center : npc.Center);
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<GlowRing>(), 0, 0f, Main.myPlayer, npc.whoAmI, npc.type);
                // ChooseNext + defense-=30 等由转换到 ChooseNextAttack 的回调处理
            }
        }

        //P2转P3
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.PhaseChange2nd)]
        public void PhaseChange2ndBehavior()
        {
            var state = State(L_EoCState.PhaseChange2nd);

            npc.velocity *= 0.96f;
            npc.dontTakeDamage = true;
            if (npc.velocity.Length() < 0.1f)
                npc.velocity = Vector2.Zero;
            if (state.Time < 60)
            {
                npc.ai[2] += 0.012f;
                if (npc.ai[2] > 0.72f)
                    npc.ai[2] = 0.72f;
            }
            else if (state.Time < 120)
            {
                npc.ai[0] = 2f;
                npc.ai[2] -= 0.012f;
                if (npc.ai[2] < 0f)
                    npc.ai[2] = 0f;
                if (state.Time == 60)
                {
                    SoundEngine.PlaySound(3, (int)npc.position.X, (int)npc.position.Y);
                    for (int i = 0; i < 20; i++)
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Vortex, Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                    SoundEngine.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                }
            }
            npc.rotation += npc.ai[2];
            ++state.Time;
        }

        //P1转P2
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.PhaseChange1st)]
        public void PhaseChange1stBehavior()
        {
            var state = State(L_EoCState.PhaseChange1st);

            npc.velocity *= 0.96f;
            npc.dontTakeDamage = true;
            if (npc.velocity.Length() < 0.1f)
                npc.velocity = Vector2.Zero;
            if (state.Time < 60)
            {
                npc.ai[2] += 0.012f;
                if (npc.ai[2] > 0.72f)
                    npc.ai[2] = 0.72f;
            }
            else if (state.Time < 120)
            {
                npc.ai[0] = 2f;
                npc.ai[2] -= 0.012f;
                if (npc.ai[2] < 0f)
                    npc.ai[2] = 0f;
                if (state.Time == 60)
                {
                    SoundEngine.PlaySound(3, (int)npc.position.X, (int)npc.position.Y);
                    for (int i = 0; i < 2; i++)
                    {
                        Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 8);
                        Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 7);
                        Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), 6);
                    }
                    for (int i = 0; i < 20; i++)
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                    SoundEngine.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
                }
            }
            npc.rotation += npc.ai[2];
            ++state.Time;
        }

        //四角等待+射月镰
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.FourCornersWait)]
        public void FourCornersWaitBehavior()
        {
            var state = State(L_EoCState.FourCornersWait);
            Player player = Main.player[npc.target];

            int flagX = Math.Sign(npc.Center.X - player.Center.X);
            int flagY = Math.Sign(npc.Center.Y - player.Center.Y);

            Vector2 direct = npc.SafeDirectionTo(player.Center);
            RotateTowards(npc, player.Center, 0.03f);
            Vector2 targetCenter = player.Center + 300 * flagY * Vector2.UnitY + flagX * 300 * Vector2.UnitX;
            bool up = (targetCenter - npc.Center).Length() > 800;
            float speed = up ? 15f : 7.5f;
            float accel = up ? 0.36f : 0.18f;
            Movement(npc, targetCenter, speed, accel);
            if (state.Time % 60 == 0)
            {
                int n = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.ServantofCthulhu);
                if (n != Main.maxNPCs)
                {
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                }
                for (float i = 1; i < 5; i += 1.5f)
                {
                    if (FargoSoulsUtil.HostCheck)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, i * direct, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                }
                npc.netUpdate = true;
            }
            if (++state.Time > 180)
            {
                // 退出由转换到 NormalDash 处理（OnPopped 会自动归零 Time）
            }
        }

        //常态三连冲
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.NormalDash)]
        public void NormalDashBehavior()
        {
            var state = State(L_EoCState.NormalDash);
            Player player = Main.player[npc.target];

            Vector2 direction = npc.SafeDirectionTo(player.Center);
            if (state.Time == 0)
            {
                npc.rotation = direction.ToRotation() - MathHelper.PiOver2;
                float chargeSpeed = 12f;
                npc.velocity = chargeSpeed * direction * (0.4f * npc.ai[2] + 1f);
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 60);
            }
            if (state.Time > 40)
            {
                RotateTowards(npc, player.Center, 0.08f);
                npc.velocity *= 0.9556f;

                if (Math.Abs(npc.velocity.X) < 0.1)
                    npc.velocity.X = 0f;
                if (Math.Abs(npc.velocity.Y) < 0.1)
                    npc.velocity.Y = 0f;
            }
            else // 冲刺方向跟随速度
            {
                Vector2 vel = 1.5f * Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2);
                if (state.Time <= 70 && state.Time % 8 == 0 && FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
            }
            if (++state.Time > 85)
            {
                npc.ai[2] += 1f;
                state.Time = 0;
                // 冲满3次由转换到 MoonShoot 处理
            }
        }

        //眼状散射月矢
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.MoonShoot)]
        public void MoonShootBehavior()
        {
            var state = State(L_EoCState.MoonShoot);
            Player player = Main.player[npc.target];

            if (state.Time < 60)
            {
                npc.velocity *= 0.96f;
                RotateTowards(npc, player.Center, 0.08f);
                FancyFireballs(npc, state.Time);
            }
            else
            {
                float i = state.Time - 60f;
                for (float j = -1; j <= 1; j += 2)
                {
                    double angle = i * MathHelper.TwoPi / 20 * j;
                    Vector2 EllipseVel = new(150f * (float)Math.Cos(angle) * (1 - 0.15f * (float)Math.Sin(angle) * (float)Math.Sin(angle)), 300f * (float)Math.Sin(angle));
                    EllipseVel *= (j + 2f) / 2f;
                    Vector2 vel = EllipseVel.RotatedBy(npc.rotation + MathHelper.PiOver2) / 10f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.Center.X + 12 * vel.X, npc.Center.Y + 12 * vel.Y, 40);
                }
            }
            ++state.Time;
        }

        //P2P3挂机
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.Phase2Wait)]
        public void Phase2WaitBehavior()
        {
            var state = State(L_EoCState.Phase2Wait);
            Player player = Main.player[npc.target];

            if (npc.alpha > 0)
                npc.alpha -= 3;
            if (npc.alpha < 0)
                npc.alpha = 0;
            for (int i = 0; i < 3; i++)
            {
                int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                Main.dust[d].noGravity = true;
                Main.dust[d].noLight = true;
                Main.dust[d].velocity *= 4f;
            }
            float speed = 6f;
            float acceleration = 0.07f;
            float distance = (player.Center - 120f * Vector2.UnitY - npc.Center).Length();
            //对速度加速度的修正
            if (distance > 400f)
            {
                speed += 2f;
                acceleration += 0.1f;
                if (distance > 600f)
                {
                    speed += 2f;
                    acceleration += 0.1f;
                    if (distance > 800f)
                    {
                        speed += 1f;
                        acceleration += 0.05f;
                        if (distance > 1200)
                        {
                            speed += 5;
                            acceleration += 0.2f;
                        }
                    }
                }
            }
            if (Main.getGoodWorld)
            {
                speed += 1f;
                acceleration += 0.1f;
            }
            Movement(npc, player.Center - 12 * Vector2.UnitY, speed, acceleration);
            RotateTowards(npc, player.Center, 0.18f);
            ++state.Time;
            // 90帧后由转换根据阶段/P3AttackChange 选择下一招式
        }

        //P2高速冲刺 + 环形月矢镰刀
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.NormalFastDash)]
        public void NormalFastDashBehavior()
        {
            var state = State(L_EoCState.NormalFastDash);
            Player player = Main.player[npc.target];

            if (state.Time == 0)
            {
                float predictDistance = 20f;
                Vector2 targetDelta = player.Center - npc.Center;
                // 根据玩家速度预测提前量
                float playerSpeedFactor = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y) / 4f;

                float predictionMultiplier = 10f - playerSpeedFactor;
                if (predictionMultiplier < 5f)
                    predictionMultiplier = 5f;
                if (predictionMultiplier > 15f)
                    predictionMultiplier = 15f;

                predictionMultiplier *= 4f;
                predictDistance *= 1.3f;

                targetDelta.X -= player.velocity.X * predictionMultiplier;
                targetDelta.Y -= player.velocity.Y * predictionMultiplier / 4f;

                // 添加随机误差
                targetDelta.X *= 1f + Main.rand.Next(-10, 11) * 0.01f;
                targetDelta.Y *= 1f + Main.rand.Next(-10, 11) * 0.01f;

                float dirLength = targetDelta.Length();
                float originalDirLength = dirLength;
                dirLength = predictDistance / dirLength;
                npc.velocity.X = targetDelta.X * dirLength;
                npc.velocity.Y = targetDelta.Y * dirLength;
                // 添加随机偏移
                npc.velocity.X += Main.rand.Next(-20, 21) * 0.1f;
                npc.velocity.Y += Main.rand.Next(-20, 21) * 0.1f;
                if (originalDirLength < 100f) // 距离较近时交换 X/Y 方向以避免太直
                {
                    if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                    {
                        float x = Math.Abs(npc.velocity.X);
                        float y = Math.Abs(npc.velocity.Y);
                        if (npc.Center.X > player.Center.X)
                            y *= -1f;
                        if (npc.Center.Y > player.Center.Y)
                            x *= -1f;
                        npc.velocity.X = y;
                        npc.velocity.Y = x;
                    }
                }
                else if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y)) // 较远时求平均调整
                {
                    float avg = (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) / 2f;
                    float x = avg;
                    float y = avg;
                    if (npc.Center.X > player.Center.X)
                        y *= -1f;
                    if (npc.Center.Y > player.Center.Y)
                        x *= -1f;
                    npc.velocity.X = y;
                    npc.velocity.Y = x;
                }
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                if (npc.ai[2] == 0)
                    npc.localAI[0] = 6 + Main.rand.Next(1, 4);//次数
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                    FargoSoulsUtil.XWay(8, npc.GetSource_FromThis(), npc.Center, ModContent.ProjectileType<BloodScythe>(), 1.5f, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0);
                    for (int i = 0; i < 8; i++)
                    {
                        for (float j = -1; j <= 1; j += 2)
                        {
                            double angle = i * MathHelper.TwoPi / 8 * j;
                            Vector2 EllipseVel = 200 * Vector2.UnitX.RotatedBy(angle);
                            EllipseVel *= (j + 2f) / 2f;
                            Vector2 vel = EllipseVel / 15f;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.Center.X + 12 * vel.X, npc.Center.Y + 12 * vel.Y, 40);
                        }
                    }
                }
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
            }
            if (state.Time == 20 && Vector2.Distance(npc.position, player.position) < 200f)
                state.Time -= 1;
            if (state.Time > 20)
            {
                npc.velocity *= 0.95f;
                if (Math.Abs(npc.velocity.X) < 0.1)
                    npc.velocity.X = 0f;
                if (Math.Abs(npc.velocity.Y) < 0.1)
                    npc.velocity.Y = 0f;
                RotateTowards(npc, player.Center, 0.22f);
            }
            if (++state.Time > 33)
            {
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                npc.ai[2]++;
                state.Time = 0;
                // 冲满 localAI[0] 次由转换到 NormalTpDash 处理
            }
        }

        //tp冲刺
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.NormalTpDash)]
        public void NormalTpDashBehavior()
        {
            var state = State(L_EoCState.NormalTpDash);
            Player player = Main.player[npc.target];

            if (state.Time == 0)
            {
                Vector2 distance = Main.player[npc.target].Center - npc.Center;
                if (distance.X == 0)
                    distance.X = 1;
                const int Xmax = 700;
                const int Xmin = 550;
                if (Math.Abs(distance.X) > Xmax)
                    distance.X = Xmax * Math.Sign(distance.X);
                else if (Math.Abs(distance.X) < Xmin)
                    distance.X = Xmin * Math.Sign(distance.X);

                TeleportDirection = Main.rand.NextBool() ? 1 : -1;
                if (TeleportDirection == 0)
                    TeleportDirection = Main.rand.NextBool() ? 1 : -1; //first dash picks side towards player

                distance.X = Math.Abs(distance.X) * TeleportDirection;

                if (distance.Y > 0) //ensure to teleport above
                    distance.Y *= -1;

                const int Ymax = 400;
                const int Ymin = 150;
                if (Math.Abs(distance.Y) > Ymax)
                    distance.Y = Ymax * Math.Sign(distance.Y);
                if (Math.Abs(distance.Y) < Ymin)
                    distance.Y = Ymin * Math.Sign(distance.Y);

                distance.X += Main.rand.NextFloat(-50, 50);
                distance.Y += Main.rand.NextFloat(-200, 200); //randomness otherwise pattern basically becomes static
                npc.localAI[0] = distance.X + player.Center.X;
                npc.localAI[1] = distance.Y + player.Center.Y;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), Vector2.Zero, ModContent.ProjectileType<EoCTpTelegraph>(), 0, 0, Main.myPlayer, 120, npc.whoAmI);
                }
                npc.netUpdate = true;
            }
            if (state.Time < 120)
            {
                float speed = 6f;
                float acceleration = 0.07f;
                float distance = (player.Center - 120f * Vector2.UnitY - npc.Center).Length();
                //对速度加速度的修正
                if (distance > 400f)
                {
                    speed += 2f;
                    acceleration += 0.1f;
                    if (distance > 600f)
                    {
                        speed += 2f;
                        acceleration += 0.1f;
                        if (distance > 800f)
                        {
                            speed += 1f;
                            acceleration += 0.05f;
                        }
                    }
                }
                if (Main.getGoodWorld)
                {
                    speed += 1f;
                    acceleration += 0.1f;
                }
                Movement(npc, player.Center - 12 * Vector2.UnitY, speed, acceleration);
                npc.velocity *= 0.95f;
                RotateTowards(npc, player.Center, 0.10f);
                npc.alpha += 4;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 4f;
                    }
                }
            }
            if (state.Time == 90)
            {
                npc.localAI[2] = (player.Center - new Vector2(npc.localAI[0], npc.localAI[1])).ToRotation();
                npc.rotation = npc.localAI[2] - MathHelper.PiOver2;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), 48 * npc.localAI[2].ToRotationVector2(), ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                        -1, (float)FalseEoC.MoveType.Straight, 60);
                }
            }
            if (state.Time == 120)
            {
                npc.Center = new Vector2(npc.localAI[0], npc.localAI[1]);
                ReleaseDust(npc, 500);
                ScreenShakeSystem.StartShake(10);
                npc.velocity = 72 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                npc.netUpdate = true;
            }
            if (state.Time > 120)
            {
                npc.velocity *= 0.975f;
                if (npc.alpha > 0)
                    npc.alpha = 0;
                for (int i = 0; i < 8; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
                if (state.Time % 3 == 0 && FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                }
            }
            ++state.Time;
        }

        //P3阴间冲刺
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.P3FastDash)]
        public void P3FastDashBehavior()
        {
            var state = State(L_EoCState.P3FastDash);
            Player player = Main.player[npc.target];

            if (state.Time == 0)
            {
                float predictDistance = 20f;
                Vector2 targetDelta = player.Center - npc.Center;
                // 根据玩家速度预测提前量
                float playerSpeedFactor = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y) / 4f;

                float predictionMultiplier = 10f - playerSpeedFactor;
                if (predictionMultiplier < 5f)
                    predictionMultiplier = 5f;
                if (predictionMultiplier > 15f)
                    predictionMultiplier = 15f;

                predictionMultiplier *= 5f;
                predictDistance *= 1.6f;

                targetDelta.X -= player.velocity.X * predictionMultiplier;
                targetDelta.Y -= player.velocity.Y * predictionMultiplier / 4f;

                // 添加随机误差
                targetDelta.X *= 1f + Main.rand.Next(-10, 11) * 0.01f;
                targetDelta.Y *= 1f + Main.rand.Next(-10, 11) * 0.01f;

                float dirLength = targetDelta.Length();
                float originalDirLength = dirLength;
                dirLength = predictDistance / dirLength;
                npc.velocity.X = targetDelta.X * dirLength;
                npc.velocity.Y = targetDelta.Y * dirLength;
                // 添加随机偏移
                npc.velocity.X += Main.rand.Next(-20, 21) * 0.1f;
                npc.velocity.Y += Main.rand.Next(-20, 21) * 0.1f;
                if (originalDirLength < 100f) // 距离较近时交换 X/Y 方向以避免太直
                {
                    if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                    {
                        float x = Math.Abs(npc.velocity.X);
                        float y = Math.Abs(npc.velocity.Y);
                        if (npc.Center.X > player.Center.X)
                            y *= -1f;
                        if (npc.Center.Y > player.Center.Y)
                            x *= -1f;
                        npc.velocity.X = y;
                        npc.velocity.Y = x;
                    }
                }
                else if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y)) // 较远时求平均调整
                {
                    float avg = (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) / 2f;
                    float x = avg;
                    float y = avg;
                    if (npc.Center.X > player.Center.X)
                        y *= -1f;
                    if (npc.Center.Y > player.Center.Y)
                        x *= -1f;
                    npc.velocity.X = y;
                    npc.velocity.Y = x;
                }
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                if (npc.ai[2] == 0)
                    npc.localAI[0] = 8 + Main.rand.Next(1, 7);//次数
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                    FargoSoulsUtil.XWay(8, npc.GetSource_FromThis(), npc.Center, ModContent.ProjectileType<BloodScythe>(), 1.5f, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0);
                    for (int i = 0; i < 8; i++)
                    {
                        for (float j = -1; j <= 1; j += 2)
                        {
                            double angle = i * MathHelper.TwoPi / 8 * j;
                            Vector2 EllipseVel = 200 * Vector2.UnitX.RotatedBy(angle);
                            EllipseVel *= (j + 3f) / 2f;
                            Vector2 vel = EllipseVel / 30f;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, player.Center.X, player.Center.Y, 40);
                            j += 2;
                        }
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        for (float j = -1; j <= 1; j += 2)
                        {
                            j += 2;
                            double angle = i * MathHelper.TwoPi / 8 * j;
                            Vector2 EllipseVel = 200 * Vector2.UnitX.RotatedBy(angle);
                            EllipseVel *= (j + 2f) / 3f;
                            Vector2 vel = EllipseVel / 10f;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.Center.X + 12 * vel.X, npc.Center.Y + 12 * vel.Y, 40);
                        }
                    }
                }
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
            }
            if (state.Time == 15 && Vector2.Distance(npc.position, player.position) < 200f)
                state.Time -= 1;
            if (state.Time > 15)
            {
                npc.velocity *= 0.95f;
                if (Math.Abs(npc.velocity.X) < 0.1)
                    npc.velocity.X = 0f;
                if (Math.Abs(npc.velocity.Y) < 0.1)
                    npc.velocity.Y = 0f;
                RotateTowards(npc, player.Center, 0.22f);
            }
            if (++state.Time > 28)
            {
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                npc.ai[2]++;
                if (npc.ai[2] < npc.localAI[0])
                    state.Time = 0;
                // 冲满且停留30帧后由转换到 ChooseRandom 处理
            }
        }

        //高频tp冲刺
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.FastTpDashs)]
        public void FastTpDashsBehavior()
        {
            var state = State(L_EoCState.FastTpDashs);
            Player player = Main.player[npc.target];

            int intervel = 80;
            if (state.Time == 0)
            {
                if (npc.ai[2] == 0)
                    npc.ai[2] = Main.rand.Next(4, 7);
                Vector2 distance = Main.rand.Next(400, 701) * Main.rand.NextVector2Unit();

                npc.localAI[0] = distance.X + player.Center.X;
                npc.localAI[1] = distance.Y + player.Center.Y;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), Vector2.Zero, ModContent.ProjectileType<EoCTpTelegraph>(),
                        -1, 0, Main.myPlayer, intervel, npc.whoAmI);
                }
                npc.netUpdate = true;
            }
            if (state.Time < intervel && state.Time > 0.75f * (float)intervel)
            {
                float speed = 6f;
                float acceleration = 0.07f;
                float distance = (player.Center - 120f * Vector2.UnitY - npc.Center).Length();
                //对速度加速度的修正
                if (distance > 400f)
                {
                    speed += 2f;
                    acceleration += 0.1f;
                    if (distance > 600f)
                    {
                        speed += 2f;
                        acceleration += 0.1f;
                        if (distance > 800f)
                        {
                            speed += 1f;
                            acceleration += 0.05f;
                        }
                    }
                }
                if (Main.getGoodWorld)
                {
                    speed += 1f;
                    acceleration += 0.1f;
                }
                Movement(npc, player.Center - 12 * Vector2.UnitY, speed, acceleration);
                npc.velocity *= 0.95f;
                RotateTowards(npc, player.Center, 0.10f);
                npc.alpha += 4;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 3; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            if (state.Time < 3 * (float)intervel / 5f)
            {
                npc.velocity *= 0.97f;
                npc.alpha += 6;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                else
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 4f;
                    }
                }
                if (state.Time % 2 == 0 && FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                }
            }
            if (state.Time == 0.75f * intervel)
            {
                npc.localAI[2] = (player.Center - new Vector2(npc.localAI[0], npc.localAI[1])).ToRotation();
                npc.rotation = npc.localAI[2] - MathHelper.PiOver2;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), 48 * npc.localAI[2].ToRotationVector2(), ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                        -1, (float)FalseEoC.MoveType.Straight, 60);
                }
            }
            if (state.Time == intervel)
            {
                npc.Center = new Vector2(npc.localAI[0], npc.localAI[1]);
                ReleaseDust(npc, 500);
                ScreenShakeSystem.StartShake(10);
                npc.velocity = 72 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 dir = npc.SafeDirectionTo(player.Center).RotatedBy(MathHelper.PiOver2);

                        for (float j = -1; j <= 1; j += 1)
                        {
                            Vector2 target = player.Center + j * 600 * dir;
                            double angle = i * MathHelper.TwoPi / 8;
                            Vector2 EllipseVel = 200 * Vector2.UnitX.RotatedBy(angle);
                            EllipseVel *= 2f;
                            Vector2 vel = EllipseVel.RotatedBy(npc.rotation + MathHelper.PiOver2) / 20f;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, target.X, target.Y, 40);
                        }
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        for (float j = -1; j <= 1; j += 2)
                        {
                            double angle = i * MathHelper.TwoPi / 8 * j;
                            Vector2 EllipseVel = 200 * Vector2.UnitX.RotatedBy(angle);
                            EllipseVel *= (j + 2f) / 4f;
                            Vector2 vel = EllipseVel.RotatedBy(npc.rotation + MathHelper.PiOver2) / 10f;
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.Center.X + 12 * vel.X, npc.Center.Y + 12 * vel.Y, 40);
                        }
                    }
                }
                npc.netUpdate = true;
            }
            if (++state.Time > intervel)
            {
                npc.velocity *= 0.97f;
                if (npc.alpha > 0)
                    npc.alpha = 0;
                for (int i = 0; i < 8; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
                if (npc.ai[2] > 0)
                {
                    npc.ai[2] -= 1;
                }
                if (npc.ai[2] != 0)
                {
                    state.Time = 0;
                }
                npc.localAI[0] = 0f;
                npc.localAI[1] = 0f;
                npc.localAI[2] = 0f;
                if (npc.ai[2] != 0)
                    npc.netUpdate = true;
                // 次数耗尽且停留30帧后由转换到 ChooseRandom 处理
            }
        }

        //三角拘束
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.RestraintTriangle)]
        public void RestraintTriangleBehavior()
        {
            var state = State(L_EoCState.RestraintTriangle);
            Player player = Main.player[npc.target];

            if (state.Time == 0)
            {
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Triangle;
                Vector2 dir = -npc.SafeDirectionTo(player.Center);
                npc.localAI[0] = player.Center.X + player.velocity.X;
                npc.localAI[1] = player.Center.Y + player.velocity.Y;
                npc.localAI[2] = dir.ToRotation();
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 150 * MathF.PI / 180f);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, npc.localAI[2] + 150 * MathF.PI / 180f);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                        -50, (int)movetype, 6 * 15);
                }
                npc.netUpdate = true;
            }
            if (state.Time % 15 == 0 && state.Time <= 5 * 15 && state.Time > 0)
            {
                npc.localAI[2] += 120 * MathF.PI / 180f;
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, npc.localAI[2] + 150 * MathF.PI / 180f);
                }
                npc.netUpdate = true;
            }
            if ((state.Time - 80) % 15 == 0 && state.Time >= 80 && state.Time <= 80 + 15 * 6)
            {
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 30);
                ReleaseDust(npc, 100);
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                if (state.Time == 80)
                    ScreenShakeSystem.StartShake(5);
                npc.netUpdate = true;
            }
            if (state.Time > 80 && state.Time < 80 + 6 * 15)
            {
                if (state.Time % 3 == 0)
                {
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.03f * npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, npc.localAI[0], npc.localAI[1], 75);
                    Main.projectile[p].scale *= 0.8f;
                    Main.projectile[p].width = 6;
                    Main.projectile[p].height = 6;
                }
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.05f * npc.velocity.RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer);
            }
            if (state.Time > 80 + 6 * 15)
            {
                npc.velocity *= 0.92f;
                npc.alpha += 3;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            npc.alpha += 5;
            if (npc.alpha > 255)
                npc.alpha = 255;
            ++state.Time;
        }

        //方形拘束
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.RestraintSquare)]
        public void RestraintSquareBehavior()
        {
            var state = State(L_EoCState.RestraintSquare);
            Player player = Main.player[npc.target];

            if (state.Time == 0)
            {
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Square;
                Vector2 dir = -npc.SafeDirectionTo(player.Center);
                npc.localAI[0] = player.Center.X + player.velocity.X;
                npc.localAI[1] = player.Center.Y + player.velocity.Y;
                npc.localAI[2] = dir.ToRotation();
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 707 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 135 * MathF.PI / 180f);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, npc.localAI[2] + 135 * MathF.PI / 180f);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 66.67f * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                        -50, (int)movetype, 8 * 15);
                }
                npc.netUpdate = true;
            }
            if (state.Time % 15 == 0 && state.Time <= 8 * 15 && state.Time > 0)
            {
                npc.localAI[2] += 90 * MathF.PI / 180f;
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 707 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, npc.localAI[2] + 135 * MathF.PI / 180f);
                }
                npc.netUpdate = true;
            }
            if ((state.Time - 80) % 15 == 1 && state.Time >= 80 + 1 && state.Time <= 80 + 15 * 8 + 1)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 30);
                    ReleaseDust(npc, 100);
                }
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                float speed = npc.velocity.Length();
                Vector2 veldir = npc.velocity.SafeNormalize(Vector2.Zero);
                if (speed > 66.7f)
                    npc.velocity = veldir * 66.7f;
                if (state.Time == 80 + 1)
                    ScreenShakeSystem.StartShake(5);
                npc.netUpdate = true;
            }
            if (state.Time > 80 && state.Time < 80 + 8 * 15 && FargoSoulsUtil.HostCheck)
            {
                if (state.Time % 3 == 0)
                {
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.03f * npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, npc.localAI[0], npc.localAI[1], 75);
                    Main.projectile[p].scale *= 0.8f;
                    Main.projectile[p].width = 6;
                    Main.projectile[p].height = 6;
                }
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.05f * npc.velocity.RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer);
            }
            if (state.Time > 80 + 8 * 15)
            {
                npc.velocity *= 0.92f;
                npc.alpha += 3;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            npc.alpha += 5;
            if (npc.alpha > 255)
                npc.alpha = 255;
            ++state.Time;
        }

        //六芒星拘束
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.RestraintHexagon)]
        public void RestraintHexagonBehavior()
        {
            var state = State(L_EoCState.RestraintHexagon);
            Player player = Main.player[npc.target];

            FalseEoC.MoveType movetype = FalseEoC.MoveType.Hexagon;
            if (state.Time == 0)
            {
                Vector2 dir = -npc.SafeDirectionTo(player.Center);
                npc.localAI[0] = player.Center.X + player.velocity.X;
                npc.localAI[1] = player.Center.Y + player.velocity.Y;
                npc.localAI[2] = dir.ToRotation();
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 150 * MathF.PI / 180f);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, npc.localAI[2] + 150 * MathF.PI / 180f);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                        -50, (int)movetype, 3 * 15);
                }
                npc.netUpdate = true;
            }
            if (state.Time % 15 == 0 && state.Time <= 5 * 15 && state.Time > 0)
            {
                npc.localAI[2] += 120 * MathF.PI / 180f;
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                if (state.Time % 45 == 0)
                {
                    npc.localAI[2] -= 60 * MathF.PI / 180f;
                    spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                    Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 150 * MathF.PI / 180f);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                    -50, (int)movetype, 3 * 15);
                }
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, npc.localAI[2] + 150 * MathF.PI / 180f);
                }
                npc.netUpdate = true;
            }
            if ((state.Time - 80) % 15 == 0 && state.Time >= 80 && state.Time <= 80 + 15 * 6)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 30);
                    ReleaseDust(npc, 100);
                }
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                if (state.Time == 80)
                    ScreenShakeSystem.StartShake(5);
                npc.netUpdate = true;
            }
            if (state.Time > 80 && state.Time < 80 + 6 * 15 && FargoSoulsUtil.HostCheck)
            {
                if (state.Time % 3 == 0)
                {
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.03f * npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, npc.localAI[0], npc.localAI[1], 75);
                    Main.projectile[p].scale *= 0.8f;
                    Main.projectile[p].width = 6;
                    Main.projectile[p].height = 6;
                }
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.05f * npc.velocity.RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer);
            }
            if (state.Time > 80 + 6 * 15)
            {
                npc.velocity *= 0.92f;
                npc.alpha += 3;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            npc.alpha += 5;
            if (npc.alpha > 255)
                npc.alpha = 255;
            ++state.Time;
        }

        //八芒星拘束
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.RestraintOctagonal)]
        public void RestraintOctagonalBehavior()
        {
            var state = State(L_EoCState.RestraintOctagonal);
            Player player = Main.player[npc.target];

            if (state.Time == 0)
            {
                npc.ai[2] = Main.rand.Next(0, 3);
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Octagonal;
                Vector2 dir = -npc.SafeDirectionTo(player.Center);
                npc.localAI[0] = player.Center.X + player.velocity.X;
                npc.localAI[1] = player.Center.Y + player.velocity.Y;
                npc.localAI[2] = dir.ToRotation();
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 157.5f * MathF.PI / 180f);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, npc.localAI[2] + 157.5f * MathF.PI / 180f);

                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                        -50, (int)movetype, 15 * 8);
                }
                npc.netUpdate = true;
            }
            if (state.Time % 15 == 0 && state.Time <= 7 * 15 && state.Time > 0)
            {
                npc.localAI[2] += 135 * MathF.PI / 180f;
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 693 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, npc.localAI[2] + 157.5f * MathF.PI / 180f);
                }
                npc.netUpdate = true;
            }
            if ((state.Time - 80) % 15 == 0 && state.Time >= 80 && state.Time <= 80 + 8 * 15)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 30);
                    ReleaseDust(npc, 100);
                }
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                if (state.Time == 80)
                    ScreenShakeSystem.StartShake(5);
                npc.netUpdate = true;
            }
            if (state.Time > 80 && state.Time < 80 + 15 * 8 && FargoSoulsUtil.HostCheck)
            {
                if (state.Time % 3 == 0)
                {
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.03f * npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, npc.localAI[0], npc.localAI[1], 100);
                    Main.projectile[p].scale *= 0.8f;
                    Main.projectile[p].width = 6;
                    Main.projectile[p].height = 6;
                }

                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.05f * npc.velocity.RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer);
            }
            if (state.Time > 80 + 8 * 15)
            {
                npc.velocity *= 0.92f;
                npc.alpha += 3;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            npc.alpha += 5;
            if (npc.alpha > 255)
                npc.alpha = 255;
            ++state.Time;
        }

        //圆形拘束
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.RestraintRound)]
        public void RestraintRoundBehavior()
        {
            var state = State(L_EoCState.RestraintRound);
            Player player = Main.player[npc.target];

            int r = 600;
            if (state.Time == 0)
            {
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Round;
                Vector2 dir = -npc.SafeDirectionTo(player.Center);
                npc.localAI[0] = player.Center.X + player.velocity.X;
                npc.localAI[1] = player.Center.Y + player.velocity.Y;
                npc.localAI[2] = dir.ToRotation();
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + r * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                Vector2 vel = Vector2.UnitX.RotatedBy(npc.localAI[2] + 90f * MathF.PI / 180f);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, npc.localAI[2] + 90 * MathF.PI / 180f);

                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                        -50, (int)movetype, 50 * 2);
                }
                npc.netUpdate = true;
            }
            if (state.Time % 10 == 0 && state.Time <= 50 * 2 && state.Time > 0)
            {
                npc.localAI[2] += 15f * 0.1f;
                Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + r * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, npc.localAI[2] + 90 * MathF.PI / 180f);
                }
                npc.netUpdate = true;
            }
            if ((state.Time - 80) % 10 == 0 && state.Time >= 80 && state.Time <= 80 + 50 * 2)
            {
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 30);
                    ReleaseDust(npc, 100);
                }
                if ((state.Time - 80) % 20 == 0)
                    SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                if (state.Time == 80)
                    ScreenShakeSystem.StartShake(5);
                npc.netUpdate = true;
            }
            if (state.Time > 80 && state.Time < 80 + 50 * 2 && FargoSoulsUtil.HostCheck)
            {
                if (state.Time % 3 == 0)
                {
                    int p = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.03f * npc.velocity, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer, npc.localAI[0], npc.localAI[1], 65);
                    Main.projectile[p].scale *= 0.8f;
                    Main.projectile[p].width = 6;
                    Main.projectile[p].height = 6;
                }

                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, 0.05f * npc.velocity.RotatedBy(-MathHelper.PiOver2), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1, Main.myPlayer);
            }

            if (state.Time > 80 + 50 * 2)
            {
                npc.velocity *= 0.92f;
                npc.alpha += 6;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            else if (state.Time >= 80)
            {
                npc.velocity = npc.velocity.RotatedBy(2f / 15f);
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
            }
            else
            {
                npc.alpha += 4;
                if (npc.alpha > 255)
                    npc.alpha = 255;
            }
            ++state.Time;
        }

        //四排月镰
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.FaintVisibleFourRowsScythe)]
        public void FaintVisibleFourRowsScytheBehavior()
        {
            var state = State(L_EoCState.FaintVisibleFourRowsScythe);
            Player player = Main.player[npc.target];

            if (state.Time == 0 || state.Time == 60)
            {
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Straight;
                if (state.Time == 0)
                {
                    npc.localAI[0] = player.Center.X + player.velocity.X;
                    npc.localAI[1] = player.Center.Y + player.velocity.Y;
                }
                npc.localAI[2] = Main.rand.NextFloat(0, MathHelper.TwoPi);
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * MathHelper.PiOver2 + MathHelper.PiOver4 + npc.localAI[2];
                    Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 1000 * Vector2.UnitX.RotatedBy(angle);
                    Vector2 vel = Vector2.UnitX.RotatedBy(angle + 135 * MathF.PI / 180f);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, vel.ToRotation());
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                        -50, (int)movetype, 25);
                    }
                }
                npc.netUpdate = true;
            }
            npc.localAI[2] = Main.rand.NextFloat(0, MathHelper.TwoPi);
            if (state.Time == 80 || state.Time == 80 + 60)
            {
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                ScreenShakeSystem.StartShake(5);
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Straight;
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * MathHelper.PiOver2 + MathHelper.PiOver4 + npc.localAI[2];
                    Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 1000 * Vector2.UnitX.RotatedBy(angle);
                    Vector2 vel = Vector2.UnitX.RotatedBy(angle + 135 * MathF.PI / 180f);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int p = Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                        -1, (int)movetype, 15);
                        Main.projectile[p].localAI[2] = 1;//启用发射弹幕
                    }
                }
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                npc.netUpdate = true;
            }
            if (state.Time < 80 + 140)
            {
                npc.velocity *= 0.94f;
                npc.alpha += 5;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                for (int i = 0; i < 6; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
            }
            ++state.Time;
        }

        //圆形月镰
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.FaintVisibleRoundScythe)]
        public void FaintVisibleRoundScytheBehavior()
        {
            var state = State(L_EoCState.FaintVisibleRoundScythe);
            Player player = Main.player[npc.target];

            if (state.Time == 0 || state.Time == 60)
            {
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Round;
                if (state.Time == 0)
                {
                    npc.localAI[0] = player.Center.X + player.velocity.X;
                    npc.localAI[1] = player.Center.Y + player.velocity.Y;
                }
                npc.localAI[2] = Main.rand.NextFloat(0, MathHelper.TwoPi);
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * MathHelper.PiOver2 + MathHelper.PiOver4 + npc.localAI[2];
                    Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 600 * Vector2.UnitX.RotatedBy(angle);
                    Vector2 vel = Vector2.UnitX.RotatedBy(angle + 90 * MathF.PI / 180f);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, Vector2.Zero, ModContent.ProjectileType<SuperEoCTpTelegraph>(), 0, 0, Main.myPlayer,
                        80, npc.whoAmI, vel.ToRotation());
                        Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                        -50, (int)movetype, 24);//一半
                    }
                }
                npc.netUpdate = true;
            }
            if (state.Time == 80 || state.Time == 80 + 60)
            {
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.alpha = 0;
                ScreenShakeSystem.StartShake(5);
                FalseEoC.MoveType movetype = FalseEoC.MoveType.Round;
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * MathHelper.PiOver2 + MathHelper.PiOver4 + npc.localAI[2];
                    Vector2 spawnPos = new Vector2(npc.localAI[0], npc.localAI[1]) + 600 * Vector2.UnitX.RotatedBy(angle);
                    Vector2 vel = Vector2.UnitX.RotatedBy(angle + 90 * MathF.PI / 180f);
                    if (FargoSoulsUtil.HostCheck)
                    {
                        int p = Projectile.NewProjectile(npc.GetSource_FromThis(), spawnPos, 80 * vel, ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                            -1, (int)movetype, 24);
                        Main.projectile[p].localAI[2] = 1;//启用发射弹幕
                    }
                }
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                npc.netUpdate = true;
            }
            if (state.Time < 80 + 40 + 60)
            {
                npc.velocity *= 0.94f;
                npc.alpha += 6;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                else
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 4f;
                    }
                }
            }
            ++state.Time;
        }

        //状态24：选择下一招式
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.ChooseNextAttack)]
        public void ChooseNextAttackBehavior()
        {
            var state = State(L_EoCState.ChooseNextAttack);
            // 招式选择由转换完成：P3→SuperTpDash，P4→RegisterWeightedTransition 加权随机招式
            ++state.Time;
        }

        //超级tp冲
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.SuperTpDash)]
        public void SuperTpDashBehavior()
        {
            var state = State(L_EoCState.SuperTpDash);
            Player player = Main.player[npc.target];

            if (state.Time == 0)
            {
                Vector2 distance = player.Center + 350 * Main.rand.NextVector2Unit();

                distance.X += Main.rand.NextFloat(-50, 50);
                distance.Y += Main.rand.NextFloat(-200, 200); //randomness otherwise pattern basically becomes static
                npc.localAI[0] = distance.X;
                npc.localAI[1] = distance.Y;
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), Vector2.Zero, ModContent.ProjectileType<EoCTpTelegraph>(), 0, 0, Main.myPlayer, 120, npc.whoAmI);
                npc.netUpdate = true;
            }
            if (state.Time < 120)
            {
                float speed = 6f;
                float acceleration = 0.07f;
                float distance = (player.Center - 120f * Vector2.UnitY - npc.Center).Length();
                //对速度加速度的修正
                if (distance > 400f)
                {
                    speed += 2f;
                    acceleration += 0.1f;
                    if (distance > 600f)
                    {
                        speed += 2f;
                        acceleration += 0.1f;
                        if (distance > 800f)
                        {
                            speed += 1f;
                            acceleration += 0.05f;
                        }
                    }
                }
                if (Main.getGoodWorld)
                {
                    speed += 1f;
                    acceleration += 0.1f;
                }
                Movement(npc, player.Center - 12 * Vector2.UnitY, speed, acceleration);
                npc.velocity *= 0.95f;
                RotateTowards(npc, player.Center, 0.10f);
                npc.alpha += 4;
                if (npc.alpha > 255)
                    npc.alpha = 255;
                else
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                        Main.dust[d].noGravity = true;
                        Main.dust[d].noLight = true;
                        Main.dust[d].velocity *= 4f;
                    }
                }
            }
            if (state.Time == 100)
            {
                npc.localAI[2] = (player.Center - new Vector2(npc.localAI[0], npc.localAI[1])).ToRotation();
                npc.rotation = npc.localAI[2] - MathHelper.PiOver2;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), new Vector2(npc.localAI[0], npc.localAI[1]), 48 * npc.localAI[2].ToRotationVector2(), ModContent.ProjectileType<FalseEoC>(), 0, 0, Main.myPlayer,
                        -1, (float)FalseEoC.MoveType.Straight, 60);
                }
            }
            if (state.Time == 120)
            {
                npc.Center = new Vector2(npc.localAI[0], npc.localAI[1]);
                ReleaseDust(npc, 500);
                ScreenShakeSystem.StartShake(10);
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                npc.velocity = 72 * Vector2.UnitX.RotatedBy(npc.localAI[2]);
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                }
                npc.netUpdate = true;
            }
            if (state.Time > 120)
            {
                npc.velocity *= 0.975f;
                if (npc.alpha > 0)
                    npc.alpha = 0;
                for (int i = 0; i < 8; i++)
                {
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, recolor ? DustID.Vortex : DustID.BloodWater, 0f, 0f, 0, default, 1.5f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].noLight = true;
                    Main.dust[d].velocity *= 4f;
                }
                if (state.Time % 3 == 0 && FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Normalize(npc.velocity), ModContent.ProjectileType<BloodScythe>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer);
                }
                ShootBackMoonBolt(npc, 1);
            }
            ++state.Time;
        }

        //P3冲刺散射
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.P3MoonShootDash)]
        public void P3MoonShootDashBehavior()
        {
            var state = State(L_EoCState.P3MoonShootDash);
            Player player = Main.player[npc.target];

            if (state.Time == 0)
            {
                float predictDistance = 20f;
                Vector2 targetDelta = player.Center - npc.Center;
                // 根据玩家速度预测提前量
                float playerSpeedFactor = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y) / 4f;

                float predictionMultiplier = 10f - playerSpeedFactor;
                if (predictionMultiplier < 5f)
                    predictionMultiplier = 5f;
                if (predictionMultiplier > 15f)
                    predictionMultiplier = 15f;
                predictionMultiplier *= 3.5f;
                predictDistance *= 1.3f;
                targetDelta.X -= player.velocity.X * predictionMultiplier;
                targetDelta.Y -= player.velocity.Y * predictionMultiplier / 4f;
                // 添加随机误差
                targetDelta.X *= 1f + Main.rand.Next(-10, 11) * 0.01f;
                targetDelta.Y *= 1f + Main.rand.Next(-10, 11) * 0.01f;

                float dirLength = targetDelta.Length();
                float originalDirLength = dirLength;
                dirLength = predictDistance / dirLength;
                npc.velocity.X = targetDelta.X * dirLength;
                npc.velocity.Y = targetDelta.Y * dirLength;
                // 添加随机偏移
                npc.velocity.X += Main.rand.Next(-20, 21) * 0.1f;
                npc.velocity.Y += Main.rand.Next(-20, 21) * 0.1f;
                if (originalDirLength < 100f) // 距离较近时交换 X/Y 方向以避免太直
                {
                    if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y))
                    {
                        float x = Math.Abs(npc.velocity.X);
                        float y = Math.Abs(npc.velocity.Y);
                        if (npc.Center.X > player.Center.X)
                            y *= -1f;
                        if (npc.Center.Y > player.Center.Y)
                            x *= -1f;
                        npc.velocity.X = y;
                        npc.velocity.Y = x;
                    }
                }
                else if (Math.Abs(npc.velocity.X) > Math.Abs(npc.velocity.Y)) // 较远时求平均调整
                {
                    float avg = (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) / 2f;
                    float x = avg;
                    float y = avg;
                    if (npc.Center.X > player.Center.X)
                        y *= -1f;
                    if (npc.Center.Y > player.Center.Y)
                        x *= -1f;
                    npc.velocity.X = y;
                    npc.velocity.Y = x;
                }
                SoundEngine.PlaySound(36, (int)npc.position.X, (int)npc.position.Y, -1);
                if (npc.ai[2] == 0)
                    npc.localAI[0] = 5 + Main.rand.Next(0, 3);//次数
                npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
                if (FargoSoulsUtil.HostCheck)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center + WillDashTrail.Offset(npc), Vector2.Zero, ModContent.ProjectileType<MoonlightTrail>(), 0, 0, Main.myPlayer, npc.whoAmI, 43);
                    FargoSoulsUtil.XWay(8, npc.GetSource_FromThis(), npc.Center, ModContent.ProjectileType<BloodScythe>(), 1.5f, FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 0);
                }
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
            }
            if (state.Time == 30 && Vector2.Distance(npc.position, player.position) < 200f)
                state.Time -= 1;
            if (state.Time > 30 && state.Time <= 30 + 20)
            {
                npc.localAI[1] += 0.03f;
                if (npc.localAI[1] > 0.6f)
                    npc.localAI[1] = 0.6f;
            }
            if (state.Time > 30 + 20 && state.Time <= 30 + 40)
            {
                npc.localAI[1] -= 0.03f;
                if (npc.localAI[1] < 0)
                    npc.localAI[1] = 0;
            }
            if (state.Time == 30)
                npc.localAI[2] = npc.SafeDirectionTo(player.Center).ToRotation();
            if (state.Time > 30)
            {
                npc.velocity *= 0f;
                if (Math.Abs(npc.velocity.X) < 0.1)
                    npc.velocity.X = 0f;
                if (Math.Abs(npc.velocity.Y) < 0.1)
                    npc.velocity.Y = 0f;
                npc.rotation += npc.localAI[1];
                float i = state.Time - 60f;
                if (FargoSoulsUtil.HostCheck)
                {
                    for (float j = -1; j <= 1; j += 2)
                    {
                        double angle = i * MathHelper.TwoPi / 20 * j;
                        Vector2 EllipseVel = new(150f * (float)Math.Cos(angle) * (1 - 0.15f * (float)Math.Sin(angle) * (float)Math.Sin(angle)), 300f * (float)Math.Sin(angle));
                        EllipseVel *= (j + 2f) / 2f;
                        Vector2 vel = EllipseVel.RotatedBy(npc.localAI[2]) / 10f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, npc.Center.X + 12 * vel.X, npc.Center.Y + 12 * vel.Y, 40);
                    }
                }
            }
            if (++state.Time > 30 + 40)
            {
                npc.netUpdate = true;
                if (npc.netSpam > 10)
                    npc.netSpam = 10;
                npc.ai[2]++;
                state.Time = 0;
                npc.localAI[1] = 0;
                // 冲满 localAI[0] 次由转换到 ChooseRandom 处理
            }
        }

        //过渡态：选择随机招式
        [AutoloadAsBehavior<EntityAIState<L_EoCState>, L_EoCState>(L_EoCState.ChooseRandom)]
        public void ChooseRandomBehavior()
        {
            var state = State(L_EoCState.ChooseRandom);
            // 随机招式选择由 RegisterWeightedTransition 的转换回调完成
            ++state.Time;
        }
        #endregion

        #region 辅助方法
        // 对应原 ChooseNext 的分布：P4 均匀选 9~15（8=FastTpDashs 权重0）；P3 为 50% 冲刺8、50% 均匀9~15（权重7:1）。
        // 在 RegisterWeightedTransition 的 condition 里每帧调用，重建 weights 数组：近期用过（oldAtk）的招式权重置0，
        // 保证 total 恒 >0，避免 Main.rand.NextFloat(0) 选中空目标后把栈抽空。
        private void RebuildRandomWeights(float[] weights, bool p4)
        {
            float sum = 0f;
            for (int i = 0; i < RandomAttacks.Length; i++)
            {
                if (RandomAttacks[i] is not L_EoCState atk)
                {
                    weights[i] = 0f;
                    continue;
                }
                int num = (int)atk;
                if (oldAtk.Contains(num))
                {
                    weights[i] = 0f; // 近期用过 → 排除
                }
                else if (p4)
                {
                    weights[i] = num == (int)L_EoCState.FastTpDashs ? 0f : 1f;
                }
                else
                {
                    weights[i] = num == (int)L_EoCState.FastTpDashs ? 7f : 1f;
                }
                sum += weights[i];
            }
            if (sum <= 0f) // 理论上不会发生（oldAtk最多3个）；保险起见退化为均匀
            {
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = 1f;
            }
        }

        private void RecordLast()
        {
            int memorylength = 3;
            if (StateMachine?.CurrentState != null)
                oldAtk.Enqueue((int)StateMachine.CurrentState.Identifier);
            while (oldAtk.Count > memorylength)
                oldAtk.Dequeue();
        }

        private static void FancyFireballs(NPC npc, int repeats)
        {
            if (FargoSoulsUtil.HostCheck)
            {
                float modifier = 0;
                for (int i = 0; i < repeats; i++)
                    modifier = MathHelper.Lerp(modifier, 1f, 0.08f);

                float distance = 1400 * (1f - modifier);
                float rotation = MathHelper.TwoPi * modifier;
                const int max = 6;
                for (int i = 0; i < max; i++)
                {
                    int d = Dust.NewDust(npc.Center + distance * Vector2.UnitX.RotatedBy(rotation + MathHelper.TwoPi / max * i), 0, 0, DustID.SnowSpray, npc.velocity.X * 0.3f, npc.velocity.Y * 0.3f, 150);
                    int p = Dust.NewDust(npc.Center + distance * Vector2.UnitX.RotatedBy(-rotation + MathHelper.TwoPi / max * i), 0, 0, DustID.Vortex, npc.velocity.X * 0.3f, npc.velocity.Y * 0.3f, 150);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].scale = 1.5f - 0.8f * modifier;
                    Main.dust[p].noGravity = true;
                    Main.dust[p].scale = 1.5f - 0.8f * modifier;
                }
            }
        }

        private static void ReleaseDust(NPC npc, int num = 2)
        {
            if (FargoSoulsUtil.HostCheck)
            {
                for (int i = 0; i < num; i++)
                {
                    int randdistance = Main.rand.Next(200, 600);
                    float randangle = Main.rand.NextFloat(0, 2 * MathF.PI);
                    Vector2 vel = randdistance * Vector2.UnitX.RotatedBy(randangle) / 10;
                    int d = Dust.NewDust(npc.Center, 0, 0, DustID.SnowSpray, vel.X, vel.Y, 150);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].scale = Main.rand.NextFloat(1.2f, 1.5f);
                }
                for (int i = 0; i < num; i++)
                {
                    int randdistance = Main.rand.Next(50, 600);
                    float randangle = Main.rand.NextFloat(0, 2 * MathF.PI);
                    Vector2 vel = randdistance * Vector2.UnitX.RotatedBy(randangle) / 5;
                    Vector2 spawnPos = npc.Center + vel / 10;
                    int p = Dust.NewDust(spawnPos, 0, 0, DustID.Vortex, vel.X, vel.Y, 150);
                    Main.dust[p].noGravity = true;
                    Main.dust[p].scale = Main.rand.NextFloat(1.2f, 1.5f);
                }
            }
        }

        private static void ShootBackMoonBolt(NPC npc, int num)
        {
            for (int i = 0; i < num; i++)
            {
                float angle = Main.rand.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);
                Vector2 vel = npc.velocity.RotatedBy(angle);
                Vector2 targetPos = npc.Center - 10 * npc.velocity;
                if (FargoSoulsUtil.HostCheck)
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, vel, ModContent.ProjectileType<MoonBolt>(), FargoSoulsUtil.ScaledProjectileDamage(npc.defDamage), 1f, Main.myPlayer, targetPos.X, targetPos.Y, 40);
            }
        }

        public static void Movement(NPC npc, Vector2 targetPos, float speed = 7.5f, float acceleration = 0.18f)
        {
            Vector2 target = speed * npc.SafeDirectionTo(targetPos);
            // 向期望速度加速
            if (npc.velocity.X < target.X)
            {
                npc.velocity.X += acceleration;
                if (npc.velocity.X < 0f && target.X > 0f)
                    npc.velocity.X += acceleration;
            }
            else if (npc.velocity.X > target.X)
            {
                npc.velocity.X -= acceleration;
                if (npc.velocity.X > 0f && target.X < 0f)
                    npc.velocity.X -= acceleration;
            }
            if (npc.velocity.Y < target.Y)
            {
                npc.velocity.Y += acceleration;
                if (npc.velocity.Y < 0f && target.Y > 0f)
                    npc.velocity.Y += acceleration;
            }
            else if (npc.velocity.Y > target.Y)
            {
                npc.velocity.Y -= acceleration;
                if (npc.velocity.Y > 0f && target.Y < 0f)
                    npc.velocity.Y -= acceleration;
            }
        }

        /// <summary>
        /// 通常turnSpeed参考取值为:常态0.03，normaldashed 0.08，往后0.08，0.12，0.22等
        /// </summary>
        public static void RotateTowards(NPC npc, Vector2 targetPos, float turnSpeed)
        {
            Vector2 direction = targetPos - npc.Center;
            float targetRotation = direction.ToRotation() - MathHelper.PiOver2; // 克苏鲁之眼需要+90度
            targetRotation = MathHelper.WrapAngle(targetRotation);
            float currentRotation = MathHelper.WrapAngle(npc.rotation);
            if (currentRotation < targetRotation)
            {
                if (targetRotation - currentRotation > Math.PI)
                    currentRotation -= turnSpeed;
                else
                    currentRotation += turnSpeed;
            }
            else if (currentRotation > targetRotation)
            {
                if (currentRotation - targetRotation > Math.PI)
                    currentRotation += turnSpeed;
                else
                    currentRotation -= turnSpeed;
            }
            if (currentRotation > targetRotation - turnSpeed && currentRotation < targetRotation + turnSpeed)
                currentRotation = targetRotation;
            currentRotation = MathHelper.WrapAngle(currentRotation);
            npc.rotation = currentRotation;
        }

        private static bool AliveCheck(NPC npc, Player player)
        {
            bool length = npc.ai[3] != 3 && Vector2.Distance(npc.Center, player.Center) > 5000f;
            if (!player.active || player.dead || length || Main.IsItDay())
            {
                npc.TargetClosest();
                player = Main.player[npc.target];
                if (!player.active || player.dead || length || Main.IsItDay())
                {
                    npc.EncourageDespawn(10);
                    npc.velocity.Y -= 0.04f;
                    return false;
                }
            }
            return true;
        }
        #endregion

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            base.SendExtraAI(npc, bitWriter, binaryWriter);
            binaryWriter.Write7BitEncodedInt(TeleportDirection);
            binaryWriter.Write7BitEncodedInt(HyperTime);
            binaryWriter.Write7BitEncodedInt(P3AttackChange);
            if (StateMachine?.CurrentState != null)
            {
                binaryWriter.Write((float)(int)StateMachine.CurrentState.Identifier);
                binaryWriter.Write(StateMachine.CurrentState.Time);
            }
            else
            {
                binaryWriter.Write(0f);
                binaryWriter.Write(0);
            }
            binaryWriter.Write(npc.localAI[0]);
            binaryWriter.Write(npc.localAI[1]);
            binaryWriter.Write(npc.localAI[2]);
            binaryWriter.Write(npc.localAI[3]);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            base.ReceiveExtraAI(npc, bitReader, binaryReader);
            TeleportDirection = binaryReader.Read7BitEncodedInt();
            HyperTime = binaryReader.Read7BitEncodedInt();
            P3AttackChange = binaryReader.Read7BitEncodedInt();
            float state = binaryReader.ReadSingle();
            int time = binaryReader.ReadInt32();
            if (StateMachine?.CurrentState != null)
            {
                StateMachine.CurrentState.Identifier = (L_EoCState)(int)state;
                StateMachine.CurrentState.Time = time;
            }
            else
            {
                syncedState = (L_EoCState)(int)state;
                syncedTime = time;
            }
            npc.localAI[0] = binaryReader.ReadSingle();
            npc.localAI[1] = binaryReader.ReadSingle();
            npc.localAI[2] = binaryReader.ReadSingle();
            npc.localAI[3] = binaryReader.ReadSingle();
        }
    }
}
