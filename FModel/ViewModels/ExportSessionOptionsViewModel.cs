using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.UEFormat.Enums;
using FModel.Framework;
using FModel.Settings;

namespace FModel.ViewModels;

public class ExportSessionOptionsViewModel : ViewModel
{
    // --- Override toggle ---
    public bool OverrideOptions { get; set => SetProperty(ref field, value); }

    // --- Mesh ---
    public IEnumerable<EMeshFormat> MeshFormats { get; } = Enum.GetValues<EMeshFormat>();
    public EMeshFormat SelectedMeshFormat { get; set => SetProperty(ref field, value); }

    public IEnumerable<ENaniteMeshFormat> NaniteMeshFormats { get; } = Enum.GetValues<ENaniteMeshFormat>();
    public ENaniteMeshFormat SelectedNaniteMeshFormat { get; set => SetProperty(ref field, value); }

    public IEnumerable<EMeshQuality> MeshQualities { get; } = Enum.GetValues<EMeshQuality>();
    public EMeshQuality SelectedMeshQuality { get; set => SetProperty(ref field, value); }

    // --- Socket / Compression ---
    public IEnumerable<ESocketFormat> SocketFormats { get; } = Enum.GetValues<ESocketFormat>();
    public ESocketFormat SelectedSocketFormat { get; set => SetProperty(ref field, value); }

    public IEnumerable<EFileCompressionFormat> CompressionFormats { get; } = Enum.GetValues<EFileCompressionFormat>();
    public EFileCompressionFormat SelectedCompressionFormat { get; set => SetProperty(ref field, value); }

    // --- Material ---
    public IEnumerable<EMaterialDepth> MaterialDepths { get; } = Enum.GetValues<EMaterialDepth>();
    public EMaterialDepth SelectedMaterialDepth { get; set => SetProperty(ref field, value); }
    public bool ExportMaterials { get; set => SetProperty(ref field, value); }

    // --- Texture ---
    public IEnumerable<ETexturePlatform> TexturePlatforms { get; } = Enum.GetValues<ETexturePlatform>();
    public ETexturePlatform SelectedTexturePlatform { get; set => SetProperty(ref field, value); }

    public IEnumerable<ETextureFormat> TextureFormats { get; } = Enum.GetValues<ETextureFormat>();
    public ETextureFormat SelectedTextureFormat { get; set => SetProperty(ref field, value); }

    public bool ExportHdrTexturesAsHdr { get; set => SetProperty(ref field, value); }

    // --- Morph ---
    public bool ExportMorphTargets { get; set => SetProperty(ref field, value); }

    public ExportSessionOptionsViewModel()
    {
        ResetToUserDefaults();
    }

    public void ResetToUserDefaults()
    {
        SelectedMeshFormat = UserSettings.Default.MeshExportFormat;
        SelectedNaniteMeshFormat = UserSettings.Default.NaniteMeshExportFormat;
        SelectedMeshQuality = UserSettings.Default.MeshQuality;
        SelectedSocketFormat = UserSettings.Default.SocketExportFormat;
        SelectedCompressionFormat = UserSettings.Default.CompressionFormat;
        SelectedMaterialDepth = UserSettings.Default.MaterialExportFormat;
        ExportMaterials = UserSettings.Default.SaveEmbeddedMaterials;
        SelectedTexturePlatform = UserSettings.Default.CurrentDir.TexturePlatform;
        SelectedTextureFormat = UserSettings.Default.TextureExportFormat;
        ExportHdrTexturesAsHdr = UserSettings.Default.SaveHdrTexturesAsHdr;
        ExportMorphTargets = UserSettings.Default.SaveMorphTargets;
    }

    public ExportOptions BuildOptions() => new(
        SelectedMeshFormat,
        SelectedNaniteMeshFormat,
        SelectedMeshQuality,
        SelectedTexturePlatform,
        SelectedTextureFormat,
        100,
        ExportHdrTexturesAsHdr,
        SelectedMaterialDepth,
        ExportMaterials,
        ExportMorphTargets,
        SelectedSocketFormat,
        SelectedCompressionFormat
    );
}
