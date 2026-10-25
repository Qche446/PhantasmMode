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

    /// <summary>手工排进某一层的绘制，见 <see cref="EnqueueRender"/>。</summary>
    private static readonly Dictionary<MonoPixelationLayer, Queue<Action>> manualDrawActions = [];

    /// <summary>已经为"层值非法"告警过的消费者类型，同类型只提示一次。</summary>
    private static readonly HashSet<Type> reportedInvalidLayer = [];

    /// <summary>已经为"绘制抛异常"告警过的类型，同类型只提示一次。</summary>
    private static readonly HashSet<Type> reportedDrawFailure = [];

    /// <summary>这一帧某层有没有内容。空层不贴出。下标就是枚举值本身，加层时记得同步 <see cref="Layers"/>。</summary>
    private static readonly bool[] layerHasContent = new bool[Layers.Length];

    /// <summary>某层连续空了多少帧，到 <see cref="FramesUntilRetire"/> 就回收目标。</summary>
    private static readonly int[] layerEmptyFrames = new int[Layers.Length];

    /// <summary>一个层连续这么多帧没有内容就释放它的目标，下次有内容时再建（约 10 秒）。</summary>
    private const int FramesUntilRetire = 600;

    /// <summary>这一帧是否正在往像素化目标里画。诊断用。</summary>
    public static bool CurrentlyRendering { get; private set; }

    /// <summary>当前已建立的离屏目标数量。层出现过内容才建，连续空下去会被回收。</summary>
    public static int TargetCount => targets.Count;

    /// <inheritdoc/>
    public override void Load()
    {
        if (Main.dedServ)
            return;

        foreach (MonoPixelationLayer layer in Layers)
        {
            buckets[layer] = [];
            manualDrawActions[layer] = [];
        }

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
        manualDrawActions.Clear();
        ClearLayerState();
        CurrentlyRendering = false;
    }

    /// <summary>
    /// 半分辨率目标：屏幕尺寸除以 2，分辨率变化时重建。
    /// <para>
    /// 不交给受管目标的闲置回收：那个回收只释放底层纹理，登记表里的条目还在。
    /// 这里的层由 <see cref="Accumulate"/> 自己按"连续空了 <see cref="FramesUntilRetire"/> 帧"整条摘掉。
    /// </para>
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
        {
            // 这一帧不累积，就别留着上一帧的判断，否则回到游戏里会先贴出一帧陈旧内容。
            ClearLayerState();
            return;
        }

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
                int index = (int)layer;
                List<IMonoPixelatedPrimitive> bucket = buckets[layer];

                // 这一帧没有内容的层什么都不做：不建目标、不清屏、也不贴出。
                // 目标先留着，连续空够 FramesUntilRetire 帧才整条摘掉，之后再有内容时重建。
                if (bucket.Count == 0)
                {
                    layerHasContent[index] = false;
                    if (targets.TryGetValue(layer, out MonoManagedRenderTarget? value) && ++layerEmptyFrames[index] >= FramesUntilRetire)
                    {
                        value.Retire();
                        targets.Remove(layer);
                        layerEmptyFrames[index] = 0;
                    }

                    continue;
                }

                layerHasContent[index] = true;
                layerEmptyFrames[index] = 0;

                if (!targets.TryGetValue(layer, out MonoManagedRenderTarget? target))
                    targets[layer] = target = CreateTarget();

                device.SetRenderTarget(target.Target);
                device.Viewport = new Viewport(0, 0, target.Target.Width, target.Target.Height);
                device.Clear(Color.Transparent);

                // 同一层里的图元攒成一个批次提交：见 MonoPrimitiveBatch，省掉每个实体一次的缓冲上传。
                using (MonoPrim.Batch())
                {
                    // 先取 count 再按索引遍历：万一某个消费者在绘制里往同一层加了东西，也不会中途改集合。
                    for (int i = 0; i < bucket.Count; i++)
                    {
                        IMonoPixelatedPrimitive primitive = bucket[i];
                        try
                        {
                            primitive.RenderPixelatedPrimitives();
                        }
                        catch (Exception exception)
                        {
                            // 一个消费者炸掉不该让整层、整帧消失：记一次告警，接着画下一个。
                            ReportDrawFailure(primitive.GetType(), exception);
                        }
                    }

                    DrainManualDraws(layer);
                }
            }
        }
        finally
        {
            CurrentlyRendering = false;
            device.SetRenderTargets(oldTargets);
            device.Viewport = oldViewport;
        }
    }

    /// <summary>清掉"这一帧哪层有内容"与空帧计数。</summary>
    private static void ClearLayerState()
    {
        for (int i = 0; i < Layers.Length; i++)
        {
            layerHasContent[i] = false;
            layerEmptyFrames[i] = 0;
        }

        ClearManualDraws();
    }

    /// <summary>
    /// 把一段手工绘制排进某一层，和该层的消费者共用同一个批次：执行时机在该层所有
    /// <see cref="IMonoPixelatedPrimitive.RenderPixelatedPrimitives"/> 之后，因此引用的状态已经算好了。
    /// <para>
    /// 排进来的绘制只跑一次。某层这一帧没有任何消费者时该层不绘制，排进来的内容随之丢弃。
    /// </para>
    /// </summary>
    /// <param name="draw">绘制回调，内部同样用 <c>MonoPrim.*</c>。</param>
    /// <param name="layer">落在哪一层，默认 <see cref="MonoPixelationLayer.BeforeProjectiles"/>。</param>
    public static void EnqueueRender(Action draw, MonoPixelationLayer layer = MonoPixelationLayer.BeforeProjectiles)
    {
        if (Main.dedServ || draw is null)
            return;

        if (!Enum.IsDefined(layer))
            layer = MonoPixelationLayer.AfterProjectiles;

        if (!manualDrawActions.TryGetValue(layer, out Queue<Action>? queue))
            manualDrawActions[layer] = queue = new Queue<Action>();
        queue.Enqueue(draw);
    }

    /// <summary>把活动 NPC 与弹幕里实现了本接口的那些按层分桶。</summary>
    private static void Collect()
    {
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is IMonoPixelatedPrimitive pixelated)
                buckets[ResolveLayer(pixelated)].Add(pixelated);
        }

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.ModNPC is IMonoPixelatedPrimitive pixelated)
                buckets[ResolveLayer(pixelated)].Add(pixelated);
        }
    }

    /// <summary>
    /// 取消费者声明的层，非法值（强转出来的数、装进别的类型的枚举）当最后一层处理，
    /// 否则拿它索引字典会抛 <c>KeyNotFoundException</c> 把整帧的累积打断。
    /// </summary>
    private static MonoPixelationLayer ResolveLayer(IMonoPixelatedPrimitive pixelated)
    {
        MonoPixelationLayer layer = pixelated.LayerToRenderTo;
        if (Enum.IsDefined(layer))
            return layer;

        if (reportedInvalidLayer.Add(pixelated.GetType()))
        {
            Monochrome.Instance?.Logger.Warn(
                $"像素化图元：{pixelated.GetType().Name}.LayerToRenderTo 是 {(int)layer}（不是 MonoPixelationLayer 的成员），按 AfterProjectiles 绘制。");
        }

        return MonoPixelationLayer.AfterProjectiles;
    }

    /// <summary>排空某一层的手工绘制。只跑排进来的那批，回调里再排的留到下一帧。</summary>
    private static void DrainManualDraws(MonoPixelationLayer layer)
    {
        if (!manualDrawActions.TryGetValue(layer, out Queue<Action>? queue))
            return;

        int pending = queue.Count;
        for (int i = 0; i < pending; i++)
        {
            Action draw = queue.Dequeue();
            try
            {
                draw();
            }
            catch (Exception exception)
            {
                ReportDrawFailure(draw.Method.DeclaringType ?? typeof(Action), exception);
            }
        }
    }

    /// <summary>清掉所有手工绘制。</summary>
    private static void ClearManualDraws()
    {
        foreach (Queue<Action> queue in manualDrawActions.Values)
            queue.Clear();
    }

    /// <summary>同一类型只告警一次：绘制异常每帧都会重来，逐帧打印会把日志刷爆。</summary>
    private static void ReportDrawFailure(Type owner, Exception exception)
    {
        if (!reportedDrawFailure.Add(owner))
            return;

        Monochrome.Instance?.Logger.Warn(
            $"像素化图元：{owner.Name} 的绘制抛出 {exception.GetType().Name}：{exception.Message}。该类型之后仍会尝试绘制，其余图元不受影响。");
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
    /// <para>这一帧没有内容的层直接跳过：目标里留着上一帧的内容，贴出去就成了残影。</para>
    /// </summary>
    private static void Blit(MonoPixelationLayer layer)
    {
        if (Main.dedServ || !layerHasContent[(int)layer])
            return;
        if (!targets.TryGetValue(layer, out MonoManagedRenderTarget? target) || target.IsUninitialized)
            return;

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        Main.spriteBatch.Draw(target.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
        Main.spriteBatch.End();
    }
}
