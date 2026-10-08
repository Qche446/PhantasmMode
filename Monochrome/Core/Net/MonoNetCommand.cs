using System;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace Monochrome.Core.Net;

/// <summary>
/// <c>/mononet</c>：网络层的诊断。只读的随时可用（<c>help</c> / <c>stat</c> / <c>fields</c>），
/// <c>ping</c> 会发包，只在开发期可用（判定见 <see cref="MonoDebug"/>）。
/// </summary>
public sealed class MonoNetCommand : ModCommand
{
    /// <summary>命令触发词。<b>必须是自成一词的第一段</b>，否则会与别的命令撞键并静默失效。</summary>
    private const string Trigger = "mononet";

    /// <inheritdoc/>
    public override string Command => Trigger;

    /// <inheritdoc/>
    public override CommandType Type => CommandType.Chat;

    /// <inheritdoc/>
    public override string Usage =>
        "/mononet help      显示这条清单" +
        "\n/mononet stat      通道账本、各种拒写与未知计数、上次 ping" +
        "\n/mononet fields    逐个字段列出 id、名字与当前值" +
        "\n/mononet requests  列出发往服务端的请求 id 与名字" +
        "\n/mononet tables    列出实体字段表：名字、形状哈希、流量与各字段的声明顺序" +
        "\n/mononet ping      向服务端发一次 ping（仅开发期）";

    /// <inheritdoc/>
    public override string Description => "Monochrome 网络层的诊断：世界级字段的通道账本、上行请求与实体字段表，外加 ping。（输入 /mononet help 看全部子命令）";

    /// <inheritdoc/>
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        try
        {
            string[] tokens = CommandTokens(args);
            string sub = tokens.Length > 0 ? tokens[0].ToLowerInvariant() : "help";

            if (sub == "ping" && !MonoDebug.IsDeveloperSession)
            {
                caller.Reply(MonoDebug.ExplainBlocked("ping", "help / stat / fields / tables"), Color.Orange);
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

                case "stat":
                case "status":
                    caller.Reply(MonoNet.Describe(), Color.LightGreen);
                    return;

                case "fields":
                    caller.Reply(MonoNet.DescribeFields(), Color.LightGreen);
                    return;

                case "requests":
                    caller.Reply(MonoNet.DescribeRequests(), Color.LightGreen);
                    return;

                case "tables":
                    caller.Reply(MonoNet.DescribeTables(), Color.LightGreen);
                    return;

                case "ping":
                    caller.Reply(MonoNetSystem.Ping(), Color.LightGreen);
                    return;

                default:
                    caller.Reply($"不存在子命令「{sub}」。可用 /mononet help 查询全部子命令。", Color.Orange);
                    return;
            }
        }
        catch (Exception exception)
        {
            // tML 对任何异常都回 Usage，那是"看起来像打错了、其实是崩了"，所以自己兜住。
            MonoLog.Error(MonoLogLevel.Error, $"/mononet 处理时抛异常（input=\"{input}\"）：{exception}");
            caller.Reply($"内部错误：{exception.GetType().Name} — {exception.Message}（详见 /monocore log）", Color.Red);
        }
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
