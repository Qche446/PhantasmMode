using System.Text;

namespace Monochrome.Content.Ai;

/// <summary>
/// 无数据状态机：状态就是一个枚举值，机器只记住当前在哪个状态。
/// <para>
/// <b>协作地位</b>：计时器、弹幕计数、阶段标记这些状态局部数据全部由消费者自己保管。
/// 机器只做三件事——按状态派发每帧行为、按注册顺序检查转换条件、在切换发生时触发
/// <see cref="Changed"/>。怎么进入一个状态、用什么条件离开、下一个状态是谁，都由消费者注册进来。
/// </para>
/// <para>
/// <b>怎么用</b>：在消费者的惰性初始化里建一台，注册行为与转换，之后每帧调一次 <see cref="Update"/>。
/// <code>
/// Fsm = new MonoFsm&lt;MyAI&gt;(MyAI.Idle);
/// Fsm.Behavior(MyAI.Idle, IdleBehavior);                       // 或 [MonoBehavior] + Fsm.AutoBind(this)
/// Fsm.To(MyAI.Idle, MyAI.Charge, () =&gt; Timer &gt; 120);        // 固定目标
/// Fsm.To(MyAI.Charge, () =&gt; PickNextAttack(), () =&gt; Timer &gt; 90);   // 目标交给委托，随机与权重写在这里
/// Fsm.Changed += (oldState, newState) =&gt; Timer = 0;           // 计时归零就是"刚进入"的信号
///
/// public override void AI(NPC npc)
/// {
///     Fsm.Update();
///     Timer++;
/// }
/// </code>
/// </para>
/// <para>
/// <b>一帧内的链式切换</b>：切换之后会立刻检查新状态的转换，条件仍成立就继续切下去。
/// 因此离开条件要能随状态变化失效，避免两个状态互相指回来。
/// </para>
/// <para>
/// <b>时机</b>：注册只在初始化做一次。机器不持有静态字段，全部数据都在这一个实例里；
/// 实例挂在实体上、随实体一起重建，模组重载时旧程序集里的委托不会被留住。
/// 不要把这台机器或它的委托放进静态字段、ModSystem、长寿命服务。
/// </para>
/// </summary>
/// <typeparam name="TId">状态标识，用一个枚举。</typeparam>
public sealed class MonoFsm<TId> where TId : struct, Enum
{
    private readonly TId initial;
    private readonly Dictionary<TId, Action> behaviors = [];
    private readonly Dictionary<TId, List<Transition>> transitions = [];
    private readonly List<HijackEntry> hijacks = [];

    /// <summary>建一台机器，并停在初始状态。构造不会触发 <see cref="Changed"/>。</summary>
    /// <param name="initial">初始状态。</param>
    public MonoFsm(TId initial)
    {
        this.initial = initial;
        Current = initial;
    }

    /// <summary>当前状态。任何时刻都有一个确定的值，不会为空。</summary>
    public TId Current { get; private set; }

    /// <summary>
    /// 每次状态切换后触发，参数依次是离开的状态与进入的状态。
    /// <para>
    /// 它在转换自己的回调之前触发，所以"每次切换都要重置"的变量（计时器、公共 ai 格）适合挂在这里。
    /// 触发时当前行为已经跑过，新状态的行为要等下一帧的 <see cref="Update"/>。
    /// </para>
    /// </summary>
    public event Action<TId, TId>? Changed;

    /// <summary>已经绑上行为的状态数。绑定全没生效时这里会是零。</summary>
    public int BehaviorCount => behaviors.Count;

    /// <summary>转换的一条登记：目标怎么算、条件是什么、切换后做什么。</summary>
    private readonly record struct Transition(Func<TId> Target, TId? Declared, Func<bool> When, Action? Then);

    /// <summary>劫持的一条登记：改写目标，以及改写后做什么。</summary>
    private readonly record struct HijackEntry(Func<TId, TId> Select, Action<TId>? Then);

    /// <summary>给一个状态绑定每帧行为。同一状态重复绑会覆盖上一条。</summary>
    /// <param name="id">状态。</param>
    /// <param name="update">每帧执行一次的方法。</param>
    public void Behavior(TId id, Action update)
    {
        behaviors[id] = update;
    }

    /// <summary>撤掉一个状态的行为。</summary>
    /// <param name="id">状态。</param>
    /// <returns>原来有绑定才返回 <see langword="true"/>。</returns>
    public bool ClearBehavior(TId id) => behaviors.Remove(id);

    /// <summary>
    /// 把 <paramref name="owner"/> 上所有带 <see cref="MonoBehaviorAttribute{TId}"/> 的方法登记为行为。
    /// <para>
    /// 只有公开、非静态、无参、返回 <see langword="void"/> 的实例方法会被登记；
    /// 其余（包括 <c>private</c> 方法——反射按公开成员查找，看不见它们）静默跳过，不报错也不警告。
    /// 漏绑的表现是状态卡住不动，用 <see cref="BehaviorCount"/> 或 <see cref="Dump"/> 核对。
    /// </para>
    /// </summary>
    /// <param name="owner">行为方法所在的实例，通常是 <c>this</c>。</param>
    public void AutoBind(object owner)
    {
        foreach (MethodInfo method in owner.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            MonoBehaviorAttribute<TId>? attribute = method.GetCustomAttribute<MonoBehaviorAttribute<TId>>();
            if (attribute is null)
                continue;
            //排除静态，泛型，有返回值，有参的方法
            if (method.IsStatic || method.IsGenericMethod || method.ReturnType != typeof(void) || method.GetParameters().Length != 0)
                continue;

            behaviors[attribute.Id] = method.CreateDelegate<Action>(owner);
        }
    }

    /// <summary>注册一条转换：条件成立时切到固定目标。</summary>
    /// <param name="from">从哪个状态出发。</param>
    /// <param name="to">切到哪个状态。</param>
    /// <param name="when">转换条件，每帧问一次。</param>
    /// <param name="then">切换后执行的回调。</param>
    public void To(TId from, TId to, Func<bool> when, Action? then = null)
    {
        Add(from, new Transition(() => to, to, when, then));
    }

    /// <summary>
    /// 注册一条转换：条件成立时切到 <paramref name="to"/> 现算出来的目标。
    /// <para>
    /// 随机招式、按权重挑、洗牌出招这类选择写在这个委托里。条件成立的那一帧求值一次。
    /// </para>
    /// </summary>
    /// <param name="from">从哪个状态出发。</param>
    /// <param name="to">算出目标状态的委托。</param>
    /// <param name="when">转换条件，每帧问一次。</param>
    /// <param name="then">切换后执行的回调。</param>
    public void To(TId from, Func<TId> to, Func<bool> when, Action? then = null)
    {
        Add(from, new Transition(to, null, when, then));
    }

    /// <summary>
    /// 从每个状态各注册一条转换，命中条件就切到同一个目标。适合"任意状态下都要强插"的切入，
    /// 以及"任何攻击打完都回待机"这类收口。
    /// </summary>
    /// <param name="to">切到哪个状态。</param>
    /// <param name="when">转换条件，每帧问一次。</param>
    /// <param name="then">切换后执行的回调。</param>
    /// <param name="except">不注册的来源状态，比如目标自身与正在播出的演出状态。</param>
    public void ToAny(TId to, Func<bool> when, Action? then = null, params TId[] except)
        => RegisterAny(new Transition(() => to, to, when, then), except);

    /// <summary>从每个状态各注册一条转换，目标由委托现算。其余与 <see cref="ToAny(TId, Func{bool}, Action?, TId[])"/> 相同。</summary>
    /// <param name="to">算出目标状态的委托。</param>
    /// <param name="when">转换条件，每帧问一次。</param>
    /// <param name="then">切换后执行的回调。</param>
    /// <param name="except">不注册的来源状态。</param>
    public void ToAny(Func<TId> to, Func<bool> when, Action? then = null, params TId[] except)
    {
        ArgumentNullException.ThrowIfNull(to);
        RegisterAny(new Transition(to, null, when, then), except);
    }

    /// <summary>
    /// 注册一条劫持：任何转换发生时都能改写它的目标。
    /// <para>
    /// 返回原值表示不接手，目标照原样；返回别的值表示改写，回调随即触发，后面的劫持不再询问。
    /// 按注册顺序取第一条改写的。
    /// </para>
    /// </summary>
    /// <param name="select">拿到原本要去的状态，返回最终要去的状态。</param>
    /// <param name="then">改写发生后执行的回调。</param>
    public void Hijack(Func<TId, TId> select, Action<TId>? then = null)
    {
        ArgumentNullException.ThrowIfNull(select);
        hijacks.Add(new HijackEntry(select, then));
    }

    /// <summary>撤掉某个状态上登记的全部转换。</summary>
    /// <param name="from">状态。</param>
    /// <returns>原来有转换才返回 <see langword="true"/>。</returns>
    public bool ClearTransitions(TId from) => transitions.Remove(from);

    /// <summary>撤掉全部劫持。</summary>
    public void ClearHijacks() => hijacks.Clear();

    /// <summary>
    /// 跑一帧：先执行当前状态的行为，再检查转换；条件成立就切换，并继续检查新状态的转换。
    /// </summary>
    public void Update()
    {
        if (behaviors.TryGetValue(Current, out Action? behavior))
            behavior();

        while (transitions.TryGetValue(Current, out List<Transition>? candidates))
        {
            Transition? picked = null;
            //遍历注册的转换表，拿到第一个触发其when的转换
            foreach (Transition candidate in candidates)
            {
                if (candidate.When())
                {
                    picked = candidate;
                    break;
                }
            }

            if (picked is null)
                return;

            Transition transition = picked.Value;
            TId leaving = Current;
            //获取目标状态
            TId target = transition.Target();

            //遍历劫持
            foreach (HijackEntry hijack in hijacks)
            {
                TId rewritten = hijack.Select(target);
                if (EqualityComparer<TId>.Default.Equals(rewritten, target))
                    continue;
                //如果目标状态相同，不执行劫持的回调
                target = rewritten;
                hijack.Then?.Invoke(target);
                break;
            }
            //执行转换的回调，先全局，后特化
            Current = target;
            Changed?.Invoke(leaving, target);
            transition.Then?.Invoke();
        }
    }

    /// <summary>回到初始状态，并把切换事件按普通转换的方式触发一次。</summary>
    public void Reset()
    {
        TId leaving = Current;
        Current = initial;
        Changed?.Invoke(leaving, initial);
    }

    /// <summary>
    /// 一行当前状况：当前状态、候选转换条数、劫持条数，以及登记里出现过却没有行为的状态。
    /// <para>
    /// 它不评估任何转换条件（条件可能有副作用），可以在绘制或命令里放心调用。
    /// </para>
    /// </summary>
    /// <returns>给调试绘制或控制台用的一行文本。</returns>
    public string Dump()
    {
        StringBuilder text = new();
        text.Append("当前状态 ").Append(Current);
        text.Append("；候选转换 ").Append(transitions.TryGetValue(Current, out List<Transition>? candidates) ? candidates.Count : 0).Append(" 条");
        text.Append("；劫持 ").Append(hijacks.Count).Append(" 条");

        HashSet<TId> declared = [Current];
        foreach (TId id in behaviors.Keys)
            declared.Add(id);
        foreach (KeyValuePair<TId, List<Transition>> pair in transitions)
        {
            declared.Add(pair.Key);
            foreach (Transition transition in pair.Value)
            {
                if (transition.Declared is TId target)
                    declared.Add(target);
            }
        }

        declared.RemoveWhere(id => behaviors.ContainsKey(id));
        if (declared.Count > 0)
            text.Append("；无行为的状态 ").Append(string.Join(", ", declared));

        return text.ToString();
    }

    private void Add(TId from, Transition transition)
    {
        if (!transitions.TryGetValue(from, out List<Transition>? list))
        {
            list = [];
            transitions[from] = list;
        }

        list.Add(transition);
    }

    private void RegisterAny(Transition transition, TId[] except)
    {
        foreach (TId id in Enum.GetValues<TId>())
        {
            if (except is not null && except.Contains(id))
                continue;

            Add(id, transition);
        }
    }
}
