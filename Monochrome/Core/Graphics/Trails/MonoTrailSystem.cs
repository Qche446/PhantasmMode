namespace Monochrome.Core.Graphics.Trails;

/// <summary>
/// 拖尾模块的装配点。
/// <para>
/// 它管两件事：<b>自动绘制</b>——<see cref="MonoTrail.Spawn"/> 过的世界空间拖尾，由这里在
/// <c>On_Main.DrawDust</c> 里每帧画一次（那时批次恰好是合上的，允许原始绘制）；
/// 以及<b>程序生成那张条纹贴图的生命周期</b>。
/// </para>
/// <para>
/// 三个装载/卸载钩子都清一次表：热重载之后静态字段里可能还留着上一版的对象与委托，
/// 留着它们会在下一次绘制里报"尝试调用已生成的 lambda 或本地函数"。
/// </para>
/// </summary>
public sealed class MonoTrailSystem : ModSystem
{
    private static readonly List<MonoTrail> autoDrawn = [];

    /// <summary>当前自动绘制的拖尾。</summary>
    public static IReadOnlyList<MonoTrail> AutoDrawn => autoDrawn;

    /// <inheritdoc/>
    public override void Load()
    {
        ClearAutoDrawn();
        MonoTrailTextures.Dispose();

        if (!Main.dedServ)
            On_Main.DrawDust += DrawAutoDrawn;
    }

    /// <inheritdoc/>
    public override void Unload()
    {
        if (!Main.dedServ)
            On_Main.DrawDust -= DrawAutoDrawn;

        ClearAutoDrawn();
        MonoTrailTextures.Dispose();
    }

    /// <inheritdoc/>
    public override void OnModUnload()
    {
        ClearAutoDrawn();
        MonoTrailTextures.Dispose();
    }

    /// <summary>把一条拖尾交给自动绘制。已在表里的实例会被忽略。</summary>
    /// <param name="trail">拖尾。</param>
    internal static void Attach(MonoTrail trail)
    {
        if (trail.Active)
            return;

        trail.Active = true;
        autoDrawn.Add(trail);
    }

    /// <summary>把一条拖尾从自动绘制里摘掉。</summary>
    /// <param name="trail">拖尾。</param>
    internal static void Detach(MonoTrail trail)
    {
        if (!trail.Active)
            return;

        trail.Active = false;
        autoDrawn.Remove(trail);
    }

    /// <summary>清空自动绘制表。</summary>
    public static void ClearAutoDrawn()
    {
        foreach (MonoTrail trail in autoDrawn)
            trail.Active = false;

        autoDrawn.Clear();
    }

    /// <summary>私有钩子：先调 <c>orig</c>，再把自动绘制的拖尾逐个画掉。</summary>
    private static void DrawAutoDrawn(On_Main.orig_DrawDust orig, Main self)
    {
        orig(self);
        if (Main.dedServ)
            return;

        // 倒着走：某条拖尾的 Draw 里若把自己 Stop 掉，索引也不会错。
        for (int i = autoDrawn.Count - 1; i >= 0; i--)
            autoDrawn[i].Draw();
    }
}
