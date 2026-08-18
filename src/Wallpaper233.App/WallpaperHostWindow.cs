using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Wallpaper233.Core;
using Windows.Media.Core;
using Windows.Media.Playback;
using WinRT.Interop;

namespace Wallpaper233.App;

public sealed class WallpaperHostWindow : IDisposable
{
    private readonly List<WallpaperSurface> _surfaces = [];

    public void Preview(LibraryItem item, WallpaperFitMode fitMode)
    {
        ArgumentNullException.ThrowIfNull(item);

        Close();
        var surface = CreateSurface(item, fitMode, desktopLayer: false);
        _surfaces.Add(surface);
        surface.Window.Activate();
    }

    public void Apply(LibraryItem item, WallpaperFitMode fitMode)
    {
        ArgumentNullException.ThrowIfNull(item);

        Close();
        foreach (var monitor in DesktopWallpaperNative.GetMonitorBounds())
        {
            var surface = CreateSurface(item, fitMode, desktopLayer: true);
            _surfaces.Add(surface);
            surface.Window.Activate();

            var hwnd = WindowNative.GetWindowHandle(surface.Window);
            DesktopWallpaperNative.AttachBehindDesktop(hwnd, monitor);
        }
    }

    public void Show(LibraryItem item) => Preview(item, WallpaperFitMode.Fill);

    public void Dispose() => Close();

    private static WallpaperSurface CreateSurface(LibraryItem item, WallpaperFitMode fitMode, bool desktopLayer)
    {
        var window = new Window { Title = $"Wallpaper233 · {item.DisplayName}" };
        if (desktopLayer)
        {
            ConfigureDesktopWindow(window);
        }

        var content = CreateContent(item, fitMode, out var mediaPlayer);
        window.Content = content;
        return new WallpaperSurface(window, mediaPlayer);
    }

    private static Grid CreateContent(LibraryItem item, WallpaperFitMode fitMode, out MediaPlayer? mediaPlayer)
    {
        mediaPlayer = null;
        var stretch = ToStretch(fitMode);
        var root = new Grid
        {
            Background = new SolidColorBrush(Colors.Black),
        };

        if (item.MediaKind == MediaKind.Image)
        {
            root.Children.Add(new Image
            {
                Source = new BitmapImage(ToFileUri(item.FilePath)),
                Stretch = stretch,
            });
            return root;
        }

        mediaPlayer = new MediaPlayer
        {
            IsLoopingEnabled = true,
            AutoPlay = true,
        };
        mediaPlayer.Source = MediaSource.CreateFromUri(ToFileUri(item.FilePath));

        var video = new MediaPlayerElement
        {
            AreTransportControlsEnabled = false,
            AutoPlay = true,
            Stretch = stretch,
        };
        video.SetMediaPlayer(mediaPlayer);
        root.Children.Add(video);
        return root;
    }

    private static void ConfigureDesktopWindow(Window window)
    {
        var presenter = OverlappedPresenter.Create();
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.SetBorderAndTitleBar(false, false);
        window.AppWindow.SetPresenter(presenter);
    }

    private void Close()
    {
        foreach (var surface in _surfaces.ToArray())
        {
            surface.Dispose();
        }

        _surfaces.Clear();
    }

    private static Stretch ToStretch(WallpaperFitMode fitMode) => fitMode switch
    {
        WallpaperFitMode.Fit => Stretch.Uniform,
        WallpaperFitMode.Stretch => Stretch.Fill,
        _ => Stretch.UniformToFill,
    };

    private static Uri ToFileUri(string path) => new(Path.GetFullPath(path), UriKind.Absolute);

    private sealed class WallpaperSurface(Window window, MediaPlayer? mediaPlayer) : IDisposable
    {
        public Window Window { get; } = window;

        private MediaPlayer? MediaPlayer { get; } = mediaPlayer;

        public void Dispose()
        {
            MediaPlayer?.Dispose();
            Window.Close();
        }
    }
}

internal readonly record struct MonitorBounds(int X, int Y, int Width, int Height);

internal static class DesktopWallpaperNative
{
    private const uint ProgmanMessage = 0x052C;
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsCaption = 0x00C00000L;
    private const long WsThickFrame = 0x00040000L;
    private const long WsMinimizeBox = 0x00020000L;
    private const long WsMaximizeBox = 0x00010000L;
    private const long WsSysMenu = 0x00080000L;
    private const long WsPopup = unchecked((long)0x80000000);
    private const long WsChild = 0x40000000L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private const uint SwpNoSendChanging = 0x0400;
    private const uint SwpShowWindow = 0x0040;
    private const int SwShownNoActivate = 4;
    private static readonly IntPtr HwndBottom = new(1);

    public static IReadOnlyList<MonitorBounds> GetMonitorBounds()
    {
        var monitors = new List<MonitorBounds>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, ref rect, _) =>
        {
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            if (width > 0 && height > 0)
            {
                monitors.Add(new MonitorBounds(rect.Left, rect.Top, width, height));
            }

            return true;
        }, IntPtr.Zero);

        if (monitors.Count == 0)
        {
            monitors.Add(new MonitorBounds(
                GetSystemMetrics(76),
                GetSystemMetrics(77),
                GetSystemMetrics(78),
                GetSystemMetrics(79)));
        }

        return monitors;
    }

    public static void AttachBehindDesktop(IntPtr hwnd, MonitorBounds bounds)
    {
        var desktopHost = FindDesktopHost();
        if (desktopHost != IntPtr.Zero)
        {
            SetParent(hwnd, desktopHost);
        }

        var style = GetWindowLongPtr(hwnd, GwlStyle).ToInt64();
        style = (style & ~(WsCaption | WsThickFrame | WsMinimizeBox | WsMaximizeBox | WsSysMenu | WsPopup)) | WsChild;
        SetWindowLongPtr(hwnd, GwlStyle, new IntPtr(style));

        var extendedStyle = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(extendedStyle | WsExToolWindow | WsExNoActivate));

        SetWindowPos(
            hwnd,
            HwndBottom,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            SwpNoActivate | SwpNoOwnerZOrder | SwpNoSendChanging | SwpShowWindow);
        ShowWindow(hwnd, SwShownNoActivate);
    }

    private static IntPtr FindDesktopHost()
    {
        var progman = FindWindow("Progman", "Program Manager");
        if (progman != IntPtr.Zero)
        {
            SendMessageTimeout(progman, ProgmanMessage, IntPtr.Zero, IntPtr.Zero, 0, 1000, out _);
        }

        var workerWindow = IntPtr.Zero;
        EnumWindows((topLevelWindow, _) =>
        {
            var shellView = FindWindowEx(topLevelWindow, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView != IntPtr.Zero)
            {
                workerWindow = FindWindowEx(IntPtr.Zero, topLevelWindow, "WorkerW", null);
            }

            return true;
        }, IntPtr.Zero);

        return workerWindow != IntPtr.Zero ? workerWindow : progman;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref Rect rect, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowName);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr childHandle, IntPtr newParentHandle);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int command);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newStyle);
}
