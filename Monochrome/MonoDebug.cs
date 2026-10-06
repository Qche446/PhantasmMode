using System.Diagnostics;

namespace Monochrome;

/// <summary>
/// 开发期功能的开关。改状态、写磁盘、把画面整个染色的功能都挂在 <see cref="IsDeveloperSession"/> 下面，
/// 正常游玩时不会被动触发。
/// <para>
/// 判定标准是"有没有调试器附加"（<see cref="Debugger.IsAttached"/>），不用编译配置——tML 构建模组时
/// 默认就是 Debug，那样等于永远放行。从 IDE 启动游戏、或给进程附加调试器都算开发期；没有调试器时
/// 设环境变量 <see cref="OverrideVariable"/><c>=1</c> 也能开。
/// </para>
/// </summary>
public static class MonoDebug
{
    /// <summary>强制打开开发期功能的环境变量。非空且不是 <c>0</c> / <c>false</c> 即视为开启。</summary>
    public const string OverrideVariable = "MONOCHROME_DEBUG";

    private static bool? overridden;

    /// <summary>是否处于开发期（有调试器附加，或环境变量强制打开）。</summary>
    public static bool IsDeveloperSession => Debugger.IsAttached || EnvironmentOverride;

    /// <summary>环境变量是否把开发期强制打开了。</summary>
    public static bool EnvironmentOverride
    {
        get
        {
            if (overridden.HasValue)
                return overridden.Value;

            string? value = null;
            try
            {
                value = Environment.GetEnvironmentVariable(OverrideVariable);
            }
            catch
            {
                // 读环境变量失败按"没设置"处理，不值得让游戏崩。
            }

            bool enabled = !string.IsNullOrWhiteSpace(value)
                && !value.Equals("0", StringComparison.Ordinal)
                && !value.Equals("false", StringComparison.OrdinalIgnoreCase);
            overridden = enabled;
            return enabled;
        }
    }

    /// <summary>当前判定依据的说明，用于日志与命令回复。</summary>
    public static string Describe()
    {
        if (Debugger.IsAttached)
            return "检测到调试器附加：开发期功能已开启。";
        if (EnvironmentOverride)
            return $"环境变量 {OverrideVariable} 已设置：开发期功能已开启。";
        return $"当前不是开发期（没有调试器附加，也没有设置 {OverrideVariable}）：会改动状态的命令已被停用。";
    }

    /// <summary>
    /// 拼一条"为什么这条命令被拒绝、怎么才能用"的回复。
    /// </summary>
    /// <param name="what">被拒绝的东西，例如"这条子命令"。</param>
    /// <param name="readOnlyCommands">同一个命令面里只读的那些子命令，用来说明哪些还能用。</param>
    public static string ExplainBlocked(string what, string readOnlyCommands)
        => $"{what}只在开发期生效，现在已被停用。\n"
         + $"想用就从 IDE 附加调试器启动游戏，或者设置环境变量 {OverrideVariable}=1 后重启游戏。\n"
         + $"只读的命令（{readOnlyCommands}）任何时候都可以用。";

    /// <summary>清掉缓存，让下一次判定重新读环境变量（供测试与热重载使用）。</summary>
    internal static void ResetOverrideCache() => overridden = null;
}
