using ReLogic.Content;

namespace Monochrome.Core.Graphics.Shaders;

/// <summary>稳定的 Effect 句柄。跟随 Asset 更新，只释放自己创建的克隆。</summary>
public sealed class MonoEffectResource : IDisposable
{
    private readonly MonoAssetInstance<Effect> instance;
    /// <summary>源资产；直接包装 Effect 时为空。</summary>
    public Asset<Effect>? Asset { get; }
    /// <summary>是否使用独立克隆。</summary>
    public bool IsIsolated { get; }
    /// <summary>已解析的实例版本。</summary>
    public int Version => instance.Version;
    /// <summary>是否已卸载。</summary>
    public bool IsDisposed => instance.IsDisposed;
    // Binding callbacks write outside MonoShader's value cache.
    internal int ParameterRevision { get; private set; }
    /// <summary>直接写入 Effect 参数后调用，通知共享此句柄的 MonoShader 失效值缓存。</summary>
    public void InvalidateParameters() => ParameterRevision++;
    /// <summary>当前实例。只在客户端绘制线程访问，不跨提交保存返回值。</summary>
    public Effect Effect => instance.Resolve();

    /// <summary>包装资产；克隆延迟到首次绘制创建，之后按源实例身份复用。</summary>
    public MonoEffectResource(Asset<Effect> asset, bool clone = false)
    {
        ArgumentNullException.ThrowIfNull(asset);
        Asset = asset;
        IsIsolated = clone;
        instance = new(() => asset.Value, clone ? static effect => effect.Clone() : null,
            static effect => MonoGraphicsDisposal.OnMainThread(effect));
    }

    /// <summary>借用调用方的 Effect，生命周期仍由调用方管理。</summary>
    public MonoEffectResource(Effect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        instance = new(() => effect);
    }

    /// <summary>释放克隆，从不释放源 Asset.Value 或借用的 Effect。</summary>
    public void Dispose() => instance.Dispose();
}
