using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Wallpaper233.Core;

namespace Wallpaper233.App;

public sealed partial class LibraryThumbnailConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not LibraryItem { MediaKind: MediaKind.Image } item || !File.Exists(item.FilePath))
        {
            return null!;
        }

        try
        {
            return new BitmapImage(new Uri(Path.GetFullPath(item.FilePath)))
            {
                DecodePixelWidth = 480,
            };
        }
        catch (UriFormatException)
        {
            return null!;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

public sealed partial class VideoBadgeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is MediaKind.Video ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
