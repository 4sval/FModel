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
        if (value is not string folderName)
            return null;

        folderName = folderName.ToLowerInvariant();

        if (targetType == typeof(Geometry))
        {
            var resource = folderName switch
            {
                "textures" or "texture" or "ui" or "icons" or "umgassets" => "TextureIconAlt",
                "config" => "ConfigIcon",
                "audio" or "wwiseaudio" or "wwise" or "fmod" => "AudioIconAlt",
                "movies" or "video" or "videos" or "cinematics" => "VideoIcon",
                "data" or "datatable" or "datatables" => "DataTableIcon",
                "blueprint" or "blueprints" => "BlueprintIcon",
                "mesh" or "meshes" or "model" or "models" => "StaticMeshIconAlt",
                "material" or "materials" => "MaterialIcon",
                "plugin" or "plugins" => "PluginIcon",
                "localization" => "LocaleIcon",
                "map" or "maps" or "world" or "worlds" => "MapIconAlt",
                "effect" or "effects" or "niagara" => "ParticleIcon",
                "animation" or "animations" or "anim" or "animsequences" or "montage" or "montages" => "AnimationIconAlt",
                "physics" => "PhysicsIcon",
                _ => null,
            };

            if (resource == null)
                return null;

            return Application.Current.FindResource(resource) as Geometry;
        }

        if (targetType == typeof(Brush))
        {
            Brush brush = folderName switch
            {
                "textures" or "texture" or "ui" or "icons" or "umgassets" => Brushes.MediumPurple,
                "config" => Brushes.LightSlateGray,
                "audio" or "wwiseaudio" or "wwise" => Brushes.MediumSeaGreen,
                "movies" or "video" or "videos" or "cinematics" => Brushes.IndianRed,
                "data" or "datatable" or "datatables" => Brushes.SteelBlue,
                "blueprint" or "blueprints" => Brushes.DodgerBlue,
                "plugin" or "plugins" => Brushes.GreenYellow,
                "localization" => Brushes.CornflowerBlue,
                "map" or "maps" or "world" or "worlds" => Brushes.Orange,
                "effect" or "effects" or "niagara" => Brushes.Gold,
                _ => Brushes.White,
            };

            return brush;
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
