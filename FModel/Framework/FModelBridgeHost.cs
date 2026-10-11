using System.IO;
using System.Windows;
using AdonisUI.Controls;
using CUE4Parse_Conversion;
using FModel.Settings;
using FModel.ViewModels;
using FModel.Views;
using Snooper.Hosting;

namespace FModel.Framework;

public sealed class FModelBridgeHost : IBridgeHost
{
    public string Name => "FModel";
    public string ImGuiIniPath => Path.Combine(UserSettings.Default.OutputDirectory, ".data", "snooper.ini");

    public bool CanBrowseAssets => true;

    public ExportSession Session => ExportSessionViewModel.Instance.Session;
    public void ShowExportSession()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var window = Helper.GetWindow<AdonisWindow>("Export Session", () => new ExportSessionWindow().Show());
            window.Topmost = true;
            window.Topmost = false;
        });
    }
}
