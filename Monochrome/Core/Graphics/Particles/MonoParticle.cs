namespace Monochrome.Core.Graphics.Particles;

/// <summary>
/// 池化视觉粒子的基类：派生类给出贴图与可选的行为/绘制覆写，实例由
/// <see cref="MonoParticleManager"/> 持有并驱动。
/// <para>
/// <b>这不是值类型的轻量粒子</b>——每个粒子是一个对象，适合"需要独立行为与绘制逻辑"的场合
/// （拖尾、软粒子、光照），数量级以数百为限。要几千个火星碎屑请走 Dust 或结构体池
/// （蓝图 §11.4）。
/// </para>
/// </summary>
public abstract class MonoParticle
{
    /// <summary>位置。<b>坐标系由 <see cref="Space"/> 决定</b>：<c>World</c> 是世界坐标，其余是屏幕/UI 坐标。</summary>
    public Vector2 Position;

    /// <summary>每帧位移（会被直接加到 <see cref="Position"/> 上，不乘时间步长）。</summary>
    public Vector2 Velocity;

    /// <summary>绘制缩放。</summary>
    public Vector2 Scale = Vector2.One;

    /// <summary>绘制颜色，会再乘上 <see cref="Opacity"/>。</summary>
    public Color Color = Color.White;

    /// <summary>当前旋转弧度。</summary>
    public float Rotation;

    /// <summary>每帧旋转增量（弧度）。</summary>
    public float RotationSpeed;

    /// <summary>不透明度乘数，范围外的值由贴图混合自然截断。</summary>
    public float Opacity = 1f;

    /// <summary>已经存活了多少帧，由管理器递增。</summary>
    public int Time;

    /// <summary>总生命周期（帧）。到点后管理器自动回收。</summary>
    public int Lifetime = 30;

    /// <summary>是否处于活动状态（已被 <see cref="Spawn"/> 且尚未到寿）。</summary>
    public bool Active { get; internal set; }

    /// <summary>生命周期进度 <c>[0,1]</c>；<see cref="Lifetime"/> 非正时恒为 1。</summary>
    public float LifetimeRatio => Lifetime <= 0 ? 1f : MathHelper.Clamp(Time / (float)Lifetime, 0f, 1f);

    /// <summary>该粒子使用的混合状态。管理器按它分组，同组合并成一次 <see cref="SpriteBatch"/> 批次。</summary>
    public virtual BlendState BlendState => BlendState.AlphaBlend;

    /// <summary>
    /// 该粒子所在的坐标空间，默认 <see cref="MonoGraphicsSpace.World"/>。
    /// <para>
    /// <b>它同时决定"画在哪个相位"</b>：<c>World</c> 在世界的绘制相位（<c>On_Main.DrawDust</c>）
    /// 用相机矩阵画、位置是世界坐标；<c>Screen</c> / <c>UI</c> 在界面相位
    /// （<c>ModSystem.PostDrawInterface</c>）画进游戏已经开好的 UI 批次、位置是屏幕坐标。
    /// </para>
    /// <para>
    /// <b>界面相位的两条限制</b>：批次由游戏自己开着（自带 <c>Main.UIScaleMatrix</c>），所以
    /// ① <c>Screen</c> 与 <c>UI</c> 对粒子而言落在同一相位、同一变换；
    /// ② 粒子的 <see cref="BlendState"/> 覆盖在界面相位<b>不生效</b>（批次已经开着，不能再 <c>Begin</c>）。
    /// 界面侧确实需要自定义混合或着色器时，请自己落在 <c>End()</c>/<c>Begin()</c> 之间画
    /// （见 <see cref="MonoSpriteBatchExtensions"/>）。
    /// </para>
    /// </summary>
    public virtual MonoGraphicsSpace Space => MonoGraphicsSpace.World;

    /// <summary>该粒子的翻转方式。</summary>
    public virtual SpriteEffects SpriteEffects => SpriteEffects.None;

    /// <summary>贴图帧；为 null 表示使用整张贴图。</summary>
    public virtual Rectangle? Frame => null;

    /// <summary>粒子贴图。</summary>
    public abstract Texture2D Texture { get; }

    /// <summary>
    /// 把本实例交给管理器开始活动。<b>重复调用同一个已活动的实例是安全的</b>（会被忽略），
    /// 不会产生重复条目。专用服务器上什么都不做。
    /// </summary>
    /// <returns>自身，便于链式写法。</returns>
    public MonoParticle Spawn()
    {
        if (!Main.dedServ)
            MonoParticleManager.Spawn(this);
        return this;
    }

    /// <summary>立即结束生命周期（把 <see cref="Time"/> 推到 <see cref="Lifetime"/>，下一帧被回收）。</summary>
    public void Kill() => Time = Lifetime;

    /// <summary>每帧的行为钩子，在位置/旋转推进<b>之前</b>调用。</summary>
    public virtual void Update() { }

    /// <summary>
    /// 默认绘制：贴图居中、按 <see cref="Scale"/>/<see cref="Rotation"/>/<see cref="Color"/> 画一次。
    /// <para>覆写它即可实现拖尾、软粒子等自定义绘制；<b>不要在这里 Begin/End 批次</b>，批次由管理器按混合状态开好。</para>
    /// </summary>
    /// <param name="spriteBatch">已按本粒子的 <see cref="BlendState"/> 开始绘制的批次。</param>
    public virtual void Draw(SpriteBatch spriteBatch)
    {
        // 只有 World 需要减屏幕位置；界面相位直接给屏幕/UI 坐标。
        Vector2 screen = Space == MonoGraphicsSpace.World ? Position - Main.screenPosition : Position;
        spriteBatch.Draw(Texture, screen, Frame, Color * Opacity, Rotation, Texture.Size() * 0.5f, Scale, SpriteEffects, 0f);
    }
}
