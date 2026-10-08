namespace Monochrome.Content.Status;

/// <summary>
/// 一个状态在某个实体上的伴生数据。
/// <para>
/// <see cref="MonoBuff"/> 是 tML 的内容单例，一个类型只有一个实例，装不下「这只 NPC 上剩几层」。
/// 每个 (实体, 状态) 因此各有一份本对象，按槽位存在 <see cref="MonoBuffSet"/> 里。
/// 消费者从 <c>npc.MonoBuffs().Data&lt;T&gt;()</c> 取它，把每实体的层数、计时、阶段都写在这里。
/// </para>
/// <para>
/// 状态消失后本对象会被 <see cref="MonoBuffSet.Release"/> 丢掉，所以不要把跨生命周期的东西存在这里。
/// </para>
/// </summary>
public sealed class MonoBuffSlotData
{
    /// <summary>建一份数据。</summary>
    /// <param name="aiSize">参数数组的长度，来自 <see cref="MonoBuff.AiSize"/>。</param>
    public MonoBuffSlotData(int aiSize)
    {
        Ai = aiSize > 0 ? new float[aiSize] : [];
    }

    /// <summary>
    /// 通用参数数组，用法对标 <c>NPC.ai</c>。
    /// <para>长度在创建时由 <see cref="MonoBuff.AiSize"/> 定下，之后不再变；内容由消费者自己约定。</para>
    /// </summary>
    public float[] Ai { get; }

    /// <summary>消费者自定义的结构化数据；重写 <see cref="MonoBuff.CreateState"/> 之后才会被填。</summary>
    public object? Custom { get; set; }
}
