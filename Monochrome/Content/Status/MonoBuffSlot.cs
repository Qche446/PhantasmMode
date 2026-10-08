namespace Monochrome.Content.Status;

/// <summary>
/// 按状态类型拿槽位的静态入口。
/// <para>
/// 用法：<c>MonoBuffSlot&lt;HallowFlameBuff&gt;.Index</c>。取的是 <see cref="MonoBuff.Slot"/>，
/// 由库在加载期分配，所以要在内容加载完成之后调用——写在静态字段初始化里会赶在分配之前。
/// </para>
/// </summary>
/// <typeparam name="T">状态类型。</typeparam>
public static class MonoBuffSlot<T> where T : MonoBuff
{
    /// <summary>这个状态在槽表里的位置。</summary>
    public static int Index => ModContent.GetInstance<T>().Slot;
}
