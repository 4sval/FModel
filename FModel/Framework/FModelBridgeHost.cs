using CUE4Parse_Conversion.Options;
using FModel.Settings;
using Snooper.Hosting;

namespace FModel.Framework;

public sealed class FModelBridgeHost : IBridgeHost
{
    public string Name => "FModel";
    public string ExportDirectory => UserSettings.Default.ModelDirectory;

    public bool OwnsLoadOptions => true;
    public bool CanBrowseAssets => true;

    public ExportOptions CreateExportOptions() => UserSettings.GetExportOptions();
}
