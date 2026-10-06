using System.Text;

namespace Monochrome.Core;

/// <summary>
/// 协程"等什么"的描述。用小结构体而不是 <c>object</c>/接口，是为了让常见等待没有分配。
/// <para>
/// 用法（<see cref="MonoCoroutine"/> 支持 <c>yield return</c>）：
/// </para>
/// <code>
/// private IEnumerator&lt;MonoWait&gt; MultiStageAttack()
/// {
///     Telegraph();
///     yield return MonoWait.For(30);       // 等 30 tick
///     Fire();
///     yield return MonoWait.Seconds(0.25); // 等 0.25 秒
///     yield break;                          // 结束（或 yield return MonoWait.Forever 停住）
/// }
///
/// MonoCoroutine routine = MonoScheduler.Start(MultiStageAttack());   // 注意括号
/// routine.Stop();                                                     // 需要中断时
/// </code>
/// </summary>
public readonly struct MonoWait
{
    /// <summary>等多少 tick。0 表示下一帧就继续。</summary>
    public long Ticks { get; }

    /// <summary>是否永不继续。用它作为最后一句 yield，等价于"到此为止"。</summary>
    public bool IsForever { get; }

    private MonoWait(long ticks, bool forever)
    {
        Ticks = ticks;
        IsForever = forever;
    }

    /// <summary>永远停下。语义上等价于结束这条协程，但保留"还能被再推一次"的可能。</summary>
    public static MonoWait Forever => new(0, true);

    /// <summary>下一帧继续。</summary>
    public static MonoWait NextTick => new(0, false);

    /// <summary>等若干 tick。</summary>
    /// <param name="ticks">tick 数。</param>
    public static MonoWait For(long ticks) => new(Math.Max(0, ticks), false);

    /// <summary>等若干秒（按 60 tick/秒换算，向上取整——宁可多等一 tick，也不要提前）。</summary>
    /// <param name="seconds">秒数。</param>
    public static MonoWait Seconds(double seconds) => For((long)Math.Ceiling(Math.Max(0.0, seconds) * 60.0));

    /// <inheritdoc/>
    public override string ToString() => IsForever ? "MonoWait(永远)" : $"MonoWait({Ticks} tick)";
}

/// <summary>
/// 一个分帧长流程（多阶段攻击、过场演出、分步建造）。
/// <para>
/// 思路来自 Monocle 的 <c>Coroutine</c>：<b>用 C# 迭代器写流程，把"等待"表达成 <c>yield return</c></b>。
/// 好处是流程的书写顺序就是执行顺序，不必把多阶段攻击拆成一坨 <c>switch (state)</c>。
/// </para>
/// <para>
/// 它是<b>帧驱动</b>的：每一步由 <see cref="MonoScheduler"/> 在到期时调 <see cref="Step"/>，
/// 所以等待精度是 tick 级，与世界时钟一致——<b>定格期间不会前进</b>。
/// </para>
/// </summary>
public sealed class MonoCoroutine
{
    private enum RoutineState : byte
    {
        /// <summary>已经结束（正常跑完）。与 <see cref="Stopped"/> 一样是<b>终态</b>。</summary>
        Done = 0,

        /// <summary>等待中（<c>Forever</c> 也停在这个状态：它还能被再推一次）。</summary>
        Waiting = 1,

        /// <summary>已排好下一步。</summary>
        Scheduled = 2,

        /// <summary>已停止，不会再有下一步。也是<b>终态</b>。</summary>
        Stopped = 3
    }

    /// <summary>
    /// 这条协程是否还活着（还能继续推进）。
    /// <para>
    /// <b>判断必须把两个终态都列出来，不能写成"大于某个值"。</b>这里踩过一次真实的坑：
    /// 枚举里 <see cref="RoutineState.Stopped"/> 的数值比 <see cref="RoutineState.Done"/> <b>大</b>，
    /// 于是 <c>state &gt; Done</c> 这句"方便的一次比较"把<b>已停止</b>也算成了活着——
    /// 症状是 <c>Stop()</c> 之后 <c>IsAlive</c> 仍然为 true，而状态字符串明明写着 <c>Stopped</c>。
    /// 验收台的 <c>协程：Stop 幂等且不再推进</c> 与 <c>Stop(cleanup)</c> 两条用例一起钉住这条语义。
    /// </para>
    /// </summary>
    public bool IsAlive => state is RoutineState.Waiting or RoutineState.Scheduled;

    private readonly IEnumerator<MonoWait> routine;
    private RoutineState state;
    private long scheduledId;

    internal MonoCoroutine(IEnumerator<MonoWait> routine, string name)
    {
        this.routine = routine;
        this.Name = name;
        state = RoutineState.Waiting;
    }

    /// <summary>诊断名，默认取迭代器方法的声明类型与方法名。</summary>
    public string Name { get; }

    /// <summary>已经推进过的步数。诊断用。</summary>
    public int Steps { get; private set; }

    /// <summary>推进到下一个 <c>yield return</c>，并按它要求的时间排下一次推进。</summary>
    internal void Step()
    {
        // 两个终态都要挡住：漏掉 Stopped 会让 Stop() 之后又从旧排程里"复活"——排程虽然被 Cancel 了，
        // 但一个已经在派发队列里被取出来的项仍然会调到这里。
        if (state is RoutineState.Done or RoutineState.Stopped)
            return;

        Steps++;

        bool moved;
        try
        {
            moved = routine.MoveNext();
        }
        catch (Exception exception)
        {
            state = RoutineState.Done;
            MonoLog.Error(MonoLogLevel.Error, $"协程「{Name}」抛异常并终止：{exception}");
            return;
        }

        if (!moved)
        {
            state = RoutineState.Done;
            routine.Dispose();
            return;
        }

        MonoWait wait = routine.Current;
        if (wait.IsForever)
        {
            state = RoutineState.Waiting;
            return;
        }

        state = RoutineState.Scheduled;
        scheduledId = MonoScheduler.Delay((int)Math.Min(int.MaxValue, wait.Ticks), static target => ((MonoCoroutine)target!).Step(), this, Name);
    }

    /// <summary>停止这条协程：撤销已排的下一步、释放迭代器。<b>幂等</b>，重复调用是空操作。</summary>
    public void Stop()
    {
        if (state is RoutineState.Done or RoutineState.Stopped)
            return;

        if (state == RoutineState.Scheduled)
            MonoScheduler.Cancel(scheduledId);

        state = RoutineState.Stopped;
        routine.Dispose();
    }

    /// <summary>
    /// 停止这条协程，并在它确实还活着时执行一次清理动作（恢复状态、取消无敌帧之类的收尾）。
    /// <para>
    /// 它存在的理由是：中途被打断的演出必须能回到"正常状态"，否则"被击杀时正好在放技能"
    /// 会留下一个永远不消失的提示，或者一个不会结束的无敌。
    /// </para>
    /// </summary>
    /// <param name="cleanup">清理动作。</param>
    public void Stop(Action cleanup)
    {
        bool wasAlive = IsAlive;
        Stop();
        if (wasAlive)
            cleanup?.Invoke();
    }

    /// <inheritdoc/>
    public override string ToString() => $"MonoCoroutine({Name}, {state}, {Steps} 步)";
}

/// <summary>时间轴上一条通道的非泛型视图。存在的唯一理由是让时间轴能在不知道具体类型的情况下取时长。</summary>
public abstract class MonoChannelBase
{
    /// <summary>通道长度（最后一个关键帧的时间），单位秒。</summary>
    public abstract double Duration { get; }

    /// <summary>关键帧数量。</summary>
    public abstract int KeyCount { get; }

    /// <summary>关键帧之间是否用了缓动。</summary>
    public abstract bool HasEase { get; }

    /// <summary>诊断行。</summary>
    public abstract string Describe();
}

/// <summary>
/// 时间轴的关键帧通道：一串 <c>(时间, 值)</c>，按时间排序，取值时插值。
/// <para>
/// 通道本身<b>不含播放状态</b>——播放状态在 <see cref="MonoTimeline"/> 上。一份通道因此可以被多条时间轴共用，
/// 这正是"通道"与"时间轴"分开的意义。
/// </para>
/// </summary>
/// <typeparam name="T">值的类型。</typeparam>
public sealed class MonoChannel<T> : MonoChannelBase
{
    private readonly double[] times;
    private readonly T[] values;
    private readonly Func<T, T, float, T> interpolate;

    /// <summary>建一条通道。关键帧会按时间排序后复制一份，所以调用方之后改自己的数组不会影响它。</summary>
    /// <param name="times">关键帧时间（秒）。</param>
    /// <param name="values">对应的值，长度必须与 <paramref name="times"/> 一致。</param>
    /// <param name="interpolate"><c>(从, 到, t) =&gt; 插值结果</c>；<c>t</c> 已经过缓动。</param>
    /// <param name="ease">关键帧之间的缓动；<c>null</c> 表示线性。</param>
    public MonoChannel(double[] times, T[] values, Func<T, T, float, T> interpolate, Func<float, float>? ease = null)
    {
        if (times is null || values is null || times.Length != values.Length || times.Length == 0)
            throw new ArgumentException("时间与值的长度必须一致，且至少有一个关键帧。");

        this.interpolate = interpolate ?? throw new ArgumentNullException(nameof(interpolate));
        Ease = ease;

        int[] order = new int[times.Length];
        for (int i = 0; i < order.Length; i++)
            order[i] = i;
        Array.Sort(order, (a, b) => times[a].CompareTo(times[b]));

        this.times = new double[times.Length];
        this.values = new T[values.Length];
        for (int i = 0; i < order.Length; i++)
        {
            this.times[i] = times[order[i]];
            this.values[i] = values[order[i]];
        }

        Duration = this.times[^1];
    }

    /// <inheritdoc/>
    public override double Duration { get; }

    /// <inheritdoc/>
    public override int KeyCount => times.Length;

    /// <summary>关键帧之间的缓动函数；<c>null</c> 表示线性。</summary>
    public Func<float, float>? Ease { get; }

    /// <inheritdoc/>
    public override bool HasEase => Ease is not null;

    /// <summary>取某个时刻的值。时间超出两端时钳制到端点（<b>不外插</b>——外插会把数值推到设计范围之外）。</summary>
    /// <param name="time">时间（秒）。</param>
    public T Evaluate(double time)
    {
        if (time <= times[0])
            return values[0];
        if (time >= times[^1])
            return values[^1];

        // 关键帧通常是几个到几十个，线性扫描比二分更短也更好读。
        for (int i = 0; i < times.Length - 1; i++)
        {
            if (time > times[i + 1])
                continue;

            double span = times[i + 1] - times[i];
            float t = span <= 0 ? 1f : (float)((time - times[i]) / span);
            if (Ease is not null)
                t = Ease(t);
            return interpolate(values[i], values[i + 1], t);
        }
        return values[^1];
    }

    /// <inheritdoc/>
    public override string Describe()
        => $"{typeof(T).Name} 通道：{KeyCount} 个关键帧，时长 {Duration:F3}s，{(Ease is null ? "线性" : "自定义缓动")}";
}

/// <summary>
/// 时间轴：一组通道 + 一个播放头。用于 Boss 战阶段编排、过场演出这类"按时间推进的一组数值"。
/// <para>
/// 三点语义是刻意固定的：
/// </para>
/// <list type="bullet">
/// <item><b>时间基准是真实秒数</b>（<see cref="MonoTime.RealTime"/>），因为演出属于表现层；
/// 需要跟随世界暂停的编排请用 <see cref="MonoScheduler"/> 的 tick 排程。</item>
/// <item><b>倒放是状态而不是负速度</b>（<see cref="Reversed"/>）：两者在到达端点时的行为不同，
/// 用速度符号表达会让"倒放到头"与"速度为负"纠缠不清。</item>
/// <item><b>通道不决定播放行为</b>：走到末尾是钳制还是回绕由 <see cref="Loop"/> 决定，只影响播放头。</item>
/// </list>
/// </summary>
public sealed class MonoTimeline
{
    private readonly Dictionary<string, MonoChannelBase> channels = [];
    private readonly List<string> order = [];

    /// <summary>播放头当前位置（秒）。</summary>
    public double Position { get; set; }

    /// <summary>播放速度倍率。<c>1</c> 正常、<c>0</c> 停住、<c>2</c> 加倍。</summary>
    public double Speed { get; set; } = 1.0;

    /// <summary>是否循环。false 时走到末尾就停住（<see cref="IsPlaying"/> 变 false）。</summary>
    public bool Loop { get; set; }

    /// <summary>是否倒放。倒放到 0 时停住（<see cref="Loop"/> 为真则从末尾续上）。</summary>
    public bool Reversed { get; set; }

    /// <summary>是否正在播放。手动 <see cref="Pause"/> 或走到端点都会让它变 false。</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>时间轴总长度 = 最长的那条通道。没有任何通道时是 0。</summary>
    public double Duration
    {
        get
        {
            double max = 0;
            for (int i = 0; i < order.Count; i++)
                max = Math.Max(max, channels[order[i]].Duration);
            return max;
        }
    }

    /// <summary>播放进度 <c>[0,1]</c>。时长为 0 时是 1（"已经结束"比"还没开始"更贴近事实）。</summary>
    public double Progress
    {
        get
        {
            double duration = Duration;
            return duration <= 0 ? 1.0 : Math.Clamp(Position / duration, 0.0, 1.0);
        }
    }

    /// <summary>通道数量。</summary>
    public int ChannelCount => order.Count;

    /// <summary>加一条通道。同名通道会被替换（改一条曲线不必重建整条时间轴）。</summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="name">通道名。</param>
    /// <param name="channel">通道。</param>
    public MonoTimeline Add<T>(string name, MonoChannel<T> channel)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(channel);

        if (!channels.ContainsKey(name))
            order.Add(name);
        channels[name] = channel;
        return this;
    }

    /// <summary>加一条浮点通道（最常见的形态，单独给一个重载省去写插值委托）。</summary>
    /// <param name="name">通道名。</param>
    /// <param name="times">关键帧时间（秒）。</param>
    /// <param name="values">关键帧值。</param>
    /// <param name="ease">缓动；<c>null</c> 为线性。</param>
    public MonoTimeline AddFloat(string name, double[] times, float[] values, Func<float, float>? ease = null)
        => Add(name, new MonoChannel<float>(times, values, static (a, b, t) => a + ((b - a) * t), ease));

    /// <summary>加一条向量通道。</summary>
    /// <param name="name">通道名。</param>
    /// <param name="times">关键帧时间（秒）。</param>
    /// <param name="values">关键帧值。</param>
    /// <param name="ease">缓动；<c>null</c> 为线性。</param>
    public MonoTimeline AddVector(string name, double[] times, Vector2[] values, Func<float, float>? ease = null)
        => Add(name, new MonoChannel<Vector2>(times, values, static (a, b, t) => Vector2.Lerp(a, b, t), ease));

    /// <summary>加一条颜色通道。</summary>
    /// <param name="name">通道名。</param>
    /// <param name="times">关键帧时间（秒）。</param>
    /// <param name="values">关键帧值。</param>
    /// <param name="ease">缓动；<c>null</c> 为线性。</param>
    public MonoTimeline AddColor(string name, double[] times, Color[] values, Func<float, float>? ease = null)
        => Add(name, new MonoChannel<Color>(times, values, static (a, b, t) => Color.Lerp(a, b, t), ease));

    /// <summary>从当前位置开始播放。</summary>
    public MonoTimeline Play()
    {
        IsPlaying = true;
        return this;
    }

    /// <summary>从指定位置开始播放。</summary>
    /// <param name="position">起始位置（秒）。</param>
    public MonoTimeline Play(double position)
    {
        Position = position;
        return Play();
    }

    /// <summary>暂停（保留播放头位置）。</summary>
    public MonoTimeline Pause()
    {
        IsPlaying = false;
        return this;
    }

    /// <summary>回到 0 并停止。倒放标志也一起清掉——"停在开头"和"倒放中"不该同时成立。</summary>
    public MonoTimeline Stop()
    {
        IsPlaying = false;
        Position = 0;
        Reversed = false;
        return this;
    }

    /// <summary>
    /// 用真实时间推进播放头。<b>每帧调一次</b>（通常挂 <see cref="MonoFramePhase.PreUpdate"/>）。
    /// <para>不播放时它什么都不做，所以调用方不必在每个调用点判 <see cref="IsPlaying"/>。</para>
    /// </summary>
    public void Update() => Update(MonoTime.RealDelta);

    /// <summary>
    /// 用<b>指定的</b>时间步长推进播放头。
    /// <para>
    /// 它存在的理由有两个，都不是"方便"：<b>一是"手动步进"</b>（暂停时逐帧检视、把时间轴当纯函数求值器用）；
    /// <b>二是可复现</b>——<see cref="Update()"/> 读的是帧时钟 <see cref="MonoTime.RealDelta"/>，
    /// 而帧时钟只在游戏里由 <c>MonoFrameSystem</c> 推进。没有这个重载，验收台只能去写一个全局时钟，
    /// 那会让"时间轴走过头"和"时钟没被推"两种失败长得一模一样（实测就是这样：两条用例报的都是"播放头没动"）。
    /// </para>
    /// <para>
    /// 步长<b>不在这里 clamp</b>：clamp 是时钟的责任（<see cref="MonoTime.RealDelta"/> 已经 clamp 到 1/30 秒）。
    /// 手动步进时调用方要自己保证 <paramref name="deltaSeconds"/> 是合理的。
    /// </para>
    /// </summary>
    /// <param name="deltaSeconds">时间步长（秒），已乘上 <see cref="Speed"/> 与倒放方向。</param>
    public void Update(double deltaSeconds)
    {
        if (!IsPlaying)
            return;

        double duration = Duration;
        double delta = deltaSeconds * Speed * (Reversed ? -1.0 : 1.0);
        Position += delta;

        // 判断用 >= / <=：正好落在端点上也该被钳制并停住。
        // 写成 > / < 的话，"把播放头手动设成 Position = Duration 再 Update()"会留在 IsPlaying = true 的假状态里——
        // 实测就是验收台里那条"非循环应该钳制在末尾"。
        if (delta > 0 && Position >= duration)
        {
            if (Loop)
                Position = duration <= 0 ? 0 : Position - duration;
            else
            {
                Position = duration;
                IsPlaying = false;
            }
        }
        else if (delta < 0 && Position <= 0)
        {
            if (Loop)
                Position = duration <= 0 ? 0 : duration + Position;
            else
            {
                Position = 0;
                IsPlaying = false;
            }
        }
    }

    /// <summary>取某个通道在当前播放头位置的值。<b>取不到就抛</b>——通道名拼错必须立刻可见，不能静默返回默认值。</summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="name">通道名。</param>
    public T Get<T>(string name)
    {
        if (!channels.TryGetValue(name, out MonoChannelBase? channel))
            throw new KeyNotFoundException($"时间轴上没有叫「{name}」的通道。已注册的有：{string.Join(", ", order)}");

        if (channel is not MonoChannel<T> typed)
            throw new InvalidCastException($"通道「{name}」的值类型不是 {typeof(T).Name}。");

        return typed.Evaluate(Position);
    }

    /// <summary>按给定位置取某个通道的值（用于"预览某一帧"），不改变播放头。</summary>
    /// <typeparam name="T">值类型。</typeparam>
    /// <param name="name">通道名。</param>
    /// <param name="position">位置（秒）。</param>
    public T GetAt<T>(string name, double position)
    {
        double saved = Position;
        Position = position;
        try
        {
            return Get<T>(name);
        }
        finally
        {
            Position = saved;
        }
    }

    /// <summary>这条时间轴上有没有叫这个名字的通道。</summary>
    /// <param name="name">通道名。</param>
    public bool HasChannel(string name) => channels.ContainsKey(name);

    /// <summary>清空全部通道并回到停止状态。</summary>
    public void Clear()
    {
        channels.Clear();
        order.Clear();
        Position = 0;
        IsPlaying = false;
    }

    /// <summary>诊断输出：时长、播放头、各通道。</summary>
    public string Describe()
    {
        StringBuilder output = new();
        output.Append("时间轴：").Append(order.Count).Append(" 条通道，时长 ").Append(Duration.ToString("F3")).Append("s，播放头 ")
              .Append(Position.ToString("F3")).Append("s（").Append((Progress * 100).ToString("F1")).Append("%）")
              .Append(IsPlaying ? " 播放中" : " 已暂停")
              .Append(Speed != 1.0 ? $" 速度 {Speed:F2}" : string.Empty)
              .Append(Reversed ? " 倒放" : string.Empty)
              .Append(Loop ? " 循环" : string.Empty);

        for (int i = 0; i < order.Count; i++)
            output.Append("\n  · ").Append(order[i]).Append("  ").Append(channels[order[i]].Describe());
        return output.ToString();
    }
}
