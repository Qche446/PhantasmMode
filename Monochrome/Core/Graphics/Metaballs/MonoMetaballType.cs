using Monochrome.Core.Graphics.RenderTargets;
using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.Metaballs;

/// <summary>
/// 一类融合球：同类型的球写进同一组层目标里互相融合，不同类型的层互不影响。
/// <para>
/// 用法是"派生一个子类来定义一整类球"：覆写 <see cref="UpdateParticle"/> 给运动、
/// <see cref="ShouldKillParticle"/> 给消亡条件，用 <see cref="CreateParticle(Vector2, Vector2, float, float, float, float, float)"/> 提交；
/// 写场用的软圆由 <see cref="FieldTexture"/> 给，合成时的材质与边由 <see cref="LayerTextures"/> /
/// <see cref="EdgeColor"/> 或覆写 <see cref="PrepareShaderForTarget"/> 决定。
/// 推进、写场与合成由 <see cref="MonoMetaballManager"/> 按类型批量做，派生类不写绘制钩子。
/// </para>
/// <para>
/// 场是"逐颗粒子把一张软圆贴图按 alpha 混合画进整屏目标"，<b>不是把球拆成三角形扇</b>：
/// 每颗粒子一个四边形、整层一次批次提交，所以形状自由度来自贴图而不是顶点几何，代价按"粒子数"
/// 而不是"粒子数 × 分段数"增长。
/// </para>
/// <para>实例由 tML 自动创建并注册（<see cref="Register"/> 里登记），<b>不要用 <c>new</c> 造</b>；取用走 <c>ModContent.GetInstance&lt;T&gt;()</c>。</para>
/// </summary>
public abstract class MonoMetaballType : ModType
{
    /// <summary>内置合成着色器的注册名（资产 <c>Assets/Effects/Metaballs/MetaballEdge.fx</c>）。</summary>
    public const string EdgeShaderName = "Monochrome.MetaballEdge";

    /// <summary>本类型活着的球。管理器在 <c>Main.OnPreDraw</c> 推进并写场。</summary>
    protected internal List<MonoMetaballInstance> Particles = [];

    /// <summary>活着的球数。</summary>
    public int ActiveParticleCount => Particles.Count;

    /// <summary>每层一张的整屏累积目标，条目数与 <see cref="LayerTextures"/> 一致（在 <see cref="Register"/> 里排队创建）。</summary>
    public readonly List<MonoManagedRenderTarget> LayerTargets = [];

    #region 行为

    /// <summary>
    /// 这一类型现在要不要画。<b>默认"有球就画"</b>——没有粒子时既不推进也不占绘制开销；
    /// 有额外判据（例如"宿主 NPC 还活着"）再覆写。
    /// </summary>
    public virtual bool ShouldRender => ActiveParticleCount >= 1;

    /// <summary>每帧推进一颗粒子：改速度、大小或 <see cref="MonoMetaballInstance.ExtraInfo"/>。位移由管理器在这之后统一施加。</summary>
    /// <param name="particle">要推进的粒子。</param>
    public abstract void UpdateParticle(MonoMetaballInstance particle);

    /// <summary>这颗粒子该不该被回收。为 <see langword="true"/> 时从 <see cref="Particles"/> 里去掉。</summary>
    /// <param name="particle">要判定的粒子。</param>
    public abstract bool ShouldKillParticle(MonoMetaballInstance particle);

    #endregion

    #region 观感

    /// <summary>
    /// 把粒子写进融合场时要画的贴图（通常是一张软圆）；返回 <see langword="null"/> 时这一类型什么都写不进去。
    /// <para>用委托而不是 <see cref="Texture2D"/> 是为了能返回渲染目标这类"每次取都可能不同"的贴图。</para>
    /// </summary>
    public virtual Func<Texture2D>? FieldTexture => null;

    /// <summary><see cref="FieldTexture"/> 里真正要画的那块矩形（贴图来自图集时用它）；<see langword="null"/> 表示整张。</summary>
    public virtual Rectangle? FieldTextureFrame => null;

    /// <summary>每层的叠加材质贴图。条目数决定层数，也决定 <see cref="LayerTargets"/> 的张数。</summary>
    public virtual Func<Texture2D>[] LayerTextures => [];

    /// <summary>边缘色。内置的边着色器拿它画"贴着轮廓的那一圈"。</summary>
    public virtual Color EdgeColor => Color.White;

    /// <summary>
    /// 这一类型画在哪个空间。<see cref="MonoGraphicsSpace.UI"/> 的层会在写场之后合成进自己的整屏目标，
    /// 再在界面相位当普通贴图贴出来。
    /// </summary>
    public virtual MonoGraphicsSpace Space => MonoGraphicsSpace.World;

    /// <summary>第 <paramref name="layerIndex"/> 层的材质是否固定贴在屏幕上（不随镜头平移）。</summary>
    /// <param name="layerIndex">层下标。</param>
    public virtual bool LayerIsFixedToScreen(int layerIndex) => false;

    /// <summary>第 <paramref name="layerIndex"/> 层材质的额外滚动偏移，用来做逐层动画。</summary>
    /// <param name="layerIndex">层下标。</param>
    public virtual Vector2 CalculateManualOffsetForLayer(int layerIndex) => Vector2.Zero;

    /// <summary>写场之外的额外绘制，在默认 <see cref="DrawInstances"/> 的末尾调用。</summary>
    public virtual void ExtraDrawing() { }

    #endregion

    #region 粒子

    /// <summary>造一颗粒子并纳入本类型。</summary>
    /// <param name="spawnPosition">球心。</param>
    /// <param name="velocity">每帧位移。</param>
    /// <param name="size">写场用的绘制边长（像素）。</param>
    /// <param name="extraInfo0">类型自定义数据 0。</param>
    /// <param name="extraInfo1">类型自定义数据 1。</param>
    /// <param name="extraInfo2">类型自定义数据 2。</param>
    /// <param name="extraInfo3">类型自定义数据 3。</param>
    public void CreateParticle(Vector2 spawnPosition, Vector2 velocity, float size, float extraInfo0 = 0f, float extraInfo1 = 0f, float extraInfo2 = 0f, float extraInfo3 = 0f)
        => Particles.Add(new(spawnPosition, velocity, size, extraInfo0, extraInfo1, extraInfo2, extraInfo3));

    /// <summary>把一颗已经造好的粒子纳入本类型。</summary>
    /// <param name="particle">粒子。</param>
    public void CreateParticle(MonoMetaballInstance particle) => Particles.Add(particle);

    /// <summary>把一批粒子纳入本类型。</summary>
    /// <param name="particles">粒子。</param>
    public void CreateParticle(IEnumerable<MonoMetaballInstance> particles) => Particles.AddRange(particles);

    /// <summary>清空本类型的全部粒子（换世界时由管理器统一调）。</summary>
    public void ClearInstances() => Particles.Clear();

    /// <summary>
    /// 推进一帧：逐颗 <see cref="UpdateParticle"/> → 施加位移 → 回收 <see cref="ShouldKillParticle"/> 为真的粒子。
    /// 由 <see cref="MonoMetaballManager"/> 调用，派生类不要自己调。
    /// </summary>
    public void Update()
    {
        for (int i = 0; i < Particles.Count; i++)
        {
            MonoMetaballInstance particle = Particles[i];
            UpdateParticle(particle);
            particle.Center += particle.Velocity;
        }

        // 倒序遍历回收：正序 RemoveAt 会跳过后一颗。
        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            if (ShouldKillParticle(Particles[i]))
                Particles.RemoveAt(i);
        }
    }

    #endregion

    #region 绘制

    /// <summary>
    /// 把本类型的全部粒子写进当前绑定的层目标。默认行为是"每颗画一张 <see cref="FieldTexture"/>，
    /// 以 <see cref="MonoMetaballInstance.Center"/> 为中心、边长 <see cref="MonoMetaballInstance.Size"/>"。
    /// <para>要非圆形状、要逐颗换贴图、要额外画几笔，就覆写它；覆写时别忘了在末尾调 <see cref="ExtraDrawing"/>。</para>
    /// </summary>
    public virtual void DrawInstances()
    {
        Func<Texture2D>? source = FieldTexture;
        if (source is null)
            return;

        Texture2D texture = source();
        if (texture is null || texture.IsDisposed)
            return;

        Rectangle? frame = FieldTextureFrame;
        Vector2 frameSize = frame?.Size() ?? texture.Size();
        if (frameSize.X <= 0f || frameSize.Y <= 0f)
            return;

        Vector2 origin = frameSize * 0.5f;
        foreach (MonoMetaballInstance particle in Particles)
        {
            if (particle.Size <= 0f)
                continue;

            Main.spriteBatch.Draw(texture, ToScreen(particle.Center), frame, Color.White, 0f, origin,
                new Vector2(particle.Size / frameSize.X, particle.Size / frameSize.Y), SpriteEffects.None, 0f);
        }

        ExtraDrawing();
    }

    /// <summary>
    /// 为第 <paramref name="layerIndex"/> 层准备合成着色器：写 uniform、绑贴图，并 <c>Apply()</c>。
    /// 默认用内置的 <see cref="EdgeShaderName"/>；要自己的材质就覆写它（写自己那份参数与贴图槽，
    /// <b>记得自己调 <c>Apply()</c></b>）。
    /// <para>槽位约定：正在绘制的层目标由绘制方绑在 <b>s0</b>，所以自己的贴图从槽 1 起绑。</para>
    /// </summary>
    /// <param name="layerIndex">层下标。</param>
    public virtual void PrepareShaderForTarget(int layerIndex)
    {
        if (!MonoShaderManager.TryGet(EdgeShaderName, out MonoShader? shader) || shader is null)
            return;

        Func<Texture2D>[] textures = LayerTextures;
        if (layerIndex < 0 || layerIndex >= textures.Length)
            return;

        Texture2D layerTexture = textures[layerIndex]();
        Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
        Vector2 layerScrollOffset = Main.screenPosition / screenSize + CalculateManualOffsetForLayer(layerIndex);
        if (LayerIsFixedToScreen(layerIndex))
            layerScrollOffset = Vector2.Zero;

        shader.SetParameter("layerSize", layerTexture.Size())
              .SetParameter("screenSize", screenSize)
              .SetParameter("layerOffset", layerScrollOffset)
              .SetParameter("edgeColor", EdgeColor.ToVector4())
              .SetTexture(layerTexture, 1, SamplerState.LinearWrap)
              .Apply();
    }

    /// <summary>
    /// 把每一层合成到<b>当前绑定的目标</b>上。<b>本方法不自己 <c>Begin</c> / <c>End</c></b>：
    /// 调用方先开批次（<c>SpriteSortMode.Immediate</c> + <see cref="BlendState.AlphaBlend"/>，
    /// 这样逐层 <see cref="PrepareShaderForTarget"/> 换出来的效果才会在紧接着的那次绘制上生效），再调它。
    /// <para>绘制位置照搬 Luminance：<c>Main.screenLastPosition - Main.screenPosition</c>——写场发生在
    /// <c>Main.OnPreDraw</c>，与合成之间隔了一帧的镜头，这个偏移把它补回来。</para>
    /// </summary>
    public void RenderLayerWithShader()
    {
        for (int i = 0; i < LayerTargets.Count; i++)
        {
            MonoManagedRenderTarget target = LayerTargets[i];
            if (target.IsRetired)
                continue;
            PrepareShaderForTarget(i);
            Main.spriteBatch.Draw(target.Target, Main.screenLastPosition - Main.screenPosition, Color.White);
        }
    }

    /// <summary>把球心换算成当前批次用的屏幕坐标：世界层减 <c>Main.screenPosition</c>，其余原样。</summary>
    /// <param name="position">球心（世界坐标，或界面/屏幕坐标）。</param>
    protected Vector2 ToScreen(Vector2 position)
        => Space == MonoGraphicsSpace.World ? position - Main.screenPosition : position;

    #endregion

    #region 生命周期

    /// <summary>
    /// 注册进 <see cref="ModTypeLookup{T}"/> 与 <see cref="MonoMetaballManager"/>，并按 <see cref="LayerTextures"/> 的条目数排队创建层目标。
    /// <para>内容注册跑在加载线程上，所以图形资源的创建推到主线程（<see cref="Main.QueueMainThreadAction"/>）。</para>
    /// </summary>
    protected sealed override void Register()
    {
        ModTypeLookup<MonoMetaballType>.Register(this);
        MonoMetaballManager.Register(this);

        if (Main.dedServ)
            return;

        int layerCount = LayerTextures.Length;
        Main.QueueMainThreadAction(() =>
        {
            for (int i = 0; i < layerCount; i++)
            {
                LayerTargets.Add(new MonoManagedRenderTarget(
                    MonoManagedRenderTarget.CreateScreenSizedTarget,
                    resetOnResize: true,
                    subjectToGarbageCollection: false));
            }
        });
    }

    /// <summary>摘掉登记、释放层目标并清空粒子。</summary>
    public override void Unload()
    {
        Dispose();
        Particles.Clear();
        MonoMetaballManager.Unregister(this);
    }

    /// <summary>
    /// 释放全部层目标。释放走 <see cref="MonoManagedRenderTarget.Retire"/>，它自己把原生释放排到主线程，
    /// 所以在卸载钩子（后台线程）里直接调也安全。
    /// </summary>
    public void Dispose()
    {
        foreach (MonoManagedRenderTarget target in LayerTargets)
            target.Retire();
        LayerTargets.Clear();
    }

    #endregion
}
