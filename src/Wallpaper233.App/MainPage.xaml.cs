using Microsoft.UI.Xaml.Controls;
using Wallpaper233.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wallpaper233.App;

/// <summary>
/// The main content page displayed inside the application window.
/// Add your UI logic, event handlers, and data binding here.
/// </summary>
public sealed partial class MainPage : Page, IDisposable
{
    private readonly WallpaperRuntime _runtime = new();
    private bool _disposed;

    public MainPage()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
        UpdateRuntimeStatus();
    }

    private void StartPreviewClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _runtime.Start(EngineMode.Preview, RenderBackend.SafeMode);
        _runtime.Tick(FrameTime.FromDelta(TimeSpan.Zero, TimeSpan.Zero));
        UpdateRuntimeStatus();
    }

    private void UpdateRuntimeStatus()
    {
        var snapshot = _runtime.Snapshot;
        RuntimeStatusText.Text = $"模式：{snapshot.Mode}\n后端：{snapshot.Backend}\n状态：{(snapshot.IsHealthy ? "正常" : "安全模式")}";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Unloaded -= OnUnloaded;
        _runtime.Dispose();
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Dispose();
    }
}
