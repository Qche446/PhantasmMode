using System.Collections.Generic;
using FargosPhantasmMode.Common;
using Monochrome.Common.MonoUtil;

namespace FargosPhantasmMode.Content.Bosses.VanillaEternity.EaterOfWorlds;

/// <summary>
/// 所有世吞头共用的相位：同一时刻全体进入同一相位，各自按冻结的站位序号表现。
/// <para>
/// 相位本身只有一份（静态字段），只由 leader 推进，所以头数变化不会让相位速度或切换次数
/// 跟着变。相位、计时与锁定角度随头的 <c>SendExtraAI</c> 下发，中途加入的客户端也能拿到。
/// </para>
/// 用法：头的 PreAI 里调 <see cref="Tick"/>，用 <see cref="Kind"/> 与 <see cref="Timer"/> 分派招式，
/// 用 <see cref="SlotOf"/> 取本头的站位序号。
/// </summary>
internal static class EowPhase
{
    /// <summary>编队阶段最长 4 秒，也就是开始扫掠的帧号。头的 UTurn 也用它当时间轴原点。</summary>
    public const int SweepStart = 4 * 60;

    /// <summary>当前相位的种类，见 <see cref="Phase"/>。0 表示还没开始。</summary>
    public static int Kind;
    /// <summary>第几次进入相位。同一个相位重复开一轮时靠它区分。</summary>
    public static int Entered;
    /// <summary>相位内经过的帧数，只由 leader 递增，随同步包下发。</summary>
    public static int Timer;
    /// <summary>进入 UTurn 那一刻，决策头相对玩家的方向角（玩家→决策头）。</summary>
    public static float UTurnAngle;
    /// <summary>SFollow 的节奏系数：进相位时按总血量取 1~0.5，血越少这一段越快。</summary>
    public static float Tempo = 1f;

    // whoAmI -> 冻结的站位序号（交错序列 0, 1, -1, 2, -2 …）
    private static readonly Dictionary<int, int> slots = [];
    private static int assigned;

    // 弧段转向的正负号，以及它是在哪一轮相位里算出来的（同一轮只算一次）
    private static int arcSign = 1;
    private static int arcSignEntered = -1;

    public static void Reset()
    {
        Kind = (int)Phase.None;
        Entered = 0;
        Timer = 0;
        UTurnAngle = 0f;
        Tempo = 1f;
        slots.Clear();
        assigned = 0;
        arcSign = 1;
        arcSignEntered = -1;
    }

    /// <summary>
    /// 本帧的写者。各端跑的是同一份 AI，用 whoAmI 最小的头当写者能让两端选出同一只；
    /// leader 被打死或相位中途分裂出新头时，接班的那只接着当前 Timer 走，不会重开相位。
    /// </summary>
    public static bool IsLeader(NPC npc) => NPC.FindFirstNPC(NPCID.EaterofWorldsHead) == npc.whoAmI;

    /// <summary>
    /// 推进相位。只有 leader 进来才真的做事，切换相位只由权威端做；客户端只把 Timer 往前数，
    /// 等 <c>ReceiveExtraAI</c> 把权威值覆盖回来。
    /// </summary>
    public static void Tick(NPC npc)
    {
        if (!IsLeader(npc))
            return;

        if (Kind == 0)
        {
            if (PhanUtil.HostCheck)
            {
                Enter(Phase.NFollow, npc);
                npc.netUpdate = true;
            }
            return;
        }

        if (PhanUtil.HostCheck)
            Reserve(); // 相位中途分裂出来的头在这里补登记

        Timer++;

        if (!PhanUtil.HostCheck)
            return;

        if (Kind == (int)Phase.NFollow)
        {
            if (Timer > 12 * 60) // 蓄力 12 秒后开始行动
            {
                Enter(Phase.UTurn, npc);
                npc.netUpdate = true;
            }
            return;
        }

        if (Kind == (int)Phase.UTurn)
        {
            Vector2 playerCenter = Main.player[npc.target].Center;

            // 全体提前到位：把计时直接推到扫掠起点，省掉剩下的等待
            if (Timer < SweepStart && AllArrived(playerCenter) && Timer > 120)
            {
                Timer = SweepStart;
                npc.netUpdate = true;
            }

            if (Timer >= SweepStart + 90 + 45 + 80) // 扫掠 + 弧段 + 收尾直飞
            {
                Enter(Phase.SFollow, npc);
                npc.netUpdate = true;
            }
            return;
        }

        if (Kind == (int)Phase.SFollow && Timer >= 8 * 60 * Tempo) // 几轮冲锋打完，进下一阶段
        {
            Enter(Phase.NFollow, npc);
            npc.netUpdate = true;
        }
    }

    /// <summary>蓄力阶段的一次性加时：体节断裂转成头时给全场提前。</summary>
    public static void AddNFollowTime(int frames)
    {
        if (!PhanUtil.HostCheck || Kind != (int)Phase.NFollow)
            return;

        Timer += frames;
    }

    /// <summary>扫掠方向：从决策头的落点指向玩家，即进入 UTurn 时的反方向。</summary>
    public static Vector2 SweepDirection => (UTurnAngle + MathHelper.Pi).ToRotationVector2();

    /// <summary>
    /// 某只头在 UTurn 里的目标点。序号 0 就是决策头的落点，正负决定排在垂线的哪一侧，
    /// 整条线段与玩家保持 600 的距离，相邻序号之间按头数摊开，最密 240。
    /// </summary>
    public static Vector2 UturnTarget(NPC head, Vector2 playerCenter)
    {
        Vector2 rdir = UTurnAngle.ToRotationVector2();
        Vector2 anchor = playerCenter + rdir * 600f;
        Vector2 perp = rdir.Perp();

        // 间距随头数开方衰减，压到 240 就不再收，避免叠在一起
        float spacing = Math.Max(240f, 1200f / MathF.Sqrt(2 * HeadCount() + 2));
        return anchor + perp * (SlotOf(head) * spacing);
    }
    /// <summary>全体头加起来的血量百分比，用来定 SFollow 的快慢。</summary>
    private static float HealthPercent()
    {
        long life = 0;
        long lifeMax = 0;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC head = Main.npc[i];
            if (!head.active || head.type != NPCID.EaterofWorldsHead)
                continue;

            life += head.life;
            lifeMax += head.lifeMax;
        }

        return lifeMax <= 0 ? 0f : (float)life / lifeMax;
    }

    private static int HeadCount()
    {
        int count = 0;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (Main.npc[i].active && Main.npc[i].type == NPCID.EaterofWorldsHead)
                count++;
        }

        return count;
    }

    /// <summary>
    /// 弧段往哪边转：看决策头此刻在玩家的哪一侧，玩家在左就往左转，回头那一下才扫得到玩家。
    /// 同一轮相位只锁定一次，全体头共用同一个符号，否则排在线段两侧的头会各转各的。
    /// </summary>
    public static int ArcSign(Vector2 playerCenter)
    {
        if (arcSignEntered == Entered)
            return arcSign;

        int leaderIndex = NPC.FindFirstNPC(NPCID.EaterofWorldsHead);
        if (leaderIndex < 0)
            return 1;

        NPC leader = Main.npc[leaderIndex];
        Vector2 toPlayer = playerCenter - leader.Center;
        float cross = leader.velocity.X * toPlayer.Y - leader.velocity.Y * toPlayer.X;
        arcSign = cross < 0f ? -1 : 1;
        arcSignEntered = Entered;
        return arcSign;
    }

    /// <summary>本头在相位里的站位序号，没登记过返回 0（leader 每帧都会补登记）。</summary>
    public static int SlotOf(NPC npc) => slots.TryGetValue(npc.whoAmI, out int slot) ? slot : 0;

    /// <summary>接收权威端下发的序号，客户端用。</summary>
    public static void SetSlot(int whoAmI, int slot) => slots[whoAmI] = slot;

    /// <summary>编队是否已经全体到位。只有权威端用它来提前结束等待。</summary>
    private static bool AllArrived(Vector2 playerCenter)
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC head = Main.npc[i];
            if (!head.active || head.type != NPCID.EaterofWorldsHead)
                continue;

            if (head.Distance(UturnTarget(head, playerCenter)) > 60f) // 差 60 以内算到位
                return false;
        }

        return true;
    }

    private static void Enter(Phase phase, NPC npc)
    {
        Kind = (int)phase;
        Entered++;
        Timer = 0;

        // 客户端留着旧表，等权威端逐个同步覆盖。清表会让整群头先聚到中间再散开。
        if (!PhanUtil.HostCheck)
            return;

        if (phase == Phase.UTurn)
        {
            Vector2 r = npc.Center - Main.player[npc.target].Center;
            UTurnAngle = r.LengthSquared() < 1f ? 0f : r.ToRotation();
        }

        if (phase == Phase.SFollow)
            Tempo = 0.5f + 0.5f * HealthPercent(); // 血越少，冲锋段越密越快，最少压到一半时长

        slots.Clear();
        assigned = 0;
        Reserve(); // 冻结：此刻在场的头按 whoAmI 顺序拿序号，此后不再重排
    }

    // 给还没登记的头补序号。已登记的保持不变，所以有头死掉或新增时，其它头的站位不会动。
    private static void Reserve()
    {
        if (!PhanUtil.HostCheck)
            return; // 序号只由权威端分配，客户端全靠 SendExtraAI 学到

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC head = Main.npc[i];
            if (!head.active || head.type != NPCID.EaterofWorldsHead || slots.ContainsKey(head.whoAmI))
                continue;

            slots[head.whoAmI] = assigned % 2 == 1 ? (assigned + 1) / 2 : -(assigned / 2);
            assigned++;
            head.netUpdate = true; // 新序号要跟着这只头同步给客户端
        }
    }

    public enum Phase : byte
    {
        None = 0,
        NFollow,
        UTurn,
        SFollow,
        Shot,
        Coil,
        UFollow,
    }
}
