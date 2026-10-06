
using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.Buffs.Boss;
using FargowiltasSouls.Content.WorldGeneration;
using FargowiltasSouls.Core.Systems;
using Luminance.Common.StateMachines;
using Luminance.Common.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace FargosPhantasmMode.Content.Bosses.Coffin
{
    public partial class P_CursedCoffin
    {
        private PushdownAutomata<EntityAIState<BehaviorStates>, BehaviorStates> stateMachine;

        public PushdownAutomata<EntityAIState<BehaviorStates>, BehaviorStates> StateMachine
        {
            get
            {
                if (stateMachine == null)
                {
                    LoadStateMachine();
                }
                return stateMachine;
            }
            private set
            {
                stateMachine = value;
            }
        }
        private void LoadStateMachine()
        {
            stateMachine = new PushdownAutomata<EntityAIState<BehaviorStates>, BehaviorStates>(new EntityAIState<BehaviorStates>(BehaviorStates.Opening));
            for (byte i = 0; i < 11; i++)
            {
                StateMachine.RegisterState(new EntityAIState<BehaviorStates>((BehaviorStates)i));
            }
            StateMachine.OnStateTransition += OnStateTransition;
            StateMachine.OnStackEmpty += OnStackEmpty;
            AutoloadAsBehavior<EntityAIState<BehaviorStates>, BehaviorStates>.FillStateMachineBehaviors<P_CursedCoffin>(StateMachine, this);
            LoadTransition_PhaseTwoTransition();
            NPC npc = ((ModNPC)(object)coffin).NPC;
            StateMachine.RegisterTransition(BehaviorStates.Opening, (BehaviorStates?)null, false, (Func<bool>)(() => Timer == -1f), (Action)null);
            StateMachine.RegisterTransition(BehaviorStates.PhaseTransition, (BehaviorStates?)BehaviorStates.SlamWShockwave, false, (Func<bool>)(() => Timer >= 90f), (Action)delegate
            {
                SoundEngine.PlaySound(in CursedCoffin.PhaseTransitionSFX, npc.Center);
                npc.netUpdate = true;
                if (FargoSoulsUtil.HostCheck)
                {
                    Vector2 maskCenter = MaskCenter;
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)maskCenter.X, (int)maskCenter.Y, ModContent.NPCType<CursedSpirit>(), 0, npc.whoAmI);
                }
                npc.velocity = Vector2.UnitY * 0.1f;
                LockVector1 = Player.Top - Vector2.UnitY * 250f;
                AI2 = 3f;
            });
            StateMachine.ApplyToAllStatesExcept((Action<BehaviorStates>)delegate (BehaviorStates state)
            {
                StateMachine.RegisterTransition(state, (BehaviorStates?)BehaviorStates.StunPunish, false, (Func<bool>)(() => Main.netMode != NetmodeID.MultiplayerClient && Main.player.Any(p => FargoExtensionMethods.Alive(p) && p.HasBuff(BuffID.Dazed) && !p.HasBuff<GrabbedBuff>()) && !Main.projectile.Any((Projectile p) => FargoExtensionMethods.TypeAlive<CoffinHand>(p))), (Action)null);
            },
            [
            BehaviorStates.StunPunish,
            BehaviorStates.PhaseTransition,
            BehaviorStates.YouCantEscape,
            BehaviorStates.SpiritGrabPunish
            ]);
            StateMachine.ApplyToAllStatesExcept((Action<BehaviorStates>)delegate (BehaviorStates state)
            {
                StateMachine.RegisterTransition(state, (BehaviorStates?)BehaviorStates.YouCantEscape, false, (Func<bool>)(() => Main.netMode != NetmodeID.MultiplayerClient && Main.player.Any(p => FargoExtensionMethods.Alive(p) && !CoffinArena.PaddedRectangle.Contains(p.Center.ToTileCoordinates()) && !p.HasBuff<GrabbedBuff>())), (Action)null);
            },
            [
            BehaviorStates.StunPunish,
            BehaviorStates.PhaseTransition,
            BehaviorStates.YouCantEscape,
            BehaviorStates.SpiritGrabPunish
            ]);
            StateMachine.ApplyToAllStatesExcept((Action<BehaviorStates>)delegate (BehaviorStates state)
            {
                StateMachine.RegisterTransition(state, (BehaviorStates?)BehaviorStates.SpiritGrabPunish, false, (Func<bool>)(() => ForceGrabPunish != 0f), (Action)delegate
                {
                    ForceGrabPunish = 0f;
                });
            },
            [
            BehaviorStates.StunPunish,
            BehaviorStates.PhaseTransition,
            BehaviorStates.YouCantEscape,
            BehaviorStates.SpiritGrabPunish
            ]);
            StateMachine.RegisterTransition(BehaviorStates.WavyShotCircle, (BehaviorStates?)BehaviorStates.WavyShotSlam, false, (Func<bool>)delegate
            {
                int num = (WorldSavingSystem.MasochistModeReal ? 60 : 70);
                bool flag = Timer > (float)(num + ((WorldSavingSystem.MasochistModeReal || AI3 < 1f) ? 20 : 50));
                bool flag2 = AI3 < 1f && WorldSavingSystem.MasochistModeReal;
                return flag && !flag2;
            }, (Action)delegate
            {
                Frame = 0;
                npc.velocity.X /= 2f;
                npc.velocity.Y = -6f;
            });
            StateMachine.RegisterTransition(BehaviorStates.GrabbyHands, (BehaviorStates?)BehaviorStates.SlamWShockwave, false, (Func<bool>)(() => Timer > 72f && Frame <= 0 && Timer > AI3 + 1f), (Action)delegate
            {
                npc.noTileCollide = true;
                LockVector1 = Player.Top - Vector2.UnitY * 250f;
                npc.velocity.Y = -4f;
                npc.velocity.X /= 2f;
                if ((float)Utilities.NonZeroSign(npc.velocity.X) != Utilities.HorizontalDirectionTo((Entity)npc, Player.Center))
                {
                    npc.velocity.X = 0f;
                }
            });
            StateMachine.RegisterTransition(BehaviorStates.SpiritGrabPunish, (BehaviorStates?)BehaviorStates.SlamWShockwave, false, (Func<bool>)(() => Timer > 70f), (Action)delegate
            {
                npc.noTileCollide = true;
                LockVector1 = Player.Top - Vector2.UnitY * 250f;
                npc.velocity = Vector2.Zero;
                npc.velocity.Y = -2f;
                AI2 = 3f;
            });
            StateMachine.RegisterTransition(BehaviorStates.HoveringForSlam, (BehaviorStates?)BehaviorStates.SlamWShockwave, false, (Func<bool>)(() => Timer > 1f && Timer == AI3), (Action)delegate
            {
                npc.velocity.Y = -5f;
                npc.velocity.X /= 2f;
                LockVector1 = Player.Top - Vector2.UnitY * 250f;
                AI2 = 0f;
            });
            StateMachine.RegisterTransition(BehaviorStates.StunPunish, (BehaviorStates?)null, false, (Func<bool>)(() => Timer > 20f && Frame <= 0), (Action)delegate
            {
                npc.frameCounter = 0.0;
                Frame = 0;
            });
            StateMachine.RegisterTransition(BehaviorStates.YouCantEscape, (BehaviorStates?)null, false, (Func<bool>)(() => Timer > 20f && Frame <= 0), (Action)delegate
            {
                npc.frameCounter = 0.0;
                Frame = 0;
            });
            StateMachine.RegisterTransition(BehaviorStates.SlamWShockwave, (BehaviorStates?)null, false, (Func<bool>)(() => Timer == -1f), (Action)null);
            StateMachine.RegisterTransition(BehaviorStates.WavyShotSlam, (BehaviorStates?)null, false, (Func<bool>)(() => Timer == -1f), (Action)null);
            StateMachine.RegisterTransition(BehaviorStates.RandomStuff, (BehaviorStates?)null, false, (Func<bool>)(() => Timer > 370f && Frame <= 0), (Action)delegate
            {
                npc.velocity = Vector2.Zero;
                npc.rotation = 0f;
                npc.frameCounter = 0.0;
                Frame = 0;
                AttackCounter = 0f;
            });
        }
        public void OnStateTransition(bool stateWasPopped, EntityAIState<BehaviorStates> oldState)
        {
            NPC npc = ((ModNPC)(object)coffin).NPC;
            npc.netUpdate = true;
            npc.TargetClosest(faceTarget: false);
            AI2 = 0f;
            AI3 = 0f;
            if (oldState != null && Attacks.Contains(oldState.Identifier))
            {
                LastAttackChoice = (byte)oldState.Identifier;
            }
        }

        public void OnStackEmpty()
        {
            ((ModNPC)(object)coffin).NPC.netUpdate = true;
            if (FargoSoulsUtil.HostCheck)
            {
                StateMachine.StateStack.Clear();
                List<BehaviorStates> attackList = Attacks.Where((BehaviorStates attack) => (uint)attack != LastAttackChoice).ToList();
                List<int> indices = new List<int>(attackList.Count);
                for (int i = 0; i < attackList.Count; i++)
                {
                    indices.Add(i);
                }
                for (int i2 = 0; i2 < attackList.Count; i2++)
                {
                    int currentIndex = indices[Main.rand.Next(0, indices.Count)];
                    StateMachine.StateStack.Push(StateMachine.StateRegistry[attackList[currentIndex]]);
                    indices.Remove(currentIndex);
                }
            }
        }

        public void LoadTransition_PhaseTwoTransition()
        {
            StateMachine.AddTransitionStateHijack((Func<BehaviorStates?, BehaviorStates?>)delegate (BehaviorStates? originalState)
            {
                ((ModNPC)(object)coffin).NPC.netUpdate = true;
                float num = 0.75f;
                if (Phase < 2 && ((ModNPC)(object)coffin).NPC.GetLifePercent() <= num)
                {
                    StateMachine.StateStack.Clear();
                    return BehaviorStates.PhaseTransition;
                }
                return originalState;
            }, (Action<BehaviorStates?>)null);
        }
    }
}
