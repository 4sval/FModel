using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FModel.Views.Resources.Converters;

public class FolderToGeometryConverter : IValueConverter
{
    public static readonly FolderToGeometryConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType != typeof(Geometry) || value is not string folderName)
            return null;

        var resource = folderName.ToLowerInvariant() switch
        {
            "textures" or "texture" or "ui" or "icons" or "umgassets" => "FolderTextureIcon",
            "config" => "FolderConfigIcon",
            "audio" => "FolderAudioIcon",
            "movies" or "video" or "videos" or "cinematics" => "FolderVideoIcon",
            "data" or "datatable" or "datatables" => "FolderDataIcon",
            _ => "FolderIconAlt",
        };

        return Application.Current.FindResource(resource) as Geometry;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
