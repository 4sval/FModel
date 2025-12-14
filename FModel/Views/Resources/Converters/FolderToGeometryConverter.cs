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
                "textures" or "texture" or "ui" or "icons" or "umgassets" or "hud" => "TextureIconAlt",
                "config" or "tags" => "ConfigIcon",
                "audio" or "wwiseaudio" or "wwise" or "fmod" or "soundbanks" => "AudioIconAlt",
                "movies" or "video" or "videos" or "cinematics" => "VideoIcon",
                "data" or "datatable" or "datatables" or "curves" => "DataTableIcon",
                "blueprint" or "blueprints" => "BlueprintIcon",
                "mesh" or "meshes" or "model" or "models" or "characters" or "environment" or "props" => "StaticMeshIconAlt",
                "material" or "materials" or "materialfunctions" => "MaterialIcon",
                "plugin" or "plugins" => "PluginIcon",
                "localization" => "LocaleIcon",
                "map" or "maps" or "world" or "worlds" => "WorldIcon",
                "effect" or "effects" or "niagara" or "vfx" => "ParticleIcon",
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
                "textures" or "texture" or "ui" or "icons" or "umgassets" or "hud" => Brushes.MediumPurple,
                "config" or "tags" => Brushes.LightSlateGray,
                "audio" or "wwiseaudio" or "wwise" or "fmod" or "soundbanks" => Brushes.MediumSeaGreen,
                "movies" or "video" or "videos" or "cinematics" => Brushes.IndianRed,
                "data" or "datatable" or "datatables" or "curves" => Brushes.SteelBlue,
                "blueprint" or "blueprints" => Brushes.DodgerBlue,
                "material" or "materials" or "materialfunctions" => Brushes.Beige,
                "plugin" or "plugins" => Brushes.GreenYellow,
                "localization" => Brushes.CornflowerBlue,
                "map" or "maps" or "world" or "worlds" => Brushes.Orange,
                "effect" or "effects" or "niagara" or "vfx" => Brushes.Gold,
                "animation" or "animations" or "anim" or "animsequences" or "montage" or "montages" => Brushes.Coral,
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
