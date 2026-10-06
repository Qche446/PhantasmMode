using System.Runtime.CompilerServices;
using System.Text;

namespace Monochrome.Core;

/// <summary>日志级别。<see cref="Debug"/> 与 <see cref="Trace"/> 在 Release 构建里被 <see cref="MonoLog"/> 直接丢弃。</summary>
public enum MonoLogLevel
{
    /// <summary>最啰嗦的一档，用于逐帧级的排查。默认关闭。</summary>
    Trace = 0,

    /// <summary>调试信息。Debug 构建默认开启，Release 默认关闭。</summary>
    Debug = 1,

    /// <summary>常规信息，默认开启。</summary>
    Info = 2,

    /// <summary>可疑但不影响运行，默认开启。</summary>
    Warn = 3,

    /// <summary>失败。默认开启，并且会在 Release 里也保留——库出问题却不出声是最坏的一种。</summary>
    Error = 4
}

/// <summary>
/// 分级日志 + 环形缓冲（供诊断导出）。
/// <para>
/// 存在的理由只有一个：<b>库模组不允许用 <c>Console.WriteLine</c> 散落各处</b>。诊断系统需要"最近 N 条发生了什么"
/// 这一份可导出的历史，而玩家也需要一条开关把它关掉。
/// </para>
/// <para>
/// 热路径上它是零分配的：<c>MonoLog.Info($"...")</c> 的重载接收 <see cref="MonoLogInterpolatedStringHandler"/>，
/// 关闭的级别连字符串都不会拼；关闭时也不会碰环形缓冲。代价是<b>真正记下去的那一条</b>会分配一个
/// <see cref="string"/>——这是"要留下历史"的固有价格，无法避免，所以默认只记 <see cref="MonoLogLevel.Info"/> 及以上。
/// </para>
/// <para>
/// 环形缓冲<b>预分配、绝不动态增长</b>（蓝图 §15.3）：诊断本身不能改变被观测对象的性能特征。
/// </para>
/// </summary>
public static class MonoLog
{
    /// <summary>环形缓冲容量。预分配这么多槽位，满了就覆盖最旧的一条。</summary>
    public const int Capacity = 512;

    private static readonly string[] messages = new string[Capacity];
    private static readonly MonoLogLevel[] levels = new MonoLogLevel[Capacity];
    private static readonly long[] stamps = new long[Capacity];

    private static int head;
    private static int count;
    private static long written;

    /// <summary>当前的最低记录级别。低于它的调用会被丢弃，且不产生任何分配。</summary>
    public static MonoLogLevel Minimum { get; set; } =
#if DEBUG
        MonoLogLevel.Debug;
#else
        MonoLogLevel.Info;
#endif

    /// <summary>记一条最啰嗦的跟踪信息。用法：<c>MonoLog.Trace($"...")</c>；默认最低级别下它会被整个跳过（连字符串都不拼）。</summary>
    /// <param name="level">本条日志的级别；处理器拿它决定值不值得拼。</param>
    /// <param name="handler">由编译器构造的惰性插值串。</param>
    public static void Trace(MonoLogLevel level,
        [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("level")] MonoLogInterpolatedStringHandler handler)
    {
        if (handler.Enabled)
            Store(MonoLogLevel.Trace, handler.ToString());
    }

    /// <summary>写进环形缓冲的历史条数（可能小于调用次数——关闭级别与 Release 下的 Debug 都不计入）。</summary>
    public static int Count => count;

    /// <summary>累计被真正记录下来的条数（含已被环形缓冲覆盖掉的）。</summary>
    public static long Written => written;

    /// <summary>这条级别的日志会不会被记下来。给调用方做"贵重参数先算不算"的短路用。</summary>
    /// <param name="level">要问的级别。</param>
    public static bool IsEnabled(MonoLogLevel level) => level >= Minimum;

    /// <summary>记一条调试信息。用法：<c>MonoLog.Debug($"...")</c>。</summary>
    /// <param name="level">本条日志的级别；处理器拿它决定值不值得拼。</param>
    /// <param name="handler">由编译器构造的惰性插值串。</param>
    public static void Debug(MonoLogLevel level,
        [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("level")] MonoLogInterpolatedStringHandler handler)
    {
        if (handler.Enabled)
            Store(MonoLogLevel.Debug, handler.ToString());
    }

    /// <summary>记一条常规信息。用法：<c>MonoLog.Info($"...")</c>。</summary>
    /// <param name="level">本条日志的级别；处理器拿它决定值不值得拼。</param>
    /// <param name="handler">由编译器构造的惰性插值串。</param>
    public static void Info(MonoLogLevel level,
        [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("level")] MonoLogInterpolatedStringHandler handler)
    {
        if (handler.Enabled)
            Store(MonoLogLevel.Info, handler.ToString());
    }

    /// <summary>记一条警告。用法：<c>MonoLog.Warn($"...")</c>。</summary>
    /// <param name="level">本条日志的级别；处理器拿它决定值不值得拼。</param>
    /// <param name="handler">由编译器构造的惰性插值串。</param>
    public static void Warn(MonoLogLevel level,
        [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("level")] MonoLogInterpolatedStringHandler handler)
    {
        if (handler.Enabled)
            Store(MonoLogLevel.Warn, handler.ToString());
    }

    /// <summary>记一条错误。用法：<c>MonoLog.Error($"...")</c>。</summary>
    /// <param name="level">本条日志的级别；处理器拿它决定值不值得拼。</param>
    /// <param name="handler">由编译器构造的惰性插值串。</param>
    public static void Error(MonoLogLevel level,
        [System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("level")] MonoLogInterpolatedStringHandler handler)
    {
        if (handler.Enabled)
            Store(MonoLogLevel.Error, handler.ToString());
    }

    /// <summary>
    /// 记一条已经拼好的字符串。
    /// <para>
    /// <b>为什么它不叫 <c>Info(string)</c>：那样会把惰性路径整个废掉。</b>插值串同时能转成
    /// <see cref="string"/> 与处理器类型，而重载决议在两者都可用时<b>总是选 <c>string</c></b>——
    /// 于是 <c>MonoLog.Info($"...")</c> 会先无条件把插值求值成字符串，处理器变成永远走不到的死代码，
    /// "关闭的级别连字符串都不拼"这条承诺当场失效。这件事的判据在执行而不是推理：
    /// <c>Monochrome.CoreTests</c> 的「日志：插值处理器在禁用时不拼串」用例就是为它写的。
    /// </para>
    /// <para>
    /// 所以只有两个入口，各自的名字说明了它是不是惰性的：<c>Xxx($"...")</c> 惰性、<c>Write(level, s)</c> 不惰性。
    /// </para>
    /// </summary>
    /// <param name="level">级别。</param>
    /// <param name="message">已经拼好的消息。</param>
    public static void Write(MonoLogLevel level, string message) => Store(level, message);

    /// <summary>
    /// 处理器版本的统一出口。<b>这里才是真正决定"要不要拼"的地方</b>：处理器已经在构造时问过
    /// <see cref="MonoLogInterpolatedStringHandler.Enabled"/>，没有启用就整段跳过，插值里的表达式也因此不会被求值。
    /// <para>
    /// 注意 <paramref name="level"/> 是<b>从外面传进来的</b>，不是从处理器里读的：处理器对自己的级别一无所知，
    /// 它只知道"这一级够不够"。让方法决定记到哪一级，处理器决定值不值得拼，职责各自单一。
    /// </para>
    /// </summary>
    private static void Flush(ref MonoLogInterpolatedStringHandler handler, MonoLogLevel level)
    {
        if (handler.Enabled)
            Store(level, handler.ToString());
    }

    /// <summary>清空环形缓冲。世界卸载与热重载时会调。</summary>
    public static void Clear()
    {
        Array.Clear(messages);
        head = 0;
        count = 0;
        written = 0;
    }

    /// <summary>按时间顺序导出环形缓冲里的历史（最旧的在前）。每行格式：<c>[级别] 相对秒数 消息</c>。</summary>
    /// <returns>已经被覆盖的条目不会再出现。</returns>
    public static string Dump()
    {
        if (count == 0)
            return "（日志为空）";

        StringBuilder output = new(count * 48);
        int start = count == Capacity ? head : 0;
        long origin = stamps[start];
        for (int i = 0; i < count; i++)
        {
            int index = (start + i) % Capacity;
            output.Append('[').Append(TagOf(levels[index])).Append("] ")
                  .Append(((stamps[index] - origin) / (double)TimeSpan.TicksPerSecond).ToString("F3"))
                  .Append("s  ")
                  .Append(messages[index])
                  .Append('\n');
        }
        return output.ToString();
    }

    /// <summary>按级别统计条数，给诊断面板一行显示用。</summary>
    public static string Describe()
    {
        int[] perLevel = new int[5];
        for (int i = 0; i < count; i++)
            perLevel[(int)levels[i]]++;

        return $"日志 {count}/{Capacity} 条（累计 {written}），最低级别 {Minimum}，" +
               $"Debug {perLevel[(int)MonoLogLevel.Debug]} / Info {perLevel[(int)MonoLogLevel.Info]} / " +
               $"Warn {perLevel[(int)MonoLogLevel.Warn]} / Error {perLevel[(int)MonoLogLevel.Error]}";
    }

    /// <summary>真正落盘到环形缓冲。<b>唯一会因为日志而分配的地方</b>。</summary>
    /// <param name="level">级别。</param>
    /// <param name="message">已经拼好的消息。</param>
    private static void Store(MonoLogLevel level, string message)
    {
        if (level < Minimum || message.Length == 0)
            return;

        messages[head] = $"{TagOf(level)} {message}";
        levels[head] = level;
        stamps[head] = DateTime.UtcNow.Ticks;

        head = (head + 1) % Capacity;
        if (count < Capacity)
            count++;
        written++;
    }

    private static string TagOf(MonoLogLevel level) => level switch
    {
        MonoLogLevel.Trace => "TRACE",
        MonoLogLevel.Debug => "DEBUG",
        MonoLogLevel.Info => "INFO ",
        MonoLogLevel.Warn => "WARN ",
        _ => "ERROR"
    };
}

/// <summary>
/// <see cref="MonoLog"/> 的惰性插值处理器。
/// <para>
/// 它做两件事：<b>级别不够时一个 <c>Append*</c> 都不会被调用</b>（所以插值里的表达式不会被求值，更不会拼成字符串），
/// 以及把整条日志一次性拼进一个 <see cref="StringBuilder"/>（只分配最终那一个 string）。
/// </para>
/// <para>
/// <b>它是怎么知道级别够不够的</b>：<c>MonoLog</c> 的每个日志方法把 level 当普通参数收下，
/// 再用 <see cref="InterpolatedStringHandlerArgumentAttribute"/> 把它喂给本类型的构造函数——
/// 构造函数拿到 level 就能当场算出 <see cref="Enabled"/>。
/// </para>
/// <para>
/// <b>作为函数参数时不能带 <c>ref</c>。</b>这不是风格问题，是硬约束：
/// <c>[InterpolatedStringHandlerArgument]</c> 只对<b>值传递</b>的处理器参数生效。
/// 写成 <c>ref MonoLogInterpolatedStringHandler</c> 会让编译器放弃处理器转换，
/// 转而把插值串当普通参数塞进来，报的是 CS7036「缺少 formattedCount」——
/// 一个和真实原因（多写了一个 <c>ref</c>）看起来毫无关系的错误。
/// </para>
/// </summary>
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public ref struct MonoLogInterpolatedStringHandler
{
    private readonly StringBuilder builder;

    /// <summary>本条日志是否达到了最低级别。false 时所有 <c>Append*</c> 都是空操作。</summary>
    public readonly bool Enabled;

    /// <summary>由编译器调用。</summary>
    /// <param name="literalLength">字面量部分的长度提示。</param>
    /// <param name="formattedCount">插值洞的个数。</param>
    /// <param name="level">本条日志的级别（<see cref="MonoLog"/> 的各个方法带着它，编译器原样传进来）。</param>
    public MonoLogInterpolatedStringHandler(int literalLength, int formattedCount, MonoLogLevel level)
    {
        Enabled = MonoLog.IsEnabled(level);
        builder = Enabled ? new StringBuilder(literalLength + (formattedCount * 12)) : null!;
    }

    /// <summary>由编译器调用。</summary>
    /// <param name="value">字面量片段。</param>
    public void AppendLiteral(string value)
    {
        if (Enabled)
            builder.Append(value);
    }

    /// <summary>由编译器调用。</summary>
    /// <typeparam name="T">插值值的类型。</typeparam>
    /// <param name="value">插值值。</param>
    public void AppendFormatted<T>(T value)
    {
        if (Enabled)
            builder.Append(value);
    }

    /// <summary>由编译器调用（带格式串的插值洞，如 <c>{x:F2}</c>）。</summary>
    /// <typeparam name="T">插值值的类型。</typeparam>
    /// <param name="value">插值值。</param>
    /// <param name="format">格式串。</param>
    public void AppendFormatted<T>(T value, string? format)
    {
        if (Enabled)
            builder.AppendFormat("{0:" + format + "}", value);
    }

    /// <summary>
    /// 由编译器调用（只有对齐、没有格式串，例如 <c>{x,4}</c>）。
    /// <para>
    /// <b>它必须单独写一个重载，不能让 <c>format</c> 带上默认值</b>：带默认值的话
    /// 编译器会选中三参数那个 <c>(T, int, string?)</c>，然后把<b>三个</b>实参都往调用点生成，
    /// 而 <c>{x,4}</c> 根本没有格式串可传 → CS7036「缺少 format」。这是实测踩到的。
    /// </para>
    /// </summary>
    /// <typeparam name="T">插值值的类型。</typeparam>
    /// <param name="value">插值值。</param>
    /// <param name="alignment">最小宽度，负数表示左对齐。</param>
    public void AppendFormatted<T>(T value, int alignment)
    {
        if (Enabled)
            AppendAligned(value, alignment, null);
    }

    /// <summary>由编译器调用（对齐 + 格式）。</summary>
    /// <typeparam name="T">插值值的类型。</typeparam>
    /// <param name="value">插值值。</param>
    /// <param name="alignment">最小宽度，负数表示左对齐。</param>
    /// <param name="format">格式串。</param>
    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        if (Enabled)
            AppendAligned(value, alignment, format);
    }

    /// <summary>对齐与格式的实际实现。两个 <c>AppendFormatted</c> 重载都走它，逻辑只写一处。</summary>
    private void AppendAligned<T>(T value, int alignment, string? format)
    {
        string text = format is null ? value?.ToString() ?? string.Empty : string.Format("{0:" + format + "}", value);
        if (alignment == 0 || text.Length >= Math.Abs(alignment))
            builder.Append(text);
        else if (alignment > 0)
            builder.Append(text.PadLeft(alignment));
        else
            builder.Append(text.PadRight(-alignment));
    }

    /// <summary>由编译器调用（<c>{span}</c> 这类）。</summary>
    /// <param name="value">要追加的字符跨度。</param>
    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        if (Enabled)
            builder.Append(value);
    }

    /// <summary>由编译器调用。</summary>
    /// <param name="value">要追加的字符串。</param>
    public void AppendFormatted(string? value)
    {
        if (Enabled)
            builder.Append(value);
    }

    /// <summary>取走拼好的消息。<b>整个日志路径上唯一的一次字符串分配。</b></summary>
    /// <returns>拼好的消息；未启用时返回空串。</returns>
    public readonly override string ToString() => Enabled ? builder.ToString() : string.Empty;
}
