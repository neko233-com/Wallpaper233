namespace Wallpaper233.Core;

public enum EngineMode
{
    Stopped,
    Preview,
    Wallpaper,
    SafeMode,
}

public enum RenderBackend
{
    Direct3D12,
    SafeMode,
}

public readonly record struct FrameTime(TimeSpan Delta, TimeSpan Runtime)
{
    public static FrameTime FromDelta(TimeSpan delta, TimeSpan runtime) => new(delta, runtime);
}

public sealed record EngineSnapshot(
    EngineMode Mode,
    RenderBackend Backend,
    TimeSpan Runtime,
    long FrameCount,
    bool IsHealthy)
{
    public static EngineSnapshot Initial { get; } = new(
        EngineMode.Stopped,
        RenderBackend.SafeMode,
        TimeSpan.Zero,
        0,
        true);
}

/// <summary>
/// Owns lifecycle state only. GPU resources belong to a renderer implementation,
/// so the core model stays independent from Direct3D and easy to test.
/// </summary>
public sealed class WallpaperRuntime : IDisposable
{
    private bool _disposed;
    private EngineSnapshot _snapshot = EngineSnapshot.Initial;

    public EngineSnapshot Snapshot
    {
        get
        {
            ThrowIfDisposed();
            return _snapshot;
        }
    }

    public void Start(EngineMode mode, RenderBackend backend = RenderBackend.Direct3D12)
    {
        ThrowIfDisposed();

        if (mode == EngineMode.Stopped)
        {
            throw new ArgumentException("Runtime cannot start in Stopped mode.", nameof(mode));
        }

        _snapshot = _snapshot with
        {
            Mode = mode,
            Backend = backend,
            IsHealthy = true,
        };
    }

    public void Tick(FrameTime frame)
    {
        ThrowIfDisposed();

        if (_snapshot.Mode == EngineMode.Stopped)
        {
            return;
        }

        if (frame.Delta < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), "Frame delta cannot be negative.");
        }

        _snapshot = _snapshot with
        {
            Runtime = frame.Runtime,
            FrameCount = checked(_snapshot.FrameCount + 1),
        };
    }

    public void EnterSafeMode()
    {
        ThrowIfDisposed();
        _snapshot = _snapshot with
        {
            Mode = EngineMode.SafeMode,
            Backend = RenderBackend.SafeMode,
            IsHealthy = false,
        };
    }

    public void Stop()
    {
        ThrowIfDisposed();
        _snapshot = _snapshot with { Mode = EngineMode.Stopped };
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
