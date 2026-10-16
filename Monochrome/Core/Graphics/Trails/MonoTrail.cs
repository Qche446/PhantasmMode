using Monochrome.Common.MonoUtil;
using Monochrome.Core.Graphics.Primitives;
using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.Trails;

/// <summary>
/// 一条拖尾：记下走过的一串点，再交给图元层画成条带。
/// <para>
/// <b>用法。</b>每 tick 把当前位置 <see cref="Update"/> 进去，在合法的绘制相位（世界相位
/// <c>On_Main.DrawDust</c> 之后、或 <c>End()</c> 与下一次 <c>Begin()</c> 之间）调一次 <see cref="Draw"/>。
/// </para>
/// <code>
/// trail ??= new MonoTrail(24) { Space = MonoGraphicsSpace.World };
/// trail.Update(Projectile.Center);
///
/// // 绘制相位
/// trail.Draw();
/// </code>
/// <para>
/// <b>默认就能看。</b><see cref="Shader"/> 与 <see cref="Texture"/> 都不给时，它用内置的
/// <c>Monochrome.StreakTrail</c> 与程序生成的条纹贴图；给了就用给的。
/// </para>
/// <para>
/// <b>两种画法。</b><see cref="Spawn"/> 过的拖尾由 <see cref="MonoTrailSystem"/> 在世界绘制相位
/// （<c>On_Main.DrawDust</c>）每帧自动画一次，调用方只要 <see cref="Update"/>；想自己控制时机就
/// 不 <see cref="Spawn"/>、在合适的位置手动调 <see cref="Draw"/>。
/// </para>
/// <para>
/// <b>它不自己推进时间</b>：<see cref="Update"/> 什么时候被调、调几次，由调用方决定。这与粒子在
/// <c>PostUpdateDusts</c> 被统一推进不同。
/// </para>
/// </summary>
public class MonoTrail
{
    /// <summary>内置拖尾着色器的注册名。</summary>
    public const string ShaderName = "Monochrome.StreakTrail";

    /// <summary>建一条拖尾。</summary>
    /// <param name="capacity">最多留几个轨迹点，至少 2。</param>
    public MonoTrail(int capacity = 32)
    {
        Buffer = new MonoTrailBuffer(capacity);
    }

    /// <summary>轨迹点。下标 0 是尾巴、最后一个是头。</summary>
    public MonoTrailBuffer Buffer { get; }

    /// <summary>记录时加到每个点上的偏移（例如把它从中心挪到贴图边缘）。</summary>
    public Vector2 Offset;

    /// <summary>与上一个点的最小间距（像素）；小于它就不记，免得慢速时在同一处堆点。</summary>
    public float MinDistance = 0f;

    /// <summary>
    /// 沿轨迹的宽度函数，入参 <c>t ∈ [0,1]</c>（0 为尾巴，1 为头），返回条带<b>全宽</b>（像素）。默认尾巴细、头粗。
    /// <para><b>从 Luminance 搬过来时要乘 2</b>：那边的 <c>WidthFunction</c> 是半宽，同一个函数在这里会画细一半。</para>
    /// </summary>
    public Func<float, float> WidthFunction = static t => 26f * t;

    /// <summary>沿轨迹的颜色函数，入参同上；颜色由 GPU 在顶点间插值。</summary>
    public Func<float, Color> ColorFunction = static _ => Color.White;

    /// <summary>可选的位置偏移函数，在减去 <c>Main.screenPosition</c> 之前加在世界坐标上。</summary>
    public Func<float, Vector2>? OffsetFunction;

    /// <summary>自定义着色器；为 <see langword="null"/> 时用内置的 <c>Monochrome.StreakTrail</c>。</summary>
    public MonoShader? Shader;

    /// <summary>给着色器采样的贴图；为 <see langword="null"/> 时用程序生成的条纹贴图。绑到采样槽 1。</summary>
    public Texture2D? Texture;

    /// <summary>上面那张贴图的采样状态；为 <see langword="null"/> 时用 <see cref="SamplerState.LinearWrap"/>。</summary>
    public SamplerState? Sampler;

    /// <summary>混合状态，默认 <see cref="BlendState.Additive"/>（拖尾多是加法的光）。</summary>
    public BlendState BlendState = BlendState.Additive;

    /// <summary>轨迹点所属的坐标空间，默认 <see cref="MonoGraphicsSpace.World"/>。</summary>
    public MonoGraphicsSpace Space = MonoGraphicsSpace.World;

    /// <summary>是否用 Catmull-Rom 样条重采样点列。点列稀疏时开。</summary>
    public bool Smoothen = true;

    /// <summary>
    /// 是否强制用不含缩放的正交投影（像素化图元）。等价于 Luminance 那套里的 <c>Pixelate</c>：
    /// 打开之后条带的粗细按屏幕像素算、不随游戏缩放变化。
    /// </summary>
    public bool UseUnscaledMatrix;

    /// <summary>重采样后的点数；<see langword="null"/> 表示按轨迹点数来。</summary>
    public int? PointsToCreate;

    /// <summary>是否已经交给 <see cref="MonoTrailSystem"/> 自动绘制。</summary>
    public bool Active { get; internal set; }

    /// <summary>
    /// 交给自动绘制：世界绘制相位每帧被调一次 <see cref="Draw"/>。重复调用同一个已活动的实例是安全的。
    /// <para><b>只对世界空间的拖尾有意义</b>；界面空间请别 <c>Spawn</c>，自己在界面相位画。</para>
    /// </summary>
    /// <returns>自身，便于链式写法。</returns>
    public MonoTrail Spawn()
    {
        if (!Main.dedServ)
            MonoTrailSystem.Attach(this);

        return this;
    }

    /// <summary>停止自动绘制。轨迹点不受影响。</summary>
    public void Stop() => MonoTrailSystem.Detach(this);

    /// <summary>
    /// 外部点列来源，例如 <c>() =&gt; Projectile.oldPos</c>。
    /// <para>
    /// 给了它就不必自己 <see cref="Update"/> 记点：每帧绘制时按它取一次。**两点由库负责**，
    /// 这样调用方不用各写一遍：① 输入按"新的在前"理解（<c>oldPos</c> 就是这个顺序），会翻成
    /// <c>RenderTrail</c> 要的"尾巴在前"，所以 <see cref="WidthFunction"/> 的 <c>t</c> 仍然是 0 为尾；
    /// ② 跳过零值——<c>oldPos</c> 里那些还没被填过的槽就是零。
    /// </para>
    /// <para>
    /// 与 Luminance 的用法一致（那边是 <c>PrimitiveRenderer.RenderTrail(Projectile.oldPos, …)</c>），
    /// 而且它顺带绕开了"自己采样会滞后一步"的问题：<c>oldPos</c> 由引擎维护。
    /// </para>
    /// </summary>
    public Func<IReadOnlyList<Vector2>>? Source;

    /// <summary>把 <see cref="Source"/> 那串点整理成"尾巴在前"的复用的表。</summary>
    private readonly List<Vector2> scratch = [];

    /// <summary>用一个新的起点重新开始记录。</summary>
    /// <param name="position">起点。</param>
    public void Reset(Vector2 position)
    {
        Buffer.Clear();
        Buffer.Add(position + Offset);
    }

    /// <summary>记一个点。距离上一个点太近时不记。</summary>
    /// <param name="position">当前位置。</param>
    /// <returns>确实记下了才返回 <see langword="true"/>。</returns>
    public bool Update(Vector2 position) => Buffer.TryAdd(position + Offset, MinDistance);

    /// <summary>清空轨迹点，但保留各项设置。</summary>
    public void Clear() => Buffer.Clear();

    /// <summary>
    /// 用一串现成的点画一次（例如 <c>Projectile.oldPos</c>），不碰自己的缓冲、也不需要 <see cref="Spawn"/>。
    /// <para>顺序与零值的处理同 <see cref="Source"/>：输入是"新的在前"，库负责翻过来并跳过零。</para>
    /// </summary>
    /// <param name="positions">外部点列。</param>
    public void DrawFrom(IReadOnlyList<Vector2> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);

        if (Main.dedServ || FillFromSource(positions) < 2)
            return;

        Render(scratch);
    }

    /// <summary>
    /// 把当前轨迹画出来。<b>必须落在合法的绘制相位里</b>，本方法不自己开批次。
    /// <para>
    /// <b>它只读不写</b>：轨迹点只由 <see cref="Update"/> 推进。绘制一帧可能跑 0 次（专用服务器），
    /// 开了 <c>CaptureEntities</c> 时又可能跑两次，状态不能跟着绘制次数变。
    /// </para>
    /// <para>
    /// 由此得到一条用法上的硬约束：<b>头部的落点就是最后一次 <see cref="Update"/> 的位置</b>。
    /// 采样点放得越早、条带前端离实体本体越远。用 <see cref="Source"/> 直接喂引擎维护的点列
    /// （例如 <c>Projectile.oldPos</c>）可以绕开这件事。
    /// </para>
    /// 轨迹点少于两个、专用服务器、或者图元渲染器还没就绪时安静返回。
    /// </summary>
    public void Draw()
    {
        if (Main.dedServ)
            return;

        if (Source is not null)
        {
            if (FillFromSource(Source()) < 2)
                return;

            Render(scratch);
            return;
        }

        if (Buffer.Count < 2)
            return;

        Render(Buffer);
    }

    /// <summary>
    /// 把外部点列整理成"尾巴在前"写进 <see cref="scratch"/>，返回点数。
    /// 输入按"新的在前"理解（<c>oldPos</c> 的顺序），所以倒着取；零值槽与间距过近的点都跳过。
    /// </summary>
    private int FillFromSource(IReadOnlyList<Vector2> positions)
    {
        scratch.Clear();

        for (int i = positions.Count - 1; i >= 0; i--)
        {
            Vector2 point = positions[i];
            if (point == Vector2.Zero)
                continue;

            point += Offset;
            if (scratch.Count > 0 && MinDistance > 0f)
            {
                Vector2 delta = point - scratch[^1];
                if (delta.LengthSquared() < MinDistance * MinDistance)
                    continue;
            }

            scratch.Add(point);
        }

        return scratch.Count;
    }

    /// <summary>提交一次绘制。</summary>
    private void Render(IReadOnlyList<Vector2> positions)
    {
        MonoShader? shader = Shader ?? ResolveShader();
        Texture2D? texture = Texture ?? (shader is null ? null : MonoTrailTextures.Streak);

        MonoPrim.RenderTrail(
            positions,
            new MonoPrimitiveSettings(
                WidthFunction,
                ColorFunction,
                OffsetFunction,
                Smoothen,
                shader,
                null,
                null,
                UseUnscaledMatrix,
                BlendState,
                null,
                Space,
                texture,
                Sampler),
            PointsToCreate);
    }

    /// <summary>取内置拖尾着色器；资产没加载到时返回 <see langword="null"/>（此时会退回顶点色路径）。</summary>
    public static MonoShader? ResolveShader()
        => MonoShaderManager.TryGet(ShaderName, out MonoShader? shader) ? shader : null;
}
