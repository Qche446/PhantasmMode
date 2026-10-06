namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>按名字保存后处理链的注册表，供内容模组之间共享同一条链。</summary>
public sealed class MonoPostFxSystem : ModSystem
{
    private static Dictionary<string, MonoPostFxPipeline>? pipelines;

    /// <summary>
    /// 取一条具名的后处理链，不存在则创建（名字不区分大小写）。
    /// <para>用名字而不是全局单例，是为了让多个模组各自拥有独立的链而互不干扰。</para>
    /// </summary>
    /// <param name="name">链名。</param>
    public static MonoPostFxPipeline GetOrCreate(string name)
    {
        pipelines ??= [with(StringComparer.OrdinalIgnoreCase)];
        if (!pipelines.TryGetValue(name, out MonoPostFxPipeline? pipeline))
            pipelines[name] = pipeline = new();
        return pipeline;
    }

    /// <inheritdoc/>
    public override void Unload() => pipelines = null;
}
