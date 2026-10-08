using System.Text;

namespace Monochrome.Core;

/// <summary>
/// 分配探针：连续采样若干帧的 <c>GC.GetAllocatedBytesForCurrentThread()</c> 差值，用来证明
/// 「热路径零分配」这条不变量（蓝图 §17.2 对核心层给的验收标准就是它）。
/// <para>
/// <b>为什么要把"它自己"扣掉</b>：探针运行时日志与调度器本身也在跑。如果只报一个总数，
/// "我测试的时候顺手记了一条日志"会被误读成"核心层在分配"。所以它按来源分开报：
/// 日志新增的条数、调度器执行的动作数、以及净分配字节数，三者一起看才有意义。
/// </para>
/// </summary>
public static class MonoAllocProbe
{
    private static bool running;
    private static long startFrame;
    private static long framesToSample;
    private static long startBytes;
    private static long startLogWritten;
    private static long startExecuted;

    /// <summary>上一次采样的结果。</summary>
    public static MonoAllocReport LastReport { get; private set; }

    /// <summary>是否正在采样。</summary>
    public static bool IsRunning => running;

    /// <summary>开始采样。</summary>
    /// <param name="frames">采多少帧。建议 ≥ 120（两秒），太短会被单次偶发分配带偏。</param>
    /// <returns>本次采样的起始状态描述。</returns>
    public static string Begin(int frames = 300)
    {
        if (running)
            return $"已经在采样了（还剩 {FramesRemaining()} 帧）。";

        running = true;
        startFrame = MonoTime.Frame;
        framesToSample = Math.Max(1, frames);
        startBytes = GC.GetAllocatedBytesForCurrentThread();
        startLogWritten = MonoLog.Written;
        startExecuted = MonoScheduler.Executed;

        return $"开始采样 {framesToSample} 帧。这段时间里让世界照常运行——结束时用 /mono core 看结果。";
    }

    /// <summary>中止采样。</summary>
    public static string Abort()
    {
        if (!running)
            return "当前没有在采样。";

        running = false;
        framesToSample = 0;
        return "采样已中止，没有留下结果。";
    }

    /// <summary>本帧起点调用一次（由 <see cref="MonoFrameSystem"/> 驱动，不需要订阅）。</summary>
    public static void Tick()
    {
        if (!running)
            return;

        long elapsed = MonoTime.Frame - startFrame;
        if (elapsed < framesToSample)
            return;

        running = false;
        LastReport = new MonoAllocReport
        {
            Frames = (int)elapsed,
            Bytes = GC.GetAllocatedBytesForCurrentThread() - startBytes,
            LogLines = MonoLog.Written - startLogWritten,
            ScheduledActions = MonoScheduler.Executed - startExecuted,
            LogEnabled = MonoLog.Minimum,
            BusSubscribers = MonoEventBus.TotalSubscribers,
            PendingSchedules = MonoScheduler.PendingCount,
            ActiveTweens = MonoTween.ActiveCount
        };
    }

    private static long FramesRemaining() => Math.Max(0, framesToSample - (MonoTime.Frame - startFrame));

    /// <summary>把上一次结果拼成可读的一行报告。</summary>
    public static string Describe()
    {
        if (running)
            return $"分配采样进行中，还剩 {FramesRemaining()} 帧。";

        if (LastReport.Frames <= 0)
            return "还没有采样结果。用 /monocore alloc [帧数] 开始。";

        MonoAllocReport r = LastReport;
        StringBuilder output = new();
        output.Append("分配采样：").Append(r.Frames).Append(" 帧内共分配 ").Append(r.Bytes).Append(" 字节")
              .Append('（').Append((r.Bytes / (double)r.Frames).ToString("F1")).Append(" 字节/帧）");

        output.Append("\n  日志新增 ").Append(r.LogLines).Append(" 条（当时最低级别 ").Append(r.LogEnabled).Append('）')
              .Append("\n  调度器执行 ").Append(r.ScheduledActions).Append(" 个动作（待执行 ").Append(r.PendingSchedules).Append('）')
              .Append("\n  活动补间 ").Append(r.ActiveTweens).Append(" 个")
              .Append("\n  事件订阅 ").Append(r.BusSubscribers).Append(" 个");

        if (r.Bytes == 0)
            output.Append("\n结论：这段时间里这条线程一次托管分配都没有发生。");
        else if (r.LogLines > 0)
            output.Append("\n结论：有分配，但同期记了 ").Append(r.LogLines)
                  .Append(" 条日志——每条日志必然分配一个 string。要测纯核心路径，先把日志级别调到 Warn 再采一次。");
        else
            output.Append("\n结论：有分配且没有日志解释它。查订阅者（").Append(r.BusSubscribers)
                  .Append(" 个）与补间的 setter——它们都在探针的采样窗口内。");
        return output.ToString();
    }
}

/// <summary>一次分配采样的结果。<b>结构体</b>：它自己不该成为一次分配。</summary>
public struct MonoAllocReport
{
    /// <summary>采样的帧数。</summary>
    public int Frames;

    /// <summary>这段时间里当前线程总共分配了多少字节。</summary>
    public long Bytes;

    /// <summary>同期写入日志的条数（每条必然分配一个 string）。</summary>
    public long LogLines;

    /// <summary>同期调度器执行的动作数。</summary>
    public long ScheduledActions;

    /// <summary>采样时的日志最低级别。</summary>
    public MonoLogLevel LogEnabled;

    /// <summary>采样时的事件订阅数。</summary>
    public int BusSubscribers;

    /// <summary>采样时的待执行排程数。</summary>
    public int PendingSchedules;

    /// <summary>采样时的活动补间数。</summary>
    public int ActiveTweens;
}

/// <summary>
/// <c>/monocore</c>：核心层的诊断与自检。
/// <para>
/// 全部子命令都是<b>只读</b>的（打印注册表、总线订阅、池状态），唯一带状态的是 <c>alloc</c>——
/// 它只是打开一个计数器，不改游戏状态，所以也不需要开发期门槛。
/// </para>
/// <para>
/// <b>命令名为什么是一个词 <c>monocore</c>，而不是 <c>/mono core</c>：</b>
/// tML 用命令触发文本的<b>第一段</b>做键，<c>"mono core"</c> 的键就是 <c>"mono"</c>——
/// 与图形命令的 <c>"mono"</c> 撞在一起。撞了之后先注册的那个吃下全部 <c>/mono ...</c>，
/// 另一个<b>永远接不到</b>，而且<b>不报错</b>。既然要有两个独立的命令，触发词就必须是<b>两个不同的第一段</b>：
/// <c>monocore</c> 与 <c>monoshader</c>。验收台里那条元数据用例就是守这件事的。
/// </para>
/// <para>
/// 子命令从第二段起（<c>args[1]</c>）：<c>/monocore selftest</c> 里 <c>selftest</c> 是子命令。
/// <b>不认识的子命令不再回显全部用法</b>，而是指向 <c>/monocore help</c>——见 <see cref="Action"/>。
/// </para>
/// </summary>
public sealed class MonoCoreCommand : ModCommand
{
    /// <summary>命令触发词。<b>必须与图形命令的第一段不同</b>，否则其中一个会静默失效。</summary>
    public override string Command => Trigger;

    /// <summary>触发词的字面量。<see cref="Command"/> 与"判断 args[0] 是不是命令名"都用它，避免两处各写一遍。</summary>
    private const string Trigger = "monocore";

    /// <inheritdoc/>
    public override CommandType Type => CommandType.Chat;

    /// <summary>
    /// 全部子命令的清单。<b>只在 <c>/monocore help</c>（以及 tML 的帮助界面）里出现</b>——
    /// 这不是"用法"，而是"可用子命令的清单"。两者分开，是为了让"打错了"这个常见情况只回一行提示。
    /// </summary>
    public override string Usage =>
        "/monocore help           显示这条清单" +
        "\n/monocore status         概要（子系统、总线、时钟、调度器、补间、日志、上次采样/自检）" +
        "\n/monocore services       子系统注册表与初始化顺序" +
        "\n/monocore bus            事件总线：11 个阶段的派发次数与订阅者" +
        "\n/monocore sched          调度器待执行项" +
        "\n/monocore log [n]        最近 n 条日志（默认 40）" +
        "\n/monocore last           重放上一次自检报告" +
        "\n/monocore where          自检报告落盘的位置" +
        "\n/monocore abort          中止正在进行的采样或自检" +
        "\n\n下面两条会起采样、写报告文件，只在开发期可用（判定见 MonoDebug）：" +
        "\n/monocore selftest [过滤] 游戏内自检（默认采样 16 帧）：总线 11 阶段 / 调度时基 / 补间 / 生命周期" +
        "\n/monocore alloc [帧数]   单独采样每帧分配（默认 300 帧）";

    /// <inheritdoc/>
    public override string Description =>
        "Monochrome 核心层诊断：子系统 / 事件总线 / 时钟 / 调度器 / 补间 / 日志 / 游戏内自检 / 分配采样。" +
        "（输入 /monocore help 看全部子命令）";

    /// <summary>
    /// 自检收尾时被调用：把摘要发到聊天，并提示报告文件在哪。
    /// <para>
    /// 只发<b>摘要 + 前若干条失败</b>：完整报告可能上百行，全塞进聊天会把世界消息冲掉。
    /// 详细内容在 <see cref="MonoSelfTest.ReportPath"/> 指的文件里，或者用 <c>/monocore last</c> 重放。
    /// </para>
    /// </summary>
    /// <param name="report">完整报告文本。</param>
    /// <param name="results">逐条用例结果。</param>
    internal static void ReportReady(string report, IReadOnlyList<MonoSelfTestResult> results)
    {
        int passed = 0;
        foreach (MonoSelfTestResult result in results)
        {
            if (result.Passed)
                passed++;
        }

        bool allPassed = passed == results.Count;
        Color color = allPassed ? Color.LightGreen : Color.OrangeRed;

        StringBuilder summary = new();
        summary.Append("核心层自检：通过 ").Append(passed).Append(" / ").Append(results.Count);
        summary.Append(allPassed ? "（全绿）" : "（有失败）");

        int failuresShown = 0;
        for (int i = 0; i < results.Count; i++)
        {
            MonoSelfTestResult result = results[i];
            if (result.Passed)
                continue;
            if (failuresShown >= 6)
                break;

            failuresShown++;
            summary.Append("\n  × ").Append(result.Name);
            if (result.Detail.Length > 0)
                summary.Append(" — ").Append(result.Detail);
        }

        if (!allPassed && failuresShown >= 6)
            summary.Append("\n  ……其余失败见报告文件。");

        summary.Append("\n完整报告：").Append(MonoSelfTest.ReportPath);
        summary.Append("\n（/monocore last 可重放，共 ").Append(report.Length).Append(" 字符）");

        SendToChat(summary.ToString(), color);
    }

    /// <summary>
    /// <c>/monocore ...</c> 的唯一入口。
    /// <para>
    /// <b>不认识的子命令只回一行提示</b>，不把整张清单砸进聊天——那张清单可能十几行，而"打错了"是最常见的情况。
    /// 要看清单就显式打 <c>/monocore help</c>（<see cref="Help"/>）。
    /// </para>
    /// </summary>
    /// <param name="caller">发起命令的调用者（聊天里就是玩家自己）。</param>
    /// <param name="input">原始输入。</param>
    /// <param name="args">按空格切开的参数：<c>args[0]</c> 是触发词 <c>monocore</c>，子命令从 <c>args[1]</c> 起。</param>
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        try
        {
            Dispatch(caller, args);
        }
        catch (Exception exception)
        {
            // tML 的 CommandLoader 对任何异常的处理是 `Reply("Usage: " + Usage, Red)`——
            // 也就是**任何**内部错误都会伪装成"打错子命令"，看起来完全不像崩溃。
            // 所以这里必须自己接住并记下来，否则真正的故障会被那句 Usage 掩盖。
            MonoLog.Error(MonoLogLevel.Error,
                $"/monocore 处理时抛异常（input=\"{input}\"，args=[{string.Join(",", args ?? [])}]）：{exception}");
            caller.Reply($"内部错误：{exception.GetType().Name} — {exception.Message}（详见 /monocore log）", Color.Red);
        }
    }

    /// <summary>
    /// 实际的分发。
    /// <para>
    /// <b>参数形状是实测出来的，别按源码想当然。</b>tML 的 <c>HandleCommand</c> 源码看着是
    /// <c>args = input.Split(' ').Skip(1)</c>，但实测（<c>/monocore status</c> 不行、
    /// <c>/monocore monocore status</c> 行）说明：<b><c>args[0]</c> 是命令名那一格</b>，
    /// <b>子命令在 <c>args[1]</c></b>；而<b>裸命令时 <c>args</c> 长度是 0</b>（不是 1）。
    /// 所以"/monocore + 子命令"这种正常写法，子命令永远落在 <c>args[1]</c>。
    /// </para>
    /// <para>
    /// 为了不再依赖这个形状，这里先判断 <c>args[0]</c> 是不是命令名/子命令：
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>args[0]</c> 是 <c>monocore</c> → 子命令在 <c>args[1]</c>（即 <c>monocore status</c> 的实测形状）。</description></item>
    /// <item><description><c>args[0]</c> 是已知子命令 → 子命令就是它（即 <c>args[0] == status</c> 的形状）。</description></item>
    /// <item><description>其它 → 当成子命令处理（拼错时就该报"不存在子命令"）。</description></item>
    /// </list>
    /// </summary>
    private void Dispatch(CommandCaller caller, string[] args)
    {
        // 去掉空段：tML 用的是 `input.Split(' ')`（**没有** RemoveEmptyEntries），
        // 所以多打一个空格会切出空串，不过滤的话它会落进"未知子命令"。
        string[] tokens = args is null ? [] : Array.FindAll(args, static token => token.Length > 0);

        int start = tokens.Length > 0 && IsCommandName(tokens[0]) ? 1 : 0;
        string sub = tokens.Length > start ? tokens[start].ToLowerInvariant() : HelpSubCommand;

        // 会起采样、写报告文件、往总线上挂订阅的子命令需要开发期；只读的随时可用。
        // help 与 abort 永远放行——没有调试器时打错字也得有提示，中止也得能用。
        if (IsStateChanging(sub) && !MonoDebug.IsDeveloperSession)
        {
            caller.Reply(MonoDebug.ExplainBlocked("这条子命令",
                "help / status / services / bus / sched / log / last / where / abort"), Color.Orange);
            return;
        }

        switch (sub)
        {
            case HelpSubCommand:
            case "?":
            case "-h":
            case "--help":
                caller.Reply(Help(), Color.LightGreen);
                break;

            // 概要。以前的"裸命令"给概要，现在裸命令给 help（那更符合"我不知道有什么"的处境）。
            case "status":
            case "overview":
                caller.Reply(Overview(), Color.LightGreen);
                break;

            case "services":
            case "service":
                caller.Reply(MonoServiceHost.Describe(), Color.LightGreen);
                break;

            case "bus":
                caller.Reply(MonoEventBus.Describe(), Color.LightGreen);
                break;

            case "sched":
            case "scheduler":
                caller.Reply(MonoScheduler.Dump(), Color.LightGreen);
                break;

            case "log":
                caller.Reply(TailLog(ReadInt(tokens, start + 1) ?? 40), Color.LightGreen);
                break;

            case "selftest":
            case "test":
                caller.Reply(MonoSelfTest.Begin(ReadString(tokens, start + 1)), Color.LightGreen);
                break;

            case "last":
                caller.Reply(MonoSelfTest.LastReport, Color.LightGreen);
                break;

            case "where":
                caller.Reply($"自检报告：{MonoSelfTest.ReportPath}", Color.LightGreen);
                break;

            case "alloc":
                caller.Reply(MonoAllocProbe.Begin(ReadInt(tokens, start + 1) ?? 300), Color.LightGreen);
                break;

            case "abort":
                caller.Reply(MonoAllocProbe.Abort() + "\n" + MonoSelfTest.Abort(), Color.LightGreen);
                break;

            default:
                // 打错了：只给"去哪查"，不把清单倒出来。
                caller.Reply($"不存在子命令「{sub}」。可用 /monocore help 查询全部子命令。", Color.Orange);
                break;
        }
    }

    /// <summary>这些子命令会改动状态：起采样、写报告文件、往总线上挂订阅。</summary>
    /// <param name="sub">已经小写化的子命令。</param>
    private static bool IsStateChanging(string sub) => sub is "selftest" or "test" or "alloc";

    /// <summary>这一格是不是命令触发词。用来判断 <c>args</c> 里包不包含命令名那一格。</summary>
    private static bool IsCommandName(string token)
        => token.Equals(Trigger, StringComparison.OrdinalIgnoreCase);

    /// <summary>取第 <paramref name="index"/> 格并转成整数；越界或不是数字时返回 null。</summary>
    private static int? ReadInt(string[] tokens, int index)
        => index < tokens.Length && int.TryParse(tokens[index], out int value) ? value : null;

    /// <summary>取第 <paramref name="index"/> 格；越界时返回空串。</summary>
    private static string ReadString(string[] tokens, int index)
        => index < tokens.Length ? tokens[index] : "";

    /// <summary>子命令清单。<b>只有显式要它的时候才输出。</b></summary>
    private string Help() => Usage;

    /// <summary>提供帮助的子命令名。分发的默认分支就是它（裸命令也给清单）。</summary>
    private const string HelpSubCommand = "help";

    /// <summary>
    /// tML 建的那个命令实例。<b>自检收尾需要用它回执</b>，而那时已经拿不到 <see cref="CommandCaller"/> 了。
    /// <para>它为 <c>null</c> 时（例如这个世界还没经过 <c>Load</c>）退化成新建一个——回执只需要能发消息。</para>
    /// </summary>
    private static MonoCoreCommand? current;

    /// <inheritdoc/>
    public override void Load() => current = this;

    /// <summary>
    /// 把多行消息发到聊天。<c>CommandCaller</c> 在自检收尾时已经过期了，所以那条路径用
    /// <c>Main.NewText(string, Color?)</c>；这里统一走它，两条路径的输出格式才不会各写一套。
    /// </summary>
    /// <param name="message">可含换行的消息。</param>
    /// <param name="color">颜色。</param>
    internal static void SendToChat(string message, Color color)
    {
        string[] lines = message.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            Main.NewText(lines[i], color);
    }

    private static string Overview()
    {
        StringBuilder output = new();
        output.Append(MonoServiceHost.Describe());
        output.Append("\n\n").Append(MonoEventBus.Describe());
        output.Append("\n\n时钟：tick ").Append(MonoTime.Tick)
              .Append("，真实时间 ").Append(MonoTime.RealTime.ToString("F2")).Append("s")
              .Append("，帧 ").Append(MonoTime.Frame)
              .Append("，本帧 dt ").Append((MonoTime.RealDelta * 1000f).ToString("F2")).Append("ms")
              .Append("（上限 ").Append((MonoTime.MaxDelta * 1000f).ToString("F1")).Append("ms）");
        output.Append("\n\n").Append(MonoScheduler.Describe());
        output.Append("\n").Append(MonoTween.Describe());
        output.Append("\n").Append(MonoLog.Describe());
        output.Append("\n").Append(MonoSelfTest.IsRunning ? "自检：采样进行中……" : "自检：用 /monocore selftest 跑，/monocore last 看重放。");
        output.Append("\n\n").Append(MonoAllocProbe.Describe());
        return output.ToString();
    }

    private static string TailLog(int count)
    {
        if (MonoLog.Count == 0)
            return "日志为空。";

        string all = MonoLog.Dump();
        string[] lines = all.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int take = Math.Clamp(count, 1, lines.Length);

        StringBuilder output = new();
        output.Append("最近 ").Append(take).Append(" 条（共 ").Append(lines.Length).Append("）：");
        for (int i = lines.Length - take; i < lines.Length; i++)
            output.Append('\n').Append(lines[i]);
        return output.ToString();
    }
}
