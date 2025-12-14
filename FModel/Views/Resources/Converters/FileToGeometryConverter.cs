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
                    "uplugin" or "upluginmanifest" or "uproject" or "uefnproject" => "PluginIcon",
                    "ini" => "ConfigIcon",
                    "locmeta" or "locres" => "LocaleIcon",
                    "lua" or "luac" => "LuaIcon",
                    "json5" or "json" => "JsonIcon",
                    "txt" or "log" or "pem" => "TxtIcon",
                    "verse" => "VerseIcon",
                    _ => "DataTableIcon"
                },
                EAssetCategory.World => "WorldIcon",
                EAssetCategory.BuildData => "MapIconAlt",
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
                EAssetCategory.Blueprint => resolvedAssetType switch
                {
                    "UserDefinedEnum" => Brushes.DarkGoldenrod,
                    "UserDefinedStruct" => Brushes.Tan,
                    "BluprintGeneratedClass" => Brushes.DodgerBlue,
                    "AnimBlueprintGeneratedClass" => Brushes.Crimson,
                    "WidgetBlueprintGeneratedClass" => Brushes.DarkViolet,
                    "RigVMBlueprintGeneratedClass" or "ControlRigBlueprintGeneratedClass" => Brushes.Teal,
                    "ClassCookedMetaData" or "StructCookedMetaData" or "EnumCookedMetaData" => Brushes.Yellow,
                    "Blueprint" => Brushes.Yellow,
                    _ => Brushes.DodgerBlue
                },
                EAssetCategory.World => Brushes.Orange,
                EAssetCategory.BuildData => Brushes.Tomato,
                EAssetCategory.Data => resolvedAssetType switch
                {
                    "uproject" => Brushes.DeepSkyBlue,
                    "uplugin" or "upluginmanifest" => Brushes.GreenYellow,
                    "ini" => Brushes.LightGray,
                    "locmeta" or "locres" => Brushes.CornflowerBlue,
                    "json5" or "json" => Brushes.LightGreen,
                    "bin" => Brushes.Yellow,
                    _ => Brushes.White
                },
                EAssetCategory.Video => Brushes.IndianRed,
                EAssetCategory.Particle => Brushes.Gold,
                EAssetCategory.Audio => Brushes.MediumSeaGreen,
                EAssetCategory.Material => Brushes.Beige,
                EAssetCategory.MaterialEditorData => Brushes.Yellow,
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
