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
                "textures" or "texture" or "ui" or "icons" or "umgassets" or "hud" or "hdri" or "tex" => "TextureIconAlt",
                "config" or "tags" => "ConfigIcon",
                "audio" or "wwiseaudio" or "wwise" or "fmod" or "soundbanks" or "banks" or "sound" or "sounds" or "cue" => "AudioIconAlt",
                "movies" or "video" or "videos" or "cinematics" => "VideoIcon",
                "data" or "datatable" or "datatables" or "curves" => "DataTableIcon",
                "blueprint" or "blueprints" or "audioblueprints" => "BlueprintIcon",
                "mesh" or "meshes" or "model" or "models" or "characters" or "environment" or "props" => "StaticMeshIconAlt",
                "material" or "materials" or "materialinstance" or "mastermaterial" => "MaterialIcon",
                "materialfunctions" or "materialfunction" => "MaterialFunctionIcon",
                "plugin" or "plugins" => "PluginIcon",
                "localization" => "LocaleIcon",
                "map" or "maps" or "world" or "worlds" => "WorldIcon",
                "effect" or "effects" or "niagara" or "vfx" or "particlesystems" or "particles" => "ParticleIcon",
                "animation" or "animations" or "anim" or "animsequences" or "animsequence" or "montage" or "montages" => "AnimationIconAlt",
                "physics" => "PhysicsIcon",
                "windows" => "MonitorIcon",
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
                "textures" or "texture" or "ui" or "icons" or "umgassets" or "hud" or "hdri" or "tex" => Brushes.MediumPurple,
                "config" or "tags" => Brushes.LightSlateGray,
                "audio" or "wwiseaudio" or "wwise" or "fmod" or "soundbanks" or "banks" or "sound" or "sounds" or "cue" => Brushes.MediumSeaGreen,
                "movies" or "video" or "videos" or "cinematics" => Brushes.IndianRed,
                "data" or "datatable" or "datatables" or "curves" => Brushes.SteelBlue,
                "blueprint" or "blueprints" or "audioblueprints" => Brushes.DodgerBlue,
                "material" or "materials" or "materialinstance" => Brushes.Beige,
                "materialfunctions" or "materialfunction" => Brushes.LavenderBlush,
                "plugin" or "plugins" => Brushes.GreenYellow,
                "localization" => Brushes.CornflowerBlue,
                "map" or "maps" or "world" or "worlds" => Brushes.Orange,
                "effect" or "effects" or "niagara" or "vfx" or "particlesystems" or "particles" => Brushes.Gold,
                "animation" or "animations" or "anim" or "animsequences" or "animsequence" or "montage" or "montages" => Brushes.Coral,
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
