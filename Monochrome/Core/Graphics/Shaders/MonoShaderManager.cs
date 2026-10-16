using ReLogic.Content;

namespace Monochrome.Core.Graphics.Shaders;

/// <summary>
/// 挂在模组主类上，声明<b>"允许 Monochrome 把重编出来的 <c>.fxc</c> 写回我的源目录"</b>。
/// <para>
/// <b>它管的是写回，不管注册。</b>注册是全场的：任何已加载模组的着色器资产都会进注册表，
/// 消费者按 <c>模组名.文件名</c> 就能取到别的模组的着色器。写回会动别人的磁盘，这件事才该由消费者表态。
/// </para>
/// <para>
/// 判据用显式声明，因为"程序集引用了 Monochrome"只说明消费者用到了它某个类型。只用到会被内联的常量时，
/// 编译器不会写出那条引用，判定就会静默失效。
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MonoShaderScopeAttribute : Attribute;

/// <summary>
/// 已编译着色器资产的中央注册表。<b>缺失资产永远不阻止模组加载</b>——加载失败只记一次警告并返回 false。
/// <para>
/// 名字约定是 <c>模组名.文件名</c>（例如 <c>Monochrome.Glow</c>），与 Luminance 一致，
/// 这样跨模组的同名文件不会互相覆盖。
/// </para>
/// </summary>
public sealed class MonoShaderManager : ModSystem
{
    private static Dictionary<string, MonoShader>? shaders;

    /// <summary>
    /// 着色器加载完成后要执行的动作队列（由 <see cref="Monochrome.PostSetupContent"/> 排空）。
    /// 用 <see cref="QueueAfterLoad"/> 入队，不要直接操作它。
    /// </summary>
    public static readonly Queue<Action> PostShaderLoadActions = new();

    /// <summary>当前已注册的着色器（名字不区分大小写）。</summary>
    public static IReadOnlyDictionary<string, MonoShader> Shaders => shaders ??= [with(StringComparer.OrdinalIgnoreCase)];

    /// <summary>是否已经跑完一轮自动加载。</summary>
    public static bool HasFinishedLoading { get; internal set; }

    /// <summary>初始化注册表（专用服务器上不做任何事）。</summary>
    public override void OnModLoad()
    {
        if (Main.dedServ)
            return;
        shaders = [with(StringComparer.OrdinalIgnoreCase)];
        HasFinishedLoading = false;
    }

    /// <summary>
    /// 按约定从一个模组里找出所有着色器资产并注册：文件名以 <c>.fxc</c> 或 <c>.xnb</c> 结尾，
    /// 且路径符合 <see cref="IsShaderAssetPath"/>。
    /// <para>
    /// <b>注册不设管辖门槛</b>：任何已加载模组的着色器都会进这张表，消费者按 <c>模组名.文件名</c>
    /// 就能取用别的模组的着色器（与 Luminance 的注册语义一致）。写回源目录（热重载）才需要该模组声明
    /// <see cref="MonoShaderScopeAttribute"/>，见 <see cref="MonoShader.CanWriteBack"/>。
    /// </para>
    /// <para>
    /// Monochrome 在 <c>PostSetupContent</c> 里对每个已加载模组各调一次，重复的名字被跳过。
    /// </para>
    /// </summary>
    /// <param name="mod">要扫描的模组。</param>
    public static void LoadForMod(Mod mod)
    {
        if (Main.dedServ || shaders is null || mod is null)
            return;

        // Mod.GetFileNames() 的实现是 `File?.GetFileNames()`——
        // **没有 .tmod 的模组返回 null，而不是空表**，所以不能直接 foreach。
        // 最典型的就是 tML 自带的 ModLoaderMod：ModLoader.Mods[0] 正是它（ModLoader.cs:179 的
        // list.Insert(0, new ModLoaderMod())），而它的构造函数从不给 File 赋值
        // （全仓库唯一的赋值点是 AssemblyManager.cs:241 的 obj.File = mod.modFile），
        // 它走的是重写的 CreateDefaultContentSource（AssemblyResourcesContentSource）。
        List<string>? files = mod.GetFileNames();
        if (files is null)
        {
            // `GetFileNames()` 对"没有 .tmod 的模组"返回 null（不是空表），例如 tML 自带的 ModLoaderMod。
            // 但一个**正常的**模组也返回 null 就说明它的内容源没建好——那会让它的着色器一个都注册不上，
            // 而此前这里是静默 return。说出来，别让人去猜。
            Monochrome.Instance?.Logger.Info($"{mod.Name}：GetFileNames() 为 null（内容源未就绪？），跳过着色器扫描。");
            return;
        }

        bool self = ReferenceEquals(mod, Monochrome.Instance);
        Dictionary<string, MonoShader> registry = shaders;

        // 先扫 .fxc 再扫 .xnb，两趟都靠 registry 里"同名已存在就跳过"来去重。
        // 顺序是刻意的：同名同时存在两种产物时以 **.fxc** 为准——那正是 `/mono shader reload`
        // 会写回的文件。不这样做的话就是"谁先被 GetFileNames() 枚举到就注册谁"，
        // 而那个顺序没有保证，于是旧的 .xnb 可能盖掉刚重载出来的新 .fxc。
        foreach (string file in files)
        {
            if (file.EndsWith(".fxc", StringComparison.OrdinalIgnoreCase))
                TryRegisterAsset(mod, file, registry, self);
        }

        int shadowed = 0;
        foreach (string file in files)
        {
            if (!file.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
                continue;
            string relative = file.Replace('\\', '/');
            if (IsShaderAssetPath(relative, self) && registry.ContainsKey($"{mod.Name}.{Path.GetFileNameWithoutExtension(relative)}"))
            {
                shadowed++;
                continue;
            }
            TryRegisterAsset(mod, file, registry, self);
        }

        // "被覆盖"这件事必须说出来：否则源目录里那份 .xnb 是完全不可见的，
        // 而它正是"改了 .fx 却没生效"这类困惑的常见来源。
        // 真正删掉它是在热重载写回 .fxc 的时候（MonoShaderReloader.RemoveStaleXnb），
        // 加载期只报告不动磁盘。
        // 只有可写回的模组才会被 /monoshader reload 顺手清理那些 .xnb，所以这行只对它们有意义。
        if (shadowed > 0 && IsReloadable(mod))
        {
            Monochrome.Instance?.Logger.Info(
                $"{mod.Name}：{shadowed} 个 .xnb 与同名 .fxc 重复，已忽略 .xnb（以 .fxc 为准）。" +
                "这些 .xnb 来自旧的 Content Pipeline；任何一次 /monoshader reload 都会顺手删掉它们。");
        }

        // 一行摘要：这个模组到底交出了多少着色器。**没有这行的话，"一个都没注册"与"这个模组本来就没有"
        // 在日志里长得一模一样**——而依赖方（例如 FPM）看到的只是"我的着色器没被注册"。
        if (CountShaderAssets(files, self) == 0)
            return;

        int own = 0;
        foreach (string name in registry.Keys)
        {
            if (name.StartsWith(mod.Name + ".", StringComparison.Ordinal))
                own++;
        }
        string scope = self
            ? "（Monochrome 自身，可写回）"
            : IsReloadable(mod)
                ? "（已声明 [MonoShaderScope]，可写回源目录）"
                : "（未声明 [MonoShaderScope]，只读，热重载不写回）";
        Monochrome.Instance?.Logger.Info(
            $"{mod.Name}：着色器注册 {own} 个{scope}，注册表共 {registry.Count} 个。");
    }

    /// <summary>
    /// 路径是否符合着色器自动加载的约定。
    /// <para>
    /// <b>只有 Monochrome 自己认 <c>Assets/Effects</c>。</b>那是很常见的目录名，别的模组放在那里的
    /// <c>.xnb</c> 多半是贴图（tML 会把 <c>.png</c> 编成同名 <c>.xnb</c>），按 <c>Effect</c> 去请求只会得到
    /// 一串加载失败的警告。外来模组只认 <c>Assets/AutoloadedEffects/Shaders</c>，与 Luminance 判据一致。
    /// </para>
    /// </summary>
    /// <param name="relativePath">模组相对路径，分隔符已统一成正斜杠。</param>
    /// <param name="self">这个模组是不是 Monochrome 自己。</param>
    private static bool IsShaderAssetPath(string relativePath, bool self)
        => !relativePath.Contains("Compiler/", StringComparison.OrdinalIgnoreCase)
        && (relativePath.Contains("Assets/AutoloadedEffects/Shaders", StringComparison.OrdinalIgnoreCase)
            || (self && relativePath.Contains("Assets/Effects", StringComparison.OrdinalIgnoreCase)));

    /// <summary>把模组里的一个候选文件按约定注册（路径与扩展名不符合就静默跳过）。</summary>
    private static void TryRegisterAsset(Mod mod, string file, Dictionary<string, MonoShader> registry, bool self)
    {
        string relative = file.Replace('\\', '/');
        if (!IsShaderAssetPath(relative, self))
            return;
        // 不能用 Path.ChangeExtension(relative, null)：它返回的是**带尾点**的名字
        // （实测 "…/StandardPrimitive.fxc" -> "…/StandardPrimitive."），
        // 而 Path.GetExtension("….StandardPrimitive.") 返回空字符串，
        // AssetRepository 于是把这个名字当成"没有扩展名"的资产，永远解析不到 .fxc。
        string withoutExtension = relative[..^Path.GetExtension(relative).Length];
        string assetPath = mod.Name + "/" + withoutExtension;
        string name = mod.Name + "." + Path.GetFileNameWithoutExtension(relative);
        if (registry.ContainsKey(name))
            return;
        TryLoad(name, assetPath, out _, IsReloadable(mod));
    }

    /// <summary>
    /// 每秒重扫一次 Luminance 的文件监视器：它建 watcher 的时机依赖两个模组的加载顺序，
    /// 只在 <c>PostSetupContent</c> 里扫一次不保证赶上（而且 watcher 被重建时也能再压住）。
    /// 节流在 <see cref="MonoShaderReloader.TrySuppressLuminanceAutoReload"/> 内部。
    /// </summary>
    public override void PostUpdateEverything() => MonoShaderReloader.TrySuppressLuminanceAutoReload();

    /// <summary>释放全部着色器并清空注册表。</summary>
    public override void Unload()
    {
        if (shaders is null)
            return;
        foreach (MonoShader shader in shaders.Values)
            shader.Dispose();
        shaders = null;
        HasFinishedLoading = false;
        PostShaderLoadActions.Clear();
    }

    /// <summary>
    /// 这个模组的着色器是否允许 Monochrome 把编好的 <c>.fxc</c> 写回它的源目录。
    /// <para>
    /// <b>它管的是写回，不管注册</b>——注册是全场的，注册表里本来就有其他模组的着色器。
    /// 热重载会动别人的磁盘，这种操作该由消费者自己表态，不该由库靠程序集引用去猜。
    /// </para>
    /// </summary>
    /// <param name="mod">要判断的模组。</param>
    public static bool IsReloadable(Mod mod)
    {
        if (mod is null)
            return false;
        if (ReferenceEquals(mod, Monochrome.Instance))
            return true;

        return Attribute.IsDefined(mod.GetType(), typeof(MonoShaderScopeAttribute), inherit: false);
    }

    /// <summary>数一个模组里 Monochrome 会注册的那些资产（<c>.fxc</c> / <c>.xnb</c>，且路径符合着色器约定）。</summary>
    /// <param name="files">模组文件表。</param>
    /// <param name="self">这个模组是不是 Monochrome 自己。</param>
    private static int CountShaderAssets(List<string> files, bool self)
    {
        int count = 0;
        foreach (string file in files)
        {
            if (!file.EndsWith(".fxc", StringComparison.OrdinalIgnoreCase)
                && !file.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
                continue;
            if (IsShaderAssetPath(file.Replace('\\', '/'), self))
                count++;
        }
        return count;
    }

    /// <summary>按资产路径加载一个着色器并注册。<b>失败只记警告，返回 false。</b></summary>
    /// <param name="name">注册名（<c>模组名.文件名</c>）。</param>
    /// <param name="assetPath">tML 资产路径（<c>模组名/相对路径</c>，不带扩展名）。</param>
    /// <param name="shader">加载成功时的包装对象；失败为 null。</param>
    /// <param name="canWriteBack">是否允许热重载把 <c>.fxc</c> 写回该模组的源目录；默认不允许。</param>
    /// <returns>是否加载成功。</returns>
    public static bool TryLoad(string name, string assetPath, out MonoShader? shader, bool canWriteBack = false)
    {
        shader = null;
        if (Main.dedServ || shaders is null)
            return false;
        try
        {
            Effect effect = ModContent.Request<Effect>(assetPath, AssetRequestMode.ImmediateLoad).Value;
            shader = new MonoShader(name, effect, assetPath, canWriteBack);
            shaders[name] = shader;
            return true;
        }
        catch (Exception ex)
        {
            Monochrome.Instance?.Logger.Warn($"无法加载着色器 '{assetPath}'：{ex.Message}");
            return false;
        }
    }

    /// <summary>按名字取着色器，不抛异常。</summary>
    /// <param name="name">注册名。</param>
    /// <param name="shader">命中的着色器；未注册为 null。</param>
    /// <returns>是否取到。</returns>
    public static bool TryGet(string name, out MonoShader? shader)
    {
        shader = null;
        return shaders is not null && shaders.TryGetValue(name, out shader);
    }

    /// <summary>按名字取着色器，未注册时抛 <see cref="KeyNotFoundException"/>。</summary>
    /// <param name="name">注册名。</param>
    /// <exception cref="KeyNotFoundException">名字未注册。</exception>
    public static MonoShader GetShader(string name)
    {
        if (!TryGet(name, out MonoShader? shader) || shader is null)
            throw new KeyNotFoundException($"Monochrome shader '{name}' is not registered.");
        return shader;
    }

    /// <summary>
    /// 直接装入（或<b>替换</b>）一个已构造好的 <see cref="Effect"/>。
    /// <para>已注册同名时走热重载路径（会失效参数缓存与 pass 缓存），不会泄漏旧对象。</para>
    /// </summary>
    /// <param name="name">注册名。</param>
    /// <param name="effect">效果对象；被替换时旧的会被 <c>Dispose</c>。</param>
    public static void SetShader(string name, Effect effect)
    {
        if (Main.dedServ || shaders is null)
            return;
        if (shaders.TryGetValue(name, out MonoShader? existing))
            existing.Replace(effect);
        else
            shaders[name] = new MonoShader(name, effect);
    }

    /// <summary><see cref="SetShader(string, Effect)"/> 的 <see cref="Ref{T}"/> 重载。</summary>
    /// <param name="name">注册名。</param>
    /// <param name="effect">效果引用。</param>
    public static void SetShader(string name, Ref<Effect> effect)
        => SetShader(name, effect.Value);

    /// <summary>
    /// 把动作排到"着色器全部加载完成"之后执行。
    /// <b>已经加载完成时立刻执行</b>，因此早晚调用都安全。
    /// </summary>
    /// <param name="action">要执行的动作。</param>
    public static void QueueAfterLoad(Action action)
    {
        if (HasFinishedLoading)
            action?.Invoke();
        else if (action is not null)
            PostShaderLoadActions.Enqueue(action);
    }

    /// <summary>
    /// 用一段新编译出来的字节码热重载已注册的着色器。
    /// <para>
    /// <b>先构造再替换</b>：<c>new Effect(...)</c> 失败时保留旧的 <see cref="Effect"/> 原样不动，
    /// 绝不因为编译失败让特效消失。成功后 <see cref="MonoShader.Replace"/> 会失效参数与 pass 缓存。
    /// </para>
    /// </summary>
    /// <param name="name">注册名。</param>
    /// <param name="bytecode">新的 <c>.fxc</c> 字节码。</param>
    /// <returns>是否重载成功。</returns>
    public static bool TryReload(string name, ReadOnlySpan<byte> bytecode)
    {
        if (Main.dedServ || shaders is null || !shaders.TryGetValue(name, out MonoShader? shader))
            return false;
        try
        {
            Effect replacement = new(Main.instance.GraphicsDevice, bytecode.ToArray());
            shader.Replace(replacement);
            return true;
        }
        catch (Exception ex)
        {
            Monochrome.Instance?.Logger.Warn($"无法热重载着色器 '{name}'：{ex.Message}");
            return false;
        }
    }
}
