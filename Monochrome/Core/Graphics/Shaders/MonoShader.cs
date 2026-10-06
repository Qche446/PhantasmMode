using ReLogic.Content;

namespace Monochrome.Core.Graphics.Shaders;

/// <summary>
/// 对已编译 <c>.fxc</c> 资产的一个小而耐用的包装：参数缓存、纹理绑定、pass 选择与热重载替换。
/// <para>
/// <b>参数缓存的意义：</b>每次 <c>SetValue</c> 都要穿过 FNA 的指针层，而绝大多数参数一帧只变一次或根本不变；
/// 缓存下来可以跳过重复写入（蓝图 §11.2「不要每帧按名字查」）。
/// </para>
/// </summary>
public sealed class MonoShader : IDisposable
{
    /// <summary>自动加载约定使用的默认 pass 名。</summary>
    public const string DefaultPassName = "AutoloadPass";

    private readonly Dictionary<string, object?> parameterCache = [with(StringComparer.Ordinal)];
    private EffectPass? cachedPass;
    private string? cachedPassName;
    private bool warnedMissingPass;

    /// <summary>注册用的名字（形如 <c>模组名.文件名</c>）。</summary>
    public string Name { get; }

    /// <summary>
    /// 模组相对资产路径（不含扩展名），形如
    /// <c>Monochrome/Assets/Effects/Primitives/StandardPrimitive</c>。
    /// <para>
    /// <b>热重载靠它反推 <c>.fx</c> 源文件</b>（见 <see cref="MonoShaderReloader"/>）：斜杠前是模组名，
    /// 后面是模组内相对路径。用 <see cref="MonoShaderManager.SetShader(string, Effect)"/> 手工装进来的着色器这里是空串，
    /// 因此不参与源文件热重载。
    /// </para>
    /// </summary>
    public string AssetPath { get; }

    /// <summary>底层的 FNA 效果对象。<b>热重载后这个引用会被换掉</b>，不要长期持有它。</summary>
    public Effect Effect { get; private set; }

    /// <summary><see cref="Effect"/> 的同义属性，便于与 Luminance 风格代码对接。</summary>
    public Effect WrappedEffect => Effect;

    /// <summary>是否已释放。</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>本次（或最近一次热重载后）装入的 UTC 时间，供诊断显示。</summary>
    public DateTime LoadedAtUtc { get; private set; }

    internal MonoShader(string name, Effect effect, string assetPath = "")
    {
        Name = name;
        AssetPath = assetPath;
        Effect = effect;
        LoadedAtUtc = DateTime.UtcNow;
    }

    /// <summary>按名字取参数。<b>名字不存在时返回 null（FNA 的字符串索引器不抛异常）</b>，不要忘记判空。</summary>
    /// <param name="name">着色器中的参数名。</param>
    public EffectParameter? this[string name] => IsDisposed ? null : Effect.Parameters[name];

    /// <summary>
    /// 把一个值写入同名参数。值未变化或参数不存在时返回 false（不视为错误）。
    /// </summary>
    /// <param name="name">着色器中的参数名。</param>
    /// <param name="value">支持 bool/int/float/Vector2/Vector3/Vector4/Color/Matrix/Texture2D/Rectangle 及其数组。</param>
    /// <returns>是否真的向 GPU 写入了一次。</returns>
    public bool TrySetParameter(string name, object? value)
    {
        // 这里真正的理由是"没有 GraphicsDevice"，所以用 dedServ 而不是 netMode == Server。
        // 两者目前等价（netMode = 2 只在 dedServ 分支里被设置：Terraria\Main.cs:6592、Netplay.cs:317），
        // 但用"网络模式"表达"图形可用性"是错的——一旦原版改成进程内 Host & Play，这条会静默失效。
        if (Main.dedServ || IsDisposed)
            return false;

        EffectParameter? parameter = Effect.Parameters[name];
        if (parameter is null)
            return false;
        if (parameterCache.TryGetValue(name, out object? old) && Equals(old, value))
            return false;

        try
        {
            switch (value)
            {
                case bool v: parameter.SetValue(v); break;
                case bool[] v: parameter.SetValue(v); break;
                case int v: parameter.SetValue(v); break;
                case int[] v: parameter.SetValue(v); break;
                case float v: parameter.SetValue(v); break;
                case Vector2 v: parameter.SetValue(v); break;
                case Vector3 v: parameter.SetValue(v); break;
                case Vector4 v: parameter.SetValue(v); break;
                case Color v: parameter.SetValue(v.ToVector3()); break;
                case Matrix v: parameter.SetValue(v); break;
                case Texture2D v: parameter.SetValue(v); break;
                case float[] v: parameter.SetValue(v); break;
                case Vector2[] v: parameter.SetValue(v); break;
                case Vector3[] v: parameter.SetValue(v); break;
                case Vector4[] v: parameter.SetValue(v); break;
                case Matrix[] v: parameter.SetValue(v); break;
                case Rectangle v: parameter.SetValue(new Vector4(v.X, v.Y, v.Width, v.Height)); break;
                case null: parameter.SetValue(0f); break;
                default: return false;
            }
        }
        catch (Exception ex)
        {
            Monochrome.Instance?.Logger.Warn($"着色器 '{Name}' 拒绝了参数 '{name}'：{ex.Message}");
            return false;
        }

        parameterCache[name] = value;
        return true;
    }

    /// <summary>
    /// 清空参数缓存。当同一个 <see cref="Effect"/> 被别处（例如原版的屏幕滤镜）绕过本类直接写参数时必须调用，
    /// 否则这里会以为"值没变"而跳过写入。
    /// </summary>
    public void ResetParameterCache()
    {
        parameterCache.Clear();
        cachedPass = null;
        cachedPassName = null;
    }

    /// <summary>写入一个参数，返回自身以便链式调用（忽略"未变化/不存在"的返回值）。</summary>
    /// <param name="name">参数名。</param>
    /// <param name="value">参数值。</param>
    public MonoShader SetParameter(string name, object? value)
    {
        TrySetParameter(name, value);
        return this;
    }

    /// <summary>
    /// 把纹理绑到指定采样槽，并把它的尺寸写进 <c>textureSize{槽位}</c> 参数（着色器可选地使用它）。
    /// </summary>
    /// <param name="texture">纹理。</param>
    /// <param name="textureIndex">采样槽位；0 通常是 SpriteBatch 的绘制纹理，自定义图元一般从 1 开始。</param>
    /// <param name="samplerState">采样状态；为 null 时沿用当前设置。</param>
    public MonoShader SetTexture(Texture2D texture, int textureIndex, SamplerState? samplerState = null)
    {
        // 同 TrySetParameter 的理由：真正的条件是"没有图形设备"，不是"网络模式是服务端"。
        if (IsDisposed || Main.dedServ)
            return this;
        Main.instance.GraphicsDevice.Textures[textureIndex] = texture;
        if (samplerState is not null)
            Main.instance.GraphicsDevice.SamplerStates[textureIndex] = samplerState;
        TrySetParameter($"textureSize{textureIndex}", texture.Size());
        return this;
    }

    /// <summary><see cref="SetTexture(Texture2D, int, SamplerState?)"/> 的资产重载。</summary>
    /// <param name="texture">纹理资产。</param>
    /// <param name="textureIndex">采样槽位。</param>
    /// <param name="samplerState">采样状态。</param>
    public MonoShader SetTexture(Asset<Texture2D> texture, int textureIndex, SamplerState? samplerState = null)
        => SetTexture(texture.Value, textureIndex, samplerState);

    /// <summary>写入一组通用的屏幕相关参数（<c>globalTime</c>/<c>screenPosition</c>/<c>screenSize</c>/<c>focusPosition</c>/<c>opacity</c>）。</summary>
    /// <param name="focusPosition">焦点位置；为 null 时取屏幕中心。</param>
    /// <param name="opacity">整体不透明度。</param>
    public MonoShader SetCommonParameters(Vector2? focusPosition = null, float opacity = 1f)
    {
        TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        TrySetParameter("screenPosition", Main.screenPosition);
        TrySetParameter("screenSize", new Vector2(Main.screenWidth, Main.screenHeight));
        TrySetParameter("focusPosition", focusPosition ?? Main.screenPosition + Main.ScreenSize.ToVector2() * 0.5f);
        TrySetParameter("opacity", opacity);
        return this;
    }

    /// <summary>
    /// 应用一个 pass。pass 会被缓存，不会每次绘制都按名字做一次线性查找。
    /// <para>
    /// 名字查不到时退回第 0 个 pass（FNA 的 <c>EffectPassCollection</c> 字符串索引器查不到时返回 null，不抛异常）。
    /// 整套 effect 一个 pass 都没有时返回 false 并只记一次日志，不会抛异常。
    /// </para>
    /// </summary>
    /// <param name="pass">pass 名；为 null 时用 <see cref="DefaultPassName"/>。</param>
    /// <returns>是否成功应用。</returns>
    public bool Apply(string? pass = null)
    {
        if (IsDisposed)
            return false;

        EffectTechnique? technique = Effect.CurrentTechnique;
        if (technique is null)
            return false;

        EffectPass? selected = ResolvePass(technique, pass);
        if (selected is null)
        {
            if (!warnedMissingPass)
            {
                warnedMissingPass = true;
                Monochrome.Instance?.Logger.Warn($"着色器 '{Name}' 没有任何可用的 pass（请求 '{pass ?? DefaultPassName}'），后续将安静跳过。");
            }
            return false;
        }

        selected.Apply();
        return true;
    }

    private EffectPass? ResolvePass(EffectTechnique technique, string? pass)
    {
        if (cachedPass is not null && string.Equals(cachedPassName, pass, StringComparison.Ordinal))
            return cachedPass;

        EffectPass? resolved = technique.Passes[pass ?? DefaultPassName] ?? technique.Passes[0];
        cachedPass = resolved;
        cachedPassName = pass;
        return resolved;
    }

    /// <summary>
    /// 用新编译出来的 effect 替换当前实现（热重载）。必须同时失效参数缓存与 pass 缓存：旧的
    /// <see cref="EffectParameter"/> / <see cref="EffectPass"/> 绑在已经被换掉的 <see cref="Effect"/> 上，
    /// 继续写会静默无效。
    /// <para>
    /// 不 <c>Dispose</c> 旧的 <see cref="Effect"/>：同一个资产名在 <c>AssetRepository</c> 的缓存里
    /// 只有一个实例，别的着色器管理系统（最典型的是 Luminance 的 <c>ManagedShader</c>）也持有它。
    /// 在这里释放，对方立刻变成"持有已释放对象"。释放交给 AssetRepository，或由 GC/FNA 终结器处理。
    /// </para>
    /// </summary>
    /// <param name="replacement">新编译的 effect。</param>
    internal void Replace(Effect replacement)
    {
        Effect = replacement;
        LoadedAtUtc = DateTime.UtcNow;
        ResetParameterCache();
        warnedMissingPass = false;
    }

    /// <summary>
    /// 标记本包装对象不再使用。<b>刻意不 <c>Dispose</c> 底层的 <see cref="Effect"/>。</b>
    /// <para>
    /// 理由与 <see cref="Replace"/> 相同：那个 <see cref="Effect"/> 可能同时被 AssetRepository 的缓存
    /// 与别的着色器管理器（例如 Luminance 的 <c>ManagedShader</c>）持有，我们只是引用者之一，不是所有者。
    /// 在模组卸载时释放它是"顺手"，但一旦释放顺序与对方不巧，就会把对方正在用的对象弄死——
    /// 而少释放一个 Effect 的代价只是等 GC，几乎为零。
    /// </para>
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        parameterCache.Clear();
        cachedPass = null;
        cachedPassName = null;
    }
}
