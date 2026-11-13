using ReLogic.Content;
using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.Primitives;

/// <summary>
/// 图元批次的 Asset/Effect 入口。提交时读取资产当前值，再写参数并应用 pass；不持有或释放 Effect。
/// 回调只设置参数和贴图，不调用 Apply、不提交绘制，也不开关 SpriteBatch。
/// </summary>
/// <remarks>
/// 这是延迟调用：回调不能捕获会在提交前改变的实体字段或共享 Parameters 对象。
/// 每条命令需要不同值时用 Create 保存值类型状态；状态中的数组和引用仍需由调用方保证不变。
/// 自定义参数先应用，随后框架覆盖 uWorldViewProjection，保证图元的坐标空间正确。
/// </remarks>
public class MonoEffectBinding
{
    private readonly Action<Effect>? configure;

    /// <summary>借用的资产；每次提交都重新读取 Value，兼容资产热重载。</summary>
    public Asset<Effect>? Asset => Resource.Asset;

    /// <summary>本绑定借用的稳定资源句柄。由创建资源的集合或调用方负责释放。</summary>
    public MonoEffectResource Resource { get; }

    /// <summary>要应用的 pass 名称；不存在时跳过这条绘制，不沿用前一个效果。</summary>
    public string PassName { get; }

    /// <summary>创建可复用绑定。没有回调时由框架只设置通用参数和应用 pass。</summary>
    /// <param name="asset">已编译的 Effect 资产。</param>
    /// <param name="configure">提交时设置自定义参数和采样槽；不得调用 EffectPass.Apply。</param>
    /// <param name="passName">Pass 名称。</param>
    public MonoEffectBinding(Asset<Effect> asset, Action<Effect>? configure = null, string passName = "AutoloadPass")
        : this(new MonoEffectResource(asset), configure, passName) { }

    /// <summary>借用现有资源句柄，可直接使用已导入的外部克隆。</summary>
    public MonoEffectBinding(MonoEffectResource resource, Action<Effect>? configure = null, string passName = "AutoloadPass")
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentException.ThrowIfNullOrEmpty(passName);
        Resource = resource;
        PassName = passName;
        this.configure = configure;
    }

    /// <summary>按值保存一条命令的参数。使用 static 回调可避免捕获；引用字段不会自动深拷贝。</summary>
    /// <typeparam name="TState">包含本条命令参数的值类型。</typeparam>
    /// <param name="asset">Effect 资产。</param>
    /// <param name="state">录制时的参数值。</param>
    /// <param name="configure">提交时把保存的参数写入当前 Effect。</param>
    /// <param name="passName">Pass 名称。</param>
    public static MonoEffectBinding Create<TState>(Asset<Effect> asset, TState state, Action<TState, Effect> configure, string passName = "AutoloadPass")
        where TState : struct
    {
        ArgumentNullException.ThrowIfNull(configure);
        return Create(new MonoEffectResource(asset), state, configure, passName);
    }

    /// <summary>按值保存本条命令状态，借用已有（可独立克隆的）资源句柄。</summary>
    public static MonoEffectBinding Create<TState>(MonoEffectResource resource, TState state, Action<TState, Effect> configure, string passName = "AutoloadPass")
        where TState : struct
    {
        ArgumentNullException.ThrowIfNull(configure);
        return new StatefulBinding<TState>(resource, state, configure, passName);
    }

    /// <summary>设置自定义参数。自定义绑定应只写入绘制状态，不直接绘制。</summary>
    /// <param name="effect">本次提交时资产提供的 Effect。</param>
    protected virtual void Configure(Effect effect) => configure?.Invoke(effect);

    internal bool Apply(Matrix matrix, bool useTexture)
    {
        if (Resource.IsDisposed)
            return false;
        Effect effect = Resource.Effect;
        if (effect.IsDisposed)
            return false;
        Resource.InvalidateParameters();

        // 与 MonoShader 一致的通用参数；生成接口的回调可以覆盖它们。
        effect.Parameters["globalTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
        effect.Parameters["screenPosition"]?.SetValue(Main.screenPosition);
        effect.Parameters["screenSize"]?.SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
        effect.Parameters["focusPosition"]?.SetValue(Main.screenPosition + Main.ScreenSize.ToVector2() * 0.5f);
        effect.Parameters["opacity"]?.SetValue(1f);
        effect.Parameters["useTexture"]?.SetValue(useTexture ? 1f : 0f);
        if (effect.GraphicsDevice.Textures[1] is Texture2D texture)
            effect.Parameters["textureSize1"]?.SetValue(new Vector2(texture.Width, texture.Height));
        Configure(effect);
        effect.Parameters["uWorldViewProjection"]?.SetValue(matrix);

        // 不跨 Effect 或 technique 缓存 pass，热重载后不会沿用旧句柄。
        EffectPass? pass = effect.CurrentTechnique?.Passes[PassName];
        if (pass is null)
            return false;
        pass.Apply();
        return true;
    }

    private sealed class StatefulBinding<TState>(MonoEffectResource resource, TState state, Action<TState, Effect> applyState, string passName)
        : MonoEffectBinding(resource, passName: passName) where TState : struct
    {
        protected override void Configure(Effect effect) => applyState(state, effect);
    }
}
