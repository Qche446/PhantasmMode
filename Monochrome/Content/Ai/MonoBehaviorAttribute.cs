namespace Monochrome.Content.Ai;

/// <summary>
/// 把一个公开实例方法登记为某个状态的每帧行为，由 <see cref="MonoFsm{TId}.AutoBind"/> 收集。
/// <para>
/// 用法：<c>[MonoBehavior&lt;MyAI&gt;(MyAI.Idle)] public void IdleBehavior() { ... }</c>，
/// 然后在初始化里调用一次 <c>Fsm.AutoBind(this)</c>。行为方法可以分散在多个 partial 文件里。
/// </para>
/// <para>
/// 方法必须公开、非静态、无参、返回 <see langword="void"/>。不满足的方法静默跳过，
/// 用 <see cref="MonoFsm{TId}.BehaviorCount"/> 或 <see cref="MonoFsm{TId}.Dump"/> 核对有没有漏绑。
/// 同一个状态写多个带该特性的方法时只有其中一个会生效，不要这样写。
/// </para>
/// </summary>
/// <typeparam name="TId">状态标识，与所属状态机的相同。</typeparam>
/// <param name="id">这个方法对应的状态。</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class MonoBehaviorAttribute<TId>(TId id) : Attribute where TId : struct, Enum
{
    /// <summary>这个方法对应的状态。</summary>
    public TId Id { get; } = id;
}
