namespace Monochrome.Core.Graphics.Shaders;

// Ownership and identity tracking can be verified without a graphics device.
internal sealed class MonoAssetInstance<T>(Func<T> source, Func<T, T>? clone = null, Action<T>? release = null) : IDisposable
    where T : class, IDisposable
{
    private T? previousSource;
    private T? instance;
    public int Version { get; private set; }
    public bool IsDisposed { get; private set; }

    public T Resolve()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        T current = source();
        if (instance is not null && ReferenceEquals(current, previousSource))
            return instance;
        // Clone first: failure leaves the previous instance intact and allows retrying.
        T replacement = clone is null ? current : clone(current);
        T? old = instance;
        instance = replacement;
        previousSource = current;
        Version++;
        if (clone is not null)
            Release(old);
        return replacement;
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;
        if (clone is not null)
            Release(instance);
        instance = previousSource = null;
    }

    private void Release(T? value)
    {
        if (value is null) return;
        if (release is not null) release(value);
        else value.Dispose();
    }
}
