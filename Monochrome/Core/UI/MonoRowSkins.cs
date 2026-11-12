namespace Monochrome.Core.UI;

/// <summary>
/// 模组列表行皮肤的注册表：模组名 → 画底板（可选再加一段画名字）的绘制。
/// <para>
/// 模组可以在加载完成后（<c>PostSetupContent</c> 及之后）注册自己那一行的皮肤，之后每次打开模组列表都会调到；
/// 条目行每进一次界面就重建一次，所以注册绑的是<b>模组名</b>，不是行对象。
/// </para>
/// <para>
/// 注册过的行会跳过原版 <c>UIPanel.DrawSelf</c>（不再画 tModLoader 的蓝底板与圆角边框），
/// 回调要自己把整块矩形铺满；分隔线、右侧信息图标、图标与按钮仍在回调之后画。
/// 名字文字不传 <c>drawName</c> 就保持 tModLoader 原样；传了就交给它。
/// </para>
/// <para>
/// 只在客户端有意义（专用服务器上钩子根本不装）。全程在 UI 线程上，不需要加锁。
/// </para>
/// </summary>
public static class MonoRowSkins
{
    private sealed class Skin
    {
        internal Skin(Action<MonoRowSkinContext> drawBackground, Action<MonoRowNameContext>? drawName)
        {
            Background = drawBackground;
            Name = drawName;
        }

        internal Action<MonoRowSkinContext> Background { get; }

        internal Action<MonoRowNameContext>? Name { get; }
    }

    private static readonly Dictionary<string, Skin> skins = new(StringComparer.Ordinal);
    private static readonly HashSet<string> disabled = new(StringComparer.Ordinal);

    /// <summary>当前注册了几个皮肤。</summary>
    public static int Count => skins.Count;

    /// <summary>注册某个模组那一行怎么画；同名重复注册以后来的为准。</summary>
    /// <param name="modName">模组名，与 <c>Mod.Name</c> 完全一致（区分大小写）。</param>
    /// <param name="drawBackground">画底板。不要 <c>Begin</c>/<c>End</c>，不要清屏，坐标是 UI 单位。</param>
    /// <param name="drawName">
    /// 可选：自己画名字。不传则保留原版文字，本库不接管。
    /// 位置、字号缩放、字体都已算好，见 <see cref="MonoRowNameContext"/>。
    /// </param>
    public static void Register(string modName, Action<MonoRowSkinContext> drawBackground, Action<MonoRowNameContext>? drawName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(modName);
        ArgumentNullException.ThrowIfNull(drawBackground);

        if (skins.ContainsKey(modName))
            MonoLog.Warn(MonoLogLevel.Warn, $"行皮肤：'{modName}' 的皮肤被后一次注册覆盖了。");

        skins[modName] = new Skin(drawBackground, drawName);
        disabled.Remove(modName);
    }

    /// <summary>注销某个模组的皮肤。返回是否真的注销了一个。</summary>
    /// <param name="modName">模组名。</param>
    public static bool Unregister(string modName)
    {
        if (string.IsNullOrEmpty(modName))
            return false;

        disabled.Remove(modName);
        return skins.Remove(modName);
    }

    /// <summary>这个模组当前有没有皮肤。</summary>
    /// <param name="modName">模组名。</param>
    public static bool IsRegistered(string modName) => !string.IsNullOrEmpty(modName) && skins.ContainsKey(modName);

    /// <summary>丢掉全部皮肤。Monochrome 卸载时会调用，模组自己一般不需要。</summary>
    public static void Clear()
    {
        skins.Clear();
        disabled.Clear();
    }

    internal static bool TryGet(string modName, out Action<MonoRowSkinContext>? draw)
    {
        if (skins.TryGetValue(modName, out Skin? skin))
        {
            draw = skin.Background;
            return true;
        }

        draw = null;
        return false;
    }

    internal static bool TryGetName(string modName, out Action<MonoRowNameContext>? draw)
    {
        if (skins.TryGetValue(modName, out Skin? skin) && skin.Name is not null)
        {
            draw = skin.Name;
            return true;
        }

        draw = null;
        return false;
    }

    /// <summary>
    /// 回调抛异常时把它停掉：继续每帧调用只会让日志与开销一起放大，而且这一行本来就已经画坏了。
    /// 底板与名字共用一条记录，所以任何一半抛异常都会让整条皮肤退回原版。
    /// </summary>
    internal static void Disable(string modName, Exception exception)
    {
        skins.Remove(modName);

        if (disabled.Add(modName))
            MonoLog.Error(MonoLogLevel.Error, $"行皮肤：'{modName}' 的皮肤抛了 {exception.GetType().Name}（{exception.Message}），已停用。以后每次进模组列表那一行都按原版画。");
    }
}
