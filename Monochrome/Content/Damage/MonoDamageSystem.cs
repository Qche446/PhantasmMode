using Monochrome.Core;

namespace Monochrome.Content.Damage;

/// <summary>
/// 伤害规则表的生命周期宿主。
/// <para>
/// 它自己不挂任何钩子：NPC 侧与玩家侧的入口分别是 <see cref="MonoDamageNPC"/> 与
/// <see cref="MonoDamagePlayer"/>，两者靠 tML 的内容自动加载注册，不需要服务代劳。
/// 这个服务存在的理由是另外两件事——把"规则表退场"接进 Monochrome 的退场顺序，
/// 以及在装配期把规则条数写进日志（"我注册的规则到底在不在表里"不该靠猜）。
/// </para>
/// <para>
/// <b>它不冻结规则表。</b>消费者可能在更晚的 <c>PostSetupContent</c> 里才登记规则，
/// 而服务的 <c>Ready</c> 跑在 Monochrome 自己的 <c>PostSetupContent</c> 里，早于消费者的。
/// 冻结交给第一次命中时自动完成，或者由消费者显式调 <see cref="MonoDamage.Freeze"/>。
/// </para>
/// </summary>
public sealed class MonoDamageSystem : IMonoService
{
    /// <summary>玩法系统号段。</summary>
    public int Order => 480;

    /// <inheritdoc/>
    public string Name => nameof(MonoDamageSystem);

    /// <inheritdoc/>
    public void PostSetup()
    {
        string state = MonoDamage.IsFrozen ? "已冻结。" : "尚未冻结，第一次命中时自动冻结。";
        MonoLog.Info(MonoLogLevel.Info, $"伤害规则表：登记 {MonoDamage.Chain.Count} 条规则，{state}");

        if (MonoDamage.Trace.Enabled)
            MonoLog.Warn(MonoLogLevel.Warn, $"伤害追踪仍处于开启状态，命中结算会比平时慢。不需要时用命令关掉。");
    }

    /// <inheritdoc/>
    public void Unload()
    {
        if (MonoDamage.Chain.Count > 0 || MonoDamage.Trace.Count > 0)
            MonoLog.Debug(MonoLogLevel.Debug, $"伤害规则表退场：清掉 {MonoDamage.Chain.Count} 条规则与追踪记录。");

        MonoDamage.Reset();
    }
}
