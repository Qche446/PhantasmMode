using Monochrome.Core.Graphics.Shaders;

namespace FargosPhantasmMode.Core.Systems;

/// <summary>FPM 自己决定导入哪些 Shader；外部资源只使用独立克隆。</summary>
internal static class PModeShaders
{
    internal static MonoShaderLibrary Library { get; } = new();
    private static readonly HashSet<string> ExternalShaders = new(StringComparer.Ordinal)
    {
        "BlobTrail", "DestroyerScanTelegraph", "GenericDeathray", "MutantP1Aura",
        "RetinazerDeathray", "SolarEnchantShader", "StyxGazerShader", "TwinsAuraShader",
        "WillBigDeathray", "WillDeathray", "WoFAuraShader", "WoFDeathray"
    };

    internal static void Initialize(Mod consumer)
    {
        Library.Dispose();
        if (Main.dedServ)
            return;
        Library.Import(consumer, clone: false, directory: "Assets/Effects");
        if (ModLoader.TryGetMod("FargowiltasSouls", out Mod source))
        {
            int count = Library.Import(source,
                static path => ExternalShaders.Contains(Path.GetFileName(path)), clone: true);
            consumer.Logger.Info($"FPM Shader：导入 {count} 个 FSS 资源，首次绘制时创建独立克隆。");
            foreach (string name in ExternalShaders)
                if (!Library.TryGet(source.Name + "." + name, out _))
                    consumer.Logger.Warn($"FSS Shader 未找到：{name}");
        }
    }

    internal static void Unload() => Library.Dispose();
}
