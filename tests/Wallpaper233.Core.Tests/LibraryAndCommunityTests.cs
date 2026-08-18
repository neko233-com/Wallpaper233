using Wallpaper233.Community;
using Wallpaper233.Core;

namespace Wallpaper233.Core.Tests;

public sealed class LibraryAndCommunityTests
{
    [Fact]
    public void LibraryRecognizesImagesAndVideosOnly()
    {
        Assert.True(LibraryCatalog.TryGetMediaKind("wallpaper.png", out var imageKind));
        Assert.Equal(MediaKind.Image, imageKind);
        Assert.True(LibraryCatalog.TryGetMediaKind("wallpaper.webm", out var videoKind));
        Assert.Equal(MediaKind.Video, videoKind);
        Assert.False(LibraryCatalog.TryGetMediaKind("wallpaper.exe", out _));
    }

    [Fact]
    public void SteamBridgeParsesWorkshopIdAndBuildsOfficialUrl()
    {
        Assert.True(SteamCommunityBridge.TryParsePublishedFileId(
            "https://steamcommunity.com/sharedfiles/filedetails/?id=123456789",
            out var id));
        Assert.Equal((ulong)123456789, id);
        Assert.Equal(
            "https://steamcommunity.com/sharedfiles/filedetails/?id=123456789",
            SteamCommunityBridge.BuildWorkshopUri("123456789").ToString());
    }
}
