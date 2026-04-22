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

        var (geometry, brush) = category switch
        {
            EAssetCategory.Blueprint => ("BlueprintIcon", "BlueprintBrush"),
            EAssetCategory.BlueprintGeneratedClass => ("BlueprintIcon", "BlueprintBrush"),
            EAssetCategory.WidgetBlueprintGeneratedClass => ("BlueprintIcon", "BlueprintWidgetBrush"),
            EAssetCategory.AnimBlueprintGeneratedClass => ("BlueprintIcon" , "BlueprintAnimBrush"),
            EAssetCategory.RigVMBlueprintGeneratedClass => ("BlueprintIcon", "BlueprintRigVMBrush"),
            EAssetCategory.UserDefinedEnum => ("BlueprintIcon", "UserDefinedEnumBrush"),
            EAssetCategory.UserDefinedStruct => ("BlueprintIcon", "UserDefinedStructBrush"),
            EAssetCategory.CookedMetaData => ("BlueprintIcon", "CookedMetaDataBrush"),

            EAssetCategory.Texture => ("TextureIconAlt", "TextureBrush"),

            EAssetCategory.StaticMesh => ("StaticMeshIconAlt", "NeutralBrush"),
            EAssetCategory.SkeletalMesh => ("SkeletalMeshIconAlt", "NeutralBrush"),
            EAssetCategory.CustomizableObject => ("StaticMeshIconAlt", "CustomizableObjectBrush"),
            EAssetCategory.NaniteDisplacedMesh => ("StaticMeshIconAlt", "NaniteDisplacedMeshBrush"),

            EAssetCategory.Material => ("MaterialIcon", "MaterialBrush"),
            EAssetCategory.MaterialEditorData => ("MaterialIcon", "MaterialEditorBrush"),
            EAssetCategory.MaterialParameterCollection => ("MaterialParameterCollectionIcon", "MaterialBrush"),
            EAssetCategory.MaterialFunction => ("MaterialFunctionIcon", "MaterialBrush"),
            EAssetCategory.MaterialFunctionEditorData => ("MaterialFunctionIcon", "MaterialEditorBrush"),

            EAssetCategory.Animation => ("AnimationIconAlt", "AnimationBrush"),
            EAssetCategory.Skeleton => ("SkeletonIcon", "NeutralBrush"),
            EAssetCategory.Rig => ("AnimationIconAlt", "NeutralBrush"),

            EAssetCategory.World => ("WorldIcon", "WorldBrush"),
            EAssetCategory.BuildData => ("MapIconAlt", "BuildDataBrush"),
            EAssetCategory.LevelSequence => ("ClapperIcon", "LevelSequenceBrush"),
            EAssetCategory.Foliage => ("FoliageIcon", "FoliageBrush"),

            EAssetCategory.ItemDefinitionBase => ("DataTableIcon", "NeutralBrush"),
            EAssetCategory.CurveBase => ("CurveIcon", "CurveBrush"),
            EAssetCategory.PhysicsAsset => ("PhysicsIcon", "NeutralBrush"),
            EAssetCategory.ObjectRedirector => ("RedirectorIcon", "ConfigBrush"),
            EAssetCategory.PhysicalMaterial => ("MaterialIcon", "NeutralBrush"),

            EAssetCategory.Audio => ("AudioIconAlt", "AudioBrush"),
            EAssetCategory.SoundBank => ("AudioIconAlt", "SoundBankBrush"),
            EAssetCategory.AudioEvent => ("AudioIconAlt", "AudioEventBrush"),

            EAssetCategory.Video => ("VideoIcon", "VideoBrush"),
            EAssetCategory.Font => ("FontIcon", "NeutralBrush"),

            EAssetCategory.Particle => ("ParticleIcon", "ParticleBrush"),

            EAssetCategory.Data => resolvedAssetType switch
            {
                "uplugin" or "upluginmanifest" => ("PluginIcon", "PluginBrush"),
                "uproject" or "uefnproject" => ("PluginIcon", "ProjectBrush"),
                "ini" => ("ConfigIcon", "ConfigBrush"),
                "locmeta" or "locres" => ("LocaleIcon", "LocalizationBrush"),
                "lua" or "luac" => ("LuaIcon", "LuaBrush"),
                "json5" or "json" => ("JsonIcon", "JsonXmlBrush"),
                "txt" or "log" => ("TxtIcon", "NeutralBrush"),
                "pem" => ("CertificateIcon", "NeutralBrush"),
                "verse" => ("VerseIcon", "NeutralBrush"),
                "function" => ("FunctionIcon", "NeutralBrush"),
                "bin" => ("DataTableIcon", "BinaryBrush"),
                "xml" => ("XmlIcon", "JsonXmlBrush"),
                "gitignore" => ("GitIcon", "GitBrush"),
                "html" => ("HtmlIcon", "HtmlBrush"),
                "js" => ("JavaScriptIcon", "JavaScriptBrush"),
                "css" => ("CssIcon", "CssBrush"),
                "csv" => ("CsvIcon", "CsvBrush"),
                _ => ("DataTableIcon", "NeutralBrush")
            },

            EAssetCategory.ByteCode => ("CodeIcon", "CodeBrush"),

            EAssetCategory.Borderlands => ("BorderlandsIcon", "BorderlandsBrush"),
            EAssetCategory.Aion2 => ("AionIcon", "AionBrush"),
            EAssetCategory.RocoKingdomWorld => ("RocoKingdomWorldIcon", "RocoKingdomWorldBrush"),
            EAssetCategory.DeltaForce => ("DeltaForceIcon", "DeltaForceBrush"),

            _ => ("AssetIcon", "NeutralBrush")
        };

        if (targetType == typeof(Geometry))
            return Application.Current.FindResource(geometry) as Geometry;
        if (targetType == typeof(Brush))
            return Application.Current.FindResource(brush) as Brush;

        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
