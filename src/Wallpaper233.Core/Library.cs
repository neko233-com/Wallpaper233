using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wallpaper233.Core;

public enum MediaKind
{
    Image,
    Video,
}

public sealed record LibraryItem(
    Guid Id,
    string DisplayName,
    string FilePath,
    MediaKind MediaKind,
    DateTimeOffset ImportedAt);

public sealed class LibraryState
{
    public List<LibraryItem> Items { get; set; } = [];
}

[JsonSerializable(typeof(LibraryState))]
[JsonSerializable(typeof(LibraryItem))]
[JsonSerializable(typeof(MediaKind))]
[JsonSerializable(typeof(WallpaperSettings))]
[JsonSerializable(typeof(WallpaperFitMode))]
internal sealed partial class WallpaperJsonContext : JsonSerializerContext
{
}

public sealed class LibraryCatalog
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".avif", ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".webp",
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".avi", ".m4v", ".mkv", ".mov", ".mp4", ".webm", ".wmv",
    };

    private readonly List<LibraryItem> _items = [];

    public LibraryCatalog(string? rootPath = null)
    {
        RootPath = rootPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wallpaper233",
            "Library");

        Directory.CreateDirectory(RootPath);
        Load();
    }

    public string RootPath { get; }

    public IReadOnlyList<LibraryItem> Items => _items;

    public IReadOnlyList<LibraryItem> ImportFiles(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var imported = new List<LibraryItem>();
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!TryGetMediaKind(path, out var mediaKind) || !File.Exists(path))
            {
                continue;
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();
            var id = Guid.NewGuid();
            var destination = Path.Combine(RootPath, $"{id:N}{extension}");
            File.Copy(path, destination, overwrite: false);

            var item = new LibraryItem(
                id,
                Path.GetFileNameWithoutExtension(path),
                destination,
                mediaKind,
                DateTimeOffset.UtcNow);

            _items.Add(item);
            imported.Add(item);
        }

        if (imported.Count > 0)
        {
            Save();
        }

        return imported;
    }

    public static bool TryGetMediaKind(string path, out MediaKind mediaKind)
    {
        var extension = Path.GetExtension(path);
        if (ImageExtensions.Contains(extension))
        {
            mediaKind = MediaKind.Image;
            return true;
        }

        if (VideoExtensions.Contains(extension))
        {
            mediaKind = MediaKind.Video;
            return true;
        }

        mediaKind = default;
        return false;
    }

    private string StatePath => Path.Combine(RootPath, "library.json");

    private void Load()
    {
        if (!File.Exists(StatePath))
        {
            return;
        }

        try
        {
            using var stream = File.OpenRead(StatePath);
            var state = JsonSerializer.Deserialize(stream, WallpaperJsonContext.Default.LibraryState);
            if (state is not null)
            {
                _items.AddRange(state.Items.Where(item => File.Exists(item.FilePath)));
            }
        }
        catch (JsonException)
        {
            // A corrupt catalog must not prevent the application from starting.
        }
        catch (IOException)
        {
            // A locked or unavailable catalog must not prevent the application from starting.
        }
    }

    private void Save()
    {
        var temporaryPath = $"{StatePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = File.Create(temporaryPath))
            {
                JsonSerializer.Serialize(stream, new LibraryState { Items = [.. _items] }, WallpaperJsonContext.Default.LibraryState);
            }

            File.Move(temporaryPath, StatePath, overwrite: true);
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
