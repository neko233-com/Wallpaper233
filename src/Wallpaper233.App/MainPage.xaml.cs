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
    private readonly WallpaperSettingsStore _settingsStore = new();
    private readonly WallpaperHostWindow _wallpaperHost = new();
    private WallpaperSettings _settings = new();
    private bool _initializing = true;
    private bool _restored;
    private bool _disposed;

    public MainPage()
    {
        _settings = _settingsStore.Load();
        InitializeComponent();
        Unloaded += OnUnloaded;
        Loaded += OnLoaded;
        SeedBuiltInSamples();
        RefreshLibrary();
        FitModeComboBox.SelectedIndex = (int)_settings.FitMode;
        RestoreOnLaunchCheckBox.IsChecked = _settings.RestoreOnLaunch;
        _initializing = false;
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
        var item = SelectedOrFirstItem();
        if (item is null)
        {
            return;
        }

        _runtime.Start(EngineMode.Preview, RenderBackend.SafeMode);
        _runtime.Tick(FrameTime.FromDelta(TimeSpan.Zero, TimeSpan.Zero));
        _wallpaperHost.Preview(item, _settings.FitMode);
        LibrarySummaryText.Text = $"正在预览：{item.DisplayName}";
    }

    private void ApplySelectedClick(object sender, RoutedEventArgs e)
    {
        var item = SelectedOrFirstItem();
        if (item is not null)
        {
            ApplyWallpaper(item);
        }
    }

    private void LibraryDoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (LibraryList.SelectedItem is LibraryItem item)
        {
            ApplyWallpaper(item);
        }
    }

    private void FitModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || FitModeComboBox.SelectedIndex < 0)
        {
            return;
        }

        _settings = _settings with { FitMode = (WallpaperFitMode)FitModeComboBox.SelectedIndex };
        _settingsStore.Save(_settings);

        if (_settings.AppliedWallpaperId is Guid id && _library.Items.FirstOrDefault(item => item.Id == id) is { } item)
        {
            _wallpaperHost.Apply(item, _settings.FitMode);
        }
    }

    private void RestoreOnLaunchClick(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _settings = _settings with { RestoreOnLaunch = RestoreOnLaunchCheckBox.IsChecked == true };
        _settingsStore.Save(_settings);
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

    private LibraryItem? SelectedOrFirstItem() =>
        LibraryList.SelectedItem as LibraryItem ?? (_library.Items.Count > 0 ? _library.Items[0] : null);

    private void ApplyWallpaper(LibraryItem item, bool persist = true)
    {
        _runtime.Start(EngineMode.Wallpaper, RenderBackend.SafeMode);
        _wallpaperHost.Apply(item, _settings.FitMode);
        if (persist)
        {
            _settings = _settings with { AppliedWallpaperId = item.Id };
            _settingsStore.Save(_settings);
        }

        LibrarySummaryText.Text = $"已应用到桌面：{item.DisplayName}";
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_restored)
        {
            return;
        }

        _restored = true;
        if (!_settings.RestoreOnLaunch || _settings.AppliedWallpaperId is not Guid id)
        {
            return;
        }

        var item = _library.Items.FirstOrDefault(candidate => candidate.Id == id);
        if (item is not null)
        {
            ApplyWallpaper(item, persist: false);
        }
    }

    private void SeedBuiltInSamples()
    {
        if (_library.Items.Count != 0)
        {
            return;
        }

        var sampleDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "SampleWallpapers");
        if (!Directory.Exists(sampleDirectory))
        {
            return;
        }

        _library.ImportFiles(Directory.EnumerateFiles(sampleDirectory, "*.png"));
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
