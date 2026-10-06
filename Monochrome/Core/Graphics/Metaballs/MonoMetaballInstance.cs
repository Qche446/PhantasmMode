namespace Monochrome.Core.Graphics.Metaballs;

/// <summary>
/// 有自己的行为与生命周期的融合球实例。形状与 <see cref="Particles.MonoParticle"/> 对齐：覆写
/// <see cref="Update"/> 给出运动与缩放，覆写 <see cref="ShouldKill"/> 自定义消亡条件，覆写
/// <see cref="DrawField"/> 决定怎么写进融合场。每个实例一个对象，由
/// <see cref="MonoMetaballManager"/> 推进、回收与累积。
/// <para>
/// 与 <see cref="MonoMetaball"/>（只有数据、每帧一次性提交）的分工：跟着某个东西走就用前者，
/// 要飘、要呼吸、要自己决定何时消失就用本类。
/// </para>
/// <para>
/// 推进顺序（每帧一次，在 <c>PostUpdateWorld</c>）：<see cref="Update"/> →
/// <c>Position += Velocity</c> → <see cref="Time"/>++ → 到寿回收。所以 <see cref="Update"/> 里读到的
/// <see cref="Time"/> 是"本帧开始时的年龄"。
/// </para>
/// </summary>
public abstract class MonoMetaballInstance
{
    /// <summary>世界坐标位置。</summary>
    public Vector2 Position;

    /// <summary>每帧位移（会被直接加到 <see cref="Position"/> 上，不乘时间步长）。</summary>
    public Vector2 Velocity;

    /// <summary>可见半径：<b>球看起来就是这么大</b>（阈值只影响融合积极性，不影响尺寸）。实际生效的是 <see cref="CurrentRadius"/>。</summary>
    public float Radius = 32f;

    /// <summary>
    /// 半径乘数，默认 1。<b>做脉动/爆开就在这里动手</b>（例如 <c>RadiusScale = 1f + 0.2f * MathF.Sin(...)</c>），
    /// 这样 <see cref="Radius"/> 始终是"设计半径"，动画不破坏它。
    /// </summary>
    public float RadiusScale = 1f;

    /// <summary>颜色。多颗球重叠处会按影响力做加权平均，所以颜色可以不同。</summary>
    public Color Color = Color.White;

    /// <summary>影响力缩放，默认 1。会让这颗球在融合时"更强势"。</summary>
    public float Strength = 1f;

    /// <summary>
    /// 本实例的影响力手感：<c>pow(Falloff, InfluencePower)</c>。
    /// <c>1</c> = 线性、<c>2</c>/<c>3</c> = 更黏。<b>逐实例生效</b>，不受
    /// <see cref="MonoMetaballSettings.InfluencePower"/>（那个只管 <see cref="MonoMetaball"/> 式的提交）影响。
    /// </summary>
    public float InfluencePower = 1f;

    /// <summary>已经存活了多少帧，由管理器递增。</summary>
    public int Time;

    /// <summary>总生命周期（帧）。<see cref="ShouldKill"/> 的默认判据就是它。</summary>
    public int Lifetime = 60;

    /// <summary>是否处于活动状态（已被 <see cref="Spawn()"/> 且尚未被回收）。</summary>
    public bool Active { get; internal set; }

    /// <summary>所属层名。由 <see cref="Spawn(string)"/> 写入，实例自己改不了。</summary>
    public string Layer { get; internal set; } = string.Empty;

    /// <summary>实际可见半径：<c>Radius * max(0, RadiusScale)</c>。</summary>
    public float CurrentRadius => Radius * Math.Max(0f, RadiusScale);

    /// <summary>生命周期进度 <c>[0,1]</c>；<see cref="Lifetime"/> 非正时恒为 1。</summary>
    public float LifetimeRatio => Lifetime <= 0 ? 1f : MathHelper.Clamp(Time / (float)Lifetime, 0f, 1f);

    /// <summary>
    /// 本实例默认属于哪一层。<see cref="Spawn()"/> 用它，所以派生类可以在这里声明自己的归属，
    /// 调用方就只要写 <c>new MyBlob(pos).Spawn();</c>，不必再传层名。
    /// </summary>
    public virtual string LayerName => MonoMetaballManager.DefaultLayerName;

    /// <summary>
    /// 把本实例交给管理器开始活动，层名取 <see cref="LayerName"/>。
    /// <para>
    /// <b>重复调同一个已活动的实例是安全的</b>（幂等）；专用服务器上什么都不做。
    /// </para>
    /// </summary>
    /// <returns>自身，便于链式写法。</returns>
    public MonoMetaballInstance Spawn() => Spawn(LayerName);

    /// <summary>
    /// 把本实例交给管理器开始活动，并指定层名。
    /// <para>
    /// <b>重复调同一个已活动的实例是安全的</b>（幂等）；传入不同的层名会把实例<b>搬到</b>那一层。
    /// 专用服务器上什么都不做。
    /// </para>
    /// </summary>
    /// <param name="layer">层名（不区分大小写）。同一层的球互相融合。</param>
    /// <returns>自身，便于链式写法。</returns>
    public MonoMetaballInstance Spawn(string layer)
    {
        if (!Main.dedServ)
            MonoMetaballManager.Spawn(layer, this);
        return this;
    }

    /// <summary>立即结束生命周期（把 <see cref="Time"/> 推到 <see cref="Lifetime"/>，下一帧被回收）。</summary>
    public void Kill() => Time = Lifetime;

    /// <summary>
    /// 每帧的行为钩子，在位移推进<b>之前</b>调用。
    /// 这里可以改 <see cref="Velocity"/>、<see cref="RadiusScale"/>、<see cref="Color"/> 等任何字段。
    /// </summary>
    public virtual void Update() { }

    /// <summary>是否该死。默认 <see cref="Time"/> 达到 <see cref="Lifetime"/>；可覆写（例如"宿主 NPC 死了就消失"）。</summary>
    public virtual bool ShouldKill() => Time >= Lifetime;

    /// <summary>
    /// 把自己写进融合场。默认写一颗圆球。
    /// <para>
    /// 覆写它可以做非圆形状，或一次写多颗球（例如"一个主球 + 两个卫星球"）：
    /// <c>brush.Ball(Position, CurrentRadius, Color, Strength, InfluencePower);</c>
    /// </para>
    /// <para><b>不要在这里改层的内容</b>（<c>Add</c>/<c>Spawn</c>）——累积正在遍历同一批数据。</para>
    /// </summary>
    /// <param name="brush">写入上下文。</param>
    public virtual void DrawField(MonoMetaballBrush brush)
        => brush.Ball(Position, CurrentRadius, Color, Strength, InfluencePower);

    /// <summary>
    /// CPU 侧的场值，供 <see cref="MonoMetaballManager.Sample"/> 使用，默认与着色器同式。
    /// 覆写了 <see cref="DrawField"/> 却做出非默认形状时，应当一并覆写这里，否则 CPU 采样会与实际绘制不一致。
    /// </summary>
    /// <param name="point">采样点（世界坐标）。</param>
    public virtual float InfluenceAt(Vector2 point)
    {
        float support = MonoMetaballManager.Settings.SupportRadius(CurrentRadius);
        float falloff = MonoMetaball.FalloffAt(Position, support, point);
        return MathF.Pow(falloff, Math.Max(0.0001f, InfluencePower)) * Strength;
    }
}
