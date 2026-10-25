using Monochrome.Core.Graphics.Shaders;

namespace FargosPhantasmMode.Common;

public static class PhanShader
{
    private const string BlobTrailName = "FargowiltasSouls.BlobTrail";
    /// <summary>
    /// 常规FSS的拖尾shader
    /// </summary>
    public static MonoShader BlobTrail => MonoShaderManager.TryGet(BlobTrailName, out MonoShader shader) ? shader : null;
    private const string MonoTrailName = "Monochrome.StreakTrail";
    /// <summary>
    /// monochrome的拖尾shader，若要和FSS的那个宽度相同，widthfunction大小得减半
    /// </summary>
    public static MonoShader StreakTrail => MonoShaderManager.TryGet(MonoTrailName, out MonoShader shader) ? shader : null;
}
