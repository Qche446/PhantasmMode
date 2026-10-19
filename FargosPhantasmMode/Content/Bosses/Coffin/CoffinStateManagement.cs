using FargowiltasSouls;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using Monochrome.Content.Ai;
using System.Linq;

namespace FargosPhantasmMode.Content.Bosses.Coffin;
public partial class P_CursedCoffin
{
    private MonoFsm<State> fsm;
    public MonoFsm<State> Fsm
    {
        get
        {
            if (fsm == null) LoadFsm();
            return fsm;
        }
        private set => fsm = value;
    }
    private void LoadFsm()
    {
        Fsm = new(State.Opening);
        Fsm.AutoBind(this);
        Fsm.Changed += OnStateChanged;
        //以劫持为基础的phase切换，时刻锁定在常态转换时
        LoadPhaseChange();
        //开幕式
        Fsm.To(State.Opening, ChooseNextAttack, () => Timer == -1);
        //转阶段态：1 -> 2
        Fsm.To(State.PhaseTransition1st, ChooseNextAttack, () => Timer >= 90, () =>
        {
            NPC npc = coffin.NPC;
            SoundEngine.PlaySound(PhaseTransitionSFX, npc.Center);
            npc.netUpdate = true;
            if (FargoSoulsUtil.HostCheck)
            {
                Vector2 maskCenter = MaskCenter;
                int n = NPC.NewNPC(npc.GetSource_FromAI(), (int)maskCenter.X, (int)maskCenter.Y, ModContent.NPCType<CursedSpirit>(), ai0: npc.whoAmI);
                Main.npc[n].netUpdate = true;
            }
            npc.velocity = Vector2.UnitY * 0.1f;
        });
        //转阶段态：2 -> 3
        Fsm.To(State.PhaseTransition2nd, ChooseNextAttack, () => Timer >= 120, () =>
        {
            NPC npc = coffin.NPC;
            SoundEngine.PlaySound(PhaseTransitionSFX, npc.Center);
            npc.netUpdate = true;
            npc.velocity = Vector2.UnitY * 0.1f;
        });
        //两个砸地转换
        Fsm.To(State.HoveringForSlam, State.SlamWShockwave, () => Timer >= 1 && Timer == AI2, () =>
        {
            NPC npc = coffin.NPC;
            npc.velocity.Y = -5;
            npc.velocity.X /= 2;
        });
        Fsm.To(State.WavyShotCircle, State.WavyShotSlam, () => Timer >= 60, () =>
        {

        });
    }

    private void OnStateChanged(State arg1, State arg2)
    {
        NPC npc = coffin.NPC;
        Timer = npc.ai[1] = npc.ai[2] = npc.ai[3] = 0;
        npc.TargetClosest(false);
        if (Attacks.Contains(arg1))
        {
            LastAttackChoice2 = LastAttackChoice1;
            LastAttackChoice1 = arg1;
        }
        npc.netUpdate = true;
    }
    private State ChooseNextAttack()
    {
        coffin.NPC.netUpdate = true;
        if (Fsm.Current.Equals(State.HoveringForSlam))
            return State.SlamWShockwave;
        else if (Fsm.Current.Equals(State.WavyShotCircle))
            return State.WavyShotSlam;
        State next;
        do
        {
            int index = random.NextInt(Attacks.Count);
            next = Attacks[index];
        }
        while (next.Equals(LastAttackChoice1) || next.Equals(LastAttackChoice2));
        return next;
    }
    public void LoadPhaseChange()
    {
        Fsm.Hijack(oldState =>
        {
            NPC npc = coffin.NPC;
            npc.netUpdate = true;
            float lifeRatio = npc.GetLifePercent();
            State next = oldState;
            if (Phase < 2 && lifeRatio <= 0.75f)
            {
                next = State.PhaseTransition1st;
                Phase = 2;
            }
            else if (Phase < 3 && lifeRatio <= 0.15f)
            {
                next = State.PhaseTransition2nd;
                Phase = 3;
            }
            return next;
        });
    }
}
