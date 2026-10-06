namespace Monochrome.Core;

// 这里原本有一个 13 条曲线的 MonoEase 静态表，与 Common/MonoUtil 的
// Easings/MonoEasing.cs（31 条 + 类型/方向分派）完全重复。两份定义只要有一处计算写法不同，
// 同一个名字就会给出两个不同的数——"缓动"这种东西又恰恰是最难一眼看出不对的（曲线形状相近、只差几个百分点），
// 所以重复的那份整体删掉了，曲线本体只留在 MonoUtil 一处。
//
// 本文件现在只剩 MonoTween：它是"把缓动按 tick 推到目标值"的驱动器，与"缓动曲线本身"是两件事。

/// <summary>
/// 数值补间：把某个 float 从 A 平滑推到 B，目标可以中途改。
/// <para>
/// 它的价值在于<b>把"每帧往目标靠一点"的公式收在一处</b>。手写那个公式（<c>x += (target - x) * 0.1f</c>）
/// 有三个已知毛病：与帧率耦合、永远到不了目标、改目标时会跳。
/// </para>
/// <para>
/// <b>它是池化的</b>：容量在类型初始化时一次性造满 512 个实例，<see cref="Spawn(float, float, int, Action{float}, Func{float, float}?, bool)"/> 借、<see cref="Stop"/> 还，
/// 于是"每个 UI 元素一个补间"不会变成每秒几百次分配。
/// </para>
/// <para>
/// <b>为什么不用 <see cref="MonoPool{T}"/></b>：那个池要求 <c>T : struct</c>，因为它的价值全在
/// "把槽位本身按 <c>ref</c> 交出去"——这只有值类型才成立。补间是<b>引用类型</b>，而引用类型的池化要点恰好相反：
/// 实例必须<b>原地复活的同一个对象</b>（<see cref="Retarget"/> 这类方法会长期持有它），不能像结构体槽位那样
/// 每次借出都整体归零。把那个 <c>struct</c> 约束去掉会让两边的语义都变差，所以这里是各自一份实现，
/// 共用的是同一份"定长、不扩容、满了降级并计数"的纪律。
/// </para>
/// <para>
/// 补间推进是<b>帧驱动</b>的，用 <see cref="MonoTime.Tick"/>，所以世界停住时补间也停——
/// 需要"定格时仍然动"的纯视觉缓动请用真实时间自己插值。
/// </para>
/// <para>
/// <b>缓动曲线不由本层定义。</b>完整表（11 类 × In/Out/InOut = 31 条，外加弹簧阻尼、Remap、Wobble 等）
/// 在 <c>Common/MonoUtil/Mathematics/Easings/MonoEasing.cs</c>。这里曾经另有一份 13 条的副表，
/// 与它重复——两份定义只要有一处写法不同，同一个名字就会给出两个不同的数，而"缓动错了几个百分点"
/// 是最难一眼看出的那类偏差。现在本层<b>只消费</b>：
/// <see cref="Spawn(float, float, int, Action{float}, MonoEaseKind, MonoEaseMode, bool)"/> 收类型与方向，
/// 内部用 <c>MonoUtil.Ease(kind, mode)</c> 取的<b>常驻委托</b>（静态表，不分配）。
/// </para>
/// </summary>
public sealed class MonoTween
{
    /// <summary>
    /// 默认缓动：<b>线性</b>，与 <c>MonoChannel</c> 的 <c>null</c> 语义一致（两处都是"不做缓动"）。
    /// <para>
    /// 它是<b>共享的静态引用</b>，而不是每次现写的 lambda：<c>t =&gt; t</c> 每次都会新建一个闭包对象，
    /// 而这个字段传多少次都是同一个引用（验收台有一条用例专门钉住这件事）。
    /// </para>
    /// </summary>
    private static readonly Func<float, float> DefaultEase = MonoUtil.LinearEase;

    /// <summary>池容量。补间通常同时只有几十个，512 足够且不会浪费。</summary>
    private const int Capacity = 512;

    /// <summary>全部实例，在静态构造里一次性造满。<b>永不扩容</b>——会扩容的池恰好在最需要它的时候制造一次 GC。</summary>
    private static readonly MonoTween[] instances = CreateInstances();

    /// <summary>空闲链表：<c>freeStack[0..freeCount)</c> 是可用实例的下标。</summary>
    private static readonly int[] freeStack = CreateFreeStack();

    private static int freeCount = Capacity;

    /// <summary>正在跑的补间。遍历顺序无关紧要，所以"活动集合"用 <c>List</c> 就够。</summary>
    private static readonly List<MonoTween> active = [];

    /// <summary>静态构造用：把实例先造好并编好号，之后永不 <c>new</c>。</summary>
    private static MonoTween[] CreateInstances()
    {
        MonoTween[] array = new MonoTween[Capacity];
        for (int i = 0; i < Capacity; i++)
        {
            array[i] = new MonoTween { poolIndex = i, pooled = true };   // 出生即"躺在池里"
        }
        return array;
    }

    /// <summary>空闲链表按逆序压栈，于是第一次借出拿到的是 0 号实例（顺序可预测，方便复现 bug）。</summary>
    private static int[] CreateFreeStack()
    {
        int[] stack = new int[Capacity];
        for (int i = 0; i < Capacity; i++)
            stack[i] = Capacity - 1 - i;
        return stack;
    }

    private Action<float>? setter;

    /// <summary>起点值。</summary>
    public float From { get; private set; }

    /// <summary>目标值。</summary>
    public float To { get; private set; }

    /// <summary>当前值。</summary>
    public float Value { get; private set; }

    /// <summary>总时长（tick）。</summary>
    public int Duration { get; private set; }

    /// <summary>已经过去的 tick 数。</summary>
    public int Elapsed { get; private set; }

    /// <summary>缓动函数。</summary>
    public Func<float, float>? Ease { get; private set; }

    /// <summary>进度 <c>[0,1]</c>。</summary>
    public float Progress => Duration <= 0 ? 1f : Math.Clamp(Elapsed / (float)Duration, 0f, 1f);

    /// <summary>是否还在跑。</summary>
    public bool IsAlive { get; private set; }

    /// <summary>结束后是否循环（往复）。</summary>
    public bool Loop { get; private set; }

    /// <summary>当前活动补间数量。诊断用。</summary>
    public static int ActiveCount => active.Count;

    /// <summary>池借出失败（容量耗尽）的累计次数。</summary>
    public static int ExhaustedCount { get; private set; }

    /// <summary>池的容量（一次性造好的实例数）。</summary>
    public static int PoolCapacity => Capacity;

    /// <summary>池里还剩多少个可借的实例。诊断用。</summary>
    public static int PoolFreeCount => freeCount;

    /// <summary>
    /// 起一个补间：在 <paramref name="durationTicks"/> 个 tick 内把值从 <paramref name="from"/> 推到 <paramref name="to"/>，
    /// 每 tick 通过 <paramref name="setter"/> 写回。
    /// <para>
    /// <paramref name="setter"/> 每 tick 都会被调用一次，<b>不要在里面做重活</b>（那是热路径）。
    /// </para>
    /// </summary>
    /// <param name="from">起点值。</param>
    /// <param name="to">终点值。</param>
    /// <param name="durationTicks">时长（tick）。≤ 0 时立刻到位。</param>
    /// <param name="setter">写回目标值的动作。</param>
    /// <param name="ease">缓动；<c>null</c> 用线性（<see cref="MonoUtil.LinearEase"/>）。</param>
    /// <param name="loop">结束后是否往复。</param>
    /// <returns>
    /// 补间实例；池满时返回 null（调用方应直接跳到终值，而不是什么都不做）。
    /// <b>时长 ≤ 0 时返回的实例已经结束并归还</b>——值已写到位，但 <see cref="IsAlive"/> 是 false，不要再持有它。
    /// </returns>
    public static MonoTween? Spawn(float from, float to, int durationTicks, Action<float> setter, Func<float, float>? ease = null, bool loop = false)
    {
        ArgumentNullException.ThrowIfNull(setter);

        MonoTween? tween = Rent();
        if (tween is null)
        {
            ExhaustedCount++;
            MonoLog.Warn(MonoLogLevel.Warn, $"补间池已满（{Capacity}），本次补间被跳过。");
            setter(to);     // 降级：直接到位。宁可没有动画，也不要停在旧值上。
            return null;
        }

        tween.setter = setter;
        tween.From = from;
        tween.To = to;
        tween.Value = from;
        tween.Duration = Math.Max(0, durationTicks);
        tween.Elapsed = 0;
        tween.Ease = ease ?? DefaultEase;
        tween.Loop = loop;
        tween.IsAlive = true;

        active.Add(tween);
        if (tween.Duration == 0)
            tween.Finish();

        return tween;
    }

    /// <summary>
    /// 起一个补间，缓动用<b>唯一的缓动表</b>里的某一条（类型 + 方向），而不是自己写一个 lambda。
    /// <para>
    /// 它是首选重载：<c>MonoUtil.Ease(kind, mode)</c> 取自静态表，
    /// 所以<b>不会像 <c>ease: t =&gt; MonoUtil.Evaluate(kind, mode, t)</c> 那样每次 Spawn 都包一个闭包</b>。
    /// </para>
    /// </summary>
    /// <param name="from">起点值。</param>
    /// <param name="to">终点值。</param>
    /// <param name="durationTicks">时长（tick）。≤ 0 时立刻到位。</param>
    /// <param name="setter">写回目标值的动作。</param>
    /// <param name="kind">缓动类型。</param>
    /// <param name="mode">缓动方向。</param>
    /// <param name="loop">结束后是否往复。</param>
    /// <returns>同 <see cref="Spawn(float, float, int, Action{float}, Func{float, float}?, bool)"/>。</returns>
    public static MonoTween? Spawn(float from, float to, int durationTicks, Action<float> setter, MonoEaseKind kind, MonoEaseMode mode, bool loop = false)
        => Spawn(from, to, durationTicks, setter, MonoUtil.Ease(kind, mode), loop);

    /// <summary>
    /// 借一个实例。<b>借出来时它一定是"没有正在跑"的干净状态</b>：置位了 <see cref="IsAlive"/>、
    /// 清掉了上一个使用者的 <c>setter</c>、清掉了 <see cref="Ease"/> 与 <see cref="Loop"/>。
    /// </summary>
    private static MonoTween? Rent()
    {
        if (freeCount == 0)
            return null;

        MonoTween tween = instances[freeStack[--freeCount]];
        tween.pooled = false;
        tween.setter = null;
        tween.Ease = null;
        tween.Loop = false;
        tween.IsAlive = true;
        return tween;
    }

    /// <summary>
    /// 把实例交回池。
    /// <para>
    /// 它<b>只验证"这个实例确实是自己的、而且还没被归还"</b>。<see cref="IsAlive"/> 不能当这个凭据——
    /// 它在借出的那一刻就被置位了，所以重复归还（或归还一个从未借出的实例）会让同一个下标被压进空闲链表两次，
    /// 那是"池逐渐吐出不存在的实例"的经典成因。
    /// </para>
    /// </summary>
    /// <returns>确实归还了一个实例才返回 true；重复归还/归还未借出的实例是<b>安全空操作</b>。</returns>
    private static bool Recycle(MonoTween tween)
    {
        if (tween.pooled)
            return false;

        int index = tween.poolIndex;
        if ((uint)index >= (uint)instances.Length || !ReferenceEquals(instances[index], tween))
            return false;

        tween.pooled = true;
        freeStack[freeCount++] = index;
        return true;
    }

    /// <summary>中途改目标（"目标会动的跟随"场景）。<b>不会跳</b>——从当前值继续往新目标走。</summary>
    /// <param name="to">新目标值。</param>
    /// <param name="durationTicks">新时长；≤ 0 保持原时长。</param>
    public void Retarget(float to, int durationTicks = 0)
    {
        if (!IsAlive)
            return;

        From = Value;
        To = to;
        Elapsed = 0;
        if (durationTicks > 0)
            Duration = durationTicks;
    }

    /// <summary>把补间跳到终点并结束（会立刻调一次 setter）。</summary>
    public void Finish()
    {
        if (!IsAlive)
            return;
        Value = To;
        setter?.Invoke(To);
        Stop();
    }

    /// <summary>停止并归还到池。此刻起这个实例不该再被使用，之后再调它的任何成员都是空操作。</summary>
    public void Stop()
    {
        if (!IsAlive)
            return;

        IsAlive = false;
        setter = null;
        active.Remove(this);
        Recycle(this);
    }

    /// <summary>
    /// 推进所有活动补间。<b>由 <see cref="MonoFrameSystem"/> 在帧末调一次</b>，消费者不需要自己调。
    /// <para>
    /// 补间的时长以 tick 计，所以这一步要做"秒换 tick"的换算：先把 dt 乘
    /// <see cref="MonoTime.TicksPerSecond"/> 换成 tick 数，再累加进余量。
    /// <b>它不是 <c>MathF.Round(1/60) = 0</c> 那个陷阱</b>：换算用累计器（把每帧的 tick 数累加，
    /// 满 1 tick 才推进一步），所以 60fps 下每帧恰好推进 1 tick，30fps 下每帧推进 2 tick，
    /// 帧率抖动也不会被四舍五入抹掉。
    /// </para>
    /// <para>它是公开的，<b>只为了让验收台能自己当 <see cref="MonoFrameSystem"/> 驱动一遍</b>；消费者调用它等于自己造帧。</para>
    /// </summary>
    /// <param name="deltaSeconds">自上一帧以来的真实秒数（已 clamp）。</param>
    public static void Update(float deltaSeconds)
    {
        tickAccumulator += deltaSeconds * MonoTime.TicksPerSecond;
        int steps = (int)tickAccumulator;
        if (steps <= 0)
            return;

        // 上限必须先夹住、再拿夹住的值去扣累计器。<b>顺序反了会永久损坏累计器</b>：
        // 一次"切出窗口回来"的帧会喂进几百 tick 的时间，若先扣掉未夹住的步数再改成 4，
        // 累计器就被减成了一个大负数，之后每一帧都要先把那个负数补回来——表现为"补间卡住不动"。
        // 更糟的是那几百个 tick 已经被无声地丢掉了。正确行为是：补 4 步，剩下的留给下一帧。
        if (steps > MAX_STEPS_PER_FRAME)
            steps = MAX_STEPS_PER_FRAME;
        tickAccumulator -= steps;

        Advance(steps);
    }

    /// <summary>
    /// 直接按 <b>tick</b> 推进所有活动补间，不经过"秒换 tick"的换算器。
    /// <para>
    /// <b>它是给验收台与诊断工具用的，不是给游戏逻辑用的。</b>游戏里补间由
    /// <see cref="MonoFrameSystem"/> 在帧末用 <see cref="Update(float)"/> 推进，消费者不需要（也不该）自己推。
    /// 但那个换算器是<b>有状态的</b>（把每帧的 dt 累加起来，满 1 tick 才走一步），于是"推进 5 tick"
    /// 在测试里没法用"调 5 次 Update"表达——那要么一步不走（余量不够），要么受上次遗留的余量影响而多走几步。
    /// 想要可复现的步进，就必须有一个把 tick 数直接说出来的入口。这里就是它。
    /// </para>
    /// <para>它<b>不动</b>换算器的余量，所以可以和 <see cref="Update(float)"/> 安全混用。</para>
    /// </summary>
    /// <param name="ticks">要推进的 tick 数；≤ 0 时什么都不做。</param>
    public static void PumpTicks(int ticks)
    {
        if (ticks <= 0)
            return;
        Advance(ticks);
    }

    /// <summary>按 tick 数推进所有活动补间。<see cref="Update(float)"/> 与 <see cref="PumpTicks"/> 都走它。</summary>
    private static void Advance(int steps)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            for (int step = 0; step < steps && active[i].IsAlive; step++)
                active[i].Step();
        }
    }

    /// <summary>单帧最多补多少 tick。防的是"切出窗口回来一次补 600 帧"。</summary>
    private const int MAX_STEPS_PER_FRAME = 4;

    private static float tickAccumulator;

    /// <summary>停掉全部补间并归还。世界卸载与模组卸载时调，验收台复位时也调。</summary>
    public static void Clear()
    {
        for (int i = active.Count - 1; i >= 0; i--)
            active[i].Stop();
        active.Clear();
        tickAccumulator = 0f;
    }

    /// <summary>诊断输出。</summary>
    public static string Describe()
        => $"补间：{active.Count} 个进行中，池 {Capacity - freeCount}/{Capacity} 占用，容量耗尽 {ExhaustedCount} 次";

    /// <summary>本实例在 <see cref="instances"/> 里的下标。<b>身份不是这个下标</b>，它只是归还时的定位凭据。</summary>
    private int poolIndex;

    /// <summary>是否已经躺在空闲链表里。防双重归还（双压会让同一个下标被借出两次）。</summary>
    private bool pooled;

    private void Step()
    {
        if (!IsAlive)
            return;

        Elapsed++;
        if (Elapsed >= Duration)
        {
            if (!Loop)
            {
                Value = To;
                setter?.Invoke(Value);
                Stop();
                return;
            }

            // 往复：把起点与终点互换，重新计时。用"互换"而不是"倒放进度"，因为循环补间通常
            // 就是"来回呼吸"，互换让每一次循环都是同一个形状。
            (From, To) = (To, From);
            Elapsed = 0;
        }

        float t = Progress;
        if (Ease is not null)
            t = Ease(t);
        Value = From + ((To - From) * t);
        setter?.Invoke(Value);
    }
}
