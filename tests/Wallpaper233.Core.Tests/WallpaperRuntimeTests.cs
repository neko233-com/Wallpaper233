using Wallpaper233.Core;

namespace Wallpaper233.Core.Tests;

public sealed class WallpaperRuntimeTests
{
    [Fact]
    public void NewRuntimeStartsStoppedAndHealthy()
    {
        using var runtime = new WallpaperRuntime();

        Assert.Equal(EngineMode.Stopped, runtime.Snapshot.Mode);
        Assert.Equal(RenderBackend.SafeMode, runtime.Snapshot.Backend);
        Assert.True(runtime.Snapshot.IsHealthy);
    }

    [Fact]
    public void TickAdvancesOnlyAfterStart()
    {
        using var runtime = new WallpaperRuntime();

        runtime.Tick(FrameTime.FromDelta(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16)));
        Assert.Equal(0, runtime.Snapshot.FrameCount);

        runtime.Start(EngineMode.Preview, RenderBackend.SafeMode);
        runtime.Tick(FrameTime.FromDelta(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16)));

        Assert.Equal(1, runtime.Snapshot.FrameCount);
        Assert.Equal(TimeSpan.FromMilliseconds(16), runtime.Snapshot.Runtime);
    }

    [Fact]
    public void NegativeFrameDeltaIsRejected()
    {
        using var runtime = new WallpaperRuntime();
        runtime.Start(EngineMode.Preview);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            runtime.Tick(FrameTime.FromDelta(TimeSpan.FromMilliseconds(-1), TimeSpan.Zero)));
    }

    [Fact]
    public void SafeModeDisablesHealthyFlag()
    {
        using var runtime = new WallpaperRuntime();
        runtime.Start(EngineMode.Wallpaper);
        runtime.EnterSafeMode();

        Assert.Equal(EngineMode.SafeMode, runtime.Snapshot.Mode);
        Assert.Equal(RenderBackend.SafeMode, runtime.Snapshot.Backend);
        Assert.False(runtime.Snapshot.IsHealthy);
    }
}
