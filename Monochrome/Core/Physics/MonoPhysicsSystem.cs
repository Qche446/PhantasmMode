using Monochrome.Common.MonoUtil.Physics;

namespace Monochrome.Core.Physics;

/// <summary>
/// 物理对象的世界级驱动：每 tick 在世界更新闸门之内把全部已注册的求解器推进一次。
/// <para>
/// <b>为什么挂在 <c>PostUpdateDusts</c></b>：它位于 <c>Main.DoUpdateInWorld</c> 之内，也就是
/// <c>ShouldUpdateEntities()</c> 闸门之内。于是命中定格与世界时间缩放为 0 时物理跟着世界一起停，
/// 与玩法逻辑一致；同时它排在 NPC / 玩家 / 弹幕更新之后，锚点读到的都是这一 tick 的最终位置。
/// （行号依据：<c>Main.cs</c> 的 16938 是闸门、17584 是 <c>PostUpdateDusts</c>。）
/// </para>
/// <para>
/// <b>只在客户端跑。</b> 物理在这里是纯表现，专用服务器上直接返回，不做任何同步。
/// 消费者若要用物理驱动玩法，应在权威端自己跑一份并同步"驱动状态"（锚点），而不是同步每个质点。
/// </para>
/// </summary>
public sealed class MonoPhysicsSystem : ModSystem
{
    private static readonly List<MonoRope> ropes = [];

    /// <summary>当前已注册的绳索。</summary>
    public static IReadOnlyList<MonoRope> Ropes => ropes;

    /// <summary>自加载以来推进过多少 tick。</summary>
    public static long Steps { get; private set; }

    /// <summary>上一个 tick 全部绳索合计执行的约束投影次数。</summary>
    public static int LastProjections { get; private set; }

    /// <summary>把一条绳索纳入每 tick 步进。重复注册同一个实例是空操作。</summary>
    /// <param name="rope">要注册的绳索。</param>
    public static void Register(MonoRope rope)
    {
        if (rope is null || ropes.Contains(rope))
            return;

        ropes.Add(rope);
    }

    /// <summary>把一条绳索移出步进表。</summary>
    /// <param name="rope">要移除的绳索。</param>
    /// <returns>确实移除掉时为 true。</returns>
    public static bool Unregister(MonoRope rope) => rope is not null && ropes.Remove(rope);

    /// <summary>清空步进表。不会释放绳索本身。</summary>
    public static void Clear()
    {
        ropes.Clear();
        LastProjections = 0;
    }

    /// <inheritdoc/>
    public override void PostUpdateDusts()
    {
        if (Main.dedServ)
            return;

        int projections = 0;
        for (int i = 0; i < ropes.Count; i++)
        {
            MonoRope rope = ropes[i];
            rope.Step();
            projections += rope.Solver.LastProjections;
        }

        LastProjections = projections;
        Steps++;
    }

    /// <inheritdoc/>
    public override void OnWorldUnload() => Clear();

    /// <inheritdoc/>
    public override void Unload() => Clear();
}
