using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.Core.Graphics.PostProcessing;

/// <summary>
/// 内置屏幕特效的<b>名字表与工厂</b>：让"有哪些现成的效果"变成一件可以枚举、可以按名字取用的事。
/// <para>
/// 存在的理由有两个：诊断命令需要一个统一入口（<c>/mono capture fx &lt;名字&gt; on</c>），
/// 而消费者也不该被迫记住每个效果的类名与默认参数。用名字拿到的实例参数都是<b>保守默认值</b>，
/// 想调风格请拿回去改属性。
/// </para>
/// <para>
/// <b>没有"注册"这一步</b>：这些类本身就在程序集里，名字表也只是把它们的构造收在一处。
/// 着色器资产缺失时 <see cref="MonoShaderPass"/> 会安静跳过，所以名字表里出现一个没有资产的条目
/// 不会导致任何异常——只会什么都不画。
/// </para>
/// </summary>
public static class MonoScreenFx
{
    /// <summary>内置效果的名字（全部小写，不区分大小写）。</summary>
    public static readonly string[] Names =
    [
        "blur",
        "colorgrade",
        "chromatic",
        "scanline",
        "grain",
        "vignette",
    ];

    /// <summary>按名字造一个效果实例，参数是保守默认值。名字不认识时返回 <see langword="null"/>。</summary>
    /// <param name="name">效果名，不区分大小写；见 <see cref="Names"/>。</param>
    /// <param name="extra">模糊链的第二个 pass；其它效果恒为 <see langword="null"/>。</param>
    /// <returns>
    /// 第一个 pass；<c>blur</c> 会返回<b>横向那一个</b>，纵向那个在 <paramref name="extra"/> 里
    /// （模糊必须成对，见 <see cref="MonoBlurPass.CreateChain"/>）。
    /// </returns>
    public static MonoShaderPass? Create(string name, out MonoShaderPass? extra)
    {
        extra = null;
        switch (name?.Trim().ToLowerInvariant())
        {
            case "blur":
                (MonoBlurPass horizontal, MonoBlurPass vertical) = MonoBlurPass.CreateChain();
                extra = vertical;
                return horizontal;
            case "colorgrade": return new MonoColorGradePass();
            case "chromatic": return new MonoChromaticAberrationPass();
            case "scanline": return new MonoScanlinePass();
            case "grain": return new MonoGrainPass();
            case "vignette": return new MonoVignettePass();
            default: return null;
        }
    }

    /// <summary>按名字造一个效果实例（忽略 <c>blur</c> 的第二个 pass）。</summary>
    /// <param name="name">效果名。</param>
    public static MonoShaderPass? Create(string name) => Create(name, out _);

    /// <summary>
    /// 把一个具名效果加进链里。名字不认识时返回 <see langword="false"/>。
    /// <para><c>blur</c> 会加两个 pass（<paramref name="order"/> 与 <paramref name="order"/> + 1），
    /// 因此给它留出至少两个连续序号。</para>
    /// </summary>
    /// <param name="pipeline">要加入的链。</param>
    /// <param name="name">效果名。</param>
    /// <param name="order">执行顺序。</param>
    /// <param name="created">造出来的 pass（模糊是第一个）。</param>
    public static bool AddTo(MonoPostFxPipeline pipeline, string name, int order, out MonoShaderPass? created)
    {
        created = null;
        if (pipeline is null)
            return false;

        MonoShaderPass? first = Create(name, out MonoShaderPass? extra);
        if (first is null)
            return false;

        // 每个效果类都有自己推荐的默认 order，但具名入口要允许调用方覆盖它。
        // 必须在 Add **之前**改：链只在加入时按 Order 排序（见 MonoShaderPass.Order）。
        first.Order = order;
        created = first;
        pipeline.Add(first);
        if (extra is not null)
        {
            extra.Order = order + 1;
            pipeline.Add(extra);
        }
        return true;
    }

    /// <summary>描述名字表与每个效果的着色器是否已经可用，供诊断命令输出。</summary>
    public static string Describe()
    {
        System.Text.StringBuilder output = new();
        output.Append("内置屏幕特效：");
        foreach (string name in Names)
        {
            MonoShaderPass? pass = Create(name);
            bool ready = pass is not null && pass.IsAvailable;
            output.Append('\n')
                  .Append("  ").Append(name.PadRight(12))
                  .Append(ready ? "着色器已就绪  " : "着色器未注册  ")
                  .Append(pass?.ShaderName ?? "(无)");
        }
        return output.ToString();
    }
}
