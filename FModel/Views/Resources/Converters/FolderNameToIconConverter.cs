using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FModel.Views.Resources.Converters;

public class FolderNameToIconConverter : IValueConverter
{
    public static readonly FolderNameToIconConverter Instance = new();

    private static readonly Geometry _defaultFolderIcon = (Geometry) Application.Current.FindResource("FolderIconAlt");
    private static readonly Geometry _folderTextureIcon = (Geometry) Application.Current.FindResource("FolderTextureIcon");
    private static readonly Geometry _folderConfigIcon = (Geometry) Application.Current.FindResource("FolderConfigIcon");
    private static readonly Geometry _folderAudioIcon = (Geometry) Application.Current.FindResource("FolderAudioIcon");
    private static readonly Geometry _folderVideoIcon = (Geometry) Application.Current.FindResource("FolderVideoIcon");
    private static readonly Geometry _folderDataIcon = (Geometry) Application.Current.FindResource("FolderDataIcon");

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string folderName)
            return _defaultFolderIcon;

        return folderName.ToLowerInvariant() switch
        {
            "textures" or "texture" or "ui" or "icons" or "umgassets" => _folderTextureIcon,
            "config" => _folderConfigIcon,
            "audio" => _folderAudioIcon,
            "movies" or "video" or "videos" => _folderVideoIcon,
            "data" or "datatable" or "datatables" => _folderDataIcon,
            _ => _defaultFolderIcon,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
