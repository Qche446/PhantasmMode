using Monochrome.Core.Graphics.RenderTargets;

namespace Monochrome.Core.Graphics.Metaballs;

/// <summary>
/// 融合球的总控：按类型推进粒子、把每层写进整屏目标，再在弹幕绘制相位把结果合成出去。
/// <para>
/// 类型自己注册（<see cref="MonoMetaballType.Register"/> 会调 <see cref="Register"/>），所以消费者
/// 只要派生一个 <see cref="MonoMetaballType"/> 并提交粒子，不必手动登记、也不必写绘制钩子。
/// </para>
/// <para>
/// 两个时机，与 Luminance 的分工一致：<b>写场</b>挂在 <c>MonoRenderTargetManager.RenderTargetUpdateLoop</c>
/// （即 <c>Main.OnPreDraw</c>，那时没有活动批次，可以切渲染目标）；<b>合成</b>挂在
/// <c>On_Main.DrawProjectiles</c> 的<b>最前面</b>，于是非 UI 层默认画在弹幕之下。
/// </para>
/// <para>
/// <see cref="MonoGraphicsSpace.UI"/> 的层在合成相位先画进自己的整屏目标，再在
/// <c>PostDrawInterface</c> 贴出去（界面相位没有可以安全切渲染目标的空隙）。
/// </para>
/// </summary>
public sealed class MonoMetaballManager : ModSystem
{
    /// <summary>已登记的类型。</summary>
    private static readonly List<MonoMetaballType> types = [];

    /// <summary>界面空间那几层的整屏合成目标（世界层不需要，直接合成到屏幕）。</summary>
    private static readonly Dictionary<MonoMetaballType, MonoManagedRenderTarget> uiTargets = [];

    /// <summary>"配错了就什么都画不出来"这类问题只提醒一次，避免每帧刷屏。</summary>
    private static bool warnedNoLayerTargets;

    /// <summary>已登记的类型。</summary>
    public static IReadOnlyList<MonoMetaballType> Types => types;

    /// <summary>把一个类型登记进来。由 <see cref="MonoMetaballType.Register"/> 调用，重复登记会被忽略。</summary>
    /// <param name="type">要登记的类型。</param>
    internal static void Register(MonoMetaballType type)
    {
        if (!types.Contains(type))
            types.Add(type);
    }

    /// <summary>把一个类型摘掉，连同它的界面合成目标。由 <see cref="MonoMetaballType.Unload"/> 调用。</summary>
    /// <param name="type">要摘掉的类型。</param>
    internal static void Unregister(MonoMetaballType type)
    {
        types.Remove(type);
        if (uiTargets.Remove(type, out MonoManagedRenderTarget? target))
            target.Retire();
    }

    /// <summary>挂上写场时机（<c>Main.OnPreDraw</c>）与合成时机（<c>On_Main.DrawProjectiles</c> 的最前面）。</summary>
    public override void Load()
    {
        if (Main.dedServ)
            return;

        MonoRenderTargetManager.RenderTargetUpdateLoop += PrepareTargets;
        On_Main.DrawProjectiles += DrawLayers;
    }

    /// <summary>摘钩子并清表。释放显存交给各类型自己的 <see cref="MonoMetaballType.Dispose"/>（它排到主线程）。</summary>
    public override void Unload()
    {
        if (!Main.dedServ)
        {
            MonoRenderTargetManager.RenderTargetUpdateLoop -= PrepareTargets;
            On_Main.DrawProjectiles -= DrawLayers;
        }

        DisposeAll();
    }

    /// <summary>模组卸载时再清一次。tML 的两个卸载钩子都会调，谁先谁后没有保证。</summary>
    public override void OnModUnload() => DisposeAll();

    /// <summary>
    /// 换世界时清空所有类型的粒子。
    /// <b>必须清</b>：粒子通常持有上一个世界的对象引用（NPC/玩家），留着会继续飘。
    /// </summary>
    public override void OnWorldUnload()
    {
        foreach (MonoMetaballType type in types)
            type.ClearInstances();
    }

    /// <summary>
    /// 写场 pass：推进每个要画的类型，再把它的每一层写进对应的整屏目标。
    /// <para>由 <see cref="MonoRenderTargetManager.RenderTargetUpdateLoop"/> 在 <c>Main.OnPreDraw</c> 调用，
    /// 那时没有任何活动批次，可以直接切渲染目标。</para>
    /// </summary>
    private static void PrepareTargets()
    {
        if (Main.dedServ || Main.gameMenu || types.Count == 0)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        foreach (MonoMetaballType type in types)
        {
            if (!type.ShouldRender)
                continue;

            // 暂停时照旧写场（画面保持住），只是不推进。
            if (!Main.gamePaused)
                type.Update();

            AccumulateType(device, type);
        }
    }

    /// <summary>把一个类型的每一层写进它自己的整屏目标（先清成透明）。</summary>
    private static void AccumulateType(GraphicsDevice device, MonoMetaballType type)
    {
        if (type.LayerTargets.Count == 0)
        {
            WarnNoLayerTargets(type);
            return;
        }

        RenderTargetBinding[] oldTargets = device.GetRenderTargets();
        Viewport oldViewport = device.Viewport;
        try
        {
            foreach (MonoManagedRenderTarget managed in type.LayerTargets)
            {
                if (managed.IsUninitialized)
                    continue;

                RenderTarget2D field = managed.Target;
                device.SetRenderTarget(field);
                device.Viewport = new Viewport(0, 0, field.Width, field.Height);
                device.Clear(Color.Transparent);

                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    Main.DefaultSamplerState,
                    DepthStencilState.None,
                    RasterizerState.CullNone,
                    null,
                    AccumulateMatrix(type.Space));

                type.DrawInstances();
                Main.spriteBatch.End();
            }
        }
        finally
        {
            device.SetRenderTargets(oldTargets);
            device.Viewport = oldViewport;
        }
    }

    /// <summary>
    /// 写场用的矩阵：世界与屏幕层是裸屏幕坐标（球心由 <see cref="MonoMetaballType.ToScreen"/> 减掉
    /// <c>Main.screenPosition</c>），界面层跟着 UI 缩放。
    /// <para><b>世界层不用 <c>Main.GameViewMatrix</c></b>：写场发生在 <c>Main.OnPreDraw</c>，那时它还带着
    /// 上一帧的缩放，场会被画到错的地方；Luminance 原版这里也是 <c>Identity</c>。</para>
    /// </summary>
    /// <param name="space">这一层所在的空间。</param>
    private static Matrix AccumulateMatrix(MonoGraphicsSpace space)
        => space == MonoGraphicsSpace.UI ? Main.UIScaleMatrix : Matrix.Identity;

    /// <summary>
    /// 合成：在所有要画的类型上依次合成。
    /// <para>挂在 <c>On_Main.DrawProjectiles</c> 且<b>先画自己的层、再调 <c>orig</c></b>——于是融合球落在弹幕之下。</para>
    /// </summary>
    private static void DrawLayers(On_Main.orig_DrawProjectiles orig, Main self)
    {
        if (!Main.dedServ && !Main.gameMenu && types.Count > 0)
        {
            GraphicsDevice device = Main.instance.GraphicsDevice;
            foreach (MonoMetaballType type in types)
            {
                if (!type.ShouldRender)
                    continue;

                if (type.Space == MonoGraphicsSpace.UI)
                    CompositeUiType(device, type);
                else
                    CompositeType(type);
            }
        }

        orig(self);
    }

    /// <summary>给一个世界层开批次、逐层合成到当前目标、收批次。</summary>
    private static void CompositeType(MonoMetaballType type)
        => CompositeType(type, Main.GameViewMatrix.TransformationMatrix);

    /// <summary>给一个类型开批次、逐层合成到当前目标、收批次。</summary>
    /// <param name="type">要合成的类型。</param>
    /// <param name="matrix">这次绘制用的矩阵。</param>
    private static void CompositeType(MonoMetaballType type, Matrix matrix)
    {
        // 批次用 PointWrap，与 FPM 原来手写的那版一致：层目标与屏幕是 1:1 的，点采样不会把边缘糊掉。
        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.PointWrap,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            matrix);

        type.RenderLayerWithShader();
        Main.spriteBatch.End();
    }

    /// <summary>把一个界面层合成进它自己的整屏目标，供界面相位贴出去。</summary>
    private static void CompositeUiType(GraphicsDevice device, MonoMetaballType type)
    {
        MonoManagedRenderTarget composite = GetUiTarget(type);
        if (composite.IsUninitialized)
            return;

        RenderTargetBinding[] oldTargets = device.GetRenderTargets();
        Viewport oldViewport = device.Viewport;
        try
        {
            RenderTarget2D target = composite.Target;
            device.SetRenderTarget(target);
            device.Viewport = new Viewport(0, 0, target.Width, target.Height);
            device.Clear(Color.Transparent);

            // 界面层的场也在屏幕像素域，所以 1:1 画，不套镜头矩阵。
            CompositeType(type, Matrix.Identity);
        }
        finally
        {
            device.SetRenderTargets(oldTargets);
            device.Viewport = oldViewport;
        }
    }

    /// <summary>
    /// 界面相位：把界面层已经合成好的整屏目标当普通贴图贴出去。
    /// <para>界面批自带 <c>Main.UIScaleMatrix</c>，所以按 <c>1 / Main.UIScale</c> 反缩一次，
    /// 整屏目标才会 1:1 落到屏幕上。</para>
    /// </summary>
    /// <param name="spriteBatch">游戏当前已经 <c>Begin</c> 的界面批次。</param>
    public override void PostDrawInterface(SpriteBatch spriteBatch)
    {
        if (Main.dedServ || uiTargets.Count == 0)
            return;

        float inverseScale = 1f / Math.Max(0.0001f, Main.UIScale);
        foreach (MonoMetaballType type in types)
        {
            if (type.Space != MonoGraphicsSpace.UI || !type.ShouldRender)
                continue;
            if (!uiTargets.TryGetValue(type, out MonoManagedRenderTarget? target) || target.IsUninitialized)
                continue;

            spriteBatch.Draw(target.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, inverseScale, SpriteEffects.None, 0f);
        }
    }

    /// <summary>取（必要时创建）一个界面层的整屏合成目标。</summary>
    private static MonoManagedRenderTarget GetUiTarget(MonoMetaballType type)
    {
        if (uiTargets.TryGetValue(type, out MonoManagedRenderTarget? existing))
            return existing;

        MonoManagedRenderTarget created = new(
            MonoManagedRenderTarget.CreateScreenSizedTarget,
            resetOnResize: true,
            subjectToGarbageCollection: false);
        uiTargets[type] = created;
        return created;
    }

    /// <summary>释放所有类型的层目标与界面合成目标，并清空登记表。释放排到主线程，卸载钩子里调是安全的。</summary>
    private static void DisposeAll()
    {
        foreach (MonoMetaballType type in types)
            type.Dispose();
        foreach (MonoManagedRenderTarget target in uiTargets.Values)
            target.Retire();
        uiTargets.Clear();
        types.Clear();
        warnedNoLayerTargets = false;
    }

    /// <summary>层目标是按 <see cref="MonoMetaballType.LayerTextures"/> 的条目数在主线程上建的；一个都没有就画不出东西。</summary>
    private static void WarnNoLayerTargets(MonoMetaballType type)
    {
        if (warnedNoLayerTargets)
            return;
        warnedNoLayerTargets = true;
        Monochrome.Instance?.Logger.Warn(
            $"融合球类型 {type.GetType().Name} 还没有层目标（LayerTextures 可能为空，或创建还没轮到主线程），这一帧不画。");
    }
}
