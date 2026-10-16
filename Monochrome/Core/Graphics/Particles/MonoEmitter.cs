using Monochrome.Common.MonoUtil;

namespace Monochrome.Core.Graphics.Particles;

/// <summary>
/// 持续发射器：按固定节拍把粒子放到一个位置上，交给 <see cref="MonoParticleManager"/> 一起推进。
/// <para>
/// <b>用法。</b>填好 <see cref="Factory"/>（建哪种粒子）与 <see cref="Configure"/>（每次发出前改字段），
/// 调 <see cref="Spawn"/> 就开始发；<see cref="Stop"/> 停。
/// </para>
/// <code>
/// new MonoEmitter
/// {
///     Position = npc.Center,
///     Direction = -MathHelper.PiOver2,
///     Spread = 0.5f,
///     Radius = 18f,
///     Shape = MonoEmitShape.Ring,
///     Count = 3,
///     Period = 4,
///     Lifetime = 60,           // 发射器自己活多少帧
///     ParticleLifetime = 40,   // 发出来的粒子活多少帧
///     Factory = static () =&gt; new MonoSpark { Color = Color.OrangeRed },
///     Configure = static p =&gt; p.Velocity *= 1.5f,
/// }.Spawn();
/// </code>
/// <para>
/// <b>它自带生命周期</b>，形状与 <see cref="MonoParticle"/> 对齐：<see cref="Time"/> 每帧加一，
/// <see cref="Lifetime"/> 到点由 <see cref="ShouldStop"/> 判定并自动 <see cref="Stop"/>。
/// 想一直发就把 <see cref="Lifetime"/> 设成 <c>&lt;= 0</c>。
/// </para>
/// <para>
/// <b>取点与取朝向是分开的</b>：<see cref="Shape"/> 决定"在哪儿生"，<see cref="Direction"/> 与
/// <see cref="Spread"/> 决定"朝哪儿飞"。算术在 <see cref="MonoEmitPattern"/> 里，是纯逻辑。
/// </para>
/// <para>
/// <b>热重载提醒。</b>发射器把调用方给的委托（<see cref="Factory"/> / <see cref="Configure"/>）存在自己身上，
/// 所以<b>不要在静态字段里长期持有它</b>：热重载之后再调到上一版的委托，会报"尝试调用已生成的 lambda 或本地函数"。
/// 给它一个有限的 <see cref="Lifetime"/>、或者在 <c>Load</c> 里重建，都能避开这件事。
/// </para>
/// </summary>
public class MonoEmitter
{
    /// <summary>发射点（世界坐标）。每帧会加上 <see cref="Velocity"/>。</summary>
    public Vector2 Position;

    /// <summary>发射器自己每帧的位移。</summary>
    public Vector2 Velocity;

    /// <summary>基准朝向（弧度，<c>0</c> = 向右）。</summary>
    public float Direction;

    /// <summary>散射半角（弧度）。给 <see cref="MathF.PI"/> 就是全向，给 <c>0</c> 就是一条直线。</summary>
    public float Spread = 0.35f;

    /// <summary>初速（像素 / 帧）。</summary>
    public float Speed = 2f;

    /// <summary>初速的随机浮动比例，<c>0.25</c> 表示 ±25%。</summary>
    public float SpeedJitter = 0.25f;

    /// <summary>取点形状的半径（像素）；<see cref="MonoEmitShape.Point"/> 时不起作用。</summary>
    public float Radius;

    /// <summary>每次发射几颗。</summary>
    public int Count = 1;

    /// <summary>每几帧发射一次，至少 1。</summary>
    public int Period = 6;

    /// <summary>
    /// 发射器自己活多少帧。<b>默认 60 帧</b>，到点自动停；<c>&lt;= 0</c> 表示一直发，那就要自己
    /// <see cref="Stop"/>。
    /// </summary>
    public int Lifetime = 60;

    /// <summary>发出来的粒子的寿命（帧）。</summary>
    public int ParticleLifetime = 60;

    /// <summary>在哪生。</summary>
    public MonoEmitShape Shape = MonoEmitShape.Point;

    /// <summary>建一粒。为 <see langword="null"/> 时这个发射器什么都不发（不会报错）。</summary>
    public Func<MonoParticle>? Factory;

    /// <summary>每次发出前拿粒子改字段。</summary>
    public Action<MonoParticle>? Configure;

    /// <summary>是否已经纳入管理器。</summary>
    public bool Active { get; internal set; }

    /// <summary>已经活了多少帧，由管理器推进。</summary>
    public int Time { get; internal set; }

    /// <summary>生命周期进度 <c>[0,1]</c>；<see cref="Lifetime"/> 非正时恒为 1。</summary>
    public float LifetimeRatio => Lifetime <= 0 ? 1f : MathHelper.Clamp(Time / (float)Lifetime, 0f, 1f);

    /// <summary>已经发出去多少粒，诊断用。</summary>
    public long Emitted { get; internal set; }

    /// <summary>按节拍发时的余数。</summary>
    private int timer;

    /// <summary>
    /// 该停了吗，每帧问一次；返回 <see langword="true"/> 就自动 <see cref="Stop"/>。
    /// <para>默认判据是"<see cref="Time"/> 到了 <see cref="Lifetime"/>"；要按别的条件停就重写它。</para>
    /// </summary>
    public virtual bool ShouldStop() => Lifetime > 0 && Time >= Lifetime;

    /// <summary>开始发射。重复调用同一个已活动的实例是安全的。专用服务器上什么都不做。</summary>
    /// <returns>自身，便于链式写法。</returns>
    public MonoEmitter Spawn()
    {
        if (!Main.dedServ)
            MonoParticleManager.Attach(this);

        return this;
    }

    /// <summary>停止发射。已经发出去的粒子不受影响。</summary>
    public void Stop() => MonoParticleManager.Detach(this);

    /// <summary>推进一帧：位移、计龄、按节拍发射、到点自停。由管理器调用。</summary>
    internal void Update()
    {
        Time++;
        Position += Velocity;

        if (!ShouldStop())
        {
            int period = Math.Max(1, Period);
            timer += 1;
            while (timer >= period)
            {
                timer -= period;
                Emit();
            }
        }

        if (ShouldStop())
            Stop();
    }

    /// <summary>归零，供复用。</summary>
    internal void Reset()
    {
        Time = 0;
        Emitted = 0;
        timer = 0;
    }

    private void Emit()
    {
        if (Factory is null)
            return;

        // 方法组而不是静态 lambda：lambda 会被编译器缓存成静态字段，热重载之后那格会指向上一版的代码。
        Func<float> unit = NextUnit;

        for (int i = 0; i < Math.Max(0, Count); i++)
        {
            MonoParticle? particle = Factory();
            if (particle is null)
                continue;

            float angle = MonoEmitPattern.Direction(Direction, Spread, unit);
            Vector2 offset = MonoEmitPattern.Offset(Shape, Radius, unit);
            float speed = Speed * (1f + (unit() * 2f - 1f) * SpeedJitter);

            particle.Position = Position + offset;
            particle.Velocity = angle.ToRotationVector2() * speed;
            particle.Lifetime = ParticleLifetime;

            Configure?.Invoke(particle);
            particle.Spawn();
            Emitted++;
        }
    }

    /// <summary>取一个 <c>[0,1)</c> 的随机数，喂给 <see cref="MonoEmitPattern"/>。</summary>
    private static float NextUnit() => Main.rand.NextFloat();
}
