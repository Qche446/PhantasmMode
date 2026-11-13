namespace Monochrome.Core.Graphics.Shaders;

/// <summary>内置效果的兼容查询入口。外部模组持有 MonoShaderLibrary 并显式导入；编译和热重载交给 tml-build。</summary>
public sealed class MonoShaderManager : ModSystem
{
    private static readonly MonoShaderLibrary library = new();
    /// <summary>Monochrome 自己的效果。</summary>
    public static IReadOnlyDictionary<string, MonoShader> Shaders => library.Shaders;
    /// <summary>内置效果是否已加载。</summary>
    public static bool HasFinishedLoading { get; internal set; }
    /// <summary>保留加载后回调的兼容入口。</summary>
    public static readonly Queue<Action> PostShaderLoadActions = new();

    internal static void LoadBuiltIns(Mod mod)
    {
        library.Import(mod, clone: false, directory: "Assets/Effects");
        HasFinishedLoading = true;
        while (PostShaderLoadActions.TryDequeue(out Action? action))
            action();
    }
    /// <summary>安全查询内置效果。</summary>
    public static bool TryGet(string name, out MonoShader? shader) => library.TryGet(name, out shader);
    /// <summary>获取内置效果。</summary>
    public static MonoShader GetShader(string name) => library.GetShader(name);
    /// <summary>加载指定资源；外部资源默认使用克隆。</summary>
    public static bool TryLoad(string name, string assetPath, out MonoShader? shader, bool clone = true)
        => library.TryLoad(name, assetPath, out shader, clone);
    /// <summary>内置效果加载后执行。</summary>
    public static void QueueAfterLoad(Action action)
    {
        if (HasFinishedLoading) action();
        else PostShaderLoadActions.Enqueue(action);
    }
    /// <summary>释放包装；源资产由 tML 释放。</summary>
    public override void Unload()
    {
        library.Dispose();
        Trails.MonoTrail.ResetEffectBinding();
        HasFinishedLoading = false;
        PostShaderLoadActions.Clear();
    }
}
