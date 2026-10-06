using System.Reflection;
using System.Text;

namespace Monochrome.Core;

/// <summary>
/// 一帧里的派发阶段。<b>每个取值都对应一个确切的 tML 时机</b>，不是"大概在这附近"。
/// <para>
/// 这张表本身就是文档：订阅之前先看"我的代码被允许在什么时候跑"。跑错时机的症状通常是
/// "位置差一帧"或"读到的是上一帧的屏幕矩阵"，而这类 bug 极难归因。
/// </para>
/// <para>
/// <b>钩子名已按本机 tML 1.4.4.9 的 <c>Terraria.ModLoader.ModSystem</c> 逐个核对</b>。
/// 核对纠正了三处原先靠命名习惯推出来的错误，它们各自都编不过：
/// </para>
/// <list type="bullet">
/// <item><description><c>PreDrawTiles</c> <b>不存在</b>。<c>ModSystem</c> 上只有 <c>PostDrawTiles</c>。</description></item>
/// <item><description><c>PostUpdateEntities</c> <b>不存在</b>。实体更新后的钩子是按类型分开的：<c>PostUpdateNPCs</c> / <c>PostUpdatePlayers</c> / <c>PostUpdateProjectiles</c>。</description></item>
/// <item><description>这些钩子在 tML 里是<b>可重写方法</b>，不是 <c>event</c>。所以这里没有 <c>+=</c> 这回事，也就不存在"忘了摘钩子"。</description></item>
/// </list>
/// <para>
/// 另一条硬事实：<b>tML 的 <c>ModSystem</c> 没有任何"绘制开始之前"的钩子</b>。绘制相位只有
/// <c>PostDrawTiles</c>（实体与世界内容画完之后）与 <c>PostDrawInterface</c>（界面画完之后）。
/// 因此 <see cref="PreDraw"/> 与 <see cref="PostUpdate"/> <b>共用</b> <c>PostUpdateDusts</c> 这个
/// "本帧更新已经基本跑完、绘制还没开始"的时刻——这就是 ModSystem 能提供的最后一个更新后时机。
/// 真正的"抢在第一批绘制之前"在图元层，那里的宿主是消费者自己的程序集（见蓝图 §18.1）。
/// </para>
/// </summary>
public enum MonoFramePhase
{
    /// <summary>实体更新之前（<c>ModSystem.PreUpdateEntities</c>）。世界还停在上一帧的状态，适合做输入闸门、状态推进。<b>在世界更新闸门之外，定格时也跑。</b></summary>
    PreUpdate = 0,

    /// <summary>
    /// <c>ModSystem.PostUpdateDusts</c>：本帧的实体、弹幕、物品、时间、世界都已经更新完，
    /// 尘埃也处理完了；这是"闸门内的最后一个更新后时刻"，紧接着 <c>PostUpdateEverything</c>。
    /// <para><b>它在世界更新闸门之内</b>：命中定格 / <c>WorldTimeScale = 0</c> 时整个不执行。</para>
    /// </summary>
    WorldUpdate = 1,

    /// <summary>
    /// 与 <see cref="WorldUpdate"/> 同一个时刻（<c>ModSystem.PostUpdateDusts</c>），
    /// 语义是"世界本帧已经彻底更新完了"。
    /// <para>
    /// <b>为什么不挂在 <c>PostUpdateEverything</c> 上</b>：那个钩子归 <see cref="MonoFrameSystem"/> 所有，
    /// 它要在那里推进 <see cref="MonoTime.Tick"/> 与补间。派发与时钟推进挤在同一个钩子里，
    /// "这个订阅者看到的 Tick 是推进前还是推进后"就变成了隐式约定。现在它是显式的：
    /// <c>PostUpdate</c> 时 <see cref="MonoTime.Tick"/> 是<b>上一帧</b>的值。
    /// </para>
    /// </summary>
    PostUpdate = 2,

    /// <summary>
    /// 绘制之前。实现上<b>与 <see cref="PostUpdate"/> 同一时刻</b>（<c>PostUpdateDusts</c>）——
    /// tML 的 <c>ModSystem</c> 没有更晚的更新后钩子，也没有绘制前钩子（见类型说明）。
    /// <para>
    /// 需要"抢在第一批绘制之前"的原始绘制，请走图元层的 <c>On_Main.DrawDust</c> 路径，
    /// 而不是在这里抢批次：这里根本没有批次。
    /// </para>
    /// </summary>
    PreDraw = 3,

    /// <summary>
    /// 世界与世界内界面画完之后（<c>ModSystem.PostDrawTiles</c>）。官方文档口径是它运行在
    /// "所有内容都画完之后"，位置接近 <c>Main.Draw</c> 的末尾。<b>专用服务器上不会触发</b>。
    /// <para>要做原始绘制请用图元层自带的路径，不要在这里抢批次。</para>
    /// </summary>
    PostDraw = 4,

    /// <summary>
    /// 界面画完之后（<c>ModSystem.PostDrawInterface</c>），拿到的是游戏已经 <c>Begin</c> 好的界面批次。
    /// <b>专用服务器上不会触发</b>。
    /// </summary>
    PostDrawInterface = 5,

    /// <summary>
    /// 世界卸载的瞬间（<c>ModSystem.OnWorldUnload</c>），不是每帧阶段。
    /// <para>
    /// 需要"退出世界就清缓存"的订阅挂这里，<b>不要</b>挂 <see cref="IMonoService.Unload"/>——
    /// 后者只在热重载 / 退出游戏时跑一次。
    /// </para>
    /// </summary>
    WorldUnload = 6,

    /// <summary>
    /// NPC 更新之后（<c>ModSystem.PostUpdateNPCs</c>）。在闸门内。
    /// <para>
    /// 它的存在理由是"读 NPC 的正确时机"：<c>PostUpdateNPCs</c> 是 tML 明确保证"NPC 数组已经改完"的挂点，
    /// 想在每帧更新之后遍历一次 NPC（写寻敌、做空间索引、采样血量）就用它，而不是自己在 <c>PostUpdateDusts</c> 里猜。
    /// </para>
    /// </summary>
    PostUpdateNPCs = 7,

    /// <summary>玩家更新之后（<c>ModSystem.PostUpdatePlayers</c>）。在闸门内。适合做"记账"类逻辑：统计、光环结算、同步快照。</summary>
    PostUpdatePlayers = 8,

    /// <summary>弹幕更新之后（<c>ModSystem.PostUpdateProjectiles</c>）。在闸门内。适合做弹幕的空间索引与碰撞后处理。</summary>
    PostUpdateProjectiles = 9,

    /// <summary>
    /// 留给调用方自己泵的阶段，<b>库不自动派发</b>。
    /// <para>
    /// 用途是"我自己的系统有一个明确的节拍，我想让订阅者跟着它走"：调用方调
    /// <see cref="MonoEventBus.Dispatch(MonoFramePhase)"/> 即可，语义与其它阶段完全一致。
    /// 留这一档是为了不必给每个新节拍都往枚举里加一个值。
    /// </para>
    /// </summary>
    Custom = 10
}

/// <summary>
/// 按帧阶段分派的<b>弱订阅</b>事件总线。
/// <para>
/// 为什么不用普通的 <c>event Action</c>：事件由发布者强引用订阅者，模组卸载后回调仍被持有，
/// 下一次触发就会碰到已卸载的类型 → <c>TypeLoadException</c>。这里用 <see cref="WeakReference{T}"/>
/// 持有目标，目标被回收后订阅自动失效。
/// </para>
/// <para>
/// 派发路径<b>不分配托管内存</b>：<c>List&lt;Entry&gt;</c> + 反向遍历 + 版本号判断。版本号解决"派发中修改集合"
/// 这个经典异常——订阅者在自己的回调里退订不会让遍历踩空，也不需要给列表拍快照。
/// 唯一的常驻分配是每次订阅时反射构造的 <see cref="Action{T}"/> 包装（一次，不在热路径上）。
/// </para>
/// <para>
/// 静态方法订阅（<c>Subscribe&lt;Foo&gt;(Foo.OnTick)</c>，或 <c>Action&lt;object&gt;</c> 形式的静态回调）
/// 不经过弱引用——静态方法没有目标可被回收，它们<b>必须显式退订</b>，<see cref="Clear()"/> 会一并清掉。
/// </para>
/// </summary>
public static class MonoEventBus
{
    /// <summary>一个订阅。同时存调用委托（派发用）与方法信息（dump 用）。</summary>
    private struct Entry
    {
        /// <summary>
        /// 要弱持有的目标；<b><see langword="null"/> 表示"不需要弱持有"</b>（静态方法组、静态 lambda）。
        /// <para>
        /// <b>不要用"委托的 <c>Target</c> 是不是 null"来判断静态</b>：现代 C# 会把静态 lambda 编译成
        /// 一个<b>单例显示类的实例方法</b>，于是它的 <c>Target</c> <b>不是 null</b>。这条被实测踩过——
        /// 按 <c>Target is null</c> 判静态，会把所有静态 lambda 都当成实例方法而拒绝注册。
        /// 这里只由 <see cref="SubscribeStatic"/> 显式写入 null，语义是"这条不参与弱引用回收"。
        /// </para>
        /// </summary>
        public WeakReference<object>? Target;

        /// <summary>把回调包装成"目标 + 无参调用"的委托。<b>订阅时构造一次</b>，派发时零分配。</summary>
        public Action<object>? Invoker;

        /// <summary>回调的方法，只用于诊断 dump。</summary>
        public MethodInfo? Method;

        /// <summary>
        /// 这条订阅是否仍然有效。<b>有效性是"每条"的，不是"每个阶段"的</b>——<see cref="Unsubscribe"/>
        /// 只把命中的那一条置为无效，同一个阶段上的其它订阅者不受影响。
        /// </summary>
        public bool Alive;

        /// <summary>订阅序号（<b>全库唯一</b>，不是按阶段重置）。只用于让句柄能定位到自己那一条。</summary>
        public int Token;

        /// <summary>是否还有效。<b>不参与弱引用的条目永远有效，直到被显式退订或 <see cref="Clear()"/>。</b></summary>
        public readonly bool IsAlive
            => Alive && Invoker is not null && (Target is null || Target.TryGetTarget(out _));
    }

    /// <summary>
    /// 按阶段分槽，避免每帧把所有阶段都扫一遍。<see cref="MonoFramePhase"/> 的取值是连续小整数。
    /// <para>
    /// 公开是为了让游戏内自检能按阶段遍历（它要断言"11 个阶段各自被派发过"），
    /// 而不是把 <c>11</c> 这个数在别处再写一遍——那样加一个阶段就会漏掉一处。
    /// </para>
    /// </summary>
    public const int PhaseCount = 11;

    private static readonly List<Entry>[] slots = CreateSlots();
    private static readonly int[] dispatchDepth = new int[PhaseCount];
    private static readonly bool[] pendingCompaction = new bool[PhaseCount];
    private static readonly long[] dispatchCounts = new long[PhaseCount];

    /// <summary>
    /// 订阅序号发生器，<b>全库唯一</b>（不按阶段重置）。
    /// <para>
    /// 它就是句柄里的 <c>Version</c>。唯一性让"退订一个句柄"能精确定位到一条订阅——
    /// 而按阶段重置的话，同一个阶段上后来的订阅会与先前的撞号，退订就会误伤。
    /// </para>
    /// </summary>
    private static int subscriptionToken;

    /// <summary>累计派发次数（按阶段分）。诊断用。</summary>
    private static readonly StringBuilder dump = new();

    /// <summary>
    /// 订阅一个阶段的<b>实例</b>回调。总线对它只持弱引用：目标被回收后订阅自动失效。
    /// <para>
    /// 回调是无参实例方法，状态从 <c>this</c> 上取。派发走的是订阅时建好的<b>开放委托</b>
    /// （实例当第一个参数传进去），所以既不在派发路径上分配，也不会把订阅者强引用住。
    /// </para>
    /// </summary>
    /// <typeparam name="T">订阅者类型。用具体类型才能在诊断 dump 里看出是谁在监听。</typeparam>
    /// <param name="phase">要订阅的阶段。</param>
    /// <param name="callback">无参实例方法组，例如 <c>probe.OnPreUpdate</c>。
    /// <b>别传 lambda</b>：它的绑定目标是编译器生成的闭包而不是订阅者，会被当成永活条目，
    /// 那个实例就再也收不回来了（这种情况下会记一条警告）。</param>
    /// <returns>取消订阅用的句柄；也可以 <c>using</c> 它（离开作用域即退订）。</returns>
    public static MonoEventSubscription Subscribe<T>(MonoFramePhase phase, Action callback) where T : class
    {
        ArgumentNullException.ThrowIfNull(callback);
        int index = IndexOf(phase);
        int token = ++subscriptionToken;

        // 实例方法：弱持有目标。这里必须造**开放**委托——实例当第一个参数传，委托本身只持有 MethodInfo。
        // 存绑定到目标的委托（closed delegate，或捕获它的闭包）会把订阅者强引用住，
        // WeakReference 于是永远不会失效，整套"弱订阅"就成了摆设。
        if (callback.Target is T target)
        {
            Action<T> open = callback.Method.CreateDelegate<Action<T>>(null);
            Action<object> invoker = instance => open((T)instance);
            slots[index].Add(new Entry
            {
                Target = new WeakReference<object>(target),
                Invoker = invoker,
                Method = callback.Method,
                Alive = true,
                Token = token
            });
            return new MonoEventSubscription(phase, token);
        }

        // 走到这一支的有两种：静态方法（Target 为 null），以及绑定目标不是 T 的委托。
        // 后者最常见的是 lambda——无论捕不捕获，绑定目标都是编译器生成的类型（闭包或 <>c 单例）。
        // 两种情况都只能注册成"永活"条目：不弱引用，必须显式退订。
        if (callback.Target is not null)
        {
            MonoLog.Write(MonoLogLevel.Warn,
                $"MonoEventBus.Subscribe<{typeof(T).Name}> 拿到的委托，绑定目标不是 {typeof(T).Name}"
                + $"（实际是 {callback.Target.GetType().Name}）。它会退化成「永活」条目：不参与弱引用回收，"
                + "必须显式退订。实例回调请传方法组，静态回调用 SubscribeStatic。");
        }

        // 静态方法：没有目标可以被回收，注册成"永活"条目。包装只是为了让形状对上 Entry.Invoker。
        void staticInvoker(object o) => callback();
        slots[index].Add(new Entry
        {
            Target = null,
            Invoker = staticInvoker,
            Method = callback.Method,
            Alive = true,
            Token = token
        });
        return new MonoEventSubscription(phase, token);
    }

    /// <summary>
    /// 订阅一个<b>静态</b>回调（目标就是"没有目标"，所以永远活着，必须显式退订）。
    /// <para>
    /// <b>为什么它必须叫 <c>SubscribeStatic</c> 而不是再叫一个 <c>Subscribe</c> 重载。</b>
    /// 这是实测踩出来的一个非常隐蔽的坑：如果这里叫 <c>Subscribe(phase, Action&lt;object&gt;)</c>，
    /// 那么<b>任何捕获了实例的 lambda</b>（<c>Subscribe(phase, _ =&gt; OnTick())</c>）都会
    /// <b>优先选中这个重载</b>而不是泛型的 <c>Subscribe&lt;T&gt;</c>——
    /// 于是它被当成"传了实例方法"而<b>拒绝注册</b>。症状是"订阅了但回调一次都不跑"，
    /// 而且看起来完全正常。用不同的方法名把这个歧义消灭掉。
    /// </para>
    /// <para>
    /// 它比泛型重载多一件泛型做不到的事：<b>重复订阅会被挡掉</b>。静态方法转换来的委托由 CLR
    /// 缓存，同一个静态方法每次都是同一个对象；而泛型重载拿到的委托一旦来自 lambda 就是每次全新对象。
    /// </para>
    /// <para>
    /// 不挡重复的后果是具体的：同一个回调被挂两次，一个事件触发两次；退订一次之后它还活着，
    /// 症状是"退订了但还在跑"。
    /// </para>
    /// </summary>
    /// <param name="phase">要订阅的阶段。</param>
    /// <param name="callback">静态回调（静态方法组或静态 lambda 都可以）。</param>
    /// <returns>取消订阅用的句柄；<b>已经订阅过同一个回调时返回空句柄（<c>Version == 0</c>）</b>。</returns>
    public static MonoEventSubscription SubscribeStatic(MonoFramePhase phase, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        int index = IndexOf(phase);
        List<Entry> list = slots[index];

        if (ContainsCallback(list, callback))
        {
            MonoLog.Warn(MonoLogLevel.Warn, $"同一个回调在 {phase} 阶段被重复订阅，已忽略第二次（否则事件会触发两次）。");
            return default;
        }

        int token = ++subscriptionToken;

        // 包装只是为了让形状对上 Entry.Invoker；静态方法不持有实例，这层闭包钉不住任何东西。
        void staticInvoker(object o) => callback();
        list.Add(new Entry
        {
            // 显式写 null：语义是"这条不参与弱引用回收"，而不是"它是静态方法"。
            Target = null,
            Invoker = staticInvoker,
            Method = callback.Method,
            Alive = true,
            Token = token
        });
        return new MonoEventSubscription(phase, token);
    }

    /// <summary>
    /// 按委托身份查重。
    /// <para>
    /// <b>只比较委托本身，不看 <c>Target</c>。</b>原来的判据是"<c>Target is null</c> 才算静态条目"，
    /// 那是错的：静态 lambda 的 <c>Target</c> 是一个编译器生成的<b>单例</b>（不是 null），
    /// 于是真正该被查重的静态回调反而查不出来。
    /// </para>
    /// <para>
    /// <b>而且不能比委托身份</b>：<see cref="Entry.Invoker"/> 里存的是包装过的委托，身份与传进来的对不上。
    /// 现在比的是<b>方法</b>，并且只看"不弱持有目标"的那些条目——同一个方法挂在两个不同实例上是合法的两件事。
    /// </para>
    /// <para>
    /// 方法身份对同一个静态方法组、同一处静态 lambda 都可靠，所以"同一个回调订阅两次"挡得住；
    /// 对每次新写的 lambda 不可靠——那时的"查不出重复"与"这确实是两个不同的委托"是一致的，没有副作用。
    /// </para>
    /// </summary>
    private static bool ContainsCallback(List<Entry> list, Action callback)
    {
        MethodInfo method = callback.Method;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Alive && list[i].Target is null && list[i].Method == method)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 退订。传入 <see cref="Subscribe{T}"/> / <see cref="SubscribeStatic"/> 返回的句柄；重复退订是安全的空操作。
    /// <para>
    /// 匹配依据是<b>全库唯一的订阅序号</b>（句柄的 <c>Version</c>），不是按阶段的版本号——
    /// 后者会让"同一个阶段上后来的订阅"与先前撞号，退订就会误伤别的订阅者。
    /// </para>
    /// <para>
    /// <b>它绝不在派发过程中真的从列表里删元素。</b>这是个实测抓到的真 bug：派发用的是
    /// "按下标反向遍历"，而 <c>List.RemoveAt(i)</c> 会把 <c>i+1</c> 之后的元素整体前移——
    /// 反向遍历已经走过那些下标了，于是<b>紧挨着被删条目的那一个订阅者会被跳过</b>，
    /// 症状是"某个回调偶尔不执行"，极难归因。
    /// </para>
    /// <para>
    /// 所以退订只把条目置为无效（<c>Alive = false</c>，派发时跳过），真正的删除留给
    /// <see cref="Dispatch"/> 收尾的 <see cref="Compact"/>——那里已经确认不在遍历中。
    /// </para>
    /// </summary>
    /// <param name="subscription">订阅句柄。</param>
    /// <returns>确实退掉了一个订阅才返回 true。</returns>
    public static bool Unsubscribe(MonoEventSubscription subscription)
    {
        int index = IndexOf(subscription.Phase);
        if (index < 0 || subscription.Version <= 0)
            return false;

        List<Entry> list = slots[index];
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].Alive || list[i].Token != subscription.Version)
                continue;

            Entry entry = list[i];
            entry.Alive = false;
            entry.Invoker = null;
            list[i] = entry;

            // 派发中退订：本轮遍历会跳过它，等派发结束再压缩。
            // 不在派发中（dispatchDepth == 0）也不必立刻压缩——交给下一次派发或 Clear()，逻辑只有一条路径。
            pendingCompaction[index] = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 派发一个阶段。
    /// <para>
    /// 反向遍历：订阅者在自己回调里退订（<c>RemoveAt</c> 只把后面的元素前移）不会让前面的项被跳过。
    /// 死掉的弱引用<b>不在遍历中移除</b>——那会让下标语义变复杂，改在派发结束后一次性压缩。
    /// </para>
    /// <para>
    /// <b>订阅者抛出的异常会向外传播</b>：这是刻意的——静默吞掉异常等于把 bug 藏起来。
    /// 基础设施自己的订阅（见 <see cref="MonoEventBusSystem"/>）各自带 try/catch，不会因为一个消费者炸掉而停摆。
    /// </para>
    /// </summary>
    /// <param name="phase">要派发的阶段。</param>
    public static void Dispatch(MonoFramePhase phase)
    {
        int index = IndexOf(phase);
        if (index < 0)
            return;

        dispatchCounts[index]++;

        List<Entry> list = slots[index];
        if (list.Count == 0)
            return;

        dispatchDepth[index]++;
        try
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Entry entry = list[i];

                // 已退订（本轮或此前）就直接跳过。索引稳定——退订不删元素，只置 Alive=false。
                if (!entry.Alive)
                    continue;

                if (entry.Target is null)
                {
                    entry.Invoker!(null!);
                    continue;
                }

                if (entry.Target.TryGetTarget(out object? target))
                    entry.Invoker!(target);
            }
        }
        finally
        {
            dispatchDepth[index]--;
            if (dispatchDepth[index] == 0 && (pendingCompaction[index] || HasDeadEntries(list)))
                Compact(index);
        }
    }

    /// <summary>清空全部订阅。模组卸载时调用。</summary>
    public static void Clear()
    {
        for (int i = 0; i < PhaseCount; i++)
        {
            slots[i].Clear();
            dispatchDepth[i] = 0;
            pendingCompaction[i] = false;
        }
    }

    /// <summary>只清一个阶段。世界卸载这类"只该清一部分"的场景用。</summary>
    /// <param name="phase">要清空的阶段。</param>
    public static void Clear(MonoFramePhase phase)
    {
        int index = IndexOf(phase);
        if (index < 0)
            return;
        slots[index].Clear();
        dispatchDepth[index] = 0;
        pendingCompaction[index] = false;
    }

    /// <summary>某个阶段当前的活订阅数（含尚未被压缩掉的死条目）。</summary>
    /// <param name="phase">要查的阶段。</param>
    public static int SubscriberCount(MonoFramePhase phase)
    {
        int index = IndexOf(phase);
        if (index < 0)
            return 0;

        int alive = 0;
        List<Entry> list = slots[index];
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].IsAlive)
                alive++;
        }
        return alive;
    }

    /// <summary>全部阶段的活订阅总数。</summary>
    public static int TotalSubscribers
    {
        get
        {
            int total = 0;
            for (int i = 0; i < PhaseCount; i++)
                total += SubscriberCount((MonoFramePhase)i);
            return total;
        }
    }

    /// <summary>
    /// 只读的订阅者 dump——诊断时能看到"谁在监听什么"。
    /// <para>
    /// <b>它会列出全部 11 个阶段，包括"一个订阅者都没有"的那些</b>，并且每个都带派发次数。
    /// 这是刻意的：<c>MonoEventBusSystem</c> 的接线是否真的在跑，唯一的证据就是"派发次数在涨"——
    /// 只列非空阶段的话，一个"订阅者为零"的总线看起来和"钩子根本没挂上"一模一样。
    /// </para>
    /// <para>
    /// <b>这个方法会分配</b>（要拼字符串、要遍历），只该在命令里调，不要在每帧路径上用。
    /// </para>
    /// </summary>
    public static string Describe()
    {
        dump.Clear();
        dump.Append("事件总线：").Append(TotalSubscribers).Append(" 个活订阅，11 个阶段的派发次数：");

        for (int i = 0; i < PhaseCount; i++)
        {
            dump.Append("\n  ").Append((MonoFramePhase)i)
                .Append("  订阅 ").Append(SubscriberCount((MonoFramePhase)i))
                .Append("，派发 ").Append(dispatchCounts[i]);

            List<Entry> list = slots[i];
            for (int j = 0; j < list.Count; j++)
            {
                Entry entry = list[j];
                dump.Append("\n    ").Append(entry.IsAlive ? "· " : "× ");

                if (entry.Target is null)
                    dump.Append("(不弱持有) ");
                else if (entry.Target.TryGetTarget(out object? target))
                    dump.Append(target.GetType().Name).Append('.');
                else
                    dump.Append("(已回收) ");

                dump.Append(entry.Method?.DeclaringType?.Name).Append('.').Append(entry.Method?.Name ?? "(未知)");
            }
        }
        return dump.ToString();
    }

    /// <summary>某个阶段累计被派发了多少次。游戏内自检用它证明"这批钩子真的在跑"，而不必先有订阅者。</summary>
    /// <param name="phase">要查的阶段。</param>
    public static long DispatchCount(MonoFramePhase phase)
    {
        int index = IndexOf(phase);
        return index < 0 ? 0 : dispatchCounts[index];
    }


    private static void Compact(int index)
    {
        List<Entry> list = slots[index];
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (!list[i].IsAlive)
                list.RemoveAt(i);
        }
        pendingCompaction[index] = false;
    }

    private static bool HasDeadEntries(List<Entry> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (!list[i].IsAlive)
                return true;
        }
        return false;
    }

    private static int IndexOf(MonoFramePhase phase)
    {
        int index = (int)phase;
        return (uint)index < PhaseCount ? index : -1;
    }

    private static List<Entry>[] CreateSlots()
    {
        List<Entry>[] result = new List<Entry>[PhaseCount];
        for (int i = 0; i < PhaseCount; i++)
            result[i] = [];
        return result;
    }
}

/// <summary>一次订阅的句柄。可以显式 <see cref="MonoEventBus.Unsubscribe"/>，也可以 <c>using</c> 它自动退订。</summary>
public readonly struct MonoEventSubscription : IDisposable, IEquatable<MonoEventSubscription>
{
    /// <summary>订阅的阶段。</summary>
    public MonoFramePhase Phase { get; }

    /// <summary>订阅版本的内部值。<b>仅供 <see cref="MonoEventBus"/> 使用</b>，不要自己解释它。</summary>
    public int Version { get; }

    internal MonoEventSubscription(MonoFramePhase phase, int version)
    {
        Phase = phase;
        Version = version;
    }

    /// <summary>退订。空句柄（<c>default</c>）是安全空操作。</summary>
    public void Dispose() => MonoEventBus.Unsubscribe(this);

    /// <inheritdoc/>
    public bool Equals(MonoEventSubscription other) => Phase == other.Phase && Version == other.Version;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is MonoEventSubscription other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine((int)Phase, Version);

    /// <summary>== 运算符。</summary>
    public static bool operator ==(MonoEventSubscription left, MonoEventSubscription right) => left.Equals(right);

    /// <summary>!= 运算符。</summary>
    public static bool operator !=(MonoEventSubscription left, MonoEventSubscription right) => !left.Equals(right);
}

/// <summary>
/// 把总线接到 tML 的帧钩子上。<b>这是 <c>Core</c> 层里第二个 <see cref="ModSystem"/></b>，
/// 它除了转发之外什么都不做——所有订阅都属于消费者，它自己不产生任何行为。
/// <para>
/// 事件订阅是<b>回调</b>而不是 <see cref="IMonoService"/> 的五个方法，因为消费者需要的是"每个阶段的回调"，
/// 而服务接口表达的是"一次装配的生命周期"。两者服务的目标不同，所以并存而不是合并。
/// </para>
/// <para>
/// <b>钩子是可重写方法，不是事件。</b>所以这个类没有 <c>Load</c> 里的 <c>+=</c>、也没有 <c>OnModUnload</c> 里的
/// <c>-=</c>：既然钩子由 tML 直接按虚方法分发，就不存在"挂重了"或"忘了摘"这两条泄漏路径，
/// 热重载也不再需要幂等守卫。这是本类唯一比原设计（蓝图 §4.1 的 <c>InstallHooks</c>/<c>HookRemoveAll</c>）简单的地方。
/// </para>
/// <para>
/// <see cref="MonoFramePhase.PreUpdate"/> <b>不由本类派发</b>：它要在帧起点和"推进时钟、跑上一帧排进来的
/// 帧末动作、驱动游戏内自检"按一个固定顺序做完，所以那一处归 <c>MonoFrameSystem</c>。拆成两个
/// <see cref="ModSystem"/> 之后，先后就只能靠类名去猜了。
/// </para>
/// <para>
/// <b>钩子名已在 tML 1.4.4.9 上逐个核对</b>（见 <see cref="MonoFramePhase"/> 的说明）：
/// <c>PreDrawTiles</c> / <c>PostUpdateEntities</c> 不存在，<c>PostUpdateEverything</c> 归时钟推进者所有。
/// </para>
/// </summary>
public sealed class MonoEventBusSystem : ModSystem
{
    /// <summary>
    /// tML 为这个 <see cref="ModSystem"/> 建的实例。游戏内自检需要在不依赖"订阅能不能派发"的前提下
    /// 手动泵一个阶段（<c>Custom</c> 本来就只由调用方泵），所以把实例留一个静态引用。
    /// <para>它只在世界加载之后非 null——<c>ModSystem</c> 的世界生命周期从这里开始。</para>
    /// </summary>
    public static MonoEventBusSystem? Instance { get; private set; }

    /// <inheritdoc/>
    public override void OnWorldLoad() => Instance = this;

    /// <inheritdoc/>
    public override void OnWorldUnload()
    {
        Instance = null;
        MonoEventBus.Dispatch(MonoFramePhase.WorldUnload);
    }

    /// <inheritdoc/>
    public override void OnModUnload()
    {
        Instance = null;
        MonoEventBus.Clear();
    }

    /// <summary>
    /// 本帧的实体/弹幕/物品/时间/世界/尘埃都更新完之后，闸门内的最后一个更新后时机。
    /// <para><b>它一次派发两个阶段</b>：<see cref="MonoFramePhase.WorldUpdate"/>（"世界更新完了"）
    /// 与 <see cref="MonoFramePhase.PostUpdate"/> / <see cref="MonoFramePhase.PreDraw"/>（"接下来该画了"）。
    /// 三个阶段的时基相同，顺序就是枚举顺序——这样订阅者只关心语义，不必知道它们挤在同一个钩子上。
    /// </para>
    /// </summary>
    public override void PostUpdateDusts()
    {
        MonoEventBus.Dispatch(MonoFramePhase.WorldUpdate);
        MonoEventBus.Dispatch(MonoFramePhase.PostUpdate);
        MonoEventBus.Dispatch(MonoFramePhase.PreDraw);
    }

    /// <summary>NPC 数组已经改完之后。<b>每帧遍历 NPC 的正确挂点。</b></summary>
    public override void PostUpdateNPCs() => MonoEventBus.Dispatch(MonoFramePhase.PostUpdateNPCs);

    /// <summary>玩家数组已经改完之后。</summary>
    public override void PostUpdatePlayers() => MonoEventBus.Dispatch(MonoFramePhase.PostUpdatePlayers);

    /// <summary>弹幕数组已经改完之后。</summary>
    public override void PostUpdateProjectiles() => MonoEventBus.Dispatch(MonoFramePhase.PostUpdateProjectiles);

    /// <summary>
    /// 世界与世界内界面画完之后。<b>专用服务器上不触发</b>——tML 的绘制钩子本身就不会在服务端被调，
    /// 所以这里不需要 <c>Main.dedServ</c> 判断；多写一个判断只会多一条永远为真的分支。
    /// </summary>
    public override void PostDrawTiles() => MonoEventBus.Dispatch(MonoFramePhase.PostDraw);

    /// <summary>界面画完之后，拿到的是游戏已经 <c>Begin</c> 好的界面批次。<b>专用服务器上不触发</b>。</summary>
    public override void PostDrawInterface(SpriteBatch spriteBatch) => MonoEventBus.Dispatch(MonoFramePhase.PostDrawInterface);
}

