using System.Globalization;
using System.Text.RegularExpressions;

namespace Wallpaper233.Community;

public static class SteamCommunityBridge
{
    public const int WallpaperEngineAppId = 431960;

    private static readonly Regex PublishedFileIdPattern = new(
        @"(?:[?&]id=|/sharedfiles/filedetails/(?:\?id=)?)(?<id>\d{5,})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static Uri BuildWorkshopUri(string? input)
    {
        if (TryParsePublishedFileId(input, out var publishedFileId))
        {
            return new Uri($"https://steamcommunity.com/sharedfiles/filedetails/?id={publishedFileId}");
        }

        var query = Uri.EscapeDataString(input?.Trim() ?? string.Empty);
        return new Uri($"https://steamcommunity.com/app/{WallpaperEngineAppId}/workshop/?searchtext={query}");
    }

    public static bool TryParsePublishedFileId(string? input, out ulong publishedFileId)
    {
        publishedFileId = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (ulong.TryParse(input.Trim(), out publishedFileId))
        {
            return true;
        }

        var match = PublishedFileIdPattern.Match(input);
        return match.Success && ulong.TryParse(match.Groups["id"].Value, out publishedFileId);
    }

    public static IReadOnlyList<string> FindInstalledWorkshopRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        AddSteamLibraryRoot(Path.Combine(programFilesX86, "Steam"));
        AddSteamLibraryRoot(Path.Combine(programFiles, "Steam"));

        return [.. roots];

        void AddSteamLibraryRoot(string steamRoot)
        {
            if (!Directory.Exists(steamRoot))
            {
                return;
            }

            AddWorkshopRoot(steamRoot);
            var libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryFile))
            {
                return;
            }

            foreach (var line in File.ReadLines(libraryFile))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var firstQuote = trimmed.IndexOf('"', 6);
                var valueStart = firstQuote < 0 ? -1 : trimmed.IndexOf('"', firstQuote + 1);
                var valueEnd = trimmed.LastIndexOf('"');
                if (valueStart >= 0 && valueEnd > valueStart)
                {
                    AddWorkshopRoot(trimmed[(valueStart + 1)..valueEnd].Replace("\\\\", "\\"));
                }
            }
        }

        void AddWorkshopRoot(string libraryRoot)
        {
            var workshopRoot = Path.Combine(
                libraryRoot,
                "steamapps",
                "workshop",
                "content",
                WallpaperEngineAppId.ToString(CultureInfo.InvariantCulture));

            if (Directory.Exists(workshopRoot))
            {
                roots.Add(workshopRoot);
            }
        }
    }

    public static IReadOnlyList<string> FindInstalledMediaFiles(int maximumFiles = 256)
    {
        var results = new List<string>();
        foreach (var root in FindInstalledWorkshopRoots())
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                {
                    if (Wallpaper233.Core.LibraryCatalog.TryGetMediaKind(path, out _))
                    {
                        results.Add(path);
                        if (results.Count >= maximumFiles)
                        {
                            return results;
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Workshop content may be changing while Steam is downloading it.
            }
            catch (UnauthorizedAccessException)
            {
                // One inaccessible item must not prevent the remaining roots from being scanned.
            }
        }

        return results;
    }
}
