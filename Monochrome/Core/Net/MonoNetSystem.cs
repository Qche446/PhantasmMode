using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Monochrome.Core.Net;

/// <summary>
/// MonoNet 的 tML 胶水：发送时机、加入世界时的全量下发、卸载时的复位。
/// <para>
/// 帧起点只做两件事：刷新"本端是不是权威端"，以及把攒下的脏字段发出去。改值发生在世界更新里，
/// 包在下一帧的起点发出——一帧的合并窗口就是这么来的，同一帧改多个字段也只产生一个包。
/// </para>
/// </summary>
public sealed class MonoNetSystem : ModSystem
{
    /// <summary>批量字段包。</summary>
    internal const byte MsgWorldFlags = 1;

    /// <summary>客户端发起的连通性测试。</summary>
    internal const byte MsgPing = 2;

    /// <summary>服务端对 ping 的回显。</summary>
    internal const byte MsgPong = 3;

    /// <summary>客户端→服务端的请求。</summary>
    internal const byte MsgRequest = 4;

    /// <inheritdoc/>
    public override void PreUpdateEntities()
    {
        MonoNet.Side = Main.netMode switch
        {
            NetmodeID.Server => MonoNetSide.Server,
            NetmodeID.MultiplayerClient => MonoNetSide.Client,
            _ => MonoNetSide.SinglePlayer,
        };
        MonoNet.LocalPlayerWhoAmI = Main.myPlayer;

        if (Main.netMode == NetmodeID.Server)
            Flush();
        else if (Main.netMode == NetmodeID.SinglePlayer)
            MonoNet.DiscardDirty();
        else
            FlushRequests();
    }

    /// <inheritdoc/>
    public override void OnWorldUnload() => MonoNet.ResetForWorld();

    /// <inheritdoc/>
    public override void OnModUnload() => MonoNet.ResetForWorld();

    /// <inheritdoc/>
    public override void NetSend(BinaryWriter writer) => MonoNet.WriteSnapshot(writer);

    /// <inheritdoc/>
    public override void NetReceive(BinaryReader reader) => MonoNet.ReadSnapshot(reader);

    /// <summary>处理批量字段包。只有服务端会下发它，所以权威端自己收到就是有人在冒充。</summary>
    /// <param name="reader">包体。</param>
    /// <param name="whoAmI">发送者编号。</param>
    internal static void HandleWorldFlags(BinaryReader reader, int whoAmI)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            MonoNet.NoteAuthorityViolation(MsgWorldFlags, whoAmI);
            return;
        }

        MonoNet.ReadBatch(reader, out _);
    }

    /// <summary>处理 ping：服务端把发起时的时间戳原样回显给发起者，往返时间由发起端自己算。</summary>
    /// <param name="reader">包体。</param>
    /// <param name="whoAmI">发送者编号。</param>
    internal static void HandlePing(BinaryReader reader, int whoAmI)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        double sentAt = reader.ReadDouble();
        ModPacket packet = Packet();
        packet.Write(MsgPong);
        packet.Write(sentAt);
        packet.Send(whoAmI);
    }

    /// <summary>处理回显：算出本地往返时间。</summary>
    /// <param name="reader">包体。</param>
    internal static void HandlePong(BinaryReader reader)
    {
        double sentAt = reader.ReadDouble();
        if (MonoNet.PendingPingStart < 0 || Math.Abs(sentAt - MonoNet.PendingPingStart) > 1e-6)
            return;

        MonoNet.LastPingMs = Math.Max(0, (MonoTime.RealTime - MonoNet.PendingPingStart) * 1000.0);
        MonoNet.PendingPingStart = -1;
        MonoLog.Info(MonoLogLevel.Info, $"ping 往返 {MonoNet.LastPingMs:F1} ms");
    }

    /// <summary>发起一次 ping。它是传输通路的冒烟测试：能回来就说明 Monochrome 的包两端都在收。</summary>
    /// <returns>给命令面的一行说明。</returns>
    internal static string Ping()
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return "单人下没有往返可测：本地就是权威端。用 /mononet stat 看账本。";

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return "这条要在客户端上执行；服务端自己 ping 没有意义。";

        if (MonoNet.PendingPingStart >= 0)
            return "上一次 ping 还没回来，稍等一下。";

        MonoNet.PendingPingStart = MonoTime.RealTime;

        ModPacket packet = Packet();
        packet.Write(MsgPing);
        packet.Write(MonoNet.PendingPingStart);
        packet.Send();

        return "已发出 ping；收到回显后往返时间会写进日志与 /mononet stat。";
    }

    /// <summary>把待发的请求发出去。只有客户端会走到这里（单机直接在本机执行，服务端会被 <see cref="MonoNet"/> 挡下）。</summary>
    private static void FlushRequests()
    {
        Action<BinaryWriter>? write;
        while ((write = MonoNet.NextOutgoing()) is not null)
        {
            ModPacket packet = Packet();
            packet.Write(MsgRequest);

            long before = packet.BaseStream.Position;
            write(packet);
            long bytes = packet.BaseStream.Position - before;

            packet.Send();

            // +3 = 消息 id 一字节 + 请求 id 两字节。
            MonoNet.ReportRequestSent((int)bytes + 3);
        }
    }

    /// <summary>处理一条请求。请求只该发给服务端，所以别处收到就计一次"位置不对"并丢弃。</summary>
    /// <param name="reader">包体。</param>
    /// <param name="whoAmI">发送者编号。</param>
    internal static void HandleRequest(BinaryReader reader, int whoAmI)
    {
        if (Main.netMode != NetmodeID.Server)
        {
            MonoNet.NoteMisplacedRequest(whoAmI);
            return;
        }

        MonoNet.ReadRequest(reader, whoAmI);
    }

    /// <summary>把当前所有脏字段发出去。一条通道一个包，一帧最多一轮。</summary>
    private static void Flush()
    {
        MonoNetChannel? channel;
        while ((channel = MonoNet.NextDirtyChannel()) is not null)
        {
            ModPacket packet = Packet();
            packet.Write(MsgWorldFlags);
            packet.Write(channel.Id);

            long before = packet.BaseStream.Position;
            MonoNet.WriteDirty(channel, packet);
            long bytes = packet.BaseStream.Position - before;

            packet.Send();
            MonoNet.CommitDirty(channel);

            // +3 = 消息 id 一字节 + 通道 id 两字节。统计只求量级对得上，不必精确到包头。
            MonoNet.ReportSent(channel, (int)bytes + 3);
        }
    }

    private static ModPacket Packet() => Monochrome.Instance!.GetPacket();
}
