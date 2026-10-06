namespace Monochrome.Core;

/// <summary>
/// 库的统一时钟。<b>tML 里有三个互不相同的时钟</b>，混用会产生三类看起来像玄学的 bug：
/// 暂停时东西还在动（用了真实时间）、单人多人行为不同（用了客户端时间）、切出窗口回来物理爆炸（用了未 clamp 的 dt）。
/// <para>
/// 因此本类把三个时钟<b>显式命名</b>，并要求每个模块声明自己用哪一个：
/// </para>
/// <list type="table">
/// <item><term><see cref="Tick"/></term><description>世界 tick。<b>玩法逻辑一律用它</b>：暂停即停、两端一致、"第 300 tick"在任何一次游戏里含义相同。</description></item>
/// <item><term><see cref="RealTime"/></term><description>真实秒数，不随世界暂停。<b>纯视觉用它</b>（呼吸、闪烁、噪声滚动），这样定格时画面依然"活着"。</description></item>
/// <item><term><see cref="Frame"/></term><description>自本进程启动以来渲染了多少帧，用于"每 N 帧做一次"的节流。</description></item>
/// </list>
/// <para>
/// <see cref="RealDelta"/> 已 clamp 到 <see cref="MaxDelta"/>：不 clamp 的话，切出窗口 10 秒再回来会喂给积分器
/// 一个巨大的 dt，绳索与布条会当场炸开。
/// </para>
/// </summary>
public static class MonoTime
{
    /// <summary>真实时间步长的上限（秒）。掉帧或切出窗口时用它封顶，防止积分器爆炸。</summary>
    public const float MaxDelta = 1f / 30f;

    /// <summary>
    /// 一秒对应多少 tick：<b>tML 的名义逻辑帧率</b>，也是"秒 → tick"唯一的换算常数。
    /// <para>
    /// 真实帧率会抖动，所以这个数只做名义换算用（例如补间把每帧的 dt 累加成 tick）。
    /// 需要"这一帧世界推进了多少"的地方请读 <see cref="Tick"/> 的增量，不要用它去乘帧数。
    /// </para>
    /// </summary>
    public const float TicksPerSecond = 60f;

    /// <summary>
    /// 本次世界加载以来的 tick 数，<b>清零过、且不跨世界累计</b>。
    /// <para>
    /// 它不等于 <c>Main.GameUpdateCount</c>（全局、跨世界累计）。归零是刻意的：回放、单元测试、
    /// "第 300 tick 触发"这类设计需要一个每次游戏都一致的原点。
    /// </para>
    /// <para>命中定格 / 世界时间缩放为 0 时它<b>不</b>推进——玩法逻辑就该跟着世界一起停。</para>
    /// <para>
    /// setter 是公开的，<b>但只有 <see cref="MonoFrameSystem"/> 与验收台该写它</b>。
    /// 把它交回去自己推进（例如"想要一个可暂停的自定义时间轴"）等于绕开整条时钟纪律。
    /// </para>
    /// </summary>
    public static long Tick { get; set; }

    /// <summary>本次世界加载以来的真实秒数，<b>不随世界暂停而停</b>。纯视觉用。</summary>
    public static double RealTime { get; set; }

    /// <summary>当前这一帧的已 clamp 真实步长（秒）。物理积分器该用它。</summary>
    public static float RealDelta { get; set; }

    /// <summary>自本进程启动以来推进过的帧数（世界加载不清零）。</summary>
    public static long Frame { get; set; }

    /// <summary>把世界相关的时钟归零。世界加载时由 <see cref="MonoFrameSystem"/> 调用；<see cref="Frame"/> 不清。</summary>
    public static void ResetWorldClock()
    {
        Tick = 0;
        RealTime = 0;
        RealDelta = 0;
    }
}

/// <summary>
/// 帧末延迟动作。
/// <para>
/// 存在的理由是"时机"：很多操作不能在触发它们的那一刻执行。典型是<b>"不能在遍历集合时改集合"</b>——
/// 一个被击杀的 NPC 想在 <c>OnKill</c> 里让另一个 NPC 消失，或者一个服务想在派发事件的过程中注销自己。
/// 直接做会踩空或者抛 <c>InvalidOperationException</c>。
/// </para>
/// <para>
/// 于是把它们推迟到"下一帧的帧起点"统一执行，顺序就是入队顺序。
/// </para>
/// <para>
/// <b>它复用 <see cref="MonoScheduler"/> 的堆</b>，所以"下一帧"和"30 tick 之后"是同一套机制、同一个容量上限，
/// 不存在两处各自膨胀的延迟队列。
/// </para>
/// </summary>
public static class MonoFrame
{
    /// <summary>
    /// 把动作推迟到<b>下一帧的帧起点</b>执行（那里所有派发都已经结束）。
    /// <para>
    /// 它<b>就是 <see cref="MonoScheduler.Delay(int, Action, string)"/>(1, action)</b>，因为帧起点的 tick 还是上一帧的值：
    /// 排在第 <c>t</c> tick 的 <c>Delay(n)</c> 到第 <c>t+n+2</c> tick 执行（见 <see cref="MonoScheduler.Delay(int, Action, string)"/> 的说明），
    /// 而帧起点正是"tick 即将 +1"的那一刻：下一个帧起点的 tick 是 <c>t+2</c>，正好与 <c>Delay(1)</c> 的到期时刻重合——两者是同一个约定（见 <see cref="MonoScheduler.Pump"/>）。
    /// 不要为了"看起来更直接"把它改写成 <c>At(Tick + 1)</c>：那会与整层的帧边界定义错开一格。
    /// </para>
    /// </summary>
    /// <param name="action">要执行的动作。</param>
    public static void Defer(Action action) => MonoScheduler.Delay(1, action);
}

/// <summary>
/// 时钟的推进者，也是"库自己在帧钩子上做的基础设施动作"的唯一集散点。
/// <para>
/// 它必须在同一个 <see cref="ModSystem"/> 里同时做三件事：推进 <see cref="MonoTime"/>、
/// 世界加载时归零、以及在正确的时机泵 <see cref="MonoScheduler"/>。分开写就会出现"时钟走了但调度器没走"这种半死状态。
/// </para>
/// </summary>
public sealed class MonoFrameSystem : ModSystem
{
    /// <inheritdoc/>
    public override void OnModUnload()
    {
        MonoTween.Clear();
        MonoScheduler.Reset();
    }

    /// <inheritdoc/>
    public override void OnWorldLoad() => MonoTime.ResetWorldClock();

    /// <inheritdoc/>
    public override void OnWorldUnload() => MonoTween.Clear();

    /// <summary>
    /// 帧起点（在世界更新闸门之外，所以定格时也会跑）。<b>库在帧钩子上做的基础设施动作都在这里</b>，
    /// 顺序就是下面代码的顺序：推进真实时钟 → 跑上一帧排进来的帧末动作 → 派发
    /// <see cref="MonoFramePhase.PreUpdate"/> → 驱动游戏内自检。
    /// <para>
    /// 写在一个方法里是刻意的：tML 按类型的 <c>FullName</c> 给 <see cref="ModSystem"/> 排序，
    /// 这几件事拆到不同类里之后先后就只能靠类名去猜，而它们之间是有依赖的——延迟动作要跑在 PreUpdate
    /// 派发之前，消费者在 PreUpdate 里看到的才是"已经收拾干净"的状态。
    /// </para>
    /// <para>
    /// 钩子用的是 tML 自己的 <c>ModSystem.PreUpdateEntities</c> 重写方法，不是 <c>+=</c> 事件——
    /// 摘钩子因此是自动的（类型卸载即止），不存在"忘了 <c>-=</c>"这条泄漏路径。
    /// </para>
    /// </summary>
    public override void PreUpdateEntities()
    {
        MonoTime.Frame++;
        // Main.gameTimeCache 是**本帧已经过去的真实时间**（一个 GameTime），所以取 ElapsedGameTime——它是 dt，不是累计时钟。
        MonoTime.RealDelta = Math.Min((float)Main.gameTimeCache.ElapsedGameTime.TotalSeconds, MonoTime.MaxDelta);
        MonoTime.RealTime += MonoTime.RealDelta;

        MonoScheduler.Pump(MonoTime.Tick);
        MonoEventBus.Dispatch(MonoFramePhase.PreUpdate);
        MonoSelfTest.Tick();
        MonoAllocProbe.Tick();
    }

    /// <summary>
    /// 世界更新之后（在闸门之内，定格时不跑）。<b>这就是 <see cref="MonoTime.Tick"/> 只在世界里前进的原因</b>，
    /// 也是补间跟着世界一起停的原因。
    /// </summary>
    public override void PostUpdateEverything()
    {
        MonoTime.Tick++;
        MonoTween.Update(MonoTime.RealDelta);
    }
}
