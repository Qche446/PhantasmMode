using System.Text;
using Monochrome.Core.Graphics.PostProcessing;
using Monochrome.Core.Graphics.Screen;
using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics;

/// <summary>
/// <c>/monoshader</c>：着色器与屏幕后处理的诊断。只读的随时可用：<c>status</c>（注册名、装入时间、资产路径）、
/// <c>capture status</c>、<c>capture fx</c>（列出内置屏幕特效）。
/// <para>
/// 会改状态的只在开发期生效：<c>reload</c>（编译并替换 <see cref="Effect"/>）、
/// <c>capture test</c>（挂一条整幅染色链验证屏幕捕获通路）、<c>capture fx &lt;名字&gt; on|off</c>。
/// 判定见 <see cref="MonoDebug"/>。
/// </para>
/// <para>
/// <b>命令名为什么是一个词 <c>monoshader</c>：</b>tML 用命令触发文本的<b>第一段</b>做键。
/// 以前这里是 <c>"mono"</c>、核心层那边是 <c>"mono core"</c>——两者的键都是 <c>"mono"</c>，
/// 撞键之后先注册的吃下全部 <c>/mono ...</c>，另一个<b>永远接不到且不报错</b>。
/// 两个独立命令的触发词必须是两个不同的第一段，所以现在是 <c>monocore</c> / <c>monoshader</c>。
/// </para>
/// </summary>
public sealed class MonoGraphicsCommand : ModCommand
{
    /// <summary>命令触发词。<b>必须与核心命令的第一段不同</b>，否则其中一个会静默失效。</summary>
    public override string Command => Trigger;

    /// <summary>触发词的字面量。<see cref="Command"/> 与"判断 args[0] 是不是命令名"都用它。</summary>
    private const string Trigger = "monoshader";

    /// <inheritdoc/>
    public override CommandType Type => CommandType.Chat;

    /// <summary>子命令清单。<b>只在 <c>/monoshader help</c> 里出现。</b></summary>
    public override string Usage =>
        "/monoshader help                       显示这条清单" +
        "\n/monoshader status                     着色器与屏幕捕获的概要" +
        "\n/monoshader reload [名字|all|*]        编译 .fx 并替换正在用的 Effect（仅开发期）" +
        "\n/monoshader capture                    屏幕捕获与后处理链的状态" +
        "\n/monoshader capture fxs                列出全部内置屏幕特效" +
        "\n/monoshader capture test [on|off]      挂一条整幅染色链，验证捕获通路（仅开发期）" +
        "\n/monoshader capture fx <名字|all> <on|off>  单独开关一条内置特效（仅开发期）";

    /// <inheritdoc/>
    public override string Description =>
        "Monochrome 的图形诊断、屏幕后处理自检与着色器热重载。（输入 /monoshader help 看全部子命令）";

    /// <summary>提供帮助的子命令名。</summary>
    private const string HelpSubCommand = "help";

    /// <summary>内置测试链。只在第一次 <c>/monoshader capture test on</c> 时创建。</summary>
    private static MonoPostFxPipeline? captureTestPipeline;

    private static MonoTintPass? captureTestPass;

    /// <summary>自检用的内置屏幕特效：名字 → 它的 pass（模糊是两个）。</summary>
    private static readonly Dictionary<string, List<MonoShaderPass>> captureFx = [with(StringComparer.OrdinalIgnoreCase)];

    private static MonoPostFxPipeline? captureFxPipeline;

    /// <summary>
    /// <c>/monoshader ...</c> 的唯一入口。
    /// <para>
    /// <b>不认识的子命令只回一行提示</b>，不把整张清单砸进聊天。清单在 <c>/monoshader help</c> 里。
    /// </para>
    /// </summary>
    /// <param name="caller">调用者。</param>
    /// <param name="input">原始输入。</param>
    /// <param name="args">按空格切开的参数：<c>args[0]</c> 是触发词 <c>monoshader</c>，子命令从 <c>args[1]</c> 起。</param>
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        try
        {
            Dispatch(caller, args);
        }
        catch (Exception exception)
        {
            // 与 /monocore 同理：tML 对任何异常都回 `Usage`，那是"看起来像打错了、其实是崩了"。
            MonoLog.Error(MonoLogLevel.Error,
                $"/monoshader 处理时抛异常（input=\"{input}\"，args=[{string.Join(",", args ?? [])}]）：{exception}");
            caller.Reply($"内部错误：{exception.GetType().Name} — {exception.Message}（详见 /monocore log）", Color.Red);
        }
    }

    /// <summary>
    /// 实际的分发。
    /// <para>
    /// <b>参数形状是实测出来的，别按源码想当然。</b>tML 的 <c>HandleCommand</c> 源码看着是
    /// <c>args = input.Split(' ').Skip(1)</c>，但实测说明<b><c>args[0]</c> 是命令名那一格、
    /// 子命令在 <c>args[1]</c></b>，而<b>裸命令时 <c>args</c> 长度是 0</b>。
    /// 这里先判断 <c>args[0]</c> 是不是命令名，是就把它剥掉，于是后续一律按
    /// <c>[子命令, 参数…]</c> 读——不再依赖那个形状。
    /// </para>
    /// </summary>
    private void Dispatch(CommandCaller caller, string[] args)
    {
        // 去掉空段：tML 用的是 `input.Split(' ')`（**没有** RemoveEmptyEntries），
        // 所以多打一个空格会切出空串，不过滤的话它会落进"未知子命令"。
        string[] tokens = args is null ? [] : Array.FindAll(args, static token => token.Length > 0);

        // 剥掉命令名那一格（如果它在）。之后 tokens[0] 一定是子命令。
        if (tokens.Length > 0 && tokens[0].Equals(Trigger, StringComparison.OrdinalIgnoreCase))
            tokens = tokens[1..];

        string sub = tokens.Length > 0 ? tokens[0].ToLowerInvariant() : HelpSubCommand;

        // 会改动状态（写磁盘 / 染色画面）的子命令需要开发期。只读的随时可用。
        // 帮助与非状态命令永远放行，否则"没有调试器时打错了都没有提示"。
        if (IsStateChanging(tokens) && !MonoDebug.IsDeveloperSession)
        {
            caller.Reply(MonoDebug.ExplainBlocked("这条子命令", "help / status / capture status / capture fx 列表"), Color.Orange);
            return;
        }

        switch (sub)
        {
            case HelpSubCommand:
            case "?":
            case "-h":
            case "--help":
                caller.Reply(Usage, Color.LightGreen);
                return;

            // 概要：不带参数，或 status。
            case "status":
            case "overview":
            case "shader":      // 兼容旧写法 /monoshader shader status —— "shader" 这段现在等于命令本身
                caller.Reply(Overview(), Color.LightGreen);
                return;

            case "reload":
                caller.Reply(Reload(tokens), Color.LightGreen);
                return;

            case "capture":
                Capture(caller, tokens);
                return;

            default:
                caller.Reply($"不存在子命令「{sub}」。可用 /monoshader help 查询全部子命令。", Color.Orange);
                return;
        }
    }

    /// <summary><c>capture</c> 段落：<c>status</c> / <c>test</c> / <c>fx</c> / <c>fxs</c>。</summary>
    /// <param name="caller">调用者。</param>
    /// <param name="tokens">已剥掉命令名与空段的参数，<c>[capture, 动作, 参数…]</c>。</param>
    private static void Capture(CommandCaller caller, string[] tokens)
    {
        string action = tokens.Length >= 2 ? tokens[1].ToLowerInvariant() : "status";

        switch (action)
        {
            case "status":
                caller.Reply(MonoScreenCaptureSystem.Describe(), Color.LightGreen);
                return;

            case "test":
                caller.Reply(ToggleCaptureTest(tokens), Color.LightGreen);
                return;

            // fxs：列出全部内置特效（不改变状态）。
            case "fxs":
            case "list":
                caller.Reply(MonoScreenFx.Describe(), Color.LightGreen);
                return;

            case "fx":
                caller.Reply(CaptureFx(tokens), Color.LightGreen);
                return;

            default:
                caller.Reply($"不存在 capture 子命令「{action}」。可用 /monoshader help 查询全部子命令。", Color.Orange);
                return;
        }
    }

    /// <summary>着色器与捕获的一行概要。</summary>
    private static string Overview() => DescribeShaders() + "\n\n" + MonoScreenCaptureSystem.Describe();

    /// <summary>
    /// 这条命令会不会改动状态（内存里的开关、磁盘上的 <c>.fxc</c>、画面上的颜色）。
    /// <para>
    /// 只读的子命令不在此列：<c>help</c> / <c>status</c> / <c>capture</c>（不带动作时就是 status）、
    /// <c>capture fxs</c>、<c>capture fx</c>（不带 <c>on</c>/<c>off</c> 时只是列表）。
    /// </para>
    /// <para>
    /// <b>按子命令的实际位置判断，不做"位置猜一半"</b>：<c>reload</c> 现在是第二段（旧写法
    /// <c>shader reload</c> 是第三段），两种都认。
    /// </para>
    /// </summary>
    /// <param name="tokens">已剥掉命令名与空段的参数，<c>[子命令, 参数…]</c>。</param>
    private static bool IsStateChanging(string[] tokens)
    {
        if (tokens.Length == 0)
            return false;

        // 旧写法 /monoshader shader status|reload ...："shader" 这一段等于命令本身。
        if (tokens[0].Equals("shader", StringComparison.OrdinalIgnoreCase))
            return tokens.Length >= 2 && tokens[1].Equals("reload", StringComparison.OrdinalIgnoreCase);

        if (tokens[0].Equals("reload", StringComparison.OrdinalIgnoreCase))
            return true;

        if (tokens[0].Equals("capture", StringComparison.OrdinalIgnoreCase))
        {
            if (tokens.Length < 2)
                return false;

            if (tokens[1].Equals("test", StringComparison.OrdinalIgnoreCase))
                return true;

            // `capture fx` 与 `capture fx list` 只是列表；带 on/off 才算改动。
            if (tokens[1].Equals("fx", StringComparison.OrdinalIgnoreCase))
                return tokens.Length >= 4;
        }

        return false;
    }


    /// <summary>列出注册表内容。资产路径为空说明它是用 <c>SetShader</c> 手工装的，不参与源文件热重载。</summary>
    private static string DescribeShaders()
    {
        StringBuilder output = new();
        output.Append("Monochrome shaders: ").Append(MonoShaderManager.Shaders.Count);
        foreach ((string name, MonoShader shader) in MonoShaderManager.Shaders)
        {
            output.Append('\n')
                  .Append(name)
                  .Append(" loaded ").Append(shader.LoadedAtUtc.ToLocalTime().ToString("HH:mm:ss"))
                  .Append(shader.AssetPath.Length > 0 ? "  <- " + shader.AssetPath : "  <- (手工装入，无源文件)");
        }
        return output.ToString();
    }

    /// <summary>不带名字就重载全部（默认做 mtime 预筛）；<c>all</c> / <c>*</c> 表示强制全部重编。</summary>
    private static string Reload(string[] tokens)
    {
        // tokens = [reload, (名字|all|*)]，也兼容旧写法 [shader, reload, (名字|all|*)]。
        if (tokens.Length >= 2 && tokens[0].Equals("shader", StringComparison.OrdinalIgnoreCase))
            tokens = tokens[1..];

        if (tokens.Length < 2)
            return MonoShaderReloader.Describe(MonoShaderReloader.ReloadAll());
        if (tokens[1] is "all" or "*")
            return MonoShaderReloader.Describe(MonoShaderReloader.ReloadAll(force: true));
        return MonoShaderReloader.Describe([MonoShaderReloader.Reload(tokens[1])]);
    }

    /// <summary>
    /// 挂/摘一条内置的整幅染色链。
    /// <para>
    /// 用<b>整幅染色</b>当自检信号是刻意的：它不需要任何着色器资产，因此"没看到颜色"只可能是
    /// 屏幕捕获链本身没通（而不是某个 <c>.fx</c> 没编出来）。看到世界变色而界面不变，
    /// 说明捕获点、ping-pong、回贴、以及"界面在 EndCapture 之后画"这条边界全部符合预期。
    /// </para>
    /// </summary>
    private static string ToggleCaptureTest(string[] tokens)
    {
        // tokens = [capture, test, (on|off)]。
        if (tokens.Length >= 3 && tokens[2] is not ("on" or "off"))
            return "用法：/monoshader capture test [on|off]";

        bool enable = tokens.Length < 3 || tokens[2] == "on";

        if (captureTestPipeline is null || captureTestPass is null)
        {
            captureTestPass = new MonoTintPass(new Color(255, 168, 120), order: 0);
            captureTestPipeline = new MonoPostFxPipeline().Add(captureTestPass);
            MonoScreenCaptureSystem.Register(captureTestPipeline, order: 1000, label: "内置自检：整幅暖色染色");
        }

        captureTestPass.Enabled = enable;
        return (enable
                ? "自检已开启：整个世界会被染成暖色（界面不受影响——这是设计如此，界面在 EndCapture 之后才画）。"
                : "自检已关闭。链仍然注册着，只是没有启用的 pass，因此原版不会为它多做一次捕获。")
            + "\n" + MonoScreenCaptureSystem.Describe();
    }

    /// <summary>
    /// <c>/monoshader capture fx</c>：把内置屏幕特效单独开关，用来在没有消费者代码的情况下确认每一条都真的画出来了。
    /// <para>
    /// <c>/monoshader capture fxs</c> 列出；<c>... capture fx &lt;名字&gt; on|off</c> 开关单个；
    /// <c>... capture fx all on|off</c> 全部。
    /// </para>
    /// </summary>
    private static string CaptureFx(string[] tokens)
    {
        // tokens = [capture, fx, (名字|all), (on|off)]。
        if (tokens.Length < 3)
            return MonoScreenFx.Describe() + "\n用法：/monoshader capture fx <名字|all> on|off";

        string what = tokens[2];
        bool on = tokens.Length >= 4 && tokens[3].Equals("on", StringComparison.OrdinalIgnoreCase);

        if (what.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            foreach (string name in MonoScreenFx.Names)
                SetCaptureFx(name, on);
            return $"已把全部内置屏幕特效切到「{(on ? "开" : "关")}」。\n" + MonoScreenCaptureSystem.Describe();
        }

        if (!SetCaptureFx(what, on))
            return $"没有叫「{what}」的内置特效。\n" + MonoScreenFx.Describe();

        return $"「{what}」已切到「{(on ? "开" : "关")}」。" +
               (on ? "注意：这些效果按默认参数叠加，全部打开会比较吵——那正是自检的目的。" : string.Empty) +
               "\n" + MonoScreenCaptureSystem.Describe();
    }

    /// <summary>按名字开关一个内置特效；第一次开启时创建、加进自检链并注册。</summary>
    private static bool SetCaptureFx(string name, bool on)
    {
        if (!captureFx.TryGetValue(name, out List<MonoShaderPass>? passes))
        {
            // 关闭一个从没创建过的效果：只校验名字合不合法，不建对象。
            if (!on)
                return MonoScreenFx.Create(name) is not null;

            MonoShaderPass? first = MonoScreenFx.Create(name, out MonoShaderPass? extra);
            if (first is null)
                return false;

            passes = [first];
            ApplyCaptureFxDemoPreset(first);
            if (extra is not null)
            {
                ApplyCaptureFxDemoPreset(extra);
                passes.Add(extra);
            }
            captureFx[name] = passes;

            captureFxPipeline ??= new MonoPostFxPipeline();
            foreach (MonoShaderPass pass in passes)
                captureFxPipeline.Add(pass);
            MonoScreenCaptureSystem.Register(captureFxPipeline, order: 2000, label: "内置自检：屏幕特效");
        }

        foreach (MonoShaderPass pass in passes)
            pass.Enabled = on;
        return true;
    }

    /// <summary>
    /// 自检用的参数：把每一条推到"一眼能看出来"的程度。
    /// <para>
    /// 正式默认值刻意保守（色阶的默认甚至完全恒等），因为一条"默认就改变画面"的 pass 会让消费者莫名其妙；
    /// 但自检需要的是"看不出变化就等于没生效"。两者的目标相反，所以这里单独给一组演示值。
    /// </para>
    /// </summary>
    private static void ApplyCaptureFxDemoPreset(MonoShaderPass pass)
    {
        switch (pass)
        {
            case MonoVignettePass vignette:
                vignette.Strength = 0.85f;
                break;
            case MonoColorGradePass grade:
                grade.Configure(new Color(255, 214, 165), saturation: 1.5f, contrast: 1.15f);
                break;
            case MonoChromaticAberrationPass aberration:
                aberration.Strength = 0.012f;
                break;
            case MonoScanlinePass scanline:
                scanline.Strength = 0.45f;
                break;
            case MonoGrainPass grain:
                grain.Strength = 0.18f;
                break;
            case MonoBlurPass blur:
                blur.Radius = 5f;
                break;
        }
    }
}
