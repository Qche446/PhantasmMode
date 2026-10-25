using FargosPhantasmMode.Common;
using FargosPhantasmMode.Global;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Core.Globals;
using Monochrome.Common.MonoUtil;
using Monochrome.Content.Worms;
using Terraria.ModLoader.IO;
namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EaterOfWorlds;
public class P_EowHead : PModeNPCBehaviour
{
    private readonly static MonoWormLayout layout = MonoWormLayout.Vanilla;
    private const float Spacing = 38f;
    public override int NPCType => NPCID.EaterofWorldsHead;
    public NPC Npc;
    // 提示音只能本地播，这两个字段保证每个相位轮次只响一次（这个实例是所有头共用的）
    private int lastKind = -1;
    private int sweepSoundEntered = -1;
    // SFollow 每一轮锁一次目标点，round 记住这是第几轮，免得每帧都重锁
    private Vector2 sFollowTarget;
    private int sFollowRound = -1;
    public Player Player => Main.player[Npc.target];
    public ref float Behind => ref Npc.ai[0];
    public ref float Ahead => ref Npc.ai[1];
    public override void OnFirstTick(NPC npc) => npc.GetGlobalNPC<EaterofWorldsHead>().RunEmodeAI = false;

    // 相位是整场共享的，哪只头同步都写同一份值；这样读写顺序不必再靠「谁是 leader」对齐
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
    {
        base.SendExtraAI(npc, bitWriter, binaryWriter);

        binaryWriter.Write(EowPhase.Kind);
        binaryWriter.Write(EowPhase.Entered);
        binaryWriter.Write(EowPhase.SlotOf(npc));
        binaryWriter.Write(EowPhase.Timer);
        binaryWriter.Write(EowPhase.UTurnAngle);
        binaryWriter.Write(EowPhase.Tempo);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
    {
        base.ReceiveExtraAI(npc, bitReader, binaryReader);

        EowPhase.Kind = binaryReader.ReadInt32();
        EowPhase.Entered = binaryReader.ReadInt32();
        EowPhase.SetSlot(npc.whoAmI, binaryReader.ReadInt32());
        EowPhase.Timer = binaryReader.ReadInt32();
        EowPhase.UTurnAngle = binaryReader.ReadSingle();
        EowPhase.Tempo = binaryReader.ReadSingle();
    }
    public override bool SafePreAI(NPC npc)
    {
        EModeGlobalNPC.eaterBoss = npc.whoAmI;
        FargoSoulsGlobalNPC.boss = npc.whoAmI;
        Npc ??= npc;
        if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            npc.TargetClosest();
        Player player = Main.player[npc.target];
        EowChain.ManageAlpha(npc);
        if (!AliveCheck(npc, player))
            return false;
        //断裂与转换
        if (EowChain.UpdateLinks(npc, layout))
            return false;
        //初始化链：螺旋编队
        if (npc.ai[0] == 0)
        {
            EowPhase.Reset(); // 新的一场，上一场的相位不能带过来
            if (PhanUtil.HostCheck)
                SpawnChain(npc);
        }

        EowPhase.Tick(npc);

        // 相位是共享状态，只有真正换相位的那一帧才播
        if (EowPhase.Kind != lastKind)
        {
            if (EowPhase.Kind == (int)EowPhase.Phase.UTurn)
                SoundEngine.PlaySound(SoundID.Roar, player.Center);

            lastKind = EowPhase.Kind;
        }

        //哎哎，领域
        if (Main.LocalPlayer.active && npc.Distance(Main.LocalPlayer.Center) < 8000f)
            Main.LocalPlayer.AddBuff(PBuffID.RottenAirFlow, 2);
        switch ((EowPhase.Phase)EowPhase.Kind)
        {
            case EowPhase.Phase.NFollow: NFollow(npc);
                break;
            case EowPhase.Phase.UTurn: UTurn(npc);
                break;
            case EowPhase.Phase.SFollow: SFollow(npc);
                break;
            case EowPhase.Phase.Shot:
                break;
            case EowPhase.Phase.Coil:
                break;
            case EowPhase.Phase.UFollow: // 下一阶段还没设计，先按原版追踪走
                EowVanilla.Move(npc, Player.Center);
                break;
            default:
                EowVanilla.Move(npc, Player.Center); // 相位没落地时退回原版移动，至少不会僵住
                break;
        }

        //EowVanilla.DigSound(npc);
        if (EowPhase.Kind == (int)EowPhase.Phase.NFollow) // 只有贴着玩家游的时候才啃土，扫掠时穿地不喷碎块
            EowVanilla.DigDust(npc);
        return false;
    }
    #region 主AI方法
    public void NFollow(NPC npc)
    {
        EowVanilla.Move(npc, Player.Center);
    }
    public void UTurn(NPC npc)
    {
        Vector2 playerCenter = Player.Center;
        int timer = EowPhase.Timer;

        // 排队：240（大于120且全部到齐可提前）
        if (timer < EowPhase.SweepStart)
        {
            // 编队：远处全速、近处收敛，避免在落点上来回过冲
            Vector2 target = EowPhase.UturnTarget(npc, playerCenter);
            float distance = npc.Distance(target);
            float speed = MathHelper.Clamp(distance * 0.15f, 2f, 26f);
            MonoWormMove.Hover(npc.Center, npc.velocity, target, speed, 0.18f, out Vector2 vel);
            npc.velocity = vel;
        }
        else if (timer < EowPhase.SweepStart + 90)//90tick冲刺
        {
            if (timer >= EowPhase.SweepStart && sweepSoundEntered != EowPhase.Entered) // 起飞那一声
            {
                sweepSoundEntered = EowPhase.Entered;
                SoundEngine.PlaySound(SoundID.ForceRoarPitched, playerCenter);
            }

            npc.velocity = EowPhase.SweepDirection * 16f;
        }
        else if (timer < EowPhase.SweepStart + 90 + 45) //45tick转向
        {
            if (npc.velocity.LengthSquared() < 1f)
                npc.velocity = EowPhase.SweepDirection * 16f;

            // 往玩家所在的一侧转，回头那一下才扫得到玩家
            npc.velocity = npc.velocity.RotatedBy(EowPhase.ArcSign(playerCenter) * MathHelper.Pi / 45f);
        }
        // 之后保持转向后的速度直飞 60 帧，然后进入下一个状态

        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        //npc.rotation = MathHelper.Lerp(npc.rotation, npc.velocity.ToRotation() + MathHelper.PiOver2, 0.08f);
        SyncMotion(npc);
    }
    public void SFollow(NPC npc)
    {
        Vector2 playerCenter = Player.Center;
        int follow = (int)(60 * EowPhase.Tempo);
        int charge = (int)(60 * EowPhase.Tempo);
        int coast = (int)(30 * EowPhase.Tempo);
        int round = follow + charge + coast;
        int t = EowPhase.Timer % round;
        int index = EowPhase.Timer / round;

        // 冲锋起点：这一帧的玩家位置只锁一次，全体头读的都是同一份
        if (t >= follow && sFollowRound != index)
        {
            sFollowRound = index;
            sFollowTarget = playerCenter;
            SoundEngine.PlaySound(SoundID.Roar, playerCenter);
        }

        if (t < follow) // 原版追踪，给玩家留出反应时间
        {
            EowVanilla.Move(npc, playerCenter);
            return;
        }

        if (t < follow + charge)
        {
            Vector2 toTarget = sFollowTarget - npc.Center;
            // 路程摊到整段上，远的快近的慢，一起到；上限 48
            float wanted = Math.Max(Math.Min(64f, toTarget.Length() / charge), 16);
            Vector2 dir = toTarget.SafeNormalize(npc.velocity.SafeNormalize(Vector2.UnitY));
            npc.velocity = Vector2.Lerp(npc.velocity, dir * wanted, 0.15f);
        }
        // 冲到位后不动速度，原样再飞 30 帧穿过去

        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        SyncMotion(npc);
    }

    // 高速位移期间给客户端补帧，照 FSS 的 netSpam 节流写法
    private static void SyncMotion(NPC npc)
    {
        if (Main.netMode != NetmodeID.Server || --npc.netSpam >= 0)
            return;

        npc.netSpam = 5;
        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
    }

    #endregion
    #region 辅助方法
    private static bool AliveCheck(NPC npc, Player player)
    {
        if (!player.active || player.dead || npc.Distance(Main.player[npc.target].Center) > 8000)
        {
            npc.TargetClosest();
            player = Main.player[npc.target];
            if (!player.active || player.dead || npc.Distance(Main.player[npc.target].Center) > 8000)
            {
                npc.EncourageDespawn(120);
                npc.velocity.Y += 0.25f;
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                return false;
            }
        }
        return true;
    }
    private static void SpawnChain(NPC head)
    {
        int free = 0;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (!Main.npc[i].active)
                free++;
        }
        int count = Math.Min(70 + 1, free); // 体节 + 尾巴
        if (count <= 0)
            return;

        Span<Vector2> points = stackalloc Vector2[count + 1];
        BuildCoil(head, points);

        int first = -1;
        int ahead = head.whoAmI;
        for (int i = 1; i <= count; i++)
        {
            bool tail = i == count;
            Vector2 center = points[i];
            int index = NPC.NewNPC(head.GetSource_FromAI(), (int)center.X, (int)(center.Y + head.height * 0.5f),
                tail ? NPCID.EaterofWorldsTail : NPCID.EaterofWorldsBody, head.whoAmI);
            if (index >= Main.maxNPCs)
                break;

            NPC segment = Main.npc[index];
            segment.ai[1] = ahead;

            if (first < 0)
                first = index;
            else
                Main.npc[ahead].ai[0] = index;

            segment.velocity = Vector2.Zero;
            //
            segment.alpha = 150;
            segment.rotation = (points[i - 1] - center).ToRotation() + MathHelper.PiOver2;
            ahead = index;
        }

        if (first < 0)
            return;

        head.ai[0] = first;
        head.netUpdate = true;
    }
    private static void BuildCoil(NPC head, Span<Vector2> points)
    {
        const int CoilTurns = 4;
        points[0] = head.Center;

        int wanted = points.Length - 1;
        if (wanted <= 0)
            return;

        Vector2 forward = head.velocity.SafeNormalize(Vector2.UnitY);
        float startAngle = (-forward).ToRotation();
        float thetaMax = MathHelper.TwoPi * CoilTurns;

        // 弧长近似 π * 圈数 * (起半径 + 终半径)，反解出终半径
        float radiusEnd = Math.Max(Spacing * 2f, wanted * Spacing / (MathHelper.Pi * CoilTurns) - Spacing);

        const float step = 0.01f;
        int placed = 1;
        Vector2 last = points[0];
        for (float theta = step; theta <= thetaMax && placed <= wanted; theta += step)
        {
            float radius = Spacing + (radiusEnd - Spacing) * (theta / thetaMax);
            Vector2 point = head.Center + (startAngle + theta).ToRotationVector2() * radius;
            if (Vector2.DistanceSquared(point, last) < Spacing * Spacing)
                continue;

            points[placed++] = point;
            last = point;
        }

        while (placed <= wanted)
        {
            last += (last - head.Center).SafeNormalize(-forward) * Spacing;
            points[placed++] = last;
        }
    }
    #endregion
}
