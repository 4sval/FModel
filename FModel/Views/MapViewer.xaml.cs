using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FModel.Extensions;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;
using FModel.Views.Resources.Controls;
using Microsoft.Win32;
using Serilog;

namespace FModel.Views;

public partial class MapViewer
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public MapViewer()
    {
        DataContext = _applicationView;
        _applicationView.MapViewer.Initialize();

        InitializeComponent();
    }

    private void OnClosing(object sender, CancelEventArgs e) => DiscordService.DiscordHandler.UpdateToSavedPresence();

    private void OnClick(object sender, RoutedEventArgs e)
    {
        if (_applicationView.MapViewer.MapImage == null) return;
        var path = Path.Combine(UserSettings.Default.TextureDirectory, "MiniMap.png");

        var saveFileDialog = new SaveFileDialog
        {
            Title = "Save MiniMap",
            FileName = "MiniMap.png",
            InitialDirectory = path.SubstringBeforeLast('\\'),
            Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*"
        };

        if (!saveFileDialog.ShowDialog().GetValueOrDefault()) return;
        path = saveFileDialog.FileName;

        using var fileStream = new FileStream(path, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(_applicationView.MapViewer.GetImageToSave()));
        encoder.Save(fileStream);

        if (File.Exists(path))
        {
            Log.Information("MiniMap.png successfully saved");
            FLogger.AppendInformation();
            FLogger.AppendText("Successfully saved 'MiniMap.png'", Constants.WHITE, true);
        }
        else
        {
            Log.Error("MiniMap.png could not be saved");
            FLogger.AppendError();
            FLogger.AppendText("Could not save 'MiniMap.png'", Constants.WHITE, true);
        }
    }

    private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var i = 0;
        foreach (var item in MapTree.Items)
        {
            if (item is not TreeViewItem { IsSelected: true })
            {
                i++;
                continue;
            }

            _applicationView.MapViewer.MapIndex = i;
            break;
        }
    }
}