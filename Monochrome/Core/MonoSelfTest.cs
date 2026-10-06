using System.Text;
using Monochrome.Common.MonoUtil;

namespace Monochrome.Core;

/// <summary>
/// 游戏内自检的<b>一台用例</b>。
/// </summary>
/// <param name="Name">用例名（会出现在聊天里，所以是中文短句）。</param>
/// <param name="Passed">是否通过。</param>
/// <param name="Detail">证据：实测到的数、或失败原因。</param>
public readonly record struct MonoSelfTestResult(string Name, bool Passed, string Detail);

/// <summary>
/// 核心层的<b>游戏内自检</b>：把只能在真实帧钩子下才能证的东西一次跑完，并给出一份可落盘的报告。
/// <para>
/// <b>为什么必须有它，而不是"进游戏看一眼不崩就行"。</b>离线验收台（<c>Monochrome.CoreTests</c>）能证明的是
/// 纯逻辑与时间基准；它证明不了下面这一整类事实，而这类事实的失败模式恰恰是<b>静默的</b>：
/// </para>
/// <list type="bullet">
/// <item><description><b>钩子真的挂上了吗</b>：<c>ModSystem</c> 的钩子名写错一个字母不会报错，只会永不触发。
/// 现象是"订阅了但回调从没跑过"——没有任何地方会红。</description></item>
/// <item><description><b>阶段顺序真的对吗</b>：<c>WorldUpdate</c> / <c>PostUpdate</c> / <c>PreDraw</c> 三个阶段挤在同一个
/// <c>PostUpdateDusts</c> 钩子上，靠枚举顺序保证先后；顺序错了只会表现为"位置差一帧"。</description></item>
/// <item><description><b>排程时基是"游戏 tick"，不是"帧"</b>：<c>Delay(n)</c> 到期于 <c>t+n+2</c>，
/// 而 <c>t</c> 只在世界更新闸门内前进——命中定格时它停，帧数却还在涨。</description></item>
/// <item><description><b>每帧 0 分配</b>：这是蓝图 §17.2 对核心层给的验收标准，只能在真实帧循环里量。</description></item>
/// </list>
/// <para>
/// <b>它怎么做到不受"总线挂了"影响。</b>自检本身要跟着帧走，但如果它靠订阅来推进自己，就会陷入
/// "用被测对象测试被测对象"。所以推进点直接写在 <see cref="MonoFrameSystem.PreUpdateEntities"/> 里，
/// 在派发<b>之后</b>调用 <see cref="Tick"/>——那正是游戏本来就会每帧走的那条路径。
/// 于是"某个阶段一次都没派发"会被如实报出来，而不是让自检卡死。
/// </para>
/// <para>
/// <b>采样窗口是 16 帧。</b>不能更短：<c>PostDraw</c> / <c>PostDrawInterface</c> 的帧必须被采到，
/// 而"调度器 Delay 到期"那条断言要 8 个 tick 才看得到；也不该更长：那条"帧号单调"的断言会随窗口变长而变脆。
/// </para>
/// </summary>
public static class MonoSelfTest
{
    /// <summary>
    /// 采样窗口（帧）。<b>必须容得下"调度器 Delay 到期"那条断言</b>：<c>Delay(5)</c> 的到期 =
    /// 排程 tick + 7，而 tick 只在世界更新闸门内前进（每帧至多 +1），所以窗口至少要有 8 帧余量。
    /// 原来的 12 帧 + <c>Delay(20)</c>（需 22 tick）是错的——那条断言在游戏里必然失败
    /// （用户报的"执行于 tick -1"就是这个）。
    /// </summary>
    private const int WindowFrames = 16;

    /// <summary>看门狗（帧）：窗口内没凑齐就强制收尾，约 15 秒。<b>它只影响何时出报告，不影响判定</b>——没派发的阶段照样会红。</summary>
    private const int WatchdogFrames = 900;

    /// <summary>排程时基那条用例延迟多少 tick。采样窗口必须容得下它。</summary>
    private const int ScheduleDelayTicks = 5;

    /// <summary>每次 <c>/mono core selftest</c> 的累计运行次数，报告里用它区分第几次。</summary>
    private static int runCount;

    /// <summary>上一份报告。命令面用它做 <c>last</c>。</summary>
    public static string LastReport { get; private set; } = "（还没跑过自检）";

    /// <summary>报告落盘的位置。命令面 <c>where</c> 会把它回显出来。</summary>
    public static string ReportPath { get; private set; } = "(尚未生成)";

    private static Runner? current;

    /// <summary>自检是否正在采样。</summary>
    public static bool IsRunning => current is not null;

    /// <summary>开始一次自检。<b>必须在世界里、且不是专用服务器</b>（后者没有绘制相位）。</summary>
    /// <param name="filter">只跑用例名里含这个子串的用例；空串跑全部。</param>
    /// <returns>给聊天的一行回执。</returns>
    public static string Begin(string filter = "")
    {
        if (current is not null)
            return "自检已经在跑了，等它结束（或者 /mono core abort 中止）。";

        if (Main.dedServ)
            return "当前是专用服务器：PostDraw / PostDrawInterface 永远不会派发，自检在这里没有意义。请在客户端跑。";

        if (Main.gameMenu)
            return "还没进世界。自检要读真实帧钩子，请先进入一个世界再跑。";

        if (MonoEventBusSystem.Instance is null)
            return "MonoEventBusSystem 还没就位（世界刚加载完会有一瞬间这样）。过一两秒再试。";

        runCount++;

        // 注意：**不要**在这里清 MonoLifecycleProbe 的记录。装配阶段（Load→Ready）早就发生过了，
        // 清掉之后"五阶段按顺序跑过"这条断言就永远看不到证据——用户报的"探针一条记录都没有"就是这个。
        // 断言读的是 History（累计），不是会被清空的 RingBuffer。
        current = new Runner(filter);
        return $"开始自检（第 {runCount} 次，采样 {WindowFrames} 帧）。结果在这里回执，同时写到 {ReportPath}。";
    }

    /// <summary>中止当前自检（清理订阅，不留下报告）。</summary>
    /// <returns>给聊天的一行回执。</returns>
    public static string Abort()
    {
        if (current is null)
            return "当前没有在跑自检。";

        Runner runner = current;
        current = null;
        runner.Dispose();
        return "自检已中止（订阅已清理，没有留下报告）。";
    }

    /// <summary>
    /// 推进自检。由 <see cref="MonoFrameSystem.PreUpdateEntities"/> 在派发之后调用，
    /// 走的就是游戏每帧都会走的那条路。
    /// </summary>
    public static void Tick() => current?.Tick();

    /// <summary>一次自检的全部状态。做成实例而不是静态：<c>Abort</c> 之后不能有残留状态影响下一轮。</summary>
    private sealed class Runner : IDisposable
    {
        private readonly string filter;
        private readonly List<MonoSelfTestResult> results = [];

        /// <summary>
        /// 自己挂上的每一条订阅。<b>收尾只能拿它逐条退订，不能调 <c>MonoEventBus.Clear()</c></b>——
        /// 那个 API 清的是整条总线，会把别的系统挂上去的订阅一并清掉。
        /// </summary>
        private readonly List<MonoEventSubscription> hooked = [];

        /// <summary>"自退订"那条用例自己的句柄：它要在回调里退掉自己。</summary>
        private MonoEventSubscription exitHandle;

        // ── 采样窗口 ─────────────────────────────────────────────────────────
        private readonly long startedFrame;
        private int frames;
        private bool finished;

        // ── 总线观测 ─────────────────────────────────────────────────────────
        private readonly int[] phaseCounts = new int[MonoEventBus.PhaseCount];
        private readonly long[] phaseFrame = new long[MonoEventBus.PhaseCount];
        private int intraFrame;
        private bool sawSplitFrames;
        private bool sawInterleavedOrder;

        // ── 订阅语义 ─────────────────────────────────────────────────────────
        private int instanceCalls;
        private int sameInstanceCalls;
        private int dispatchSubscribeCalls;
        private bool dispatchSubscribeStarted;
        private int exitCalls;

        // ── 调度器 / 冷却 ────────────────────────────────────────────────────
        private long scheduleStartTick = -1;
        private long scheduleEndTick = -1;
        private long scheduleNow = -1;
        private bool scheduleRequested;
        private long reenterFirstTick = -1;
        private long reenterSecondTick = -1;
        private bool reenterRan;
        private bool cooldownFirst;
        private bool cooldownSecond;

        // ── 补间 ─────────────────────────────────────────────────────────────
        private MonoTween? tween;
        private readonly TweenTarget tweenTarget = new();
        private readonly int tweenCallsAtStart;
        private int tweenSpurious;
        private float tweenLastValue = float.NaN;
        // ── 分配采样 ─────────────────────────────────────────────────────────
        private bool allocSampling;
        private long allocStartBytes;
        private int allocFrames;
        private long allocBytes;
        private readonly MonoLogLevel savedLogLevel;

        // ── 时间基准快照 ─────────────────────────────────────────────────────
        private long prevFrame = -1;
        private long prevTick = -1;
        private double prevRealTime = -1;
        private bool sawNonMonotonic;
        private bool sawTickJump;
        private bool sawOversizedDelta;
        private long firstTickOfWatch = -1;
        private long lastTickOfWatch = -1;

        /// <summary>补间的 setter 目标。实例方法 + 字段自增，零捕获。</summary>
        private sealed class TweenTarget
        {
            public int Calls;

            public void Apply(float value) => Calls++;
        }

        public Runner(string filter)
        {
            this.filter = filter;
            startedFrame = MonoTime.Frame;
            tweenCallsAtStart = tweenTarget.Calls;

            HookAllPhases();
            HookSubscriptionSemantics();
            HookSchedulerAndTween();

            // 分配采样：日志本身会分配（每条一个 string），所以先把级别抬到 Warn。
            // 这是探针的固有代价，报告里会连"水位"一起写出来，免得把日志的钱算到核心路径上。
            savedLogLevel = MonoLog.Minimum;
            MonoLog.Minimum = MonoLogLevel.Warn;
            allocSampling = true;
            allocStartBytes = GC.GetAllocatedBytesForCurrentThread();
        }

        /// <summary>把 11 个阶段全部订阅一遍，其中 <c>PreUpdate</c> 那个同时是自检的推进点。</summary>
        private void HookAllPhases()
        {
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PreUpdate, OnPreUpdate));
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.WorldUpdate, OnWorldUpdate));
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PostUpdate, OnPostUpdate));
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PreDraw, OnPreDraw));
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PostUpdateNPCs, OnPostUpdateNPCs));
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PostUpdatePlayers, OnPostUpdatePlayers));
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PostUpdateProjectiles, OnPostUpdateProjectiles));
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PostDraw, OnPostDraw));
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PostDrawInterface, OnPostDrawInterface));
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.WorldUnload, OnWorldUnload));

            // Custom 库不自动派发，但"收尾时手动泵一次、订阅者恰好收到一次"本身要有人接。
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.Custom, OnCustom));
        }

        private void HookSubscriptionSemantics()
        {
            // 静态计数是静态字段，上一轮留下的数不能算进这一轮。
            DedupeTarget.Calls = 0;

            // 1) 普通实例回调：每帧被调用一次。
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PreUpdate, OnInstanceCallback));

            // 2) 同一个实例注册两次：Subscribe<T> 不查重，所以两个订阅都该被调用。
            //    用两个不同的方法组（而不是两个 lambda）——lambda 在 <c>Subscribe</c> 上会踩重载/推断的坑，
            //    而且方法组不产生闭包分配。
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PreUpdate, OnSameInstanceA));
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PreUpdate, OnSameInstanceB));

            // 3) 静态回调重复订阅：同一个静态方法，方法身份相同，第二次该被挡掉。
            //    两次都记进 hooked：第二次返回的是空句柄，退订它是安全空操作。
            Action dedupe = static () => DedupeTarget.Calls++;
            hooked.Add(MonoEventBus.SubscribeStatic(MonoFramePhase.PreUpdate, dedupe));
            hooked.Add(MonoEventBus.SubscribeStatic(MonoFramePhase.PreUpdate, dedupe));

            // 4) 自退订：只在第一次调用后失效，句柄留一份给它自己用。
            exitHandle = MonoEventBus.Subscribe<Runner>(MonoFramePhase.PostUpdate, OnExitAfterFirst);
            hooked.Add(exitHandle);

            // 5) 派发中订阅：第一帧的 PostUpdate 里加一个 PostUpdate 回调，它不该在当轮被调用。
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PostUpdate, OnFirstPostUpdate));

            // 6) 弱引用订阅的目标：这个实例除了订阅之外没有被别处强引用。
            //    显式写类型参数，是为了明确走弱引用那条泛型重载。
            WeakTarget weak = new();
            hooked.Add(MonoEventBus.Subscribe<WeakTarget>(MonoFramePhase.PreUpdate, weak.OnWeakPhase));

            // weak 是局部变量，出了这个方法就没有强引用了：条目会在某次 GC 之后自动失效。
            // 回收的时点不可控，所以自检对它只测量、不判定（见 RecordSubscriptionSemantics）。
        }

        private void HookSchedulerAndTween()
        {
            // 排程时基那条用例在 OnPreUpdate 里排程（见那里），这里只安排其余几项。

            // 执行中排入的项不在同一轮跑。
            MonoScheduler.Delay(1, static state => ((Runner)state!).OnReenterFirst(), this, "selftest.reenter-1");

            // 冷却表：第一次放行、紧接着第二次被拦。
            cooldownFirst = MonoScheduler.Cooldown("selftest.cooldown", 3);
            cooldownSecond = MonoScheduler.Cooldown("selftest.cooldown", 3);

            // 补间：40 tick 把 0 推到 100。采样窗口看不到终点，所以终点只做趋势断言；
            // 自检默认由"阶段凑齐"触发收尾，只有看门狗才会让它按帧数收尾。
            tween = MonoTween.Spawn(0f, 100f, 40, tweenTarget.Apply, MonoEaseKind.Cubic, MonoEaseMode.Out, false);
        }

        // ── 每帧推进 ─────────────────────────────────────────────────────────

        public void Tick()
        {
            long frame = MonoTime.Frame;
            long tick = MonoTime.Tick;
            double realTime = MonoTime.RealTime;

            // 时钟不变量：帧号单调、tick 每帧最多 +1（它在闸门内推进，所以定格时为 0）、真实时间单调、dt 被 clamp。
            // 「tick 最多 +1」这条同时是"命中定格时 tick 会停住"那件事的间接证据。
            if (prevFrame >= 0)
            {
                if (frame <= prevFrame)
                    sawNonMonotonic = true;
                if (tick > prevTick + 1)
                    sawTickJump = true;
                if (realTime < prevRealTime)
                    sawNonMonotonic = true;
                if (MonoTime.RealDelta > MonoTime.MaxDelta + 1e-6f)
                    sawOversizedDelta = true;
            }

            prevFrame = frame;
            prevTick = tick;
            prevRealTime = realTime;

            if (frames == 0)
                firstTickOfWatch = tick;
            lastTickOfWatch = tick;
            frames++;

            if (allocSampling)
                allocFrames++;

            // 补间：记录"值有没有回退"。终点断言在这里做不了（窗口太短），只做单调性。
            if (tween is not null && tween.IsAlive)
            {
                float value = tween.Value;
                if (!float.IsNaN(tweenLastValue) && value < tweenLastValue - 1e-4f)
                    tweenSpurious++;
                tweenLastValue = value;
            }

            // 只按帧数收尾：不能"阶段凑齐就提前结束"——那会在第 1~3 帧就收尾，
            // 而"调度器 Delay(5) 到期"要 8 个 tick 才看得到。看门狗只防卡死。
            long elapsed = frame - startedFrame;
            if (frames < WindowFrames && elapsed < WatchdogFrames)
                return;

            // 收尾绝不能把异常抛回帧钩子——那会污染游戏的更新循环，而且会把自检卡在"正在采样"。
            try
            {
                Finish();
            }
            catch (Exception exception)
            {
                MonoLog.Error(MonoLogLevel.Error, $"自检收尾失败，已放弃这一轮：{exception}");
                // 收尾失败也必须摘掉订阅：其中静态条目不会随目标回收而失效，留着就是永久泄漏。
                Unhook();
                current = null;
            }
        }

        // ── 各阶段回调（全是实例方法，走弱引用订阅路径） ──────────────────────

        private void OnPreUpdate()
        {
            // 排程时基要在一个帧回调里量，这样能同时记下 MonoTime.Tick 与 MonoScheduler.Now 两个锚点。
            if (!scheduleRequested)
            {
                scheduleRequested = true;
                scheduleStartTick = MonoTime.Tick;
                scheduleNow = MonoScheduler.Now;
                MonoScheduler.Delay(ScheduleDelayTicks, static state => ((Runner)state!).OnScheduleFired(), this, "selftest.schedule");
            }

            // 帧内序号归零必须发生在任何 Observe 之前。
            intraFrame = 0;
            Observe((int)MonoFramePhase.PreUpdate);
        }

        private void OnWorldUpdate()
        {
            // 三个共用 PostUpdateDusts 的阶段必须在同一帧、且按枚举顺序出现。
            if (phaseFrame[(int)MonoFramePhase.PostUpdate] == MonoTime.Frame
                || phaseFrame[(int)MonoFramePhase.PreDraw] == MonoTime.Frame)
                sawSplitFrames = true;
            if (intraFrame != 0)
                sawInterleavedOrder = true;
            intraFrame++;
            Observe((int)MonoFramePhase.WorldUpdate);
        }

        private void OnPostUpdate()
        {
            if (phaseFrame[(int)MonoFramePhase.WorldUpdate] != MonoTime.Frame)
                sawSplitFrames = true;
            if (intraFrame != 1)
                sawInterleavedOrder = true;
            intraFrame++;
            Observe((int)MonoFramePhase.PostUpdate);
        }

        private void OnPreDraw()
        {
            if (phaseFrame[(int)MonoFramePhase.PostUpdate] != MonoTime.Frame)
                sawSplitFrames = true;
            intraFrame++;
            Observe((int)MonoFramePhase.PreDraw);
        }

        private void OnPostUpdateNPCs() => Observe((int)MonoFramePhase.PostUpdateNPCs);

        private void OnPostUpdatePlayers() => Observe((int)MonoFramePhase.PostUpdatePlayers);

        private void OnPostUpdateProjectiles() => Observe((int)MonoFramePhase.PostUpdateProjectiles);

        private void OnPostDraw() => Observe((int)MonoFramePhase.PostDraw);

        private void OnPostDrawInterface() => Observe((int)MonoFramePhase.PostDrawInterface);

        private void OnWorldUnload() => Observe((int)MonoFramePhase.WorldUnload);

        private void OnCustom() => Observe((int)MonoFramePhase.Custom);

        /// <summary>记一次阶段观测。所有阶段回调都走它，计数与帧号不会各记一套。</summary>
        private void Observe(int index)
        {
            phaseCounts[index]++;
            phaseFrame[index] = MonoTime.Frame;
        }

        private void OnInstanceCallback() => instanceCalls++;

        /// <summary>「同一个实例注册两次」用的第一个回调。</summary>
        private void OnSameInstanceA() => sameInstanceCalls++;

        /// <summary>「同一个实例注册两次」用的第二个回调。</summary>
        private void OnSameInstanceB() => sameInstanceCalls++;

        /// <summary>
        /// 派发中订阅：只做一次，在 <c>PostUpdate</c> 派发过程中加一个 <c>PostUpdate</c> 回调。
        /// 它<b>不该在当轮被调用</b>，只该在之后的帧里被调用。
        /// </summary>
        private void OnFirstPostUpdate()
        {
            if (dispatchSubscribeStarted)
                return;

            dispatchSubscribeStarted = true;
            dispatchSubscribeCalls = 0;
            hooked.Add(MonoEventBus.Subscribe<Runner>(MonoFramePhase.PostUpdate, OnLaterPostUpdate));
        }

        private void OnLaterPostUpdate() => dispatchSubscribeCalls++;

        /// <summary>自退订：只该被调用一次，退订的必须是<b>自己</b>那条句柄。</summary>
        private void OnExitAfterFirst()
        {
            if (exitCalls++ > 0)
                return;

            // 在派发过程中退订自己：反向遍历只会跳过这一条，同阶段的其它订阅者不受影响。
            MonoEventBus.Unsubscribe(exitHandle);
        }

        private void OnScheduleFired() => scheduleEndTick = MonoTime.Tick;

        private void OnReenterFirst()
        {
            reenterFirstTick = MonoTime.Tick;
            MonoScheduler.Delay(1, static state => ((Runner)state!).OnReenterSecond(), this, "selftest.reenter-2");
        }

        private void OnReenterSecond()
        {
            reenterRan = true;
            reenterSecondTick = MonoTime.Tick;
        }

        // ── 收尾 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 唯一的收尾入口（<c>finished</c> 守卫保证只走一次）。
        /// <para>
        /// <b>顺序是有讲究的</b>：先取分配字节数再恢复日志级别（反了会把恢复时的分配算进窗口）；
        /// 先取补间/总线快照再 <c>Clear()</c>（清了就读不到了）；先落盘再回执聊天（回执本身一定分配）。
        /// </para>
        /// </summary>
        private void Finish()
        {
            if (finished)
                return;
            finished = true;

            if (allocSampling)
            {
                allocBytes = GC.GetAllocatedBytesForCurrentThread() - allocStartBytes;
                MonoLog.Minimum = savedLogLevel;
                allocSampling = false;
            }

            int tweenCalls = tweenTarget.Calls;
            int weakAlive = MonoEventBus.SubscriberCount(MonoFramePhase.PreUpdate);
            int subscribersNow = MonoEventBus.TotalSubscribers;

            // 手动泵一次 Custom：它本来就只由调用方泵，自检自己派发一次是它的**规定用法**。
            MonoEventBus.Dispatch(MonoFramePhase.Custom);

            // "加载期之外注册被拒绝"要真的调一次 Register，所以放在所有断言之后。
            long rejectedBefore = MonoServiceHost.LateRegistrationRejected;
            MonoServiceHost.Register<Services.MonoLifecycleProbe>();
            long rejectedAfter = MonoServiceHost.LateRegistrationRejected;

            RecordClock();
            RecordEventBus();
            RecordSubscriptionSemantics(subscribersNow);
            RecordSchedulerAndTween(tweenCalls);
            RecordLifecycle(rejectedBefore, rejectedAfter);

            // 订阅是我们自己挂的，必须自己摘（总线的弱订阅目标里就有 this）。
            // 逐条退订，不用 MonoEventBus.Clear()——那个 API 会清掉总线上别人的订阅。
            Unhook();

            string report;
            try
            {
                report = BuildReport(rejectedBefore, rejectedAfter, subscribersNow, weakAlive, tweenCalls);
            }
            catch (Exception exception)
            {
                // 拼报告本身炸掉不该让自检永远卡在"正在采样"：那样下一轮会被 IsRunning 挡住。
                report = $"自检收尾时拼报告失败：{exception}";
            }

            LastReport = report;
            WriteReport(report);

            try
            {
                MonoCoreCommand.ReportReady(report, results);
            }
            catch (Exception exception)
            {
                MonoLog.Error(MonoLogLevel.Error, $"自检回执失败：{exception.Message}");
            }

            current = null;
        }

        // ── 断言 ─────────────────────────────────────────────────────────────

        private void Check(string name, bool passed, string detail)
        {
            if (filter.Length > 0 && !name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                return;
            results.Add(new MonoSelfTestResult(name, passed, detail));
        }

        private void RecordClock()
        {
            Check("时钟：帧号单调不减",
                !sawNonMonotonic,
                sawNonMonotonic ? "观测到帧号或真实时间回退" : $"{frames} 帧内单调");

            Check("时钟：tick 每帧推进不超过 1（它在世界更新闸门内推进，定格时为 0）",
                !sawTickJump && lastTickOfWatch >= firstTickOfWatch,
                sawTickJump
                    ? "tick 单帧跳了 >1"
                    : $"tick {firstTickOfWatch}→{lastTickOfWatch}，{frames} 帧内增量 {lastTickOfWatch - firstTickOfWatch}");

            Check("时钟：RealDelta 已被 clamp 到 MaxDelta",
                !sawOversizedDelta,
                $"本帧 {(MonoTime.RealDelta * 1000f):F2}ms / 上限 {(MonoTime.MaxDelta * 1000f):F1}ms");

            Check("时钟：MaxDelta 就是 1/30 秒",
                Math.Abs(MonoTime.MaxDelta - (1f / 30f)) < 1e-6f,
                $"{MonoTime.MaxDelta:F6}");

            Check("时钟：采样窗口里 MonoTime.Frame 确实在走",
                prevFrame > startedFrame,
                $"起始帧 {startedFrame} → 收尾帧 {prevFrame}");
        }

        private void RecordEventBus()
        {
            (MonoFramePhase Phase, string Why)[] mustDispatch =
            [
                (MonoFramePhase.PreUpdate, "PreUpdateEntities"),
                (MonoFramePhase.WorldUpdate, "PostUpdateDusts"),
                (MonoFramePhase.PostUpdate, "PostUpdateDusts"),
                (MonoFramePhase.PreDraw, "PostUpdateDusts"),
                (MonoFramePhase.PostUpdateNPCs, "PostUpdateNPCs"),
                (MonoFramePhase.PostUpdatePlayers, "PostUpdatePlayers"),
                (MonoFramePhase.PostUpdateProjectiles, "PostUpdateProjectiles"),
                (MonoFramePhase.PostDraw, "PostDrawTiles"),
                (MonoFramePhase.PostDrawInterface, "PostDrawInterface"),
            ];

            foreach ((MonoFramePhase phase, string why) in mustDispatch)
            {
                int seen = phaseCounts[(int)phase];
                Check($"总线：{phase} 被派发到订阅者",
                    seen >= 1,
                    seen >= 1 ? $"观测 {seen} 次（来自 {why}）" : $"一次都没观测到——{why} 这个钩子没挂上？");
            }

            int unloads = phaseCounts[(int)MonoFramePhase.WorldUnload];
            Check("总线：WorldUnload 在游玩期间不该派发",
                unloads == 0,
                unloads == 0 ? "未派发（正确：它不是每帧阶段）" : $"派发了 {unloads} 次——是不是刚退出了世界？");

            int customSeen = phaseCounts[(int)MonoFramePhase.Custom];
            Check("总线：Custom 只由调用方泵（收尾时手动泵一次后应恰好为 1）",
                customSeen == 1,
                $"观测 {customSeen} 次");

            long worldUpdateFrame = phaseFrame[(int)MonoFramePhase.WorldUpdate];
            long postUpdateFrame = phaseFrame[(int)MonoFramePhase.PostUpdate];
            long preDrawFrame = phaseFrame[(int)MonoFramePhase.PreDraw];
            bool sameFrame = worldUpdateFrame == postUpdateFrame && postUpdateFrame == preDrawFrame;

            Check("总线：WorldUpdate / PostUpdate / PreDraw 三个时基相同的阶段落在同一帧",
                sameFrame && !sawSplitFrames,
                sameFrame
                    ? $"三者在第 {worldUpdateFrame} 帧被观测到"
                    : $"分别在第 {worldUpdateFrame} / {postUpdateFrame} / {preDrawFrame} 帧");

            Check("总线：三个同帧阶段之间没有插进别的阶段（枚举顺序成立）",
                !sawInterleavedOrder,
                sawInterleavedOrder ? "WorldUpdate 与 PostUpdate 之间插进了其它阶段" : "顺序符合枚举顺序");

            long drawFrame = phaseFrame[(int)MonoFramePhase.PostDraw];
            Check("总线：绘制阶段发生在更新阶段之后",
                drawFrame >= postUpdateFrame,
                $"PostUpdate 第 {postUpdateFrame} 帧，PostDraw 第 {drawFrame} 帧");
        }

        private void RecordSubscriptionSemantics(int subscribersNow)
        {
            Check("总线：普通实例回调每帧被调用",
                instanceCalls >= 1,
                $"被调用 {instanceCalls} 次");

            Check("总线：同一个实例注册两次会得到两个订阅（Subscribe<T> 不查重）",
                sameInstanceCalls >= 2,
                sameInstanceCalls >= 2
                    ? $"两个回调共被调用 {sameInstanceCalls} 次"
                    : $"只被调用 {sameInstanceCalls} 次——泛型重载把重复订阅挡掉了？");

            Check("总线：静态回调重复订阅被挡掉（每个派发只该跑一次）",
                phaseCounts[(int)MonoFramePhase.PreUpdate] > 0
                && DedupeTarget.Calls == phaseCounts[(int)MonoFramePhase.PreUpdate],
                $"PreUpdate 派发 {phaseCounts[(int)MonoFramePhase.PreUpdate]} 次，"
                + $"静态回调被调用 {DedupeTarget.Calls} 次（应为 1:1）");

            Check("总线：派发中新加的订阅不在本轮跑、下一轮跑",
                dispatchSubscribeCalls >= 1,
                dispatchSubscribeCalls >= 1
                    ? $"其后各帧共被调用 {dispatchSubscribeCalls} 次"
                    : "一次都没跑——新订阅可能被本轮派发吃掉了");

            Check("总线：自退订后不再被调用",
                exitCalls == 1,
                $"自退订回调被调用 {exitCalls} 次（应为 1）");

            Check("总线：弱引用订阅的活订阅数可读（回收时机不可控，只做测量）",
                subscribersNow >= 0,
                $"收尾时活订阅 {subscribersNow} 个（含一条没有强引用目标的弱订阅）");
        }

        private void RecordSchedulerAndTween(int tweenCalls)
        {
            // 断言锚在 MonoScheduler.Now 上：契约是 Due = Now + n + 2，而泵的截止线是 Due ≤ 泵tick + 1，
            // 于是回调必定在"泵 tick = Now + n + 1"那一帧跑起来。锚在 Now 上就不依赖排程发生在帧里的哪个位置。
            Check($"调度器：Delay({ScheduleDelayTicks}) 在 Now + {ScheduleDelayTicks + 1} 那一 tick 执行",
                scheduleStartTick >= 0 && scheduleEndTick == scheduleNow + ScheduleDelayTicks + 1,
                $"排程于 tick {scheduleStartTick}（MonoScheduler.Now={scheduleNow}），执行于 tick {scheduleEndTick}，"
                + $"相对 Now 差 {scheduleEndTick - scheduleNow}（应差 {ScheduleDelayTicks + 1}）");

            Check("调度器：执行中排入的项不在同一轮执行",
                reenterRan && reenterSecondTick > reenterFirstTick,
                reenterRan
                    ? $"第一项 tick {reenterFirstTick}，第二项 tick {reenterSecondTick}"
                    : $"第二项从没执行（第一项 tick {reenterFirstTick}）");

            Check("调度器：Cooldown 第一次放行、紧接着第二次拦住",
                cooldownFirst && !cooldownSecond,
                $"首次 {cooldownFirst}，立刻第二次 {cooldownSecond}（应为 True / False）");

            Check("调度器：Rejected = 0（没有每帧重排把容量打满）",
                MonoScheduler.Rejected == 0,
                $"Rejected = {MonoScheduler.Rejected}");

            Check("调度器：Failed = 0（没有动作抛异常）",
                MonoScheduler.Failed == 0,
                $"Failed = {MonoScheduler.Failed}");

            Check("补间：按 (类型,方向) 生成时复用了常驻委托",
                tween is not null && ReferenceEquals(tween.Ease, MonoUtil.Ease(MonoEaseKind.Cubic, MonoEaseMode.Out)),
                tween is null ? "补间没借到" : "Ease 就是表里那一格，不是新 lambda");

            Check("补间：setter 每 tick 被调用",
                tweenCalls > tweenCallsAtStart,
                $"setter 被调用 {tweenCalls} 次（起始 {tweenCallsAtStart}）");

            Check("补间：值单调朝目标推进（没有回退）",
                tweenSpurious == 0,
                tweenSpurious == 0 ? "未观测到回退" : $"观测到 {tweenSpurious} 次回退");
        }

        private void RecordLifecycle(long rejectedBefore, long rejectedAfter)
        {
            Check("主机：注册表里真的有服务（不是恒为 0 的空壳）",
                MonoServiceHost.Count >= 1,
                $"{MonoServiceHost.Count} 个");

            Check("主机：装配完成（阶段 = Loaded）",
                MonoServiceHost.IsReady,
                $"IsReady = {MonoServiceHost.IsReady}");

            // 用 History（自加载以来的累计记录），不是 Records（那份会被自检的 ResetRecords 清掉）。
            IReadOnlyList<string> records = Services.MonoLifecycleProbe.History;
            string[] firstFive = ["Load", "RegisterDefs", "PostSetup", "InstallHooks", "Ready"];
            bool ordered = records.Count >= firstFive.Length;
            for (int i = 0; ordered && i < firstFive.Length; i++)
                ordered = records[i] == firstFive[i];

            Check("主机：五阶段按 Load→RegisterDefs→PostSetup→InstallHooks→Ready 顺序执行",
                ordered,
                records.Count == 0 ? "探针一条记录都没有" : string.Join(" → ", records));

            Check("主机：世界里服务仍处于已装配状态（Unload 还没跑）",
                !records.Contains("Unload"),
                records.Contains("Unload") ? "已被卸载" : "仍已装配");

            Check("主机：加载期之外的注册被拒绝（且计数可观测）",
                rejectedAfter == rejectedBefore + 1,
                $"收尾时试注册一次：拒绝计数 {rejectedBefore} → {rejectedAfter}");

            Check("主机：Describe 能列出服务",
                MonoServiceHost.Describe().Contains(nameof(Services.MonoLifecycleProbe), StringComparison.Ordinal),
                "Describe 里含 MonoLifecycleProbe");

            // 自动注册的失败模式是"静默地什么都没扫到"，所以这里断言扫描确实枚举过类型。
            Check("主机：自动注册扫描真的枚举到了类型（不是空转）",
                MonoServiceHost.AutoRegisterCandidates >= 1,
                $"扫描过 {MonoServiceHost.AutoRegisterCandidates} 个候选类型，自动注册新增 {MonoServiceHost.AutoRegistered} 个");
        }

        // ── 报告 ─────────────────────────────────────────────────────────────

        private string BuildReport(long rejectedBefore, long rejectedAfter, int subscribersNow, int weakAlive, int tweenCalls)
        {
            int passed = 0;
            foreach (MonoSelfTestResult result in results)
            {
                if (result.Passed)
                    passed++;
            }

            StringBuilder text = new();
            text.Append("Monochrome 核心层 · 游戏内自检报告\n");
            text.Append("==================================================\n");
            text.Append("第 ").Append(runCount).Append(" 次运行");
            if (filter.Length > 0)
                text.Append("（过滤：").Append(filter).Append("）");
            text.Append('\n');
            text.Append("tML ").Append(BuildInfo.tMLVersion).Append("，专用服务器 ").Append(Main.dedServ).Append('\n');
            text.Append("采样窗口 ").Append(frames).Append(" 帧（第 ").Append(startedFrame).Append(" → 第 ").Append(prevFrame).Append(" 帧）\n");
            text.Append("结果：通过 ").Append(passed).Append(" / ").Append(results.Count);
            text.Append(passed == results.Count ? "（全绿）" : "（有失败）").Append("\n\n");

            text.Append("[各阶段实测]\n");
            for (int i = 0; i < MonoEventBus.PhaseCount; i++)
            {
                text.Append("  ").Append(((MonoFramePhase)i).ToString().PadRight(22))
                    .Append("观测 ").Append(phaseCounts[i].ToString().PadLeft(3))
                    .Append("   末次帧 ").Append(phaseFrame[i])
                    .Append('\n');
            }

            text.Append("\n[每帧分配]\n");
            text.Append("  窗口 ").Append(allocFrames).Append(" 帧，共分配 ").Append(allocBytes).Append(" 字节");
            if (allocFrames > 0)
                text.Append("（约 ").Append((allocBytes / (double)allocFrames).ToString("F1")).Append(" 字节/帧）");
            text.Append('\n');
            text.Append("  采样期间日志最低级别被抬到 Warn，以排除日志自身的分配。\n");
            text.Append("  这个数包含同进程里其它模组的分配，所以它是**基线**而不是阈值：\n");
            text.Append("  判据是「跨运行可比」，不是「必须为 0」。\n");

            text.Append("\n[服务生命周期探针·自加载以来的完整记录]\n");
            IReadOnlyList<string> records = Services.MonoLifecycleProbe.History;
            text.Append("  ").Append(records.Count == 0 ? "(无记录)" : string.Join(" → ", records)).Append('\n');

            text.Append("\n[总线]\n");
            text.Append("  收尾时活订阅 ").Append(subscribersNow).Append(" 个，PreUpdate 阶段 ").Append(weakAlive).Append(" 个\n");
            text.Append("  补间 setter 共被调用 ").Append(tweenCalls).Append(" 次\n");

            text.Append("\n[用例]\n");
            foreach (MonoSelfTestResult result in results)
            {
                text.Append(result.Passed ? "  ok  " : "  ×   ").Append(result.Name);
                if (result.Detail.Length > 0)
                    text.Append("  — ").Append(result.Detail);
                text.Append('\n');
            }

            text.Append("\n[收尾探针] 加载期之外注册被拒绝：").Append(rejectedBefore).Append(" → ").Append(rejectedAfter);
            text.Append(rejectedAfter == rejectedBefore + 1 ? "（符合预期）\n" : "（异常：没有被拦住）\n");

            text.Append("\n[仍需人工制造条件的两种情况]\n");
            text.Append("  1. 命中定格（hitstop）时 tick 应当停住而帧号继续涨——自检只能证明「tick 单帧最多 +1」。\n");
            text.Append("  2. 弱引用订阅目标被 GC 回收后条目应当失效——回收时机不可控，自检只做测量、不做判定。\n");
            return text.ToString();
        }

        private void WriteReport(string report)
        {
            ReportPath = Path.Combine(ModLoader.ModPath, "Monochrome-CoreSelfTest.txt");
            try
            {
                File.WriteAllText(ReportPath, report);
            }
            catch (Exception exception)
            {
                MonoLog.Error(MonoLogLevel.Error, $"自检报告写盘失败（{ReportPath}）：{exception.Message}");
                ReportPath = $"(写盘失败：{exception.Message})";
            }
        }

        public void Dispose() => Unhook();

        /// <summary>退掉自己挂的每一条订阅。重复退订是安全空操作。</summary>
        private void Unhook()
        {
            for (int i = 0; i < hooked.Count; i++)
                MonoEventBus.Unsubscribe(hooked[i]);

            hooked.Clear();
            exitHandle = default;
        }

        /// <summary>静态回调的计数目标。同一帧只该 +1（重复订阅被挡）。</summary>
        private static class DedupeTarget
        {
            public static int Calls;
        }

        /// <summary>弱引用订阅的目标：除了那条订阅之外没有被别处强引用。</summary>
        private sealed class WeakTarget
        {
            public void OnWeakPhase() { }
        }
    }
}
