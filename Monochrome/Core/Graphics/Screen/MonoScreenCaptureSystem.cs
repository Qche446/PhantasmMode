using System.Text;
using Monochrome.Core.Graphics.PostProcessing;

namespace Monochrome.Core.Graphics.Screen;

/// <summary>
/// 把注册进来的后处理链自动作用到整个世界画面上。用法：<c>Register(pipeline)</c> 一次，之后不必再管时机。
/// <para>
/// 插入点是 <see cref="FilterManager.EndCapture"/>：本类在 <c>orig</c> 之前原地改掉 <c>screenTarget1</c>
/// （<c>Main.cs:60145</c>），原版随后会把它合成到屏幕上，回贴因此是白来的。该时机正好在世界画完、
/// 界面还没开始之间（<c>60148</c> 起才画聊天与物品栏），所以效果覆盖世界与世界内界面，不覆盖 HUD。
/// </para>
/// <para>
/// 捕获是按需申请的：原版只在 <c>CanCapture()</c> 为真时才把世界画进离屏目标
/// （<c>FilterManager.cs:219-226</c>）。本类在至少一条链真正启用了 pass 时才订阅它依赖的
/// <c>OnPostDraw</c>，空闲时立刻退订，因为订阅本身会让整局游戏每帧多付一次渲染目标切换与全屏复制。
/// 同步点选 <c>PreUpdateEntities</c>：它在 <c>DoDraw</c> 之前，且在世界更新闸门之外。
/// </para>
/// </summary>
public sealed class MonoScreenCaptureSystem : ModSystem
{
    /// <summary>注册表条目。序号只用来给同 Order 的链一个稳定的先后，别让排序算法的不稳定性改顺序。</summary>
    private sealed record Entry(MonoPostFxPipeline Pipeline, int Order, int Sequence, string Label);

    private static readonly List<Entry> entries = [];

    private static int nextSequence;
    private static bool subscribed;

    /// <summary>总开关。关掉后立刻退订，一帧的额外开销都不产生。</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>已注册的链数量。</summary>
    public static int Count => entries.Count;

    /// <summary>当前是否已经向原版申请了屏幕捕获（即 <c>Filters.Scene.OnPostDraw</c> 被订阅）。诊断用。</summary>
    public static bool CaptureRequested => subscribed;

    /// <summary>已注册的链里是否有至少一个启用的 pass。诊断用。</summary>
    public static bool HasActivePipeline
    {
        get
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Pipeline.IsActive)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 注册一条要在世界画面上生效的后处理链。重复注册同一条会被忽略。
    /// <para>
    /// 注册之后不用管时机：链里一旦有启用的 pass，本类会自动向原版申请捕获并每帧应用它。
    /// </para>
    /// </summary>
    /// <param name="pipeline">要注册的链。</param>
    /// <param name="order">执行顺序，越小越先作用到画面上（后面的链吃到的是前一条的输出）。</param>
    /// <param name="label">诊断输出里显示的名字；留空则显示内部序号。</param>
    public static void Register(MonoPostFxPipeline pipeline, int order = 0, string label = "")
    {
        if (pipeline is null)
            return;
        foreach (Entry existing in entries)
        {
            if (ReferenceEquals(existing.Pipeline, pipeline))
                return;
        }
        entries.Add(new Entry(pipeline, order, nextSequence++, label));
        entries.Sort(static (a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence));
    }

    /// <summary>注销一条链。</summary>
    /// <param name="pipeline">要注销的链。</param>
    /// <returns>确实注销了才返回 true。</returns>
    public static bool Unregister(MonoPostFxPipeline pipeline)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i].Pipeline, pipeline))
            {
                entries.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>清空注册表。</summary>
    public static void UnregisterAll() => entries.Clear();

    /// <summary>按执行顺序列出已注册链的诊断文字。</summary>
    public static string Describe()
    {
        if (entries.Count == 0)
            return "没有注册任何屏幕后处理链（用 MonoScreenCaptureSystem.Register 注册）。";
        StringBuilder output = new();
        output.Append("屏幕后处理链 ").Append(entries.Count).Append(" 条，总开关 ").Append(Enabled ? "开" : "关");
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            output.Append('\n')
                  .Append("  #").Append(i)
                  .Append(" order=").Append(entry.Order)
                  .Append("  ").Append(entry.Label.Length > 0 ? entry.Label : $"(未命名 {entry.Sequence})")
                  .Append(entry.Pipeline.IsActive ? "  [启用中]" : "  [无启用的 pass]");
        }
        output.Append("\n本帧是否已申请捕获：").Append(subscribed ? "是" : "否");
        return output.ToString();
    }

    /// <summary>挂上世界捕获钩子（客户端）。</summary>
    public override void Load()
    {
        if (!Main.dedServ)
            On_FilterManager.EndCapture += EndCaptureDetour;
    }

    /// <summary>摘掉钩子与捕获申请，并清空注册表。</summary>
    public override void Unload()
    {
        SetCaptureRequest(false);
        if (!Main.dedServ)
            On_FilterManager.EndCapture -= EndCaptureDetour;
        entries.Clear();
        nextSequence = 0;
    }

    /// <summary>
    /// 每帧在 <c>DoDraw</c> 之前同步"这一帧要不要申请屏幕捕获"。
    /// <b>必须在这个钩子里</b>——见类型说明（<c>PostUpdateEverything</c> 在更新闸门内，定格时会停）。
    /// </summary>
    public override void PreUpdateEntities() => SyncCaptureRequest();

    /// <summary>按需订阅/退订捕获申请。空闲时绝不订阅，见类型说明。</summary>
    private static void SyncCaptureRequest() => SetCaptureRequest(Enabled && HasActivePipeline);

    private static void SetCaptureRequest(bool need)
    {
        if (Main.dedServ)
            need = false;
        if (need == subscribed)
            return;
        subscribed = need;
        if (need)
            Filters.Scene.OnPostDraw += OnScreenPostDraw;
        else
            Filters.Scene.OnPostDraw -= OnScreenPostDraw;
    }

    /// <summary>
    /// 让 <c>Filters.Scene.CanCapture()</c> 返回 true，从而让原版把世界画进离屏目标。
    /// 只做这一件事：订阅一个空处理器。真正干活的是 <see cref="EndCaptureDetour"/>。
    /// </summary>
    private static void OnScreenPostDraw()
    {
        // 故意为空。这个订阅的全部意义就是让 FilterManager.CanCapture() 变成 true。
    }

    /// <summary>
    /// 世界捕获的合成点。在 <c>orig</c> <b>之前</b>把 <c>screenTarget1</c> 原地处理掉，
    /// 原版随后就会把处理过的内容合成到屏幕上（等于白送的回贴）。
    /// </summary>
    private static void EndCaptureDetour(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
    {
        ApplyToScene(screenTarget1);
        orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
    }

    /// <summary>
    /// 把每条启用的链依次原地作用到 <paramref name="scene"/> 上。
    /// <para>
    /// <b>为什么这里可以相信 <paramref name="scene"/> 里有本帧的画面</b>：原版只有
    /// <c>flag2</c> 为真时才会调用 <c>EndCapture</c>（<c>Main.cs:59939</c> 与 <c>60145</c>，两处都在
    /// <c>if (flag2)</c> 里面），而同一帧的 <c>BeginCapture</c> 只要被调用就会把渲染目标切到它。
    /// 又因为 <c>flag2</c> 里的 <c>CanCapture()</c> 要求 <c>OnPostDraw != null</c>，而
    /// <see cref="subscribed"/> 正是那个订阅的镜像，所以"我们订阅着"⇒"<c>BeginCapture</c> 没有提前返回"
    /// ⇒"<c>screenTarget1</c> 确实是本帧画面"。不满足就整个跳过，绝不去处理一块内容不明的目标。
    /// </para>
    /// </summary>
    private static void ApplyToScene(RenderTarget2D? scene)
    {
        if (Main.dedServ || !Enabled || !subscribed || entries.Count == 0)
            return;
        if (scene is null || scene.IsDisposed || scene.Width <= 0 || scene.Height <= 0)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            MonoPostFxPipeline pipeline = entries[i].Pipeline;
            if (pipeline.IsActive)
                MonoScreenCapture.Apply(pipeline, scene);
        }
    }
}
