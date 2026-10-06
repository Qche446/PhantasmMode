using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Graphics.RenderTargets;
using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.Metaballs;

/// <summary>
/// 按"层"收集融合球，并把它们画成真正的等值面。同一层内的球互相融合，不同层互不影响。
/// <para>
/// 两种提交方式，可以混在同一层里一起融合：
/// <see cref="Add"/> 提交 <see cref="MonoMetaball"/>（层在每帧 <c>PreUpdateWorld</c> 被清空，所以等于
/// 每帧重新提交）；<c>new MyBlob(pos).Spawn()</c> 提交 <see cref="MonoMetaballInstance"/>
/// （由本类在 <c>PostUpdateWorld</c> 推进，层名由 <see cref="MonoMetaballInstance.LayerName"/> 提供）。
/// </para>
/// <para>
/// <see cref="AutoDraw"/> 默认为 true，所以提交即显示，不需要手动调 <see cref="Draw"/>。GPU 路线分两段：
/// 先把每层的球加法混合进该层的半分辨率目标（<c>Monochrome.MetaballField</c>），再用 <c>smoothstep</c>
/// 阈值切成等值面并加边缘带（<c>Monochrome.MetaballComposite</c>）。时机、<see cref="DrawFallback"/>
/// 的局限与 <see cref="Sample"/> 的用途见图形文档 §5.9。
/// </para>
/// </summary>
public sealed class MonoMetaballManager : ModSystem
{
    /// <summary>累积 pass 的注册名（= 模组名 + 文件名，由 <see cref="MonoShaderManager"/> 的自动加载约定生成）。</summary>
    public const string FieldShaderName = "Monochrome.MetaballField";

    /// <summary>合成 pass 的注册名。</summary>
    public const string CompositeShaderName = "Monochrome.MetaballComposite";

    /// <summary>
    /// 没有显式指定层名时用的层。<see cref="MonoMetaballInstance.LayerName"/> 的默认值就是它，
    /// 所以 <c>new MyBlob(pos).Spawn()</c> 不需要层名——和 <c>MonoParticle.Spawn()</c> 的用法一致。
    /// </summary>
    public const string DefaultLayerName = "default";

    /// <summary>一次性提交的球（每帧被 <c>PreUpdateWorld</c> 清空）。</summary>
    private static readonly Dictionary<string, List<MonoMetaball>> layers = [with(StringComparer.OrdinalIgnoreCase)];

    /// <summary>有行为的实例（长期持有，自己决定何时死）。</summary>
    private static readonly Dictionary<string, List<MonoMetaballInstance>> instances = [with(StringComparer.OrdinalIgnoreCase)];

    /// <summary>每个层自己的半分辨率累积目标。</summary>
    private static readonly Dictionary<string, MonoManagedRenderTarget> fieldTargets = [with(StringComparer.OrdinalIgnoreCase)];

    /// <summary>复用的层名暂存表，避免每帧为了"遍历两个字典的并集"而分配。</summary>
    private static readonly List<string> scratchLayers = [];

    private static readonly MonoMetaballBrush brush = new();

    private static Texture2D? pixel;
    private static bool warnedMissingShaders;

    /// <summary>
    /// 纯加法混合 <c>(One, One)</c>，供累积 pass 使用。
    /// <para>
    /// <b>不能用 <see cref="BlendState.Additive"/></b>：FNA 里那个预设是
    /// <c>(SourceAlpha, SourceAlpha, One, One)</c>，而累积着色器的输出<b>已经预乘过</b>影响力，
    /// 用它会再乘一次 alpha，结果变成影响力的平方。
    /// </para>
    /// </summary>
    private static readonly BlendState MetaballAdditive = new()
    {
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.Add,
        AlphaBlendFunction = BlendFunction.Add,
    };

    /// <summary>GPU 融合场的全局参数（只管 <see cref="MonoMetaball"/> 式的一次性提交；实例有自己的影响力幂）。</summary>
    public static MonoMetaballSettings Settings { get; } = new();

    /// <summary>
    /// 是否自动把每一层合成到屏幕（默认 <see langword="true"/>）。
    /// <para>
    /// 打开时本类自己挂 <c>On_Main.DrawDust</c>，在世界的绘制相位把<b>所有有内容的层</b>依次合成，
    /// 调用方因此只要 <c>Spawn</c> / <c>Add</c>，<b>不需要手动调 <see cref="Draw"/></b>——和粒子一样"提交即显示"。
    /// </para>
    /// <para>
    /// 需要自己控制时机或目标（例如把场合成进某个 RenderTarget 而不是屏幕）时设为 false，再自己调
    /// <see cref="Draw"/>。<b>两者同时做会让同一层被画两遍。</b>
    /// </para>
    /// </summary>
    public static bool AutoDraw { get; set; } = true;

    /// <summary>
    /// 两个着色器是否都已注册。为 <see langword="false"/> 时 <see cref="Draw"/> 什么都不做（只记一次警告），
    /// 调用方应当改用 <see cref="DrawFallback"/>。
    /// </summary>
    public static bool GpuAvailable
        => MonoShaderManager.TryGet(FieldShaderName, out _) && MonoShaderManager.TryGet(CompositeShaderName, out _);

    /// <summary>把一颗<b>一次性</b>的球提交到指定层。专用服务器上直接忽略。</summary>
    /// <param name="layer">层名（不区分大小写）。</param>
    /// <param name="ball">球。</param>
    public static void Add(string layer, MonoMetaball ball)
    {
        if (Main.dedServ)
            return;
        if (!layers.TryGetValue(layer, out List<MonoMetaball>? list))
            layers[layer] = list = [];
        list.Add(ball);
    }

    /// <summary>把一个<b>实例</b>纳入活动集合。已在集合中的实例会被忽略；换层名则会搬家（保留年龄）。</summary>
    /// <param name="layer">层名。</param>
    /// <param name="instance">实例。</param>
    internal static void Spawn(string layer, MonoMetaballInstance instance)
    {
        if (instance.Active)
        {
            if (string.Equals(instance.Layer, layer, StringComparison.OrdinalIgnoreCase))
                return;
            Remove(instance);
        }
        else
        {
            instance.Time = 0;
        }

        if (!instances.TryGetValue(layer, out List<MonoMetaballInstance>? list))
            instances[layer] = list = [];

        instance.Layer = layer;
        instance.Active = true;
        list.Add(instance);
    }

    /// <summary>取某一层的一次性球。层不存在时返回空数组（不分配）。</summary>
    /// <param name="layer">层名。</param>
    public static IReadOnlyList<MonoMetaball> Get(string layer)
        => layers.TryGetValue(layer, out List<MonoMetaball>? list) ? list : Array.Empty<MonoMetaball>();

    /// <summary>取某一层的活动实例。层不存在时返回空数组（不分配）。</summary>
    /// <param name="layer">层名。</param>
    public static IReadOnlyList<MonoMetaballInstance> GetInstances(string layer)
        => instances.TryGetValue(layer, out List<MonoMetaballInstance>? list) ? list : Array.Empty<MonoMetaballInstance>();

    /// <summary>
    /// 采样某一层在某点的融合场强度（一次性球与实例之和）。
    /// <para>
    /// 对使用<b>默认形状</b>的球，这就是 GPU 累积场的 CPU 版本（幂次也一致）。
    /// 覆写了 <see cref="MonoMetaballInstance.DrawField"/> 却没覆写 <see cref="MonoMetaballInstance.InfluenceAt"/> 的实例，
    /// 在这里只能按默认形状估算。
    /// </para>
    /// </summary>
    /// <param name="layer">层名。</param>
    /// <param name="point">采样点（世界坐标）。</param>
    public static float Sample(string layer, Vector2 point)
    {
        float power = Math.Max(0.0001f, Settings.InfluencePower);
        float sum = 0f;
        foreach (MonoMetaball ball in Get(layer))
            sum += MathF.Pow(ball.Falloff(point), power) * ball.Strength;
        foreach (MonoMetaballInstance instance in GetInstances(layer))
            sum += instance.InfluenceAt(point);
        return sum;
    }

    /// <summary>
    /// 永久移除一个层：清空它的一次性球与实例、释放并注销它的累积目标。
    /// 层名不应该被动态批量创建，否则累积目标会一直占着显存。
    /// </summary>
    /// <param name="layer">层名。</param>
    public static void FreeLayer(string layer)
    {
        layers.Remove(layer);
        if (instances.Remove(layer, out List<MonoMetaballInstance>? list))
        {
            foreach (MonoMetaballInstance instance in list)
                instance.Active = false;
        }
        if (fieldTargets.Remove(layer, out MonoManagedRenderTarget? target))
            target.Retire();
    }

    /// <summary>
    /// 把一个层的融合场合成到<b>当前</b>渲染目标（等值面阈值 + 边缘带）。
    /// <para>
    /// <b>走的是原始绘制路径</b>，与 <c>MonoPrim.*</c> 同一约定：必须落在
    /// <c>spriteBatch.End()</c> 与下一次 <c>Begin()</c> 之间，<b>不需要也不接受 <see cref="SpriteBatch"/> 参数</b>。
    /// 这与 <see cref="DrawFallback"/> 的约定<b>相反</b>（后者需要活动批次），别混用。
    /// </para>
    /// <para>
    /// 累积已经在 <c>Main.OnPreDraw</c> 做完了，所以本方法只做一次整屏四边形绘制。
    /// 专用服务器、层里没有任何球、本帧尚未累积、着色器缺失时都安静返回。
    /// </para>
    /// </summary>
    /// <param name="layer">层名。</param>
    public static void Draw(string layer)
    {
        if (Main.dedServ)
            return;
        bool hasInline = layers.TryGetValue(layer, out List<MonoMetaball>? inline) && inline.Count > 0;
        bool hasInstances = instances.TryGetValue(layer, out List<MonoMetaballInstance>? live) && live.Count > 0;
        if (!hasInline && !hasInstances)
            return;
        if (!fieldTargets.TryGetValue(layer, out MonoManagedRenderTarget? target) || target.IsUninitialized)
            return;
        if (!MonoShaderManager.TryGet(CompositeShaderName, out MonoShader? shader) || shader is null)
        {
            WarnMissingShaders();
            return;
        }

        RenderTarget2D field = target.Target;
        shader.SetParameter("uThreshold", Settings.Threshold)
              .SetParameter("uSoftness", Math.Max(0.0001f, Settings.Softness))
              .SetParameter("uEdgeWidth", Math.Max(0f, Settings.EdgeWidth))
              .SetParameter("uEdgeColor", Settings.EdgeColor.ToVector4())
              .SetParameter("uOpacity", Settings.Opacity);

        Vector2 screen = new(Main.screenWidth, Main.screenHeight);
        // 场是半分辨率的，所以缩放到整屏；用 Screen 空间（不含相机变换的裸正交投影）。
        MonoPrimitiveRenderer.RenderQuad(
            field,
            screen * 0.5f,
            screen / field.Size(),
            0f,
            Color.White,
            shader,
            BlendState.AlphaBlend,
            MonoGraphicsSpace.Screen);
    }

    /// <summary>
    /// CPU 回退绘制：用半透明圆盘 + 一圈描边近似"融合球"，实例与一次性球都会画。
    /// <para>
    /// <b>这是降级形态，不是真正的等值面提取</b>——它不会让两个球粘连。
    /// 只在 GPU 路径不可用（着色器资产缺失）或你确实需要"在活动批次里直接画"时使用；正式效果请用 <see cref="Draw"/>。
    /// </para>
    /// <para>
    /// 约定与 <see cref="Draw"/> 相反：调用方需自行保证 <paramref name="spriteBatch"/> 已 <c>Begin</c>
    /// （世界变换矩阵），且本次调用落在两次 <c>Begin</c>/<c>End</c> 之间。
    /// </para>
    /// </summary>
    /// <param name="spriteBatch">已开始绘制的批次。</param>
    /// <param name="layer">层名。</param>
    /// <param name="edgeThickness">描边宽度（像素）。</param>
    public static void DrawFallback(SpriteBatch spriteBatch, string layer, float edgeThickness = 3f)
    {
        pixel ??= ModContent.Request<Texture2D>("Terraria/Images/MagicPixel").Value;
        foreach (MonoMetaball ball in Get(layer))
        {
            spriteBatch.Draw(pixel, ball.Position - Main.screenPosition, null, ball.Color * 0.22f, 0f, Vector2.One * 0.5f, new Vector2(ball.Radius * 2f), SpriteEffects.None, 0f);
            MonoPrimitiveRenderer.DrawCircle(ball.Position, ball.Radius, ball.Color, edgeThickness);
        }
        foreach (MonoMetaballInstance instance in GetInstances(layer))
        {
            spriteBatch.Draw(pixel, instance.Position - Main.screenPosition, null, instance.Color * 0.22f, 0f, Vector2.One * 0.5f, new Vector2(instance.CurrentRadius * 2f), SpriteEffects.None, 0f);
            MonoPrimitiveRenderer.DrawCircle(instance.Position, instance.CurrentRadius, instance.Color, edgeThickness);
        }
    }

    /// <summary>每帧世界更新开始时清空所有<b>一次性</b>球，使 <see cref="Add"/> 成为"每帧重新提交"。实例不受影响。</summary>
    public override void PreUpdateWorld()
    {
        if (Main.dedServ)
            return;
        foreach (List<MonoMetaball> list in layers.Values)
            list.Clear();
    }

    /// <summary>
    /// 推进所有实例：<c>Update()</c> → <c>Position += Velocity</c> → <c>Time++</c> → 回收。
    /// 顺序与 <c>MonoParticleManager</c> 一致。
    /// </summary>
    public override void PostUpdateWorld()
    {
        if (Main.dedServ || instances.Count == 0)
            return;
        foreach (List<MonoMetaballInstance> list in instances.Values)
        {
            // 倒序遍历：RemoveAt 会改变后续索引。Update() 往列表尾部加东西也不影响已处理的区间。
            for (int i = list.Count - 1; i >= 0; i--)
            {
                MonoMetaballInstance instance = list[i];
                instance.Update();
                instance.Position += instance.Velocity;
                instance.Time++;
                if (instance.ShouldKill())
                {
                    list.RemoveAt(i);
                    instance.Active = false;
                }
            }
        }
    }

    /// <summary>
    /// 世界卸载时清空全部实例与一次性球。
    /// <b>必须清实例</b>：它们通常持有上一个世界的对象引用（NPC/玩家），留着会继续飘。
    /// </summary>
    public override void OnWorldUnload()
    {
        ClearInstances();
        if (Main.dedServ)
            return;
        foreach (List<MonoMetaball> list in layers.Values)
            list.Clear();
    }

    /// <summary>把所有实例从活动集合里摘掉。实例对象本身不被释放，只是变为非活动。</summary>
    public static void ClearInstances()
    {
        foreach (List<MonoMetaballInstance> list in instances.Values)
        {
            foreach (MonoMetaballInstance instance in list)
                instance.Active = false;
        }
        instances.Clear();
    }

    /// <summary>挂上累积时机（<c>Main.OnPreDraw</c>）与自动合成时机（<c>On_Main.DrawDust</c>）。</summary>
    public override void Load()
    {
        if (Main.dedServ)
            return;
        MonoRenderTargetManager.RenderTargetUpdateLoop += PrepareTargets;
        On_Main.DrawDust += DrawAllLayers;
    }

    /// <summary>摘钩子、释放全部累积目标与层数据。</summary>
    public override void Unload()
    {
        if (!Main.dedServ)
        {
            MonoRenderTargetManager.RenderTargetUpdateLoop -= PrepareTargets;
            On_Main.DrawDust -= DrawAllLayers;
        }
        foreach (MonoManagedRenderTarget target in fieldTargets.Values)
            target.Retire();
        fieldTargets.Clear();
        layers.Clear();
        instances.Clear();
        scratchLayers.Clear();
        pixel = null;
        warnedMissingShaders = false;
    }

    /// <summary>
    /// 自动合成：在所有有内容的层上依次调 <see cref="Draw"/>。
    /// <para>
    /// 挂在 <c>On_Main.DrawDust</c> 且先调 <c>orig</c>——那是经过核实的"落在
    /// <c>spriteBatch.End()</c> 与下一次 <c>Begin()</c> 之间"的时机，正好允许原始绘制。
    /// </para>
    /// </summary>
    private static void DrawAllLayers(On_Main.orig_DrawDust orig, Main self)
    {
        orig(self);
        if (!AutoDraw)
            return;
        CollectActiveLayers();
        foreach (string name in scratchLayers)
            Draw(name);
    }

    /// <summary>
    /// 累积 pass：把每一层的球（一次性球 + 实例）用加法混合画进该层的半分辨率目标。
    /// 由 <see cref="MonoRenderTargetManager.RenderTargetUpdateLoop"/> 在 <c>Main.OnPreDraw</c> 调用，
    /// 此时没有任何活动批次，因此可以直接做原始绘制与切换渲染目标。
    /// </summary>
    private static void PrepareTargets()
    {
        CollectActiveLayers();
        if (scratchLayers.Count == 0)
            return;
        if (!MonoShaderManager.TryGet(FieldShaderName, out MonoShader? shader) || shader is null)
        {
            WarnMissingShaders();
            return;
        }

        GraphicsDevice device = Main.instance.GraphicsDevice;
        RenderTargetBinding[] oldTargets = device.GetRenderTargets();
        Viewport oldViewport = device.Viewport;
        brush.Begin(shader, MetaballAdditive, Math.Clamp(Settings.Segments, 3, 256));
        try
        {
            foreach (string name in scratchLayers)
                AccumulateLayer(device, name);
        }
        finally
        {
            device.SetRenderTargets(oldTargets);
            device.Viewport = oldViewport;
        }
    }

    /// <summary>把"有内容的层名"收进复用暂存表（两个字典的并集），避免每帧分配。</summary>
    private static void CollectActiveLayers()
    {
        scratchLayers.Clear();
        foreach ((string name, List<MonoMetaball> inline) in layers)
        {
            if (inline.Count > 0 || (instances.TryGetValue(name, out List<MonoMetaballInstance>? live) && live.Count > 0))
                scratchLayers.Add(name);
        }
        foreach ((string name, List<MonoMetaballInstance> live) in instances)
        {
            if (live.Count > 0 && !layers.ContainsKey(name))
                scratchLayers.Add(name);
        }
    }

    /// <summary>把一层的全部内容画进它的累积目标（先清成透明，加法累积必须从零开始）。</summary>
    private static void AccumulateLayer(GraphicsDevice device, string layer)
    {
        RenderTarget2D field = GetFieldTarget(layer).Target;
        device.SetRenderTarget(field);
        // SetRenderTarget 通常会把视口设成目标尺寸，这里显式写一次，免得依赖该行为。
        device.Viewport = new Viewport(0, 0, field.Width, field.Height);
        device.Clear(Color.Transparent);

        // 先取 count 再按索引遍历：万一某个实例的 DrawField 往同一层里加了东西，
        // 也不会在遍历中途改变集合（新加的下一帧再算）。
        if (layers.TryGetValue(layer, out List<MonoMetaball>? inline))
        {
            int count = inline.Count;
            for (int i = 0; i < count; i++)
            {
                MonoMetaball ball = inline[i];
                brush.Ball(ball.Position, ball.Radius, ball.Color, ball.Strength, Settings.InfluencePower);
            }
        }
        if (instances.TryGetValue(layer, out List<MonoMetaballInstance>? live))
        {
            int count = live.Count;
            for (int i = 0; i < count; i++)
                live[i].DrawField(brush);
        }
    }

    /// <summary>取（必要时创建）某一层的半分辨率累积目标。</summary>
    private static MonoManagedRenderTarget GetFieldTarget(string layer)
    {
        if (fieldTargets.TryGetValue(layer, out MonoManagedRenderTarget? existing))
            return existing;

        MonoManagedRenderTarget created = new(
            (width, height) =>
            {
                int divisor = Math.Max(1, Settings.ResolutionDivisor);
                return new RenderTarget2D(
                    Main.instance.GraphicsDevice,
                    Math.Max(1, width / divisor),
                    Math.Max(1, height / divisor),
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None);
            },
            resetOnResize: true,
            // 每帧都要用，交给闲置回收只会让它反复重建。
            subjectToGarbageCollection: false);
        fieldTargets[layer] = created;
        return created;
    }

    /// <summary>把一个实例从它当前的层里摘掉。</summary>
    private static void Remove(MonoMetaballInstance instance)
    {
        if (instances.TryGetValue(instance.Layer, out List<MonoMetaballInstance>? list))
            list.Remove(instance);
        instance.Active = false;
    }

    /// <summary>着色器资产缺失只提醒一次，避免每帧刷屏。</summary>
    private static void WarnMissingShaders()
    {
        if (warnedMissingShaders)
            return;
        warnedMissingShaders = true;
        Monochrome.Instance?.Logger.Warn(
            $"融合球缺少着色器资产（{FieldShaderName} / {CompositeShaderName}），GPU 路径不可用；请改用 MonoMetaballManager.DrawFallback。");
    }
}
