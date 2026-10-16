using Monochrome.Common.MonoUtil.Damage;

namespace Monochrome.Content.Damage.Rules;

/// <summary>
/// 内置规则的共同骨架：限定目标类型，外加一个可选条件。
/// <para>
/// <c>targetType</c> 填具体类型时，规则会被分进那一类的执行序列，别的目标根本不会遍历到它；
/// 留 <c>-1</c> 时它对所有目标可见，靠 <c>when</c> 过滤。
/// </para>
/// <para>
/// <c>when</c> 做成委托，省掉为一行条件单开一个类。绝大多数条件就是一句判断
/// （例如"这个 boss 处在第二阶段"）。条件里要读实体就自己用
/// <c>info.TargetIndex</c> 去索引——库不碰实体，这一层是消费者的自由。
/// </para>
/// </summary>
public abstract class MonoPredicatedRule : MonoDamageRule
{
    private readonly int targetType;
    private readonly Func<MonoDamageInfo, bool>? when;

    /// <summary>建一条带目标类型与条件的规则。</summary>
    /// <param name="targetType">只管这一类目标；<c>-1</c> 表示所有目标。</param>
    /// <param name="when">额外条件，为 <c>null</c> 表示不额外过滤。</param>
    protected MonoPredicatedRule(int targetType, Func<MonoDamageInfo, bool>? when)
    {
        this.targetType = targetType;
        this.when = when;
    }

    /// <inheritdoc/>
    public override int TargetType => targetType;

    /// <inheritdoc/>
    public override bool AppliesTo(in MonoDamageInfo info) => when is null || when(info);
}
