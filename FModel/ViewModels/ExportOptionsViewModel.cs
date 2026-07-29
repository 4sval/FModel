using System;
using System.Collections.Generic;
using System.Windows.Threading;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.UEFormat.Enums;
using FModel.Framework;
using FModel.Settings;

namespace FModel.ViewModels;

public class ExportOptionsViewModel : ViewModel
{
    public bool OverrideOptions
    {
        get;
        set => SetProperty(ref field, value);
    }

    private DispatcherTimer? _feedbackTimer;
    public string? FeedbackMessage
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value)) return;
            RaisePropertyChanged(nameof(HasFeedback));
            _feedbackTimer?.Stop();
            if (string.IsNullOrWhiteSpace(value)) return;

            if (_feedbackTimer == null)
            {
                _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
#pragma warning disable CA2011
                _feedbackTimer.Tick += (_, _) => FeedbackMessage = null;
#pragma warning restore CA2011
            }
            _feedbackTimer.Start();
        }
    }
    public bool HasFeedback => FeedbackMessage != null;

    public string OutputDirectory
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ShowExportImmediatelyOption { get; }
    public bool ExportImmediately
    {
        get;
        set => SetProperty(ref field, value);
    }

    public IEnumerable<EMeshFormat> MeshFormats { get; } = Enum.GetValues<EMeshFormat>();
    public EMeshFormat SelectedMeshFormat
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            RaisePropertyChanged(nameof(SocketSettingsEnabled));
            RaisePropertyChanged(nameof(CompressionSettingsEnabled));
            RaisePropertyChanged(nameof(TextureFormatsEnabled));

            if (value == EMeshFormat.USD)
            {
                _preUsdTextureFormat = SelectedTextureFormat;
                SelectedTextureFormat = ETextureFormat.Png;
            }
            else if (_preUsdTextureFormat.HasValue)
            {
                SelectedTextureFormat = _preUsdTextureFormat.Value;
                _preUsdTextureFormat = null;
            }
        }
    }

    public IEnumerable<ENaniteMeshFormat> NaniteMeshFormats { get; } = Enum.GetValues<ENaniteMeshFormat>();
    public ENaniteMeshFormat SelectedNaniteMeshFormat
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            RaisePropertyChanged(nameof(ShowNaniteWarning));
        }
    }

    public bool ShowNaniteWarning => SelectedNaniteMeshFormat != ENaniteMeshFormat.NoNanite;

    public IEnumerable<EMeshQuality> MeshQualities { get; } = Enum.GetValues<EMeshQuality>();
    public EMeshQuality SelectedMeshQuality
    {
        get;
        set => SetProperty(ref field, value);
    }

    public IEnumerable<ESocketFormat> SocketFormats { get; } = Enum.GetValues<ESocketFormat>();
    public ESocketFormat SelectedSocketFormat
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool SocketSettingsEnabled => SelectedMeshFormat == EMeshFormat.ActorX;

    public IEnumerable<EFileCompressionFormat> CompressionFormats { get; } = Enum.GetValues<EFileCompressionFormat>();
    public EFileCompressionFormat SelectedCompressionFormat
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool CompressionSettingsEnabled => SelectedMeshFormat == EMeshFormat.UEFormat;

    public IEnumerable<EMaterialDepth> MaterialDepths { get; } = Enum.GetValues<EMaterialDepth>();
    public EMaterialDepth SelectedMaterialDepth
    {
        get;
        set => SetProperty(ref field, value);
    }
    public bool ExportMaterials
    {
        get;
        set => SetProperty(ref field, value);
    }

    public IEnumerable<ETexturePlatform> TexturePlatforms { get; } = Enum.GetValues<ETexturePlatform>();
    public ETexturePlatform SelectedTexturePlatform
    {
        get;
        set => SetProperty(ref field, value);
    }

    public IEnumerable<ETextureFormat> TextureFormats { get; } = Enum.GetValues<ETextureFormat>();
    public ETextureFormat SelectedTextureFormat
    {
        get;
        set => SetProperty(ref field, value);
    }

    private ETextureFormat? _preUsdTextureFormat;
    public bool TextureFormatsEnabled => SelectedMeshFormat != EMeshFormat.USD;

    public bool ExportHdrTexturesAsHdr
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int TextureQuality
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ExportMorphTargets
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ExportAllTextureMips
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ExportOptionsViewModel(bool showExportImmediatelyOption = false)
    {
        ShowExportImmediatelyOption = showExportImmediatelyOption;
        ResetToUserDefaults();
    }

    public void ResetToUserDefaults()
    {
        OutputDirectory = UserSettings.Default.ModelDirectory;
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
        TextureQuality = UserSettings.Default.TextureQuality;
        ExportAllTextureMips = UserSettings.Default.ExportAllTextureMips;
        ExportImmediately = UserSettings.Default.ExportImmediately;

        OverrideOptions = false;
        FeedbackMessage = "Reset to defaults";
    }

    public void SaveAsUserDefaults()
    {
        UserSettings.Default.ModelDirectory = OutputDirectory;
        UserSettings.Default.MeshExportFormat = SelectedMeshFormat;
        UserSettings.Default.NaniteMeshExportFormat = SelectedNaniteMeshFormat;
        UserSettings.Default.MeshQuality = SelectedMeshQuality;
        UserSettings.Default.SocketExportFormat = SelectedSocketFormat;
        UserSettings.Default.CompressionFormat = SelectedCompressionFormat;
        UserSettings.Default.MaterialExportFormat = SelectedMaterialDepth;
        UserSettings.Default.SaveEmbeddedMaterials = ExportMaterials;
        UserSettings.Default.CurrentDir.TexturePlatform = SelectedTexturePlatform;
        UserSettings.Default.TextureExportFormat = SelectedTextureFormat;
        UserSettings.Default.SaveHdrTexturesAsHdr = ExportHdrTexturesAsHdr;
        UserSettings.Default.SaveMorphTargets = ExportMorphTargets;
        UserSettings.Default.TextureQuality = TextureQuality;
        UserSettings.Default.ExportAllTextureMips = ExportAllTextureMips;
        UserSettings.Default.ExportImmediately = ExportImmediately;
        UserSettings.Save();

        OverrideOptions = false;
        FeedbackMessage = "Saved as default";
    }

    public ExportOptions BuildOptions() => new(
        SelectedMeshFormat,
        SelectedNaniteMeshFormat,
        SelectedMeshQuality,
        SelectedTexturePlatform,
        SelectedTextureFormat,
        TextureQuality,
        ExportHdrTexturesAsHdr,
        SelectedMaterialDepth,
        ExportMaterials,
        ExportMorphTargets,
        SelectedSocketFormat,
        SelectedCompressionFormat,
        ExportAllTextureMips
    );
}
