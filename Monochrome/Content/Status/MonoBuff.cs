using Terraria.DataStructures;

namespace Monochrome.Content.Status;

/// <summary>
/// 状态系统的基类，对标 tML 的 <see cref="ModBuff"/>。
/// <para>
/// 直接继承它，tML 的内容自动加载就会把这个子类注册成一条真实 buff，不需要任何额外声明。
/// 叠层规则、时长、属性影响、转换条件都写在下面的钩子里，库不干预。
/// </para>
/// <para>
/// <b>怎么用</b>：把原本散在 <see cref="ModBuff"/>、<see cref="ModPlayer"/>、<see cref="GlobalNPC"/>
/// 三处的逻辑收进这一个类。tML 自己的钩子（<see cref="ModBuff.Update(Player, ref int)"/>、
/// <see cref="ModBuff.ReApply(Player, int, int)"/>、<see cref="ModBuff.ModifyBuffText"/>、图标绘制、
/// <see cref="ModBuff.RightClick"/>）照常重写；下面这些从 <see cref="ModPlayer"/> /
/// <see cref="GlobalNPC"/> 搬过来的钩子由库按实体逐个派发，只在状态活着的时候被调用。
/// </para>
/// <para>
/// 每实体的数据（层数、计时、阶段）用 <c>npc.MonoBuffs().Data&lt;T&gt;()</c> 取，
/// 见 <see cref="MonoBuffSet"/> 与 <see cref="MonoBuffSlotData"/>。
/// </para>
/// </summary>
public abstract class MonoBuff : ModBuff
{
    /// <summary>
    /// 这个状态在槽表里的位置。由库在加载期按内容顺序分配，消费方只读。
    /// </summary>
    public int Slot { get; internal set; } = -1;

    /// <summary>伴生数据里 <c>Ai</c> 数组的长度。默认 4，对标 <c>NPC.ai</c>。</summary>
    public virtual int AiSize => 4;

    /// <summary>
    /// 建这个状态在某实体上的自定义数据，返回值会填进 <see cref="MonoBuffSlotData.Custom"/>。
    /// <para>默认返回 <see langword="null"/>，此时只用 <c>Ai</c> 数组。</para>
    /// </summary>
    public virtual object? CreateState() => null;

    /// <summary>状态在玩家身上出现的那一 tick 调用一次。</summary>
    /// <param name="player">持有者。</param>
    public virtual void OnApplied(Player player) { }

    /// <summary>状态在 NPC 身上出现的那一 tick 调用一次。</summary>
    /// <param name="npc">持有者。</param>
    public virtual void OnApplied(NPC npc) { }

    /// <summary>
    /// 状态在玩家身上消失的那一 tick 调用一次。
    /// <para>原版不区分「到期」与「被清掉」，两种情况都走这里。</para>
    /// </summary>
    /// <param name="player">持有者。</param>
    public virtual void OnRemoved(Player player) { }

    /// <summary>
    /// 状态在 NPC 身上消失的那一 tick 调用一次。
    /// <para>原版不区分「到期」与「被清掉」，两种情况都走这里。</para>
    /// </summary>
    /// <param name="npc">持有者。</param>
    public virtual void OnRemoved(NPC npc) { }

    /// <summary>玩家每帧重置属性之后调用，用来把这一帧的属性加成写上去。</summary>
    /// <param name="player">持有者。</param>
    public virtual void ResetEffects(Player player) { }

    /// <summary>玩家装备结算之后调用。</summary>
    /// <param name="player">持有者。</param>
    public virtual void PostUpdateEquips(Player player) { }

    /// <summary>玩家结算生命再生时调用，DoT 写在这里。</summary>
    /// <param name="player">持有者。</param>
    public virtual void UpdateBadLifeRegen(Player player) { }

    /// <summary>玩家受击时调用。</summary>
    /// <param name="player">持有者。</param>
    /// <param name="info">这次受击的信息。</param>
    public virtual void OnHurt(Player player, Player.HurtInfo info) { }

    /// <summary>玩家绘制特效时调用。</summary>
    /// <param name="player">持有者。</param>
    /// <param name="drawInfo">这次绘制的信息。</param>
    /// <param name="r">红光分量。</param>
    /// <param name="g">绿光分量。</param>
    /// <param name="b">蓝光分量。</param>
    /// <param name="a">透明分量。</param>
    /// <param name="fullBright">是否全亮绘制。</param>
    public virtual void DrawEffects(Player player, PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright) { }

    /// <summary>NPC 每帧重置属性之后调用。</summary>
    /// <param name="npc">持有者。</param>
    public virtual void ResetEffects(NPC npc) { }

    /// <summary>NPC 的 AI 每 tick 调用一次。</summary>
    /// <param name="npc">持有者。</param>
    public virtual void AI(NPC npc) { }

    /// <summary>NPC 结算生命再生时调用，DoT 写在这里。</summary>
    /// <param name="npc">持有者。</param>
    /// <param name="damage">预计每 hit 伤害，用来显示数字。</param>
    public virtual void UpdateLifeRegen(NPC npc, ref int damage) { }

    /// <summary>NPC 受击结算时调用。</summary>
    /// <param name="npc">持有者。</param>
    /// <param name="modifiers">这次受击的修正器。</param>
    public virtual void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers) { }

    /// <summary>NPC 绘制特效时调用，通常只改 <paramref name="drawColor"/> 做染色。</summary>
    /// <param name="npc">持有者。</param>
    /// <param name="drawColor">NPC 的绘制颜色。</param>
    public virtual void DrawEffects(NPC npc, ref Color drawColor) { }

    /// <summary>NPC 本体绘制之前调用。返回 <see langword="false"/> 会拦掉本体绘制。</summary>
    /// <param name="npc">持有者。</param>
    /// <param name="spriteBatch">绘制目标。</param>
    /// <param name="screenPos">屏幕位置偏移。</param>
    /// <param name="drawColor">NPC 的绘制颜色。</param>
    public virtual bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => true;

    /// <summary>NPC 本体绘制之后调用。</summary>
    /// <param name="npc">持有者。</param>
    /// <param name="spriteBatch">绘制目标。</param>
    /// <param name="screenPos">屏幕位置偏移。</param>
    /// <param name="drawColor">NPC 的绘制颜色。</param>
    public virtual void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) { }

    /// <summary>NPC 死亡时调用。</summary>
    /// <param name="npc">持有者。</param>
    public virtual void OnKill(NPC npc) { }
}
