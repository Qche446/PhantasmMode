namespace Monochrome.Content.Status;

/// <summary>
/// 状态系统的装配点：加载期给每个 <see cref="MonoBuff"/> 分配槽位，并对外提供 buff 类型到槽位的映射。
/// <para>
/// 槽位在 <see cref="PostSetupContent"/> 里按 tML 的内容顺序一趟扫完，所以只要类名不动，
/// 每个状态的槽位每次运行都一样。消费方不参与分配，也不该去改它。
/// </para>
/// <para>
/// 分配完成之前 <see cref="Resolve"/> 一律返回 -1，各处查询都会安静地当作"没有这个状态"。
/// 内容加载之后才会跑到的代码不受影响。
/// </para>
/// </summary>
public sealed class MonoBuffSystem : ModSystem
{
    private static MonoBuff[] instances = [];

    /// <summary>已注册的状态数量，也就是槽位总数。</summary>
    public static int SlotCount => instances.Length;

    /// <summary>按槽位排列的状态实例。仅本程序集内部使用。</summary>
    internal static MonoBuff[] Instances => instances;

    /// <inheritdoc/>
    public override void PostSetupContent()
    {
        List<MonoBuff> found = [];
        foreach (MonoBuff buff in ModContent.GetContent<MonoBuff>())
            found.Add(buff);

        instances = [.. found];
        for (int i = 0; i < instances.Length; i++)
            instances[i].Slot = i;
    }

    /// <summary>把一个原版 buff 类型映射到槽位；不是本系统的状态时返回 -1。</summary>
    /// <param name="buffType">原版 buff 类型 id。0（空槽）与原版 buff 都会落到 -1。</param>
    internal static int Resolve(int buffType) => (ModContent.GetModBuff(buffType) as MonoBuff)?.Slot ?? -1;
}
