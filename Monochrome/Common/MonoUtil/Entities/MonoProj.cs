namespace Monochrome.Common.MonoUtil;

/// <summary>
/// 弹幕侧的通用工具。目前只有"按所有者清场"这一件。
/// </summary>
public static class MonoProj
{
    /// <summary>
    /// 收走所有实现了 <see cref="IMonoOwnedProj{TOwner}"/> 的活动弹幕。
    /// <para>
    /// 谁调谁负责跑在权威端：本方法只遍历并处理，不做 <c>HostCheck</c>，
    /// 也没有"要不要同步"的判断——那是调用方所在上下文的事。
    /// </para>
    /// </summary>
    /// <typeparam name="TOwner">所有者类型。</typeparam>
    /// <returns>被处理掉的弹幕数量。</returns>
    public static int KillOwned<TOwner>()
    {
        int killed = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not IMonoOwnedProj<TOwner> owned)
                continue;

            if (owned.SetActiveFalseInsteadOfKill)
                projectile.active = false;
            else
                projectile.Kill();
            killed++;
        }
        return killed;
    }
}
