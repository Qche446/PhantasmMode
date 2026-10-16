using Monochrome.Core.Graphics.RenderTargets;

namespace Monochrome.Core.Graphics.Primitives;

/// <summary>
/// 像素化图元的收集、绘制与贴出。
/// <para>
/// 四个层各有一张<b>半分辨率</b>离屏目标。每帧在世界绘制之前，把实现
/// <see cref="IMonoPixelatedPrimitive"/> 的活动 NPC 与弹幕按 <see cref="IMonoPixelatedPrimitive.LayerToRenderTo"/>
/// 分桶，逐个画进对应目标；随后在这些层的世界绘制时机把目标以 <b>2 倍、点采样</b>贴到屏幕上——
/// 半分辨率加最近邻放大就是"像素块"的来源。
/// </para>
/// <para>
/// 消费者的图元要落在"屏幕坐标 + 不含缩放的正交投影"上，对应
/// <c>MonoPrimitiveSettings.UseUnscaledMatrix = true</c>（<c>Space</c> 保持默认的
/// <see cref="MonoGraphicsSpace.World"/>，位置照旧给世界坐标）。
/// </para>
/// </summary>
public sealed class MonoPixelationSystem : ModSystem
{
    /// <summary>层的固定顺序。遍历一律用它，避免依赖字典的枚举顺序。</summary>
    private static readonly MonoPixelationLayer[] Layers =
    [
        MonoPixelationLayer.BeforeNPCs,
        MonoPixelationLayer.AfterNPCs,
        MonoPixelationLayer.BeforeProjectiles,
        MonoPixelationLayer.AfterProjectiles,
    ];

    private static readonly Dictionary<MonoPixelationLayer, MonoManagedRenderTarget> targets = [];
    private static readonly Dictionary<MonoPixelationLayer, List<IMonoPixelatedPrimitive>> buckets = [];

    /// <summary>这一帧是否正在往像素化目标里画。诊断用。</summary>
    public static bool CurrentlyRendering { get; private set; }

    /// <summary>当前已建立的离屏目标数量。一个层只有出现过内容才会建。</summary>
    public static int TargetCount => targets.Count;

    /// <inheritdoc/>
    public override void Load()
    {
        if (Main.dedServ)
            return;

        foreach (MonoPixelationLayer layer in Layers)
            buckets[layer] = [];

        MonoRenderTargetManager.RenderTargetUpdateLoop += Accumulate;
        On_Main.DoDraw_DrawNPCsOverTiles += DrawNpcLayers;
        On_Main.DrawProjectiles += DrawProjectileLayers;
    }

    /// <inheritdoc/>
    public override void Unload()
    {
        if (!Main.dedServ)
        {
            MonoRenderTargetManager.RenderTargetUpdateLoop -= Accumulate;
            On_Main.DoDraw_DrawNPCsOverTiles -= DrawNpcLayers;
            On_Main.DrawProjectiles -= DrawProjectileLayers;
        }

        // 只摘本地表，不做 Retire()：受管目标都登记在 MonoRenderTargetManager 上，由它的卸载钩子
        // 统一排到主线程释放；在这里 Retire() 会当场同步 Dispose，撞 FNA 的主线程检查。
        targets.Clear();
        buckets.Clear();
        CurrentlyRendering = false;
    }

    /// <summary>
    /// 半分辨率目标：屏幕尺寸除以 2，分辨率变化时重建。
    /// <para>每帧都要用，所以不交给闲置回收——那只会让它反复重建。</para>
    /// </summary>
    private static MonoManagedRenderTarget CreateTarget()
        => new(
            static (width, height) => new RenderTarget2D(
                Main.instance.GraphicsDevice,
                Math.Max(1, width / 2),
                Math.Max(1, height / 2),
                false,
                SurfaceFormat.Color,
                DepthFormat.None),
            resetOnResize: true,
            subjectToGarbageCollection: false);

    /// <summary>
    /// 收集 + 绘制：把这一帧要实现该接口的活动 NPC 与弹幕分桶，逐个画进各自层的目标。
    /// 挂在 <c>MonoRenderTargetManager.RenderTargetUpdateLoop</c>（时机是 <c>Main.OnPreDraw</c>），
    /// 那里没有开着的批次，允许原始绘制与切换目标。
    /// </summary>
    private static void Accumulate()
    {
        if (Main.gameMenu || !MonoManagedRenderTarget.IsAvailable)
            return;

        foreach (List<IMonoPixelatedPrimitive> bucket in buckets.Values)
            bucket.Clear();
        Collect();

        GraphicsDevice device = Main.instance.GraphicsDevice;
        RenderTargetBinding[] oldTargets = device.GetRenderTargets();
        Viewport oldViewport = device.Viewport;
        try
        {
            CurrentlyRendering = true;
            foreach (MonoPixelationLayer layer in Layers)
            {
                List<IMonoPixelatedPrimitive> bucket = buckets[layer];

                // 从没出现过内容的层不建目标；建过而这一帧空了的层仍要清一次，
                // 否则它最后画过的那一帧会一直留在屏幕上。
                if (bucket.Count == 0 && !targets.ContainsKey(layer))
                    continue;

                if (!targets.TryGetValue(layer, out MonoManagedRenderTarget? target))
                    targets[layer] = target = CreateTarget();

                device.SetRenderTarget(target.Target);
                device.Viewport = new Viewport(0, 0, target.Target.Width, target.Target.Height);
                device.Clear(Color.Transparent);

                // 先取 count 再按索引遍历：万一某个消费者在绘制里往同一层加了东西，也不会中途改集合。
                for (int i = 0; i < bucket.Count; i++)
                    bucket[i].RenderPixelatedPrimitives();
            }
        }
        finally
        {
            CurrentlyRendering = false;
            device.SetRenderTargets(oldTargets);
            device.Viewport = oldViewport;
        }
    }

    /// <summary>把活动 NPC 与弹幕里实现了本接口的那些按层分桶。</summary>
    private static void Collect()
    {
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is IMonoPixelatedPrimitive pixelated)
                buckets[pixelated.LayerToRenderTo].Add(pixelated);
        }

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.ModNPC is IMonoPixelatedPrimitive pixelated)
                buckets[pixelated.LayerToRenderTo].Add(pixelated);
        }
    }

    /// <summary>NPC 那一组时机：<c>BeforeNPCs</c> 在 <c>orig</c> 之前，<c>AfterNPCs</c> 在之后。</summary>
    private static void DrawNpcLayers(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
    {
        Blit(MonoPixelationLayer.BeforeNPCs);
        orig(self);
        Blit(MonoPixelationLayer.AfterNPCs);
    }

    /// <summary>弹幕那一组时机：<c>BeforeProjectiles</c> 在 <c>orig</c> 之前，<c>AfterProjectiles</c> 在之后。</summary>
    private static void DrawProjectileLayers(On_Main.orig_DrawProjectiles orig, Main self)
    {
        Blit(MonoPixelationLayer.BeforeProjectiles);
        orig(self);
        Blit(MonoPixelationLayer.AfterProjectiles);
    }

    /// <summary>
    /// 把某一层的半分辨率目标以 2 倍、点采样贴到屏幕上。
    /// <para>
    /// 矩阵用 <c>Main.GameViewMatrix.TransformationMatrix</c>（只管缩放），与其它世界绘制一致：
    /// 目标里的内容是"世界坐标减 <c>Main.screenPosition</c>"，所以平移已经算在里面了。
    /// </para>
    /// </summary>
    private static void Blit(MonoPixelationLayer layer)
    {
        if (Main.dedServ || !targets.TryGetValue(layer, out MonoManagedRenderTarget? target) || target.IsUninitialized)
            return;

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        Main.spriteBatch.Draw(target.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
        Main.spriteBatch.End();
    }
}
