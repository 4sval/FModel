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

        var (geometry, brush) = folderName switch
        {
            "ai" => ("AIIcon", "AIBrush"),
            "textures" or "texture" or "ui" or "icons" or "umgassets" or "hud" or "hdri" or "tex" => ("TextureIconAlt", "TextureBrush"),
            "config" or "tags" => ("ConfigIcon", "ConfigBrush"),
            "audio" or "wwiseaudio" or "wwise" or "fmod" or "sound" or "sounds" or "cue" => ("AudioIconAlt", "AudioBrush"),
            "soundbanks" or "banks" => ("AudioIconAlt", "SoundBankBrush"),
            "audioevent" or "audioevents" => ("AudioIconAlt", "AudioEventBrush"),
            "movies" or "video" or "videos" or "cinematics" => ("VideoIcon", "VideoBrush"),
            "data" or "datatable" or "datatables" => ("DataTableIcon", "DataTableBrush"),
            "curves" => ("CurveIcon", "CurveBrush"),
            "bp" or "blueprint" or "blueprints" or "audioblueprints" => ("BlueprintIcon", "BlueprintBrush"),
            "staticmesh" or "mesh" or "meshes" or "model" or "models" or "characters" or "environment" or "props" => ("StaticMeshIconAlt", "NeutralBrush"),
            "material" or "materials" or "materialinstance" or "mastermaterial" => ("MaterialIcon", "MaterialBrush"),
            "materialfunctions" or "materialfunction" => ("MaterialFunctionIcon", "MaterialBrush"),
            "plugin" or "plugins" => ("PluginIcon", "PluginBrush"),
            "localization" => ("LocaleIcon", "LocalizationBrush"),
            "map" or "maps" or "world" or "worlds" => ("WorldIcon", "WorldBrush"),
            "effect" or "effects" or "niagara" or "vfx" or "particlesystems" or "particles" => ("ParticleIcon", "ParticleBrush"),
            "animation" or "animations" or "anim" or "animsequences" or "animsequence" or "montage" or "montages" => ("AnimationIconAlt", "AnimationBrush"),
            "physics" or "physicasset" or "physicassets" => ("PhysicsIcon", "NeutralBrush"),
            "windows" => ("MonitorIcon", "NeutralBrush"),
            "locale" or "localization" or "l10n" => ("LocaleIcon", "LocalizationBrush"),
            "skeleton" or "skeletons" => ("SkeletonIcon", "NeutralBrush"),
            "certificate" or "certificates" => ("CertificateIcon", "NeutralBrush"),
            "fonts" or "font" => ("FontIcon", "NeutralBrush"),
            _ => (null, "NeutralBrush"),
        };

        if (targetType == typeof(Geometry) && geometry != null)
            return Application.Current.FindResource(geometry) as Geometry;
        if (targetType == typeof(Brush))
            return Application.Current.FindResource(brush) as Brush;

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
