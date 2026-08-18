using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wallpaper233.Community;
using Wallpaper233.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace Wallpaper233.App;

public sealed partial class MainPage : Page, IDisposable
{
    private readonly WallpaperRuntime _runtime = new();
    private readonly LibraryCatalog _library = new();
    private readonly WallpaperHostWindow _wallpaperHost = new();
    private bool _disposed;

    public MainPage()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
        RefreshLibrary();
    }

    private void NavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
        LibraryView.Visibility = tag == "library" ? Visibility.Visible : Visibility.Collapsed;
        CommunityView.Visibility = tag == "community" ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = tag == "settings" ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void ImportFilesClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.Thumbnail,
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
        };

        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".webp", ".avif", ".mp4", ".webm", ".mkv", ".mov" })
        {
            picker.FileTypeFilter.Add(extension);
        }

        if (App.MainWindow is null)
        {
            return;
        }

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
        var files = await picker.PickMultipleFilesAsync();
        await ImportPathsAsync(files.Select(file => file.Path));
    }

    private void OpenLibraryClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = _library.RootPath,
            UseShellExecute = true,
        });
    }

    private void StartPreviewClick(object sender, RoutedEventArgs e)
    {
        _runtime.Start(EngineMode.Preview, RenderBackend.SafeMode);
        _runtime.Tick(FrameTime.FromDelta(TimeSpan.Zero, TimeSpan.Zero));
        if (_library.Items.Count > 0)
        {
            _wallpaperHost.Show(_library.Items[0]);
            LibrarySummaryText.Text = $"正在预览：{_library.Items[0].DisplayName}";
        }
    }

    private void LibraryDoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (LibraryList.SelectedItem is LibraryItem item)
        {
            _runtime.Start(EngineMode.Preview, RenderBackend.SafeMode);
            _wallpaperHost.Show(item);
            LibrarySummaryText.Text = $"正在预览：{item.DisplayName}";
        }
    }

    private async void OpenWorkshopClick(object sender, RoutedEventArgs e)
    {
        var uri = SteamCommunityBridge.BuildWorkshopUri(WorkshopInput.Text);
        await Launcher.LaunchUriAsync(uri);
        CommunityInfo.Message = "已打开 Steam Workshop 页面。下载和订阅由 Steam Client 负责。";
    }

    private async void ScanWorkshopClick(object sender, RoutedEventArgs e)
    {
        var files = await Task.Run(() => SteamCommunityBridge.FindInstalledMediaFiles());
        await ImportPathsAsync(files);
        CommunityInfo.Message = files.Count == 0
            ? "没有找到已下载的 Wallpaper Engine 媒体文件。请先在 Steam 中下载，或直接拖入本地文件。"
            : $"已扫描到 {files.Count} 个媒体文件，并导入可识别内容。";
    }

    private void PageDragOver(object sender, DragEventArgs e) => SetDragOperation(e);

    private void DropZoneDragOver(object sender, DragEventArgs e) => SetDragOperation(e);

    private async void PageDrop(object sender, DragEventArgs e) => await HandleDropAsync(e);

    private async void DropZoneDrop(object sender, DragEventArgs e) => await HandleDropAsync(e);

    private static void SetDragOperation(DragEventArgs e)
    {
        e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems)
            ? DataPackageOperation.Copy
            : DataPackageOperation.None;
        e.Handled = true;
    }

    private async Task HandleDropAsync(DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        await ImportPathsAsync(items.OfType<Windows.Storage.StorageFile>().Select(file => file.Path));
        e.Handled = true;
    }

    private async Task ImportPathsAsync(IEnumerable<string> paths)
    {
        var pathList = paths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (pathList.Length == 0)
        {
            return;
        }

        var imported = await Task.Run(() => _library.ImportFiles(pathList));
        RefreshLibrary();
        LibrarySummaryText.Text = imported.Count == 0
            ? "没有识别到支持的图片或视频"
            : $"已导入 {imported.Count} 个文件，共 {_library.Items.Count} 个项目";
    }

    private void RefreshLibrary()
    {
        LibraryList.ItemsSource = _library.Items.ToArray();
        LibrarySummaryText.Text = _library.Items.Count == 0
            ? "拖拽图片或视频开始使用"
            : $"共 {_library.Items.Count} 个项目";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Unloaded -= OnUnloaded;
        _wallpaperHost.Dispose();
        _runtime.Dispose();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Dispose();
    }
}
