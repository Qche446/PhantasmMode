using Monochrome.Core;
using Monochrome.Core.Net;
using Terraria.ModLoader.IO;

namespace Monochrome.Content.Status;

/// <summary>
/// 状态系统的跨端同步：把声明了 <see cref="MonoBuff.SyncAi"/> 的状态的 <c>Ai</c> 数组带过端。
/// <para>
/// <b>它解决什么。</b>层数这类每实体数据放在 <see cref="MonoBuffSlotData.Ai"/> 里，而原版只同步 buff 本身
/// （<c>buffType</c> 与 <c>buffTime</c>），两端读到的层数于是可能不是同一个数。消费者的减伤、DoT、
/// 图标都读它，症状是"同一发伤害在服务端与客户端算出来不一样"。
/// </para>
/// <para>
/// <b>谁发。</b>NPC 侧由权威端决定：值变了就把 <c>npc.netUpdate</c> 立起来，随这只实体自己那份
/// extra-AI 出去（<c>SendExtraAI</c> / <c>ReceiveExtraAI</c>）。玩家侧由玩家自己的客户端上行，
/// 服务端落值后转发给其余客户端；中途加入的人由服务端在 <c>SyncPlayer</c> 时补一份全量。
/// </para>
/// <para>
/// <b>默认关。</b>只有重写 <see cref="MonoBuff.SyncAi"/> 返回 <see langword="true"/> 的状态才进包，
/// 同步的是整根 <c>Ai</c> 数组；<see cref="MonoBuffSlotData.Custom"/> 不参与。单人与专用服务器上
/// 这条路整个不发生。
/// </para>
/// </summary>
internal static class MonoBuffSync
{
    /// <summary>NPC 侧的实体字段表：一格自己序列化的载荷，内容就是 <see cref="MonoBuffSet.WriteSync"/> 写的那份。</summary>
    private static readonly MonoNetFields<NPC> npcTable = MonoNet.Fields<NPC>("monochrome.buffAi")
        .Custom("ai",
            static (writer, npc) => npc.MonoBuffs().WriteSync(MonoBuffSystem.SyncAiOf, all: false, writer),
            static (reader, npc) => npc.MonoBuffs().ReadSync(reader));

    /// <summary>NPC 侧写一包：在场的、声明了同步的、值变过的槽位。</summary>
    /// <param name="npc">实体。</param>
    /// <param name="writer">包体。</param>
    internal static void WriteNpc(NPC npc, BinaryWriter writer) => npcTable.Write(npc, writer);

    /// <summary>NPC 侧读一包。</summary>
    /// <param name="npc">实体。</param>
    /// <param name="reader">包体。</param>
    internal static void ReadNpc(NPC npc, BinaryReader reader) => npcTable.Read(npc, reader);

    /// <summary>
    /// 权威端：<c>Ai</c> 变了就把这只 NPC 的同步立起来。
    /// <para>只有服务端会下发实体同步，所以在别的端上什么都不做——单机没有对端，客户端上发也不合法。</para>
    /// </summary>
    /// <param name="npc">实体。</param>
    internal static void FlagNpc(NPC npc)
    {
        if (Main.netMode == NetmodeID.Server && npc.MonoBuffs().AnyAiDirty(MonoBuffSystem.SyncAiOf))
            npc.netUpdate = true;
    }

    /// <summary>客户端把自己改过的 <c>Ai</c> 发给服务端；不是本地玩家或没有变化时不发。</summary>
    /// <param name="player">玩家。</param>
    internal static void PushPlayerChanges(Player player)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || player.whoAmI != Main.myPlayer)
            return;

        MonoBuffSet set = player.MonoBuffs();
        if (!set.AnyAiDirty(MonoBuffSystem.SyncAiOf))
            return;

        ModPacket packet = Monochrome.Instance!.GetPacket();
        packet.Write(MonoNetSystem.MsgPlayerBuffAi);
        packet.Write((byte)player.whoAmI);
        set.WriteSync(MonoBuffSystem.SyncAiOf, all: false, packet);
        packet.Send();
    }

    /// <summary>服务端把某个玩家的全量 <c>Ai</c> 发给 <paramref name="toWho"/>（<c>-1</c> = 除 <paramref name="ignoreWho"/> 外所有人）。</summary>
    /// <param name="player">玩家。</param>
    /// <param name="toWho">接收者编号；<c>-1</c> 表示除 <paramref name="ignoreWho"/> 外所有人。</param>
    /// <param name="ignoreWho">要跳过的人。</param>
    internal static void PushPlayerFull(Player player, int toWho, int ignoreWho)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = Monochrome.Instance!.GetPacket();
        packet.Write(MonoNetSystem.MsgPlayerBuffAi);
        packet.Write((byte)player.whoAmI);
        player.MonoBuffs().WriteSync(MonoBuffSystem.SyncAiOf, all: true, packet);
        packet.Send(toWho, ignoreWho);
    }

    /// <summary>
    /// 处理玩家侧 <c>Ai</c> 包。服务端只认"客户端替自己说话"，落值之后转发给其余客户端；客户端直接落值。
    /// </summary>
    /// <param name="reader">包体。</param>
    /// <param name="whoAmI">发送者编号。</param>
    internal static void HandlePlayerAi(BinaryReader reader, int whoAmI)
    {
        int playerIndex = reader.ReadByte();
        if (playerIndex >= Main.maxPlayers)
            return;

        if (Main.netMode == NetmodeID.Server && playerIndex != whoAmI)
        {
            MonoLog.Warn(MonoLogLevel.Warn,
                $"玩家 {whoAmI} 发来的是玩家 {playerIndex} 的状态 Ai，已丢弃：客户端只能替自己说话。");
            return;
        }

        Player player = Main.player[playerIndex];
        player.MonoBuffs().ReadSync(reader);

        if (Main.netMode == NetmodeID.Server)
            PushPlayerFull(player, -1, whoAmI);
    }
}
