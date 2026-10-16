using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage;

/// <summary>
/// 给弹幕挂伤害标签。
/// <para>
/// <see cref="MonoDamageInfo.Tags"/> 的来源就是这里：投射物自己不知道"我这一下算火属性"，
/// 由发射它的消费者在生成后写一笔，命中时规则表再把这份掩码读出来。
/// </para>
/// <para>
/// <b>每个弹幕一份实例</b>（<c>InstancePerEntity</c>）。这是 tML 里挂每实体数据的常规形态，
/// 代价是每颗弹幕多一个小对象；换来的是生命周期跟着弹幕走，不需要任何清理代码。
/// 只有真的写了标签的弹幕才有非零掩码，没写的那些只是一个字段为 0 的实例。
/// </para>
/// </summary>
public sealed class MonoProjTags : GlobalProjectile
{
    private ulong tags;

    /// <inheritdoc/>
    public override bool InstancePerEntity => true;

    /// <summary>这份标签掩码。</summary>
    public ulong Tags => tags;

    /// <summary>加上一个或多个标签。</summary>
    /// <param name="tag">标签掩码。</param>
    public void Add(ulong tag) => tags |= tag;

    /// <summary>去掉一个或多个标签。</summary>
    /// <param name="tag">标签掩码。</param>
    public void Remove(ulong tag) => tags &= ~tag;

    /// <summary>是否带某个标签。</summary>
    /// <param name="tag">单个标签的掩码。</param>
    public bool Has(ulong tag) => (tags & tag) != 0UL;

    /// <summary>清空全部标签。</summary>
    public void Clear() => tags = 0UL;

    /// <summary>取一颗弹幕的标签掩码。没有挂过就是 0。</summary>
    /// <param name="projectile">要查的弹幕。</param>
    public static ulong TagMaskOf(Projectile projectile) => projectile.GetGlobalProjectile<MonoProjTags>().Tags;

    /// <summary>取一个 NPC 接触伤害的标签掩码。NPC 自己不带标签，这里恒为 0，留着是为了让两侧的调用形态一致。</summary>
    /// <param name="npc">要查的 NPC。</param>
    public static ulong TagMaskOf(NPC npc) => 0UL;

    /// <summary>取一个物品的标签掩码。</summary>
    /// <param name="item">要查的物品。</param>
    public static ulong TagMaskOf(Item item) => MonoItemTags.TagMaskOf(item);
}

/// <summary>
/// 给物品挂伤害标签。与 <see cref="MonoProjTags"/> 成对：近战物品的那一段伤害走物品这条钩子，
/// 不经过弹幕。
/// <para>
/// 用 <c>Add</c> 拼标签，让"这一下同时是火和物理"这类复合写法保持简短。
/// </para>
/// </summary>
public sealed class MonoItemTags : GlobalItem
{
    private ulong tags;

    /// <inheritdoc/>
    public override bool InstancePerEntity => true;

    /// <summary>这份标签掩码。</summary>
    public ulong Tags => tags;

    /// <summary>加上一个或多个标签。</summary>
    /// <param name="tag">标签掩码。</param>
    public void Add(ulong tag) => tags |= tag;

    /// <summary>去掉一个或多个标签。</summary>
    /// <param name="tag">标签掩码。</param>
    public void Remove(ulong tag) => tags &= ~tag;

    /// <summary>是否带某个标签。</summary>
    /// <param name="tag">单个标签的掩码。</param>
    public bool Has(ulong tag) => (tags & tag) != 0UL;

    /// <summary>清空全部标签。</summary>
    public void Clear() => tags = 0UL;

    /// <summary>取一个物品的标签掩码。没有挂过就是 0。</summary>
    /// <param name="item">要查的物品。</param>
    public static ulong TagMaskOf(Item item) => item.GetGlobalItem<MonoItemTags>().Tags;
}
/// <summary>
/// 给 <see cref="Item"/> 和 <see cref="Projectile"/> 挂伤害标签的扩展方法。
/// </summary>
public static class MonoTagExtension
{
    /// <summary>
    /// 获取物品的伤害标签。
    /// </summary>
    /// <param name="item">要获取标签的物品。</param>
    /// <returns></returns>
    public static MonoItemTags MonoTag(this Item item) => item.GetGlobalItem<MonoItemTags>();

    /// <summary>
    /// 获取弹幕的伤害标签。
    /// </summary>
    /// <param name="projectile">要获取标签的弹幕。</param>
    /// <returns></returns>
    public static MonoProjTags MonoTag(this Projectile projectile) => projectile.GetGlobalProjectile<MonoProjTags>();
}
