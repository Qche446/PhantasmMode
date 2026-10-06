using System.Text;

namespace Monochrome.Core;

/// <summary>
/// 延迟调用 / 冷却表 / 协程式调度。<b>替代到处手写的计时器字段</b>。
/// <para>
/// 时间基准是 <see cref="MonoTime.Tick"/>（世界 tick），不是帧数，也不是真实时间。这个选择决定了三件事：
/// <b>①</b> 世界暂停（命中定格 / 时间缩放为 0）时，"30 tick 后"不会提前到期，因为 tick 本身没走；
/// <b>②</b> 多人下两端的到期时刻一致；<b>③</b> 回放与测试可复现。
/// </para>
/// <para>
/// 实现是<b>固定容量的最小堆</b>（按到期 tick 排序，同 tick 按入队序号，保证顺序可复现）。
/// 满了就拒绝并计数，<b>绝不扩容</b>——一个会在压力下扩容的调度器，会在最需要它的时候制造一次 GC。
/// </para>
/// <para>
/// 泵的时机由 <see cref="MonoFrameSystem"/> 在帧起点负责（见那里的注释）。调用方只需要
/// <see cref="Delay(int, Action, string)"/>（"N tick 后做一件事"）与 <see cref="Cooldown"/>（"这段时间内只放行一次"），
/// 不必关心时机。
/// </para>
/// </summary>
public static class MonoScheduler
{
    /// <summary>一个待执行项。<c>Due</c> 是"到哪一 tick 该跑"，<c>Sequence</c> 让同 tick 的执行顺序 = 入队顺序。</summary>
    private struct Pending
    {
        public long Due;
        public long Sequence;

        /// <summary>要执行的动作。取消时置 null（<b>不移动堆数组</b>——那样会打乱下标，而取消是低频动作）。</summary>
        public Action<object?>? Action;

        /// <summary>状态参数。与 <see cref="Action"/> 一起构成"零闭包"的排程。</summary>
        public object? State;

        /// <summary>
        /// 目标是否还在。为空表示不检查；为假时这一项会被<b>安静丢弃</b>——不执行、不记日志。
        /// <para>判定发生在动作之前，所以"目标已经没了"这件正常事不会先抛一次异常再被记成错误。</para>
        /// </summary>
        public Func<object?, bool>? Alive;

        /// <summary>是否已被取消。<b>不移动堆数组</b>——取消是低频动作，为它打乱下标不划算。</summary>
        public bool Cancelled;

        public string Label;
    }

    /// <summary>默认容量。堆里放的是待执行项，长期占用这么多就是内存泄漏级的浪费。</summary>
    public const int DefaultCapacity = 512;

    private static Pending[] heap = new Pending[DefaultCapacity];
    private static int count;
    private static long sequence;

    /// <summary>当前 tick 的快照。泵的时候更新它，<see cref="Now"/> 读它。</summary>
    private static long now;

    /// <summary>累计执行成功的动作数。</summary>
    private static long executed;

    /// <summary>因容量耗尽被拒绝的调度次数。<b>非 0 说明要么容量太小，要么有东西在疯狂排程（很可能是每帧重排而不是排一次）。</b></summary>
    private static long rejected;

    /// <summary>抛异常的动作数。调度器的原则是"一个坏动作不能拖垮其余排程"。</summary>
    private static long failed;

    /// <summary>到期时存活判定为假、被安静丢弃的项数。<b>这是正常现象</b>，不计入 <see cref="Failed"/>。</summary>
    private static long skipped;

    /// <summary>被取消或过期清掉的项数。诊断用。</summary>
    private static long cancelled;

    /// <summary>当前待执行的项数。</summary>
    public static int PendingCount => count;

    /// <summary>调度器眼中的"现在"。等于最近一次泵时的 <see cref="MonoTime.Tick"/>。</summary>
    public static long Now => now;

    /// <summary>累计执行的动作数。</summary>
    public static long Executed => executed;

    /// <summary>被拒绝的调度次数。<b>它不包含"被取消"的项</b>——后者是正常用法，不该和容量不足混为一谈。</summary>
    public static long Rejected => rejected;

    /// <summary>抛异常的动作数。</summary>
    public static long Failed => failed;

    /// <summary>到期时目标已经不在了、被安静丢弃的项数。<b>它不是错误。</b></summary>
    public static long Skipped => skipped;

    /// <summary>被取消（或清理掉的过期项）的累计数量。</summary>
    public static long Cancelled => cancelled;

    /// <summary>
    /// 把动作排在 <paramref name="ticks"/> 个 tick 之后执行。0 与负数都按 1 处理。
    /// <para>
    /// <b>实际到期时刻是"排程那一 tick + n + 2"。</b>那两个 1 都有出处，缺一不可：
    /// 第一个来自 <see cref="Pump"/> 的时基（帧起点泵的时候 <see cref="MonoTime.Tick"/> 还是上一帧的值，
    /// 泵完才在世界更新里 +1，所以泵的截止线是 <c>tick + 1</c>，堆里的 <c>Due</c> 比它少 1 就永远追不上）；
    /// 第二个来自"不早于 n tick"这条保证本身。合起来才是"从第 100 tick 排 <c>Delay(30)</c>、
    /// 第 131 tick 执行"。
    /// </para>
    /// <para>
    /// 这个数字不是随手写的：验收台的 <c>调度器：Delay 的到期时刻正确</c>（从 100 排 30，129/130 都不动、131 执行）、
    /// <c>同 tick 按入队顺序</c>（从 0 排 5，第 6 执行）、<c>执行中排入的项不在同一轮跑</c> 三条用例共同钉住它。
    /// 改动这里之前先把它们看一遍——它们存在的意义就是不让时基漂移。
    /// </para>
    /// </summary>
    /// <param name="ticks">延迟的 tick 数。</param>
    /// <param name="action">要执行的动作。</param>
    /// <param name="label">诊断里显示的名字。</param>
    /// <returns>排程序号，交给 <see cref="Cancel"/> 可以撤销它；容量耗尽时返回 0（<c>Cancel(0)</c> 是安全空操作）。</returns>
    /// <remarks>
    /// 这一档没有存活判定可传：状态槽里放的是动作本身。要按目标实体判断，请用带 <c>state</c> 的那个重载。
    /// </remarks>
    public static long Delay(int ticks, Action action, string label = "")
    {
        ArgumentNullException.ThrowIfNull(action);
        return Schedule(now + Math.Max(1, ticks) + 2, static state => ((Action)state!)(), action, label, null);
    }

    /// <summary>
    /// 带状态参数的延迟调用。时基与 <see cref="Delay(int, Action, string)"/> 相同（到期 = 排程 tick + n + 2）。
    /// <para>
    /// 它是<b>避免闭包分配</b>的那条路：<c>Delay(30, a =&gt; a!.Foo(), myObj)</c> 里那个 lambda
    /// 只要不捕获局部变量，编译器就会把它缓存成静态委托，于是整个排程零分配。
    /// 写成 <c>Delay(30, () =&gt; myObj.Foo())</c> 每次都会分配一个闭包——功能一样，代价不同。
    /// </para>
    /// </summary>
    /// <param name="ticks">延迟的 tick 数。</param>
    /// <param name="action">要执行的动作，接受状态参数。</param>
    /// <param name="state">状态参数。</param>
    /// <param name="label">诊断里显示的名字。</param>
    /// <param name="alive">
    /// 目标是否还在（<c>null</c> 表示不检查）。到期时为假就<b>安静丢弃</b>这一项，不执行也不记日志。
    /// 常传 <see cref="MonoAlive.Npc"/> 这类判定："NPC 死了，这条延时 AI 就不该再跑"。
    /// </param>
    /// <returns>排程序号；容量耗尽时返回 0。</returns>
    public static long Delay(int ticks, Action<object?> action, object? state, string label = "", Func<object?, bool>? alive = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Schedule(now + Math.Max(1, ticks) + 2, action, state, label, alive);
    }

    /// <summary>把一个动作排在<b>指定 tick</b>执行（绝对时刻）。已经过去的时刻按"下一帧"处理（到期 = 当前 tick + 2）。</summary>
    /// <param name="tick">绝对 tick。</param>
    /// <param name="action">要执行的动作。</param>
    /// <param name="label">诊断里显示的名字。</param>
    /// <returns>排程序号；容量耗尽时返回 0。</returns>
    /// <remarks>与 <see cref="Delay(int, Action, string)"/> 一样，这一档没有存活判定可传。</remarks>
    public static long At(long tick, Action action, string label = "")
    {
        ArgumentNullException.ThrowIfNull(action);
        return Schedule(Math.Max(tick, now + 2), static state => ((Action)state!)(), action, label, null);
    }

    /// <summary>把带状态的动作排在指定 tick 执行。已经过去的时刻按"下一帧"处理。</summary>
    /// <param name="tick">绝对 tick。</param>
    /// <param name="action">要执行的动作，接受状态参数。</param>
    /// <param name="state">状态参数。</param>
    /// <param name="label">诊断里显示的名字。</param>
    /// <param name="alive">目标是否还在；语义与 <see cref="Delay(int, Action{object?}, object?, string, Func{object?, bool}?)"/> 相同。</param>
    /// <returns>排程序号；容量耗尽时返回 0。</returns>
    public static long At(long tick, Action<object?> action, object? state, string label = "", Func<object?, bool>? alive = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        return Schedule(Math.Max(tick, now + 2), action, state, label, alive);
    }

    /// <summary>
    /// 取消一个排程。因为排程的句柄是"序号"，所以只有拿到序号才取消得掉——
    /// 这也是刻意的：<b>用 lambda 当键去取消是另一个陷阱</b>（每次写的 lambda 都是新实例，永远匹配不上）。
    /// </summary>
    /// <param name="sequenceId">排程时返回的序号。</param>
    /// <returns>确实找到并取消才返回 true。</returns>
    public static bool Cancel(long sequenceId)
    {
        if (sequenceId == 0)
            return false;

        for (int i = 0; i < count; i++)
        {
            if (heap[i].Sequence != sequenceId || heap[i].Cancelled)
                continue;

            heap[i].Cancelled = true;
            heap[i].Action = null;
            heap[i].State = null;       // 别拖住状态对象：一个被取消的协程不该因为排程还活着而被引用
            cancelled++;
            return true;
        }
        return false;
    }

    /// <summary>取消全部排程。世界卸载与卸载时会调。</summary>
    public static void Clear()
    {
        for (int i = 0; i < count; i++)
        {
            heap[i].Cancelled = true;
            heap[i].Action = null;
            heap[i].State = null;
        }
        count = 0;
    }

    /// <summary>
    /// 清空并重置全部计数。模组卸载时用。
    /// <para>
    /// <b>它必须把冷却表也清掉。</b>漏了这一句的后果不是"数据不准"，而是<b>跨世界的假冷却</b>：
    /// 上一局为 <c>"hit-sound"</c> 记的 <c>last</c> 还在字典里，新世界里第一次调用
    /// <c>Cooldown("hit-sound", 6)</c> 会因为"距上次还不到 6 tick"而被拒绝——
    /// 症状是"刚进世界头几帧音效不响"，而原因在一个早就没了的旧世界实例里。
    /// 验收台的 <c>调度器：冷却表</c> 用例抓的就是它（它跑在 Reset 之后，却读到了上一条用例留下的记录）。
    /// </para>
    /// </summary>
    public static void Reset()
    {
        Clear();
        cooldowns.Clear();
        now = 0;
        sequence = 0;
        executed = 0;
        rejected = 0;
        failed = 0;
        skipped = 0;
        cancelled = 0;
    }

    /// <summary>
    /// 执行所有"到 <paramref name="tick"/> + 1 为止"的到期项，即 <c>Due &lt;= tick + 1</c>。
    /// <para>
    /// <b>那个 <c>+ 1</c> 是全层的时基约定，不是 off-by-one。</b>帧起点泵的时候
    /// <see cref="MonoTime.Tick"/> 还是上一帧的值，泵完才在世界更新里 +1；于是
    /// <c>Delay(n)</c> 的实际到期时刻是<b>第 <c>t+n+1</c> tick</b>。写成严格的 <c>Due &lt;= tick</c>
    /// 会让每一处排程都晚一格，并且与本层所有"帧边界"的定义打架。
    /// </para>
    /// <para>
    /// 这个约定的判据在验收台里：<c>调度器：Delay 的到期时刻正确</c>（30 tick 从第 100 排、第 131 跑）、
    /// <c>同 tick 按入队顺序</c>（5 tick 从第 0 排、第 6 跑）、<c>执行中排入的项不在同一轮跑</c>
    /// 三条用例各自从不同角度钉住它。改动这里之前先把它们看一遍。
    /// </para>
    /// <para>
    /// 它是公开的，<b>只为了让验收台能自己当 <see cref="MonoFrameSystem"/> 驱动一遍</b>。
    /// 消费者调它等于自己造一个时钟——那时候"tick"这个词就不再有任何保证了。
    /// </para>
    /// </summary>
    /// <param name="tick">当前的 tick 值（由 <see cref="MonoFrameSystem"/> 传 <see cref="MonoTime.Tick"/>）。</param>
    public static void Pump(long tick)
    {
        now = tick;
        long deadline = now + 1;

        // 本轮只处理"泵开始时就已经在堆里"的那些项。快照是必需的：
        // 一个动作在自己的执行体里再排一个到期项时，堆顶会立刻满足 Due <= deadline，
        // 于是同一个 while 会把它当场跑掉——这就是"派发中修改集合"的调度器版本，
        // 也是「执行中排入的项不在同一轮跑」那条用例抓到的真实缺陷。
        // 用 for 而不是 while：上界在进入循环时就固定，新排入的项只能等下一轮。
        int batch = count;
        for (int i = 0; i < batch && count > 0; i++)
        {
            if (heap[0].Due > deadline)
                break;

            Pending pending = Pop();
            if (pending.Cancelled || pending.Action is null)
                continue;   // 已被 Cancel

            // 目标已经不在了：正常现象（NPC 死了、弹幕没了），安静丢弃。
            if (!IsStillAlive(pending))
                continue;

            executed++;
            try
            {
                pending.Action(pending.State);
            }
            catch (Exception exception)
            {
                failed++;
                MonoLog.Error(MonoLogLevel.Error, $"调度项「{Display(pending.Label)}」抛异常：{exception}");
            }
        }
    }

    /// <summary>
    /// 问一次"这一项的目标还在不在"。没有判定函数的项一律当作还在。
    /// <para>
    /// 判定为假：计入 <see cref="Skipped"/> 并丢弃，不执行、不记日志。
    /// 判定自己抛异常：计入 <see cref="Failed"/> 并丢弃——那是消费者的判定写错了，属于真 bug。
    /// </para>
    /// </summary>
    /// <param name="pending">要检查的项。</param>
    private static bool IsStillAlive(in Pending pending)
    {
        if (pending.Alive is null)
            return true;

        try
        {
            if (pending.Alive(pending.State))
                return true;
        }
        catch (Exception exception)
        {
            failed++;
            MonoLog.Error(MonoLogLevel.Error, $"调度项「{Display(pending.Label)}」的存活判定抛异常，已丢弃：{exception}");
            return false;
        }

        skipped++;
        return false;
    }

    /// <summary>诊断里显示的名字。空标签给一个占位，免得日志里出现一对空引号。</summary>
    /// <param name="label">排程时的标签。</param>
    private static string Display(string label) => label.Length > 0 ? label : "(未命名)";

    /// <summary>把已取消的项从堆里挤出去。低频调用，避免"取消过的项永远占着容量"。</summary>
    public static void TrimHeap() => Compact();

    /// <summary>把已取消的项从堆里挤出去。</summary>
    private static void Compact()
    {
        for (int i = count - 1; i >= 0; i--)
        {
            if (!heap[i].Cancelled && heap[i].Action is not null)
                continue;

            count--;
            if (i != count)
            {
                heap[i] = heap[count];
                SiftDown(i);
                SiftUp(i);
            }
            heap[count] = default;
        }
    }

    /// <summary>
    /// 冷却表：判断"距上次发生是否已经过了 <paramref name="ticks"/> 个 tick"，并记录这一次。
    /// <para>
    /// 典型用法是音效与粒子的节流：<c>if (MonoScheduler.Cooldown("hit-sound", 6)) SoundEngine.PlaySound(...);</c>。
    /// 它替代"再写一个 <c>int soundTimer</c> 字段并每帧递减"——那种写法在一个类里会积累出十几个计时器字段。
    /// </para>
    /// </summary>
    /// <param name="key">冷却键，必须稳定（不要用玩家名字这类会变的东西）。</param>
    /// <param name="ticks">冷却 tick 数。</param>
    /// <returns>已经过了冷却期（调用方可以执行）返回 true，并且这一次会被记为新的起点。</returns>
    public static bool Cooldown(string key, int ticks)
    {
        if (cooldowns.TryGetValue(key, out long last) && now - last < ticks)
            return false;
        cooldowns[key] = now;
        return true;
    }

    /// <summary>查冷却剩余 tick 数，0 表示可用。</summary>
    /// <param name="key">冷却键。</param>
    /// <param name="ticks">冷却总长。</param>
    public static long CooldownRemaining(string key, int ticks)
    {
        if (!cooldowns.TryGetValue(key, out long last))
            return 0;
        return Math.Max(0, ticks - (now - last));
    }

    /// <summary>
    /// 清掉过期的冷却记录，避免字典无限增长。
    /// <para>
    /// 它由 <see cref="MonoSchedulerSystem"/> 低频调用（不是每帧），因为冷却键通常来自玩家 / NPC，
    /// 数量有限但会随着实体生灭而增长。<b>不清的话就是一条缓慢的内存泄漏。</b>
    /// </para>
    /// </summary>
    /// <param name="retention">超过这个 tick 数的记录会被清掉。</param>
    public static void TrimCooldowns(int retention = 600)
    {
        if (cooldowns.Count == 0)
            return;

        List<string>? expired = null;
        foreach ((string key, long last) in cooldowns)
        {
            if (now - last <= retention)
                continue;
            (expired ??= []).Add(key);
        }

        if (expired is null)
            return;
        for (int i = 0; i < expired.Count; i++)
            cooldowns.Remove(expired[i]);
    }

    private static readonly Dictionary<string, long> cooldowns = [];

    /// <summary>
    /// 起一个协程：接受迭代器方法<b>的返回值</b>（<c>IEnumerable&lt;MonoWait&gt;</c>）或 <see cref="IEnumerator{T}"/>。
    /// <para>
    /// <c>MonoScheduler.Start(MyAttack());</c>——<b>不要传 <c>MyAttack</c> 这个方法组</b>（那是个委托，不会自动调用）。
    /// 这个重载同时接受两种形态，就是为了让"忘了加括号"不再是一个静默的 bug：它会当场抛，
    /// 并在消息里点名这个最常见的错法。
    /// </para>
    /// </summary>
    /// <param name="routine">迭代器方法的返回值或它的枚举器。</param>
    /// <returns>协程句柄，可用来停止它。</returns>
    public static MonoCoroutine Start(object routine)
    {
        ArgumentNullException.ThrowIfNull(routine);

        IEnumerator<MonoWait> enumerator;
        string name;
        switch (routine)
        {
            case IEnumerable<MonoWait> enumerable:
                enumerator = enumerable.GetEnumerator();
                name = enumerable.GetType().Name;
                break;

            case IEnumerator<MonoWait> e:
                enumerator = e;
                name = e.GetType().Name;
                break;

            default:
                throw new ArgumentException(
                    $"Start 的入参必须是迭代器方法返回的 IEnumerable<MonoWait> 或 IEnumerator<MonoWait>，" +
                    $"实际拿到的是 {routine.GetType().Name}。" +
                    "最常见的原因是漏了括号：写 MyAttack() 而不是 MyAttack。", nameof(routine));
        }

        MonoCoroutine coroutine = new(enumerator, name);
        coroutine.Step();       // 立刻走第一步：与"协程在本帧就开始"的直觉一致
        return coroutine;
    }

    /// <summary>诊断输出：待执行数、累计执行、取消数、拒绝数、失败数、目标消失而作废的数。</summary>
    public static string Describe()
        => $"调度器：待执行 {count}/{heap.Length}，累计执行 {executed}，取消 {cancelled}，拒绝 {rejected}，失败 {failed}，目标消失作废 {skipped}，当前 tick {now}";

    /// <summary>把待执行项按到期顺序 dump 出来（诊断用，会分配）。</summary>
    public static string Dump()
    {
        if (count == 0)
            return Describe() + "\n（没有待执行项）";

        // 复制一份再排序，避免破坏堆序。
        Pending[] copy = new Pending[count];
        Array.Copy(heap, copy, count);
        Array.Sort(copy, static (a, b) => a.Due != b.Due ? a.Due.CompareTo(b.Due) : a.Sequence.CompareTo(b.Sequence));

        StringBuilder output = new();
        output.Append(Describe());
        for (int i = 0; i < copy.Length; i++)
        {
            output.Append("\n  +").Append(copy[i].Due - now).Append(" tick  ")
                  .Append(copy[i].Label.Length > 0 ? copy[i].Label : "(未命名)")
                  .Append("  seq=").Append(copy[i].Sequence)
                  .Append(copy[i].Action is null ? "  [已取消]" : string.Empty);
        }
        return output.ToString();
    }

    private static long Schedule(long due, Action<object?> action, object? state, string label, Func<object?, bool>? alive)
    {
        if (count == heap.Length)
        {
            rejected++;
            MonoLog.Warn(MonoLogLevel.Warn, $"调度器已满（{heap.Length} 项），拒绝排入「{Display(label)}」。");
            return 0;
        }

        long id = ++sequence;
        heap[count] = new Pending { Due = due, Sequence = id, Action = action, State = state, Alive = alive, Label = label ?? string.Empty };
        SiftUp(count);
        count++;
        return id;
    }

    private static Pending Pop()
    {
        Pending root = heap[0];
        count--;
        if (count > 0)
        {
            heap[0] = heap[count];
            SiftDown(0);
        }
        heap[count] = default;
        return root;
    }

    private static void SiftUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (!Precedes(heap[index], heap[parent]))
                break;
            (heap[index], heap[parent]) = (heap[parent], heap[index]);
            index = parent;
        }
    }

    private static void SiftDown(int index)
    {
        while (true)
        {
            int left = (index * 2) + 1;
            if (left >= count)
                return;

            int smallest = left;
            int right = left + 1;
            if (right < count && Precedes(heap[right], heap[left]))
                smallest = right;

            if (!Precedes(heap[smallest], heap[index]))
                return;

            (heap[index], heap[smallest]) = (heap[smallest], heap[index]);
            index = smallest;
        }
    }

    /// <summary>堆序：先比到期 tick，再比入队序号——同 tick 的执行顺序就是入队顺序，可复现。</summary>
    private static bool Precedes(in Pending a, in Pending b)
        => a.Due != b.Due ? a.Due < b.Due : a.Sequence < b.Sequence;
}

/// <summary>
/// 常用的排程存活判定，直接传给 <see cref="MonoScheduler.Delay(int, Action{object?}, object?, string, Func{object?, bool}?)"/>
/// 的 <c>alive</c> 参数。
/// <para>
/// 判定为假时那一项会被安静丢弃：不执行、不记日志。这正是"目标已经不在了"该有的样子——
/// NPC 死亡、弹幕消失都是正常流程；真正异常的仍然是异常。
/// </para>
/// </summary>
public static class MonoAlive
{
    /// <summary>NPC 还在场。实体槽位会被复用，所以目标 NPC 一死，挂在它身上的排程就该作废。</summary>
    public static bool Npc(object? state) => state is NPC npc && npc.active;

    /// <summary>弹幕还在场，判据与 <see cref="Npc"/> 相同。</summary>
    public static bool Projectile(object? state) => state is Projectile projectile && projectile.active;

    /// <summary>玩家还在场，判据与 <see cref="Npc"/> 相同。</summary>
    public static bool Player(object? state) => state is Player player && player.active;
}

/// <summary>
/// 调度器自己的 <see cref="ModSystem"/>：只做一件世界级的事——清掉过期的冷却记录。
/// <para>
/// 它故意与 <see cref="MonoFrameSystem"/> 分开：<see cref="MonoFrameSystem"/> 是每帧都要跑的基础设施，
/// 而这里是一分钟几次的清理，混在一起会让"每帧路径上有什么"变得不清晰。
/// </para>
/// </summary>
public sealed class MonoSchedulerSystem : ModSystem
{
    private const int TrimInterval = 300;
    private int sinceTrim;

    /// <inheritdoc/>
    public override void PostUpdateEverything()
    {
        if (++sinceTrim < TrimInterval)
            return;
        sinceTrim = 0;
        MonoScheduler.TrimCooldowns(TrimInterval * 2);

        // 顺手把已取消的排程项从堆里挤出去。取消是低频动作，所以这件事也低频做就够了。
        MonoScheduler.TrimHeap();
    }

    /// <inheritdoc/>
    public override void OnWorldUnload() => MonoScheduler.Clear();

    /// <inheritdoc/>
    public override void OnModUnload() => MonoScheduler.Reset();
}
