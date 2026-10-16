namespace Monochrome.Common.MonoUtil;

/// <summary>
/// 给弹幕打的"我归某个所有者管"标记。所有者清场时用 <see cref="MonoProj.KillOwned{TOwner}"/> 一次收走自己那一批。
/// <para>
/// 泛型参数写所有者类型，是为了让不同的所有者各收各的——同一场战斗里两套弹幕互不干扰。
/// 所有者不必是 NPC：玩家、模组系统、某个战斗阶段的对象都能当。
/// </para>
/// </summary>
/// <typeparam name="TOwner">所有者类型，只用于区分批次。</typeparam>
public interface IMonoOwnedProj<TOwner>
{
    /// <summary>
    /// 清场时是否只把弹幕设成不活动。默认 <see langword="false"/>，即走正常击杀。
    /// 不能触发击杀效果（不掉落、不出特效、不跑 <c>OnKill</c>）的弹幕在这里返回 <see langword="true"/>。
    /// </summary>
    bool SetActiveFalseInsteadOfKill => false;
}
