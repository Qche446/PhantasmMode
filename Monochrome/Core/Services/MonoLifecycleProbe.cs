namespace Monochrome.Core.Services;

/// <summary>
/// 一个<b>只做记录</b>的子系统：它把 <see cref="IMonoService"/> 的五个阶段各打印一条日志、各存一条到自己的
/// 环形缓冲里。
/// <para>
/// <b>它存在的理由是一个很容易被忽略的事实：</b>在此之前没有任何东西调用 <c>MonoServiceHost.Register&lt;T&gt;()</c>，
/// 所以 <c>MonoServiceHost</c> 的注册表恒为 0——五个阶段<b>一次都没有真的跑过</b>。
/// "机制写好了、也在验收台里过了"和"它在游戏里真的被驱动过"是两件事，
/// 而后者只能靠"真的注册一个服务"来证明。
/// </para>
/// <para>
/// 阶段名<b>不靠日志证</b>：日志环形缓冲会在世界卸载时被清掉，而阶段记录是静态的，活得过整局。
/// 游戏内自检（<c>/mono core selftest</c>）读的就是这里。
/// </para>
/// <para>
/// 它是<b>正式加载路径</b>上的消费者，不是测试夹具：这一段代码跑的就是任何真实子系统会跑的那条路。
/// Order 取 900——按惯例号段，这是"诊断与收尾"，所以它排在其它服务之后。
/// </para>
/// </summary>
public sealed class MonoLifecycleProbe : IMonoService
{
    /// <summary>记录阶段的环形缓冲容量。五个阶段 + 两个退场阶段，8 足够。</summary>
    private const int RecordCapacity = 8;

    private static readonly MonoRingBuffer<string> records = new(RecordCapacity);

    /// <summary>
    /// <b>自加载以来所有阶段记录</b>（累计，不清空）。
    /// <para>
    /// 与 <see cref="records"/> 分开的原因很实际：游戏内自检会在开始前调 <see cref="ResetRecords"/> 清掉
    /// 环形缓冲，于是"五阶段是否按顺序跑过"这条断言就永远看不到加载期那五条记录，必然失败
    /// （用户报的 <c>探针一条记录都没有</c> 就是这个）。这份只增不减的快照让自检能看到完整的装配历史。
    /// </para>
    /// </summary>
    private static readonly List<string> history = [];

    /// <summary>自加载以来的全部阶段记录（最旧的在前），不受 <see cref="ResetRecords"/> 影响。</summary>
    public static IReadOnlyList<string> History => history;

    /// <summary>已经记录的阶段数（累计，含被环形缓冲覆盖的）。</summary>
    public static long RecordCount => records.Written;

    /// <inheritdoc/>
    public int Order => 900;

    /// <inheritdoc/>
    public string Name => nameof(MonoLifecycleProbe);

    /// <summary>按发生顺序取全部记录（最旧的在前）。</summary>
    public static IReadOnlyList<string> Records
    {
        get
        {
            List<string> list = new((int)Math.Min(records.Count, RecordCapacity));
            // 用手写循环而不是 LINQ：这个类本身处在"零分配"的演示位置上，读起来也该是零分配的。
            for (int i = 0; i < records.Count; i++)
                list.Add(records[i]);
            return list;
        }
    }

    /// <summary>清空记录。自检开始前调一次，避免把上一局的记录算进来。</summary>
    public static void ResetRecords() => records.Clear();

    /// <inheritdoc/>
    public void Load() => Record("Load");

    /// <inheritdoc/>
    public void RegisterDefs() => Record("RegisterDefs");

    /// <inheritdoc/>
    public void PostSetup() => Record("PostSetup");

    /// <inheritdoc/>
    public void InstallHooks() => Record("InstallHooks");

    /// <inheritdoc/>
    public void Ready() => Record("Ready");

    /// <inheritdoc/>
    public void Unload() => Record("Unload");

    /// <inheritdoc/>
    public void DisposeBuffers() => Record("DisposeBuffers");

    /// <summary>记一条：进环形缓冲，并且写一条 Info 日志（加载期看一眼控制台就知道装配到哪一步了）。</summary>
    /// <param name="stage">阶段名。</param>
    private static void Record(string stage)
    {
        records.Add(stage);
        history.Add(stage);
        MonoLog.Info(MonoLogLevel.Info, $"[生命周期探针] {stage}（第 {records.Written} 个阶段）");
    }
}
