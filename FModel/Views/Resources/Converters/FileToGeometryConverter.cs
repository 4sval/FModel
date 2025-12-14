using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FModel.Views.Resources.Converters;

public class FileToGeometryConverter : IMultiValueConverter
{
    public static readonly FileToGeometryConverter Instance = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not EAssetCategory category || values[1] is not string resolvedAssetType)
            return null;

        if (targetType == typeof(Geometry))
        {
            var resource = category switch
            {
                EAssetCategory.Texture => "TextureIconAlt",
                EAssetCategory.StaticMesh => "StaticMeshIconAlt",
                EAssetCategory.SkeletalMesh => "SkeletalMeshIconAlt",
                EAssetCategory.Skeleton => "SkeletonIcon",
                EAssetCategory.Material => "MaterialIcon",
                EAssetCategory.Blueprint => "BlueprintIcon",
                EAssetCategory.Audio => "AudioIconAlt",
                EAssetCategory.Animation => "AnimationIconAlt",
                EAssetCategory.Font => "FontIcon",
                EAssetCategory.PhysicsAsset => "PhysicsIcon",
                EAssetCategory.Video => "VideoIcon",
                EAssetCategory.Data => resolvedAssetType switch
                {
                    "uplugin" => "PluginIcon",
                    "ini" => "ConfigIcon",
                    "locmeta" or "locres" => "LocaleIcon",
                    "lua" or "luac" => "LuaIcon",
                    "json5" or "json" => "JsonIcon",
                    "txt" or "log" or "pem" => "TxtIcon",
                    _ => "DataTableIcon"
                },
                EAssetCategory.Map => "MapIconAlt",
                EAssetCategory.Particle => "ParticleIcon",
                _ => "AssetIcon"
            };

            return Application.Current.FindResource(resource) as Geometry;
        }

        if (targetType == typeof(Brush))
        {
            var brush = category switch
            {
                EAssetCategory.Texture => Brushes.MediumPurple,
                EAssetCategory.Blueprint => Brushes.DodgerBlue,
                EAssetCategory.Map => Brushes.Orange,
                EAssetCategory.Data => resolvedAssetType switch
                {
                    "uplugin" => Brushes.GreenYellow,
                    "ini" => Brushes.LightGray,
                    "locmeta" or "locres" => Brushes.CornflowerBlue,
                    "json5" or "json" => Brushes.LightGreen,
                    _ => Brushes.White
                },
                EAssetCategory.Video => Brushes.IndianRed,
                EAssetCategory.Particle => Brushes.Gold,
                EAssetCategory.Audio => Brushes.MediumSeaGreen,
                EAssetCategory.Material => Brushes.Beige,
                EAssetCategory.Animation => Brushes.Coral,
                _ => Brushes.White
            };

            return brush;
        }

        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
