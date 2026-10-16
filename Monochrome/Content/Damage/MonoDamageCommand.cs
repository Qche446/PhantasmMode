using System.Text;
using Monochrome.Common.MonoUtil.Damage;
using Monochrome.Core;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace Monochrome.Content.Damage;

/// <summary>
/// <c>/monodamage</c>：伤害规则表与命中追踪的诊断。
/// <para>
/// 只读的随时可用（<c>rules</c> / <c>tags</c> / <c>trace</c> / <c>trace dump</c>）；开启追踪会让每次命中
/// 为每条规则多存一份累加器快照，属于开发期开销，只在开发期可用（判定见 <see cref="MonoDebug"/>）。
/// </para>
/// <para>
/// 触发词第一段必须与其它命令不同，否则其中一个会静默失效——那条不变量由离线验收台的
/// 「各命令的触发词第一段互不相同」用例守着。
/// </para>
/// </summary>
public sealed class MonoDamageCommand : ModCommand
{
    /// <summary>命令触发词。</summary>
    private const string Trigger = "monodamage";

    /// <inheritdoc/>
    public override string Command => Trigger;

    /// <inheritdoc/>
    public override CommandType Type => CommandType.Chat;

    /// <inheritdoc/>
    public override string Usage =>
        "/monodamage help          显示这条清单" +
        "\n/monodamage rules         规则表：按执行顺序列出每条规则的段、次序与目标" +
        "\n/monodamage tags          已登记的伤害标签与位号" +
        "\n/monodamage trace         追踪状态（开关、记录条数、累计命中）" +
        "\n/monodamage trace dump    最近一次命中的完整过程" +
        "\n/monodamage trace clear   清空记录" +
        "\n/monodamage trace off     关闭追踪" +
        "\n/monodamage trace on      开启追踪（仅开发期）";

    /// <inheritdoc/>
    public override string Description =>
        "Monochrome 伤害规则表的诊断：规则清单、伤害标签、命中追踪。（输入 /monodamage help 看全部子命令）";

    /// <inheritdoc/>
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        try
        {
            string[] tokens = CommandTokens(args);
            string sub = tokens.Length > 0 ? tokens[0].ToLowerInvariant() : "help";
            string option = tokens.Length > 1 ? tokens[1].ToLowerInvariant() : "";

            if (sub == "trace" && option == "on" && !MonoDebug.IsDeveloperSession)
            {
                caller.Reply(MonoDebug.ExplainBlocked("开启命中追踪",
                    "help / rules / tags / trace / trace dump / trace clear / trace off"), Color.Orange);
                return;
            }

            switch (sub)
            {
                case "help":
                case "?":
                case "-h":
                case "--help":
                    caller.Reply(Usage, Color.LightGreen);
                    return;

                case "rules":
                    caller.Reply(DescribeRules(), Color.LightGreen);
                    return;

                case "tags":
                    caller.Reply(DescribeTags(), Color.LightGreen);
                    return;

                case "trace":
                    caller.Reply(HandleTrace(option), Color.LightGreen);
                    return;

                default:
                    caller.Reply($"不存在子命令「{sub}」。可用 /monodamage help 查询全部子命令。", Color.Orange);
                    return;
            }
        }
        catch (Exception exception)
        {
            // tML 对任何异常都回 Usage，那是"看起来像打错了、其实是崩了"，所以自己兜住。
            MonoLog.Error(MonoLogLevel.Error, $"/monodamage 处理时抛异常（input=\"{input}\"）：{exception}");
            caller.Reply($"内部错误：{exception.GetType().Name} — {exception.Message}（详见 /monocore log）", Color.Red);
        }
    }

    /// <summary>规则表本体，外加冻结状态与两条只能靠断言才看得见的计数。</summary>
    private static string DescribeRules()
    {
        MonoDamageChain chain = MonoDamage.Chain;

        StringBuilder output = new();
        output.Append("规则表：").Append(chain.IsFrozen ? "已冻结" : "尚未冻结（第一次命中时自动冻结）")
              .Append("；冻结后被拒的登记 ").Append(chain.LateRegistrationRejected).Append(" 次")
              .Append("；已被移除 ").Append(chain.Removed).Append(" 条")
              .Append("；已建立分片的目标类型 ").Append(chain.CachedTargetTypes).Append(" 个。\n");
        output.Append(chain.Describe());

        return output.ToString();
    }

    /// <summary>已登记的伤害标签。位号是库按登记顺序分配的，消费者不写。</summary>
    private static string DescribeTags()
    {
        IReadOnlyList<string> names = MonoDamageTag.Names;

        StringBuilder output = new();
        output.Append("已登记 ").Append(names.Count).Append(" 个伤害标签（位号从小到大）：");
        for (int bit = 0; bit < names.Count; bit++)
            output.Append("\n  ").Append(bit.ToString("D2")).Append("  ").Append(names[bit]);

        output.Append("\n消费者自己的标签用 MonoDamageTag.Register 在加载期登记一次。");
        return output.ToString();
    }

    /// <summary>处理 <c>trace</c> 的各个选项。</summary>
    private static string HandleTrace(string option) => option switch
    {
        "" or "stat" or "status" => DescribeTraceState(),
        "on" => TurnTraceOn(),
        "off" => TurnTraceOff(),
        "dump" or "show" => MonoDamage.Trace.Describe(),
        "clear" => ClearTrace(),
        _ => $"不认识的 trace 选项「{option}」。可用：on / off / dump / clear。",
    };

    private static string DescribeTraceState()
    {
        MonoDamageTrace trace = MonoDamage.Trace;

        StringBuilder output = new();
        output.Append("命中追踪：").Append(trace.Enabled ? "已开启" : "已关闭")
              .Append("；本次记录 ").Append(trace.Count).Append(" 条，容量 ").Append(trace.Capacity)
              .Append("；累计命中 ").Append(trace.HitCount).Append(" 次。");
        output.Append(trace.Enabled
            ? "每次命中会为每条规则存一份累加器快照，看完记得关。"
            : "用 /monodamage trace on 开启（仅开发期）。");

        return output.ToString();
    }

    private static string TurnTraceOn()
    {
        MonoDamage.Trace.Enabled = true;
        return "命中追踪已开启。打一下目标，再用 /monodamage trace dump 看每条规则改了什么。";
    }

    private static string TurnTraceOff()
    {
        MonoDamage.Trace.Enabled = false;
        MonoDamage.Trace.Clear();
        return "命中追踪已关闭，记录已清空。";
    }

    private static string ClearTrace()
    {
        MonoDamage.Trace.Clear();
        return "追踪记录已清空。";
    }

    /// <summary>
    /// 剥掉命令名那一格与空段。tML 的 <c>args[0]</c> 是命令名、裸命令时长度为 0，
    /// 而切分用的是 <c>input.Split(' ')</c>（不去空段），所以两件事都要处理。
    /// </summary>
    private static string[] CommandTokens(string[] args)
    {
        string[] tokens = args is null ? [] : Array.FindAll(args, static token => token.Length > 0);
        if (tokens.Length > 0 && tokens[0].Equals(Trigger, StringComparison.OrdinalIgnoreCase))
            tokens = tokens[1..];
        return tokens;
    }
}
