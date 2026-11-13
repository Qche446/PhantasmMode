using Monochrome.Core.Graphics.Shaders;

namespace Monochrome.CoreTests;

// Tests use fake resources, not uninitialized Effect/GraphicsDevice objects.
internal static class ShaderResourceChecks
{
    private sealed class Resource : IDisposable
    {
        public int Disposals;
        public void Dispose() => Disposals++;
    }

    internal static void ReusesClone()
    {
        Resource source = new();
        int clones = 0;
        using MonoAssetInstance<Resource> slot = new(() => source, _ => { clones++; return new(); });
        Resource first = slot.Resolve();
        if (ReferenceEquals(first, source) || !ReferenceEquals(first, slot.Resolve()) || clones != 1)
            throw new Exception("Clone must be distinct and reused until source changes.");
    }

    internal static void ReplacesClone()
    {
        Resource source = new(), original = source;
        using MonoAssetInstance<Resource> slot = new(() => source, _ => new());
        Resource old = slot.Resolve();
        source = new();
        Resource replacement = slot.Resolve();
        if (ReferenceEquals(old, replacement) || old.Disposals != 1 || slot.Version != 2
            || original.Disposals != 0 || source.Disposals != 0)
            throw new Exception("Replacing source must dispose only the old owned clone.");
    }

    internal static void FailedCloneCanRetry()
    {
        Resource source = new();
        bool fail = false;
        using MonoAssetInstance<Resource> slot = new(() => source,
            _ => fail ? throw new InvalidOperationException("simulated clone failure") : new());
        Resource old = slot.Resolve();
        source = new();
        fail = true;
        try { slot.Resolve(); throw new Exception("Expected clone failure."); }
        catch (InvalidOperationException) { }
        if (old.Disposals != 0 || slot.Version != 1)
            throw new Exception("Failed replacement must preserve the previous owned instance.");
        fail = false;
        slot.Resolve();
        if (old.Disposals != 1 || slot.Version != 2)
            throw new Exception("Successful retry must retire the old clone once.");
    }

    internal static void BorrowedSurvivesUnload()
    {
        Resource source = new(), original = source;
        MonoAssetInstance<Resource> slot = new(() => source);
        if (!ReferenceEquals(source, slot.Resolve())) throw new Exception("Expected borrowed source.");
        source = new();
        slot.Resolve();
        slot.Dispose();
        if (original.Disposals != 0 || source.Disposals != 0)
            throw new Exception("Borrowed resources must never be disposed by the wrapper.");
    }

    internal static void UnloadIsIdempotent()
    {
        Resource source = new();
        MonoAssetInstance<Resource> slot = new(() => source, _ => new());
        Resource clone = slot.Resolve();
        slot.Dispose();
        slot.Dispose();
        if (clone.Disposals != 1 || source.Disposals != 0)
            throw new Exception("Unload must dispose the owned clone exactly once.");
        try { slot.Resolve(); throw new Exception("Expected use-after-unload rejection."); }
        catch (ObjectDisposedException) { }
    }
}
