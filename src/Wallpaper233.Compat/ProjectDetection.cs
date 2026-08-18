using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wallpaper233.Compat;

public enum SourceFormat
{
    Unsupported,
    Wallpaper233Package,
    WallpaperEngineImportCandidate,
}

public sealed record Wallpaper233Manifest(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("entry")] string Entry);

public sealed record ProjectDetectionResult(SourceFormat Format, string RootPath, string? Reason = null);

public static class ProjectDetector
{
    public const string Wallpaper233ManifestName = "wallpaper233.json";
    public const string LegacyProjectManifestName = "project.json";

    public static ProjectDetectionResult Detect(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var fullPath = Path.GetFullPath(rootPath);
        if (!Directory.Exists(fullPath))
        {
            return new(SourceFormat.Unsupported, fullPath, "Project directory does not exist.");
        }

        var ownManifestPath = Path.Combine(fullPath, Wallpaper233ManifestName);
        if (File.Exists(ownManifestPath))
        {
            return new(SourceFormat.Wallpaper233Package, fullPath);
        }

        var legacyManifestPath = Path.Combine(fullPath, LegacyProjectManifestName);
        if (File.Exists(legacyManifestPath))
        {
            return new(
                SourceFormat.WallpaperEngineImportCandidate,
                fullPath,
                "Detected as a local import candidate; no legacy runtime is loaded.");
        }

        return new(SourceFormat.Unsupported, fullPath, "No supported manifest was found.");
    }

    public static bool TryReadManifest(string rootPath, out Wallpaper233Manifest? manifest)
    {
        manifest = null;
        var path = Path.Combine(Path.GetFullPath(rootPath), Wallpaper233ManifestName);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using var stream = File.OpenRead(path);
            manifest = JsonSerializer.Deserialize<Wallpaper233Manifest>(stream);
            return manifest is not null;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
