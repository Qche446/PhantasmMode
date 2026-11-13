using ReLogic.Content;

namespace Monochrome.Core.Graphics.Shaders;

/// <summary>
/// 消费者持有的 Shader 集合。只扫描显式指定的模组和目录，由消费者筛选资源。
/// 不同集合拥有独立克隆，不修改源模组或 Luminance 注册表；卸载时由消费者 Dispose。
/// </summary>
public sealed class MonoShaderLibrary : IDisposable
{
    private readonly Dictionary<string, MonoShader> shaders = new(StringComparer.Ordinal);
    /// <summary>已导入效果，键为「模组名.文件名」。</summary>
    public IReadOnlyDictionary<string, MonoShader> Shaders => shaders;

    /// <summary>
    /// 扫描指定目录及子目录的 .fxc/.xnb（优先 .fxc）。include 接收无扩展名的模组内相对路径。
    /// 调用方须确保该目录的 .xnb 是 Effect。默认克隆，重复导入同一路径复用包装。
    /// </summary>
    public int Import(Mod source, Predicate<string>? include = null, bool clone = true,
        string directory = "Assets/AutoloadedEffects/Shaders")
    {
        ArgumentNullException.ThrowIfNull(source);
        if (Main.dedServ)
            return 0;
        string prefix = directory.Replace('\\', '/').Trim('/') + "/";
        List<string>? files = source.GetFileNames();
        if (files is null)
            return 0;
        int previousCount = shaders.Count;
        foreach (string extension in new[] { ".fxc", ".xnb" })
        {
            foreach (string file in files)
            {
                string path = file.Replace('\\', '/');
                if (!path.StartsWith(prefix, StringComparison.Ordinal)
                    || !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    continue;
                string relative = path[..^extension.Length];
                if (include is not null && !include(relative))
                    continue;
                string key = source.Name + "." + Path.GetFileName(relative);
                string assetPath = source.Name + "/" + relative;
                if (shaders.TryGetValue(key, out MonoShader? existing)
                    && existing.AssetPath == assetPath && existing.Resource.IsIsolated == clone)
                    continue;
                TryLoad(key, assetPath, out _, clone);
            }
        }
        return shaders.Count - previousCount;
    }

    /// <summary>导入完整资产路径（无扩展名）。加载失败记录警告并返回 false；重复导入返回已有对象。</summary>
    public bool TryLoad(string name, string assetPath, out MonoShader? shader, bool clone = true)
    {
        shader = null;
        if (Main.dedServ)
            return false;
        if (shaders.TryGetValue(name, out shader))
        {
            if (shader.AssetPath != assetPath || shader.Resource.IsIsolated != clone)
                throw new InvalidOperationException($"Shader alias '{name}' already refers to a different resource or instance mode.");
            return true;
        }
        try
        {
            Asset<Effect> asset = ModContent.Request<Effect>(assetPath, AssetRequestMode.ImmediateLoad);
            shader = new MonoShader(name, new MonoEffectResource(asset, clone), assetPath);
            shaders.Add(name, shader);
            return true;
        }
        catch (Exception ex)
        {
            Monochrome.Instance?.Logger.Warn($"无法导入着色器 '{assetPath}'：{ex.Message}");
            return false;
        }
    }

    /// <summary>安全查找已经导入的着色器。</summary>
    public bool TryGet(string name, out MonoShader? shader) => shaders.TryGetValue(name, out shader);
    /// <summary>获取已导入效果；未导入时抛出异常。</summary>
    public MonoShader GetShader(string name) => shaders.TryGetValue(name, out MonoShader? shader)
        ? shader : throw new KeyNotFoundException($"Shader '{name}' was not imported by this consumer.");
    /// <summary>释放本集合的包装和克隆，不影响源资产；清空后可以重新导入。</summary>
    public void Dispose()
    {
        foreach (MonoShader shader in shaders.Values)
            shader.Dispose();
        shaders.Clear();
    }
}
