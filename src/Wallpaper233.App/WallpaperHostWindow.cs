using System.Runtime.InteropServices;
using Microsoft.UI;
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
    private Window? _window;
    private MediaPlayer? _mediaPlayer;

    public void Show(LibraryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        Close();
        _window = new Window { Title = $"Wallpaper233 · {item.DisplayName}" };
        _window.Content = CreateContent(item);
        _window.Activate();

        var hwnd = WindowNative.GetWindowHandle(_window);
        DesktopWallpaperNative.AttachBehindDesktop(hwnd);
    }

    public void Dispose()
    {
        Close();
    }

    private Grid CreateContent(LibraryItem item)
    {
        var root = new Grid
        {
            Background = new SolidColorBrush(Colors.Black),
        };

        if (item.MediaKind == MediaKind.Image)
        {
            root.Children.Add(new Image
            {
                Source = new BitmapImage(ToFileUri(item.FilePath)),
                Stretch = Stretch.UniformToFill,
            });
            return root;
        }

        _mediaPlayer = new MediaPlayer
        {
            IsLoopingEnabled = true,
            AutoPlay = true,
        };
        _mediaPlayer.Source = MediaSource.CreateFromUri(ToFileUri(item.FilePath));

        var video = new MediaPlayerElement
        {
            AreTransportControlsEnabled = false,
            AutoPlay = true,
            Stretch = Stretch.UniformToFill,
        };
        video.SetMediaPlayer(_mediaPlayer);
        root.Children.Add(video);
        return root;
    }

    private void Close()
    {
        _mediaPlayer?.Dispose();
        _mediaPlayer = null;
        _window?.Close();
        _window = null;
    }

    private static Uri ToFileUri(string path) => new(Path.GetFullPath(path));
}

internal static class DesktopWallpaperNative
{
    private const uint ProgmanMessage = 0x052C;
    private const int GwlExStyle = -20;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private const int SwShownNoActivate = 4;
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;

    public static void AttachBehindDesktop(IntPtr hwnd)
    {
        var desktopHost = FindDesktopHost();
        if (desktopHost != IntPtr.Zero)
        {
            SetParent(hwnd, desktopHost);
        }

        var style = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(style | WsExToolWindow | WsExNoActivate));

        SetWindowPos(
            hwnd,
            IntPtr.Zero,
            GetSystemMetrics(SmXVirtualScreen),
            GetSystemMetrics(SmYVirtualScreen),
            GetSystemMetrics(SmCxVirtualScreen),
            GetSystemMetrics(SmCyVirtualScreen),
            SwpNoActivate | SwpShowWindow);
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

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowName);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

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
