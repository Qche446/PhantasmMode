using System.Diagnostics;
using System.Text;

namespace Monochrome.Core.Graphics.Shaders;

/// <summary>
/// 着色器热重载：把 <c>.fx</c> 交给随模组打包的 <c>fxc</c> 编成字节码，构造成功后才替换正在使用的
/// <see cref="Effect"/>，让"改 <c>.fx</c> → <c>/mono shader reload</c>"不必重启游戏。
/// <para>
/// 顺序是：编到临时文件 → 构造新 <see cref="Effect"/> → 构造成功才替换 → 原子写回源目录的 <c>.fxc</c>。
/// 任何一步失败都保留旧的 <see cref="Effect"/>。
/// </para>
/// <para>
/// 源文件优先取所属模组的 <c>SourceFolder</c>；没有源目录时退回从 <c>.tmod</c> 里取出 <c>.fx</c>
/// 写到临时目录，所以 <c>*.fx</c> 必须留在包里（见 §5.5）。
/// </para>
/// </summary>
public static class MonoShaderReloader
{
    /// <summary>随包携带的编译器所在目录（模组内相对路径，用正斜杠）。</summary>
    public const string CompilerFolder = "Assets/AutoloadedEffects/Compiler";

    /// <summary>编译器文件名。</summary>
    public const string CompilerFileName = "fxc.exe";

    /// <summary>编译器依赖的动态库文件名，必须和 <see cref="CompilerFileName"/> 放在同一个目录。</summary>
    public const string CompilerLibraryName = "d3dcompiler_47.dll";

    /// <summary>用环境变量指向本机编译器（打包的那份不可用时用）。</summary>
    public const string CompilerEnvironmentVariable = "MONOCHROME_SHADER_COMPILER";

    /// <summary>编译超时，毫秒。</summary>
    public const int CompileTimeoutMs = 15000;

    /// <summary>一次重载的结果。</summary>
    /// <param name="Name">着色器注册名。</param>
    /// <param name="Success">是否成功。</param>
    /// <param name="Message">结果说明（失败时是编译器的错误输出）。</param>
    public readonly record struct Result(string Name, bool Success, string Message);

    private static string? compilerDirectory;

    /// <summary>
    /// 重载所有从资产自动加载的着色器。专用服务器上返回空表。
    /// <para>
    /// <b>这里默认做 mtime 预筛</b>（<paramref name="force"/> 为 false 时）：源文件不比旁边的 <c>.fxc</c> 新
    /// 就跳过。因为"全部重载"是个顺手动作，而每次重编都要在主线程阻塞一次 <c>fxc</c>——
    /// 十几个着色器就是几秒的卡顿。被跳过的项会在结果里<b>明确写出来</b>，不会让你以为它编过了。
    /// </para>
    /// </summary>
    /// <param name="force">true 表示不管 mtime 一律重编。</param>
    public static IReadOnlyList<Result> ReloadAll(bool force = false)
    {
        List<Result> results = [];
        if (Main.dedServ)
            return results;
        foreach ((string name, MonoShader shader) in MonoShaderManager.Shaders)
            results.Add(ReloadShader(name, shader, skipIfUpToDate: !force));
        return results;
    }

    /// <summary>
    /// 按注册名重载一个着色器。名字未注册时返回失败结果。
    /// <para>
    /// <b>点名就一律重编，不做 mtime 预筛。</b>你已经明确说了要这个，mtime 检查只可能给你一个意外的
    /// "什么都没发生"（编辑器保留 mtime、`.fxc` 被别的工具刷新过、你怀疑产物损坏……）。
    /// </para>
    /// </summary>
    /// <param name="name">注册名（不区分大小写）。</param>
    public static Result Reload(string name)
    {
        if (Main.dedServ)
            return new Result(name, false, "专用服务器上没有图形设备");
        if (!MonoShaderManager.TryGet(name, out MonoShader? shader) || shader is null)
            return new Result(name, false, $"没有注册名为 '{name}' 的着色器（用 /mono shader status 看列表）");
        return ReloadShader(name, shader, skipIfUpToDate: false);
    }

    /// <summary>源文件不比旁边的 <c>.fxc</c> 新就认为无需重编。只有源目录里的文件适用（包内取出的没有可比对象）。</summary>
    private static bool IsUpToDate(string source)
    {
        try
        {
            string fxc = Path.ChangeExtension(source, ".fxc");
            return File.Exists(fxc) && File.GetLastWriteTimeUtc(fxc) > File.GetLastWriteTimeUtc(source);
        }
        catch
        {
            return false;
        }
    }

    private static Result ReloadShader(string name, MonoShader shader, bool skipIfUpToDate)
    {
        if (!TryFindSource(shader, out string source, out string? sourceRoot, out string reason))
            return new Result(name, false, reason);

        if (skipIfUpToDate && sourceRoot is not null && IsUpToDate(source))
            return new Result(name, true, "已是最新，跳过（源文件不比 .fxc 新；点名重载可强制）");

        string? compiler = EnsureCompiler(out string compilerReason);
        if (compiler is null)
            return new Result(name, false, compilerReason);

        string tempDirectory = Path.Combine(Path.GetTempPath(), "MonochromeShaderReload");
        string tempOutput = Path.Combine(tempDirectory, Path.GetFileNameWithoutExtension(source) + ".fxc");
        try
        {
            Directory.CreateDirectory(tempDirectory);
        }
        catch (Exception ex)
        {
            return new Result(name, false, $"无法创建临时目录：{ex.Message}");
        }

        if (!TryCompile(compiler, NormalizeSource(source), tempOutput, out string compileError))
            return new Result(name, false, compileError);

        byte[] bytecode;
        try
        {
            bytecode = File.ReadAllBytes(tempOutput);
        }
        catch (Exception ex)
        {
            return new Result(name, false, $"读取编译产物失败：{ex.Message}");
        }

        // 先构造再替换：TryReload 内部 new Effect 失败会保留旧 Effect，返回 false。
        if (!MonoShaderManager.TryReload(name, bytecode))
            return new Result(name, false, "new Effect(...) 失败——字节码不是完整效果（fx_2_0）？");

        // 同一个 Effect 实例被两边的注册表共享，所以换完自己那份必须同步过去，
        // 否则用 Luminance 那套 API 渲染的模组（FPM 就是）会一直看到旧效果。
        SyncLuminanceRegistry(name, shader.Effect);

        string persisted = sourceRoot is not null ? PersistFxc(source, sourceRoot, bytecode) : "；源来自包内，未写回 .fxc";
        return new Result(name, true, $"已重载 {bytecode.Length} 字节{persisted}");
    }

    /// <summary>
    /// 把源文件规范化成 <c>fxc</c> 能吃的形式，返回应当交给编译器的路径。UTF-8 BOM 去掉即可；
    /// UTF-16 要解码后按无 BOM 的 UTF-8 重写（只去 BOM 不够，NUL 字节照样非法）；没有 BOM 就原样返回。
    /// <para>
    /// 需要改写时只落一个临时文件，不碰原文件。带 BOM 的文件会被 <c>fxc</c> 在 <c>(1,1)</c> 报
    /// <c>X3000: Illegal character in shader file</c>，报错位置在最开头，很容易被误读成语法错。
    /// </para>
    /// </summary>
    /// <param name="source">原始 <c>.fx</c> 路径。</param>
    private static string NormalizeSource(string source)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(source);
        }
        catch
        {
            // 读不出来就交给编译器去报错，那里的信息更贴近问题。
            return source;
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return WriteNormalized(source, bytes[3..]);

        if (bytes.Length >= 2 && ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF)))
        {
            // ReadAllText 会按 BOM 自动判定编码。
            return WriteNormalized(source, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(File.ReadAllText(source)));
        }

        return source;
    }

    /// <summary>把规范化后的内容写到临时文件并返回该路径。</summary>
    private static string WriteNormalized(string source, byte[] content)
    {
        string temp = Path.Combine(Path.GetTempPath(), "MonochromeShaderReload", "normalized", Path.GetFileName(source));
        Directory.CreateDirectory(Path.GetDirectoryName(temp)!);
        File.WriteAllBytes(temp, content);
        return temp;
    }

    /// <summary>
    /// 把新字节码写回源目录的 <c>.fxc</c>。先写 <c>.tmp</c> 再 <c>Move</c>，避免留下半个文件。
    /// <para>
    /// <b>只写进 <paramref name="sourceRoot"/> 之内</b>：目标解析后若不在该模组源目录里就直接拒绝。
    /// 这是"热重载不碰无关模组文件"的显式围栏，不依赖调用链上恰好只传进合法路径。
    /// </para>
    /// <para>写回成功后顺手清掉同名的 <c>.xnb</c>，见 <see cref="RemoveStaleXnb"/>。</para>
    /// </summary>
    private static string PersistFxc(string source, string sourceRoot, byte[] bytecode)
    {
        try
        {
            string target = Path.ChangeExtension(source, ".fxc");
            string fullRoot = Path.GetFullPath(sourceRoot) + Path.DirectorySeparatorChar;
            string fullTarget = Path.GetFullPath(target);
            if (!fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                return $"；目标 {fullTarget} 不在模组源目录内，已拒绝写回（本次重载仍然生效）";

            string temp = target + ".tmp";
            File.WriteAllBytes(temp, bytecode);
            File.Move(temp, target, overwrite: true);
            return $"；已写回 {Path.GetFileName(target)}{RemoveStaleXnb(source, fullRoot)}";
        }
        catch (Exception ex)
        {
            return $"；但写回 .fxc 失败：{ex.Message}（本次重载仍然生效）";
        }
    }

    /// <summary>
    /// 删掉刚写回的 <c>.fxc</c> 旁边的同名 <c>.xnb</c>。
    /// <para>
    /// <b>为什么这两份东西不该共存</b>：tML 不编译 <c>.fx</c>（<c>AssetInitializer</c> 只注册了
    /// <c>.fxc</c> 的读取器），所以源目录里的 <c>.xnb</c> 只可能来自旧的 Content Pipeline /
    /// ModdersToolkitFXBuilder。它是同一个着色器的另一份产物，内容却和刚编出来的 <c>.fxc</c> 无关了。
    /// 注册表虽然以 <c>.fxc</c> 优先（见 <see cref="MonoShaderManager.LoadForMod"/> 的两趟扫描），
    /// 但留着两份"同名不同内容"的产物迟早会咬人——Luminance 自己的
    /// <c>ProcessCompiledFile</c> 也是直接删掉 <c>.xnb</c>，这里跟它保持一致。
    /// </para>
    /// <para>和写回一样受 <paramref name="fullRoot"/> 的前缀围栏约束；失败只报告，不影响本次重载。</para>
    /// </summary>
    private static string RemoveStaleXnb(string source, string fullRoot)
    {
        try
        {
            string fullXnb = Path.GetFullPath(Path.ChangeExtension(source, ".xnb"));
            if (!fullXnb.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullXnb))
                return string.Empty;
            File.Delete(fullXnb);
            return $"；已删除过期的 {Path.GetFileName(fullXnb)}";
        }
        catch (Exception ex)
        {
            return $"；但删除同名 .xnb 失败：{ex.Message}";
        }
    }

    /// <summary>把 <c>.fx</c> 编译到 <paramref name="output"/>。失败时 <paramref name="error"/> 是编译器输出。</summary>
    private static bool TryCompile(string compiler, string source, string output, out string error)
    {
        error = string.Empty;
        if (File.Exists(output))
        {
            try
            {
                File.Delete(output);
            }
            catch
            {
                // 删不掉就让编译器覆盖；下面的 ExitCode/存在性检查仍然能判断结果。
            }
        }

        try
        {
            ProcessStartInfo info = new(compiler)
            {
                Arguments = $"/nologo /T fx_2_0 /Fo \"{output}\" \"{source}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // fxc 从自己所在目录找 d3dcompiler_47.dll，所以工作目录必须是它那里。
                WorkingDirectory = Path.GetDirectoryName(compiler) ?? Path.GetTempPath(),
            };
            using Process? process = Process.Start(info);
            if (process is null)
            {
                error = "无法启动编译器进程";
                return false;
            }

            // 输出量很小（一次编译最多几百字节），同步读完再等退出不会撑满管道。
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(CompileTimeoutMs))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // 已经退出就无所谓。
                }
                error = $"编译器超时（{CompileTimeoutMs / 1000} 秒）";
                return false;
            }

            if (process.ExitCode != 0 || !File.Exists(output))
            {
                string detail = (stderr + "\n" + stdout).Trim();
                error = detail.Length > 0 ? detail : $"编译器退出码 {process.ExitCode} 且没有产出文件";
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            error = $"无法运行编译器：{ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// 找出这个着色器对应的 <c>.fx</c>。
    /// <b>源目录优先</b>——那才是开发者刚改过的文件；包内的可能是上一次构建时的版本。
    /// </summary>
    private static bool TryFindSource(MonoShader shader, out string source, out string? sourceRoot, out string reason)
    {
        source = string.Empty;
        sourceRoot = null;
        reason = string.Empty;

        string assetPath = shader.AssetPath;
        if (string.IsNullOrEmpty(assetPath))
        {
            reason = $"'{shader.Name}' 是用 SetShader 手工装进来的，没有源文件可重载";
            return false;
        }

        int slash = assetPath.IndexOf('/');
        if (slash <= 0 || slash == assetPath.Length - 1)
        {
            reason = $"'{shader.Name}' 的资产路径不正常：{assetPath}";
            return false;
        }

        string modName = assetPath[..slash];
        string relative = assetPath[(slash + 1)..] + ".fx";

        // 1) 源目录。
        if (ModLoader.TryGetMod(modName, out Mod? owner) && owner is not null && !string.IsNullOrEmpty(owner.SourceFolder))
        {
            string candidate = Path.Combine(owner.SourceFolder, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                source = candidate;
                sourceRoot = owner.SourceFolder;
                return true;
            }
        }

        // 2) 包内：取出来写到临时目录再编。
        if (ModLoader.TryGetMod(modName, out Mod? packed) && packed is not null)
        {
            string? entry = FindEntry(packed, relative);
            if (entry is not null)
            {
                byte[]? bytes = packed.GetFileBytes(entry);
                if (bytes is not null && bytes.Length > 0)
                {
                    try
                    {
                        string temp = Path.Combine(Path.GetTempPath(), "MonochromeShaderReload", Path.GetFileName(relative));
                        Directory.CreateDirectory(Path.GetDirectoryName(temp)!);
                        File.WriteAllBytes(temp, bytes);
                        source = temp;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        reason = $"从包内取出 {relative} 失败：{ex.Message}";
                        return false;
                    }
                }
            }
        }

        reason = $"找不到 '{relative}'（源目录与包内都没有）。确认它在 {modName} 的 Assets/Effects 下，且没有被打包规则排除";
        return false;
    }

    /// <summary>
    /// 在模组文件表里按"统一成正斜杠、忽略大小写"找到真实条目名。
    /// <b>不能直接拿反斜杠路径去 GetFileBytes</b>——条目名用的是归档时的分隔符。
    /// <b>也注意 <c>GetFileNames()</c> 对没有 .tmod 的模组返回 null</b>（见 §5.5）。
    /// </summary>
    private static string? FindEntry(Mod mod, string desired)
    {
        List<string>? files = mod.GetFileNames();
        if (files is null)
            return null;
        foreach (string file in files)
        {
            if (file.Replace('\\', '/').Equals(desired, StringComparison.OrdinalIgnoreCase))
                return file;
        }
        return null;
    }

    /// <summary>
    /// 拿到可用的 <c>fxc.exe</c>：优先用已经释放好的，其次环境变量指定的，
    /// 最后从包内的 <c>Assets/AutoloadedEffects/Compiler</c> 释放到临时目录。
    /// </summary>
    private static string? EnsureCompiler(out string reason)
    {
        reason = string.Empty;
        if (compilerDirectory is not null)
        {
            string cached = Path.Combine(compilerDirectory, CompilerFileName);
            if (File.Exists(cached))
                return cached;
        }

        string? fromEnvironment = Environment.GetEnvironmentVariable(CompilerEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment) && File.Exists(fromEnvironment))
        {
            compilerDirectory = Path.GetDirectoryName(fromEnvironment);
            return fromEnvironment;
        }

        Mod? mod = Monochrome.Instance;
        if (mod is null)
        {
            reason = "Monochrome 实例还没就绪，拿不到包内编译器";
            return null;
        }

        string targetDirectory = Path.Combine(Path.GetTempPath(), "MonochromeShaderCompiler");
        try
        {
            Directory.CreateDirectory(targetDirectory);
            foreach (string fileName in new[] { CompilerFileName, CompilerLibraryName })
            {
                string desired = CompilerFolder + "/" + fileName;
                string? entry = FindEntry(mod, desired);
                if (entry is null)
                {
                    reason = $"包内没有 {desired}";
                    return null;
                }

                byte[]? bytes = mod.GetFileBytes(entry);
                if (bytes is null || bytes.Length == 0)
                {
                    reason = $"{desired} 内容为空";
                    return null;
                }

                string destination = Path.Combine(targetDirectory, fileName);
                // 已经释放过且大小一致就跳过，省掉几 MB 的写入。
                if (File.Exists(destination) && new FileInfo(destination).Length == bytes.Length)
                    continue;
                File.WriteAllBytes(destination, bytes);
            }
        }
        catch (Exception ex)
        {
            reason = $"释放编译器失败：{ex.Message}";
            return null;
        }

        compilerDirectory = targetDirectory;
        return Path.Combine(targetDirectory, CompilerFileName);
    }

    /// <summary>Luminance 的模组名（Monochrome 对它是 <c>weakReferences</c>，可能不在场）。</summary>
    public const string LuminanceModName = "Luminance";

    /// <summary>重载后是否把新 <see cref="Effect"/> 同步给 Luminance 的注册表（仅当 Luminance 在场且已注册同名项）。默认 <see langword="true"/>。</summary>
    public static bool SyncLuminance { get; set; } = true;

    /// <summary>是否停用 Luminance 自己的"文件变动自动重编译"。默认 <see langword="true"/>，只保留 <c>/mono shader reload</c>。</summary>
    public static bool SuppressLuminanceAutoReload { get; set; } = true;

    private static bool warnedLuminance;
    private static int watcherSweepCountdown;
    private static bool reportedWatcherSweep;

    /// <summary>
    /// 把刚重载出来的 <see cref="Effect"/> 同步给 Luminance 的注册表。
    /// <para>
    /// 同一个资产名在两边的注册表里是同一个 <see cref="Effect"/> 实例（都走
    /// <c>mod.Assets.Request&lt;Effect&gt;(...)</c>，被 <c>AssetRepository</c> 缓存成同一对象），
    /// 所以 Monochrome 换掉自己那份之后，Luminance 的 <c>ManagedShader</c> 还指着旧的。
    /// 只同步"它已经注册过的名字"——<c>SetShader</c> 在名字不存在时会新建条目，那等于改变对方的行为。
    /// </para>
    /// </summary>
    private static void SyncLuminanceRegistry(string name, Effect effect)
    {
        if (!SyncLuminance || !ModLoader.HasMod(LuminanceModName))
            return;
        try
        {
            if (Luminance.Core.Graphics.ShaderManager.TryGetShader(name, out _))
                Luminance.Core.Graphics.ShaderManager.SetShader(name, new Ref<Effect>(effect));
            if (Luminance.Core.Graphics.ShaderManager.TryGetFilter(name, out _))
                Luminance.Core.Graphics.ShaderManager.SetFilter(name, new Ref<Effect>(effect));
        }
        catch (Exception ex)
        {
            WarnLuminanceOnce($"同步 Luminance 注册表失败（{name}）：{ex.Message}");
        }
    }

    /// <summary>
    /// 停用 Luminance 自己的"文件变动自动重编译"（它用 <c>FileSystemWatcher</c> 盯着各模组的着色器目录），
    /// 只留 <c>/mono shader reload</c> 这条主动路径，避免两套机制争夺"谁写回 <c>.fxc</c>"。
    /// <para>
    /// 只摘 watcher，不动它的编译队列：Luminance 还会把"有 <c>.fx</c> 但没有产物"的文件排队做首次编译，
    /// 那件事与文件变动无关，保留有益。监视器没有公开开关，只能走反射，所以这里失败属于正常情况
    /// （Luminance 改了内部结构），只记一行日志。
    /// </para>
    /// </summary>
    public static void TrySuppressLuminanceAutoReload()
    {
        if (!SuppressLuminanceAutoReload || Main.dedServ || !ModLoader.HasMod(LuminanceModName))
            return;
        // 每秒扫一次就够。watcher 由 Luminance 在它自己的 PostSetupContent 里建，而两个模组的先后
        // 依赖 sortAfter——重复扫比"赌一次调用正好赶上"更稳。
        if (--watcherSweepCountdown > 0)
            return;
        watcherSweepCountdown = 60;
        SweepLuminanceWatchers();
    }

    /// <summary>
    /// 真正去停 watcher，只把 <c>EnableRaisingEvents</c> 置 false：摘下列表项会连带杀掉 Luminance 的
    /// 首次编译（它的 <c>CompilingFiles</c> 队列只在遍历 <c>ShaderWatchers</c> 时被消费），
    /// 而 <c>Dispose()</c> 之后对方再读 watcher 的属性会抛 <c>ObjectDisposedException</c>。
    /// <para>
    /// 用 <c>typeof</c> 而不是按程序集名解析字符串：Mod 程序集在各自的 <c>AssemblyLoadContext</c> 里，
    /// 字符串解析容易静默失败（那正是先前"监视器还在"的根因），而 Monochrome 对 Luminance 有编译期引用。
    /// </para>
    /// </summary>
    private static void SweepLuminanceWatchers()
    {
        try
        {
            if (!ResolveLuminanceMembers())
                return;

            // 成员句柄已经缓存，这里每次只做两次 GetValue。
            object? container = watchersProperty is not null ? watchersProperty.GetValue(null) : watchersField?.GetValue(null);
            if (container is not System.Collections.IList watchers)
                return;

            int stopped = 0;
            foreach (object? watcher in watchers)
            {
                if (watcher is null || fileWatcherProperty!.GetValue(watcher) is not FileSystemWatcher fs)
                    continue;
                if (!fs.EnableRaisingEvents)
                    continue;
                fs.EnableRaisingEvents = false;
                stopped++;
            }

            if (stopped > 0 && !reportedWatcherSweep)
            {
                reportedWatcherSweep = true;
                Monochrome.Instance?.Logger.Info(
                    $"已停用 Luminance 的 {stopped} 个着色器文件监视器，只保留 /mono shader reload；Luminance 的首次编译队列不受影响。");
            }
        }
        catch (Exception ex)
        {
            WarnLuminanceOnce($"停用 Luminance 文件监视器失败（不影响其它功能）：{ex.Message}");
        }
    }

    // Luminance 成员的反射句柄：解析一次后缓存。见 ResolveLuminanceMembers 的说明。
    private static bool resolvedLuminanceMembers;
    private static PropertyInfo? watchersProperty;
    private static FieldInfo? watchersField;
    private static PropertyInfo? fileWatcherProperty;

    /// <summary>
    /// 解析一次 Luminance 的成员并缓存下来。sweep 每秒跑一次，带 <c>BindingFlags</c> 的查找要走元数据，
    /// 每次现查纯属白花；把"解析失败"也缓存下来则能避免每秒重复查与刷日志。
    /// <para>
    /// 注意 <c>ShaderWatchers</c> 是<b>属性</b>而不是字段
    /// （<c>ShaderRecompilationMonitor.cs:31-35</c>），所以这里属性优先、字段作为防御分支。
    /// </para>
    /// </summary>
    private static bool ResolveLuminanceMembers()
    {
        if (resolvedLuminanceMembers)
            return watchersProperty is not null || watchersField is not null;
        resolvedLuminanceMembers = true;

        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        Type monitor = typeof(Luminance.Core.Graphics.ShaderRecompilationMonitor);

        watchersProperty = monitor.GetProperty("ShaderWatchers", flags);
        if (watchersProperty is null)
            watchersField = monitor.GetField("ShaderWatchers", flags);
        if (watchersProperty is null && watchersField is null)
        {
            WarnLuminanceOnce("Luminance 的 ShaderWatchers 成员解析不到（版本可能已变），无法停用其文件监视器。");
            return false;
        }

        // ShaderWatcher 是嵌套 record，FileWatcher 是位置参数自动生成的属性。
        Type? watcherType = monitor.GetNestedType("ShaderWatcher", BindingFlags.Public | BindingFlags.NonPublic);
        fileWatcherProperty = watcherType?.GetProperty("FileWatcher", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fileWatcherProperty is null)
        {
            WarnLuminanceOnce("Luminance 的 ShaderWatcher.FileWatcher 解析不到，无法停用其文件监视器。");
            return false;
        }
        return true;
    }

    /// <summary>与 Luminance 协作相关的失败只提醒一次，避免刷屏。</summary>
    private static void WarnLuminanceOnce(string message)
    {
        if (warnedLuminance)
            return;
        warnedLuminance = true;
        Monochrome.Instance?.Logger.Warn(message);
    }

    /// <summary>把结果表整理成给命令行回复的一行行文本。</summary>
    /// <param name="results">结果表。</param>
    public static string Describe(IReadOnlyList<Result> results)
    {
        if (results.Count == 0)
            return "没有可重载的着色器。";
        StringBuilder output = new();
        int ok = 0;
        foreach (Result result in results)
        {
            if (result.Success)
                ok++;
            output.Append(result.Success ? "[成功] " : "[失败] ").Append(result.Name).Append('：').Append(result.Message).Append('\n');
        }
        output.Append($"共 {results.Count} 个，成功 {ok} 个。");
        return output.ToString();
    }
}
