using System.Text.Json;

namespace Wallpaper233.Core;

public enum WallpaperFitMode
{
    Fill,
    Fit,
    Stretch,
}

public sealed record WallpaperSettings(
    Guid? AppliedWallpaperId = null,
    WallpaperFitMode FitMode = WallpaperFitMode.Fill,
    bool RestoreOnLaunch = true);

public sealed class WallpaperSettingsStore
{
    public WallpaperSettingsStore(string? rootPath = null)
    {
        RootPath = rootPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wallpaper233");

        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public string SettingsPath => Path.Combine(RootPath, "settings.json");

    public WallpaperSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new();
        }

        try
        {
            using var stream = File.OpenRead(SettingsPath);
            return JsonSerializer.Deserialize(stream, WallpaperJsonContext.Default.WallpaperSettings) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
        catch (IOException)
        {
            return new();
        }
    }

    public void Save(WallpaperSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var temporaryPath = $"{SettingsPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = File.Create(temporaryPath))
            {
                JsonSerializer.Serialize(stream, settings, WallpaperJsonContext.Default.WallpaperSettings);
            }

            File.Move(temporaryPath, SettingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
