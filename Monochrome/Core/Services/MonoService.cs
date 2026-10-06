using System.Text;

namespace Monochrome.Core.Services;

/// <summary>
/// 一个 Monochrome 子系统。生命周期是<b>显式五阶段</b>，取代"往 <see cref="ModSystem"/> 里塞静态状态"的做法。
/// <para>
/// 阶段顺序固定，不可自定义：
/// <c>Load → RegisterDefs → PostSetup → InstallHooks → Ready</c>，
/// 反向退场时是 <c>Unload</c>（再回到调用方的 <c>DisposeBuffers</c>）。
/// 由 <see cref="MonoServiceHost"/> 统一驱动，同一次加载里每个阶段只会被调用一次。
/// </para>
/// <para>
/// <b>实现必须是可无参构造的类</b>（<see cref="MonoServiceHost.Register{T}"/> 用 <c>new()</c> 约束实例化，
/// 自动注册也用公开的无参构造）。构造函数里不要碰世界、玩家、图形设备——那是 <see cref="Load"/> 之后的事。
/// </para>
/// <para>
/// <b>只有 <see cref="Order"/> 与 <see cref="Name"/> 必须实现</b>，其余成员都有默认的空实现；
/// 没被显式注册的实现类会在 <c>PostSetupContent</c> 被自动扫到并注册（<see cref="AutoRegister"/>）。
/// </para>
/// </summary>
public interface IMonoService
{
    /// <summary>
    /// 是否让 <see cref="MonoServiceHost"/> 在加载期自动注册这个服务。默认 <c>true</c>。
    /// <para>
    /// 它是<b>静态</b>成员：判断发生在实例存在之前，扫描时先读它、不满足就直接跳过。
    /// 共享用的中间基类、只当替身用的测试类请标 <c>abstract</c>；确实不想被自动注册就写 <c>false</c>，
    /// 再自己找地方 <see cref="MonoServiceHost.Register{T}"/>。
    /// </para>
    /// </summary>
    static virtual bool AutoRegister => true;

    /// <summary>
    /// 排序权重，<b>越小越先初始化</b>；同序按注册顺序。
    /// <para>
    /// 惯例号段：<c>0–99</c> 基础设施（日志、时钟）、<c>100–299</c> 数据与注册表、
    /// <c>300–599</c> 玩法系统、<c>600–899</c> 表现系统、<c>900+</c> 诊断与收尾。
    /// </para>
    /// </summary>
    int Order { get; }

    /// <summary>诊断里显示的短名字。默认用类型名即可，实现里通常写 <c>=> nameof(MyService);</c>。</summary>
    string Name { get; }

    /// <summary>建立自己的运行时状态。<b>不要在这里读世界数据</b>：此时世界可能还没加载，内容也还没注册完。</summary>
    void Load()
    {
    }

    /// <summary>只注册数据（Def / 表 / 规则），不产生运行时状态。需要 tML 内容注册表时在这里做。</summary>
    void RegisterDefs()
    {
    }

    /// <summary>内容注册完成之后的装配：解析跨系统引用、预热缓存。对应 tML 的 <c>PostSetupContent</c>。</summary>
    void PostSetup()
    {
    }

    /// <summary>
    /// 只挂<b>本服务自己</b>的钩子。库绝不代消费者挂钩子——tML 的 detour 归属按"钩子委托的宿主程序集"判定，
    /// 库代挂会让卸载清理错乱（见蓝图 §18.1）。消费者要挂钩子请用 <c>MonoUtil.AddHooks</c>，宿主是消费者自己。
    /// </summary>
    void InstallHooks()
    {
    }

    /// <summary>所有服务都 <see cref="InstallHooks"/> 完之后调用一次：此时可以安全地假定别人已经就位。</summary>
    void Ready()
    {
    }

    /// <summary>
    /// 卸载。<b>必须与 <see cref="Load"/> 严格对称</b>，漏一条就是热重载泄漏。
    /// 由 <see cref="MonoServiceHost"/> 按注册的逆序调用。
    /// </summary>
    void Unload()
    {
    }

    /// <summary>释放内部缓冲/池（可选）。在所有服务 <see cref="Unload"/> 之后调用，也可以留空实现。</summary>
    void DisposeBuffers()
    {
    }
}

/// <summary>
/// 子系统注册表与生命周期驱动器。
/// <para>
/// 注册表是静态的，但<b>整个进程中只有一个实例在驱动它</b>——这就是"避免 tML 的 ModSystem 与静态状态互相踩踏"
/// 的全部手法：顺序在一处、状态在一处、断言在一处。
/// </para>
/// <para>
/// 它<b>不碰任何帧钩子</b>：帧上的事归 <c>MonoFrameSystem</c> / <c>MonoEventBusSystem</c> / <c>MonoSchedulerSystem</c>。
/// 四者职责不重叠，这也是"哪个 ModSystem 该干什么"唯一需要记住的一句话。
/// </para>
/// <para>
/// 消费者在 <c>Mod.Load</c> 里注册：<c>MonoServiceHost.Register&lt;MyService&gt;();</c>。
/// </para>
/// </summary>
public sealed class MonoServiceHost : ModSystem
{
    /// <summary>生命周期的当前阶段。<see cref="MonoServiceHost.Load()"/> 期间只允许注册，<see cref="MonoServiceHost.OnModUnload"/> 之后再注册会被拒绝。</summary>
    private enum ServicePhase
    {
        Created,
        Loading,
        Loaded,
        Unloading,
        Unloaded
    }

    /// <summary>按 <see cref="IMonoService.Order"/> 排好序的服务表。注册顺序决定同 Order 的先后（见 <see cref="sequence"/>）。</summary>
    private static readonly List<IMonoService> services = [];

    /// <summary>
    /// 注册序号。用它做同 Order 的次级排序键，<b>而不是用 <c>List.IndexOf</c></b>——
    /// 排序比较器里再去查一次正在被排序的那个列表，是给自己埋一个"比较器不一致即抛
    /// InvalidOperationException"的雷。服务是单例，用引用相等当键足够。
    /// </summary>
    private static readonly Dictionary<IMonoService, int> sequence = [];

    private static ServicePhase phase = ServicePhase.Created;
    private static int registrationCounter;

    /// <summary>
    /// 被拒绝的注册次数（在 <see cref="Load"/> 阶段之外调 <see cref="Register{T}"/> / <see cref="RegisterInstance"/>）。
    /// <para>
    /// 它是给游戏内自检用的：拒绝这件事只留下一条日志，而<b>日志环形缓冲会在世界卸载时被清掉</b>，
    /// 所以"晚注册真的被拦住了"没法靠事后翻日志证明。这个计数器让它可以被断言。
    /// 正常情况下它<b>恒为 0</b>——非 0 就说明有东西在加载期之外注册服务。
    /// </para>
    /// </summary>
    public static int LateRegistrationRejected { get; private set; }

    /// <summary>自动注册扫描见过的候选类型数（实现了 <see cref="IMonoService"/> 的具体类）。<b>恒为 0 就说明扫描没真的枚举到东西。</b></summary>
    public static int AutoRegisterCandidates { get; private set; }

    /// <summary>自动注册扫描累计新增的服务数。已经显式注册过的类型不会被重复构造，所以这个数通常小于候选数。</summary>
    public static int AutoRegistered { get; private set; }

    /// <summary>重入守卫：tML 热重载时同一个实例的 <c>ModSystem.Load</c> 可能被调用多次（见 <see cref="MonoServiceHost.Load()"/>）。</summary>
    private bool loaded;

    /// <summary>已注册的服务数量。</summary>
    public static int Count => services.Count;

    /// <summary>当前是否处于 <see cref="IMonoService.Ready"/> 之后的稳态。诊断与断言用。</summary>
    public static bool IsReady => phase == ServicePhase.Loaded;

    /// <summary>
    /// 注册一个子系统。只允许在 <c>Mod.Load</c>（即本类的 <see cref="Load"/> 阶段）调用。
    /// <para>
    /// 在别处注册会被<b>拒绝并记日志</b>，而不是静默接受：晚注册的服务会错过 <c>PostSetup</c> 与
    /// <c>InstallHooks</c>，那类 bug 表现为"功能随机失效"，是这里最该提前拦住的一种。
    /// </para>
    /// </summary>
    /// <typeparam name="T">服务类型，必须可无参构造。</typeparam>
    /// <returns>实例化好的服务实例，方便调用方自己留一份引用；<b>注册被拒绝时返回 null</b>（已经记了日志）。</returns>
    public static T Register<T>() where T : class, IMonoService, new()
    {
        if (phase != ServicePhase.Loading)
        {
            LateRegistrationRejected++;
            MonoLog.Error(MonoLogLevel.Error, $"MonoServiceHost.Register<{typeof(T).Name}()> 被拒绝：注册只允许在 Mod.Load 阶段进行，当前阶段是 {phase}。"
                          + $"晚注册的服务会错过 PostSetup / InstallHooks。");
            return null!;
        }

        T service = new();
        RegisterInstance(service);
        return service;
    }

    /// <summary>
    /// 注册一个已经建好的实例（服务需要构造参数时用这个）。
    /// <para>
    /// <b>不要</b>用它在加载期之外补注册；理由与 <see cref="Register{T}"/> 相同。
    /// 这个重载存在的唯一理由是给"需要注入配置"的服务一条路。
    /// </para>
    /// </summary>
    /// <param name="service">要注册的实例。</param>
    public static void RegisterInstance(IMonoService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (phase != ServicePhase.Loading)
        {
            LateRegistrationRejected++;
            MonoLog.Error(MonoLogLevel.Error, $"MonoServiceHost.RegisterInstance({service.GetType().Name}) 被拒绝：注册只允许在 Mod.Load 阶段进行，当前阶段是 {phase}。");
            return;
        }

        for (int i = 0; i < services.Count; i++)
        {
            if (ReferenceEquals(services[i], service))
            {
                MonoLog.Warn(MonoLogLevel.Warn, $"服务 {service.Name} 被重复注册，已忽略。");
                return;
            }
        }

        services.Add(service);
        sequence[service] = registrationCounter++;
        services.Sort(static (a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : KeyOf(a).CompareTo(KeyOf(b)));
    }

    /// <summary>按类型取一个已注册的服务。不在注册表里返回 null——查不到不是异常，是"这个系统没装"。</summary>
    /// <typeparam name="T">服务类型。</typeparam>
    public static T? Get<T>() where T : class, IMonoService
    {
        for (int i = 0; i < services.Count; i++)
        {
            if (services[i] is T match)
                return match;
        }
        return null;
    }

    /// <summary>
    /// 取一个<b>必须存在</b>的服务，不在注册表里就抛异常。
    /// <para>
    /// 服务之间互相引用时用这个：写错名字或忘了注册会让它当场炸在启动期，而不是在某个功能被用到时才空引用。
    /// </para>
    /// </summary>
    /// <typeparam name="T">服务类型。</typeparam>
    public static T Require<T>() where T : class, IMonoService
        => Get<T>() ?? throw new InvalidOperationException(
            $"服务 {typeof(T).Name} 没有注册。检查它有没有在某个 Mod.Load 里调 MonoServiceHost.Register<{typeof(T).Name}>()。");

    /// <summary>已注册服务的只读视图，顺序就是实际初始化顺序。诊断用。</summary>
    public static IReadOnlyList<IMonoService> All => services;

    /// <summary>列出注册表与当前阶段。给 <c>/mono services</c> 之类命令用。</summary>
    public static string Describe()
    {
        StringBuilder output = new();
        output.Append("Monochrome 子系统 ").Append(services.Count).Append(" 个，阶段 ").Append(phase);
        if (services.Count == 0)
            return output.ToString();

        output.Append("（初始化顺序 = 下面这个顺序，退场是它的逆序）：");
        for (int i = 0; i < services.Count; i++)
        {
            IMonoService service = services[i];
            output.Append("\n  #").Append(i.ToString("D2"))
                  .Append("  order=").Append(service.Order.ToString("D4"))
                  .Append("  ").Append(service.Name)
                  .Append("  (").Append(service.GetType().Name).Append(')');
        }
        return output.ToString();
    }

    /// <inheritdoc/>
    public override void Load()
    {
        // 幂等：tML 的热重载路径下 Load 可能被再次调用，而静态表在两次之间并不会被清空。
        // 没有这道守卫，二次加载会把同一批服务再注册一遍（然后每个钩子挂两次）。
        if (loaded)
        {
            MonoLog.Warn(MonoLogLevel.Warn, $"MonoServiceHost.Load 被重复调用，已忽略第二次（热重载路径）。");
            return;
        }
        loaded = true;

        phase = ServicePhase.Loading;
        registrationCounter = 0;
    }

    /// <inheritdoc/>
    public override void PostSetupContent()
    {
        if (phase != ServicePhase.Loading)
            return;

        // 自动注册必须趁注册窗口还开着做。扫描点也只能在这里：MonoServiceHost.Load 跑的时候
        // 别的模组程序集还没加载完，那时扫不到东西。
        AutoRegisterServices();

        phase = ServicePhase.Loaded;
        RunStage("Load", static service => service.Load());
        RunStage("RegisterDefs", static service => service.RegisterDefs());
        RunStage("PostSetup", static service => service.PostSetup());
        RunStage("InstallHooks", static service => service.InstallHooks());
        RunStage("Ready", static service => service.Ready());

        MonoLog.Debug(MonoLogLevel.Debug, $"子系统装配完成，共 {services.Count} 个：{NamesOf(services)}");
    }

    /// <summary>
    /// 扫一遍所有已加载模组的程序集，把实现了 <see cref="IMonoService"/>、类型可实例化、且
    /// <see cref="IMonoService.AutoRegister"/> 为真的类型注册进来。
    /// <para>
    /// 显式注册过的类型会跳过——扫描会构造一个新实例，所以去重必须按<b>类型</b>，按引用挡不住。
    /// 抽象类、开放泛型、没有公开无参构造的类型注册不了，各自有一条日志说明原因。
    /// </para>
    /// </summary>
    private static void AutoRegisterServices()
    {
        int added = 0;
        int existing = 0;
        int failed = 0;
        int candidates = 0;

        foreach (Mod mod in ModLoader.Mods)
        {
            Assembly? code = mod.Code;
            if (code is null)
                continue;

            foreach (Type type in TypesOf(code))
            {
                if (type == typeof(IMonoService)
                    || type.IsAbstract
                    || type.IsGenericTypeDefinition
                    || !typeof(IMonoService).IsAssignableFrom(type))
                    continue;

                candidates++;

                if (Contains(type))
                {
                    existing++;
                    continue;
                }

                // ModType（ModSystem / ModProjectile / ModNPC / ModItem …）不能自动构造：tML 已经为它建了
                // 自己的实例，其中 ModProjectile / ModNPC 这类还是<b>每实体一个</b>；这里再 new 一个，
                // 同一个类型就会同时存在两个实例。已经显式注册过的不受影响——上面那条 Contains 已经放行。
                if (typeof(ModType).IsAssignableFrom(type))
                {
                    failed++;
                    MonoLog.Warn(MonoLogLevel.Warn, $"{type.FullName} 是 ModType，不能自动注册：tML 已经为它建了自己的实例"
                                                    + $"（ModProjectile / ModNPC 这类还是每实体一个），再 new 一个会让同一个类型出现两个实例。"
                                                    + $"确实要把它当服务，请用 RegisterInstance 传入那个真实实例。");
                    continue;
                }

                if (!WantsAutoRegister(type))
                    continue;

                if (type.GetConstructor(Type.EmptyTypes) is null)
                {
                    failed++;
                    MonoLog.Warn(MonoLogLevel.Warn, $"{type.FullName} 实现了 IMonoService 但没有公开的无参构造，"
                                                    + $"自动注册跳过它；需要它运行请用 RegisterInstance 显式注册。");
                    continue;
                }

                try
                {
                    RegisterInstance((IMonoService)Activator.CreateInstance(type)!);
                    added++;
                }
                catch (Exception exception)
                {
                    failed++;
                    MonoLog.Error(MonoLogLevel.Error, $"{type.FullName} 自动注册失败，已跳过：{exception}");
                }
            }
        }

        AutoRegisterCandidates += candidates;
        AutoRegistered += added;

        MonoLog.Info(MonoLogLevel.Info, $"子系统自动注册：扫描到 {candidates} 个候选类型，新增 {added} 个，"
                                        + $"{existing} 个已经显式注册过，失败 {failed} 个。");
    }

    /// <summary>取一个程序集里的全部类型。加载失败的那些跳过——签名引用了未解析程序集时 <c>GetTypes</c> 会整体抛。</summary>
    /// <param name="assembly">要枚举的程序集。</param>
    private static List<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return [.. assembly.GetTypes()];
        }
        catch (ReflectionTypeLoadException exception)
        {
            // 部分类型加载失败时，Types 里其余的类型仍然是好的，把它们用起来。
            List<Type> loaded = [];
            foreach (Type? type in exception.Types)
            {
                if (type is not null)
                    loaded.Add(type);
            }
            return loaded;
        }
        catch (Exception exception)
        {
            MonoLog.Warn(MonoLogLevel.Warn, $"{assembly.GetName().Name} 的类型枚举失败，自动注册跳过程序集：{exception.Message}");
            return [];
        }
    }

    /// <summary>注册表里是不是已经有这个类型的实例。</summary>
    /// <param name="type">要查的类型。</param>
    private static bool Contains(Type type)
    {
        for (int i = 0; i < services.Count; i++)
        {
            if (services[i].GetType() == type)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 读一个类型的 <see cref="IMonoService.AutoRegister"/>。
    /// <para>
    /// 静态虚接口成员只能经由泛型约束调用，所以这里绕一层泛型探针。读失败按"不自动注册"处理并记一条警告——
    /// 自动注册少一个总比加载崩掉好。
    /// </para>
    /// </summary>
    /// <param name="type">要读的类型，调用方已确认它实现了 <see cref="IMonoService"/>。</param>
    private static bool WantsAutoRegister(Type type)
    {
        try
        {
            return (bool)autoRegisterProbe.MakeGenericMethod(type).Invoke(null, null)!;
        }
        catch (Exception exception)
        {
            MonoLog.Warn(MonoLogLevel.Warn, $"{type.FullName} 的 AutoRegister 读取失败，按不自动注册处理：{exception.Message}");
            return false;
        }
    }

    /// <summary>泛型探针：它让 <c>T.AutoRegister</c> 这一次静态虚调用能针对任意类型发生。</summary>
    private static bool ReadAutoRegister<T>() where T : IMonoService => T.AutoRegister;

    private static readonly MethodInfo autoRegisterProbe =
        typeof(MonoServiceHost).GetMethod(nameof(ReadAutoRegister), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <inheritdoc/>
    public override void OnModUnload()
    {
        // 逆序退场：后建的先拆，避免"拆基础设施时上层还在用它"。
        for (int i = services.Count - 1; i >= 0; i--)
        {
            IMonoService service = services[i];
            try
            {
                service.Unload();
            }
            catch (Exception exception)
            {
                MonoLog.Error(MonoLogLevel.Error, $"服务 {service.Name} 的 Unload 抛异常：{exception}");
            }
        }

        for (int i = services.Count - 1; i >= 0; i--)
        {
            try
            {
                services[i].DisposeBuffers();
            }
            catch (Exception exception)
            {
                MonoLog.Error(MonoLogLevel.Error, $"服务 {services[i].Name} 的 DisposeBuffers 抛异常：{exception}");
            }
        }

        // 诊断系统在这里断言"注册表已空"能提前抓住热重载泄漏。这条日志就是那个断言：
        // 正常情况下它必须报 0，报出非 0 就说明有人把服务表当成了自己的长期容器。
        if (services.Count != 0)
            MonoLog.Warn(MonoLogLevel.Warn, $"MonoServiceHost 退场时注册表里还有 {services.Count} 个服务条目；这通常意味着有东西往静态表里塞了不该塞的东西。");

        services.Clear();
        sequence.Clear();
        phase = ServicePhase.Unloaded;
        loaded = false;
    }

    /// <summary>按已排好的顺序跑一个阶段，单个服务炸掉不影响其余服务继续装配。</summary>
    private static void RunStage(string stage, Action<IMonoService> action)
    {
        for (int i = 0; i < services.Count; i++)
        {
            IMonoService service = services[i];
            try
            {
                action(service);
            }
            catch (Exception exception)
            {
                MonoLog.Error(MonoLogLevel.Error, $"服务 {service.Name} 的 {stage} 阶段抛异常：{exception}");
            }
        }
    }

    /// <summary>同 Order 时用注册序号定序，让初始化顺序可复现而不是"看排序算法心情"。</summary>
    private static int KeyOf(IMonoService service)
        => sequence.TryGetValue(service, out int key) ? key : int.MaxValue;

    private static string NamesOf(List<IMonoService> list)
    {
        StringBuilder output = new();
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0)
                output.Append(", ");
            output.Append(list[i].Name);
        }
        return output.ToString();
    }
}
