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

        resolvedAssetType = resolvedAssetType.ToLowerInvariant();

        if (targetType == typeof(Geometry))
        {
            var resource = category switch
            {
                EAssetCategory.BlueprintGeneratedClass => "BlueprintIcon",
                EAssetCategory.WidgetBlueprintGeneratedClass => "BlueprintIcon",
                EAssetCategory.AnimBlueprintGeneratedClass => "BlueprintIcon",
                EAssetCategory.RigVMBlueprintGeneratedClass => "BlueprintIcon",
                EAssetCategory.Blueprint => "BlueprintIcon",
                EAssetCategory.UserDefinedEnum => "BlueprintIcon",
                EAssetCategory.UserDefinedStruct => "BlueprintIcon",
                EAssetCategory.CookedMetaData => "BlueprintIcon",

                EAssetCategory.StaticMesh => "StaticMeshIconAlt",
                EAssetCategory.SkeletalMesh => "SkeletalMeshIconAlt",
                EAssetCategory.Skeleton => "SkeletonIcon",

                EAssetCategory.Texture => "TextureIconAlt",

                EAssetCategory.Material or EAssetCategory.MaterialEditorData => "MaterialIcon",
                EAssetCategory.MaterialFunction => "MaterialFunctionIcon",

                EAssetCategory.Animation => "AnimationIconAlt",

                EAssetCategory.World => "WorldIcon",
                EAssetCategory.BuildData => "MapIconAlt",
                EAssetCategory.LevelSequence => "ClapperIcon",

                EAssetCategory.PhysicsAsset => "PhysicsIcon",
                EAssetCategory.CurveBase => "CurveIcon",
                EAssetCategory.ItemDefinitionBase => "DataTableIcon",
                EAssetCategory.Data => resolvedAssetType switch
                {
                    "uplugin" or "upluginmanifest" or "uproject" or "uefnproject" => "PluginIcon",
                    "ini" => "ConfigIcon",
                    "locmeta" or "locres" => "LocaleIcon",
                    "lua" or "luac" => "LuaIcon",
                    "json5" or "json" => "JsonIcon",
                    "txt" or "log" or "pem" => "TxtIcon",
                    "verse" => "VerseIcon",
                    "function" => "FunctionIcon",
                    _ => "DataTableIcon"
                },

                EAssetCategory.Audio => "AudioIconAlt",
                EAssetCategory.Video => "VideoIcon",
                EAssetCategory.Font => "FontIcon",

                EAssetCategory.Particle => "ParticleIcon",
                _ => "AssetIcon"
            };

            return Application.Current.FindResource(resource) as Geometry;
        }

        if (targetType == typeof(Brush))
        {
            var brush = category switch
            {
                EAssetCategory.BlueprintGeneratedClass => Brushes.DodgerBlue,
                EAssetCategory.WidgetBlueprintGeneratedClass => Brushes.DarkViolet,
                EAssetCategory.AnimBlueprintGeneratedClass => Brushes.Crimson,
                EAssetCategory.RigVMBlueprintGeneratedClass => Brushes.Teal,
                EAssetCategory.Blueprint => Brushes.Yellow,
                EAssetCategory.CookedMetaData => Brushes.Yellow,
                EAssetCategory.UserDefinedEnum => Brushes.DarkGoldenrod,
                EAssetCategory.UserDefinedStruct => Brushes.Tan,

                EAssetCategory.Texture => Brushes.MediumPurple,

                EAssetCategory.Material or EAssetCategory.MaterialFunction => Brushes.BurlyWood,
                EAssetCategory.MaterialEditorData => Brushes.Yellow,

                EAssetCategory.Animation => Brushes.Coral,

                EAssetCategory.World => Brushes.Orange,
                EAssetCategory.BuildData => Brushes.Tomato,
                EAssetCategory.LevelSequence => Brushes.Coral,

                EAssetCategory.CurveBase => Brushes.HotPink,

                EAssetCategory.Data => resolvedAssetType switch
                {
                    "uproject" or "uefnproject" => Brushes.DeepSkyBlue,
                    "uplugin" or "upluginmanifest" => Brushes.GreenYellow,
                    "ini" => Brushes.LightGray,
                    "locmeta" or "locres" => Brushes.CornflowerBlue,
                    "json5" or "json" => Brushes.LightGreen,
                    "bin" => Brushes.Yellow,
                    _ => Brushes.White
                },
                EAssetCategory.Audio => Brushes.MediumSeaGreen,
                EAssetCategory.Video => Brushes.IndianRed,

                EAssetCategory.Particle => Brushes.Gold,
                
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
