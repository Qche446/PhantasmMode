using Monochrome.Core.Graphics.Screen;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>按名字保存后处理链的注册表，供内容模组之间共享同一条链。</summary>
public sealed class MonoPostFxSystem : ModSystem
{
    private static Dictionary<string, MonoPostFxPipeline>? pipelines;
    private static Dictionary<string, MonoScreenFilter>? filters;
    private static Dictionary<string, MonoPostFxPipeline>? filterPipelines;

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

    /// <summary>
    /// 取一条绑在指定着色器上的逐帧滤镜，不存在则创建（名字不区分大小写）。
    /// <para>
    /// 它自带一条只含自己的链，并自动挂进 <see cref="MonoScreenCaptureSystem"/>，
    /// 所以消费者只要在需要的那一帧调一次 <see cref="MonoScreenFilter.Activate"/>，
    /// 时机与淡入淡出都由库负责。
    /// </para>
    /// </summary>
    /// <param name="shaderName">着色器的注册名（形如 <c>Monochrome.Vignette</c>）。</param>
    public static MonoScreenFilter GetFilter(string shaderName)
    {
        ArgumentException.ThrowIfNullOrEmpty(shaderName);

        filters ??= [with(StringComparer.OrdinalIgnoreCase)];
        if (filters.TryGetValue(shaderName, out MonoScreenFilter? existing))
            return existing;

        filterPipelines ??= [with(StringComparer.OrdinalIgnoreCase)];
        MonoPostFxPipeline pipeline = new();
        filterPipelines[shaderName] = pipeline;

        MonoScreenFilter created = new(shaderName, pipeline);
        filters[shaderName] = created;
        MonoScreenCaptureSystem.Register(pipeline, order: 0, label: shaderName);
        return created;
    }

    /// <summary>
    /// 推进全部逐帧滤镜。由 <see cref="MonoScreenCaptureSystem"/> 在每帧的
    /// <c>PreUpdateEntities</c> 里调用，消费者不需要自己调。
    /// </summary>
    internal static void TickFilters()
    {
        if (filters is null)
            return;

        foreach (MonoScreenFilter filter in filters.Values)
            filter.Tick();
    }

    /// <inheritdoc/>
    public override void Unload()
    {
        pipelines = null;
        filters = null;
        filterPipelines = null;
    }
}
