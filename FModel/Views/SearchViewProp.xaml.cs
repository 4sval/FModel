using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FModel.Extensions;
using FModel.Services;
using FModel.ViewModels;

namespace FModel.Views;

public partial class SearchViewProp
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public SearchViewProp()
    {
        DataContext = _applicationView;
        InitializeComponent();

        Activate();
        WpfLickMyAss.Focus();
        WpfLickMyAss.SelectAll();

        _applicationView.CUE4Parse.SearchVmProp.RefreshFilter();
    }

    private void OnDeleteSearchClick(object sender, RoutedEventArgs e)
    {
        _applicationView.CUE4Parse.SearchVmProp.FilterText = string.Empty;
        _applicationView.CUE4Parse.SearchVmProp.RefreshFilter();
    }

    private async void OnAssetDoubleClick(object sender, RoutedEventArgs e)
    {
        if (SearchListView.SelectedItem is not AssetItem assetItem)
            return;

        WindowState = WindowState.Minimized;
        MainWindow.YesWeCats.AssetsListName.ItemsSource = null;
        var folder = _applicationView.CustomDirectories.GoToCommand.JumpTo(assetItem.FullPath.SubstringBeforeLast('/'));
        if (folder == null) return;

        MainWindow.YesWeCats.Activate();

        do { await Task.Delay(100); } while (MainWindow.YesWeCats.AssetsListName.Items.Count < folder.AssetsList.Assets.Count);

        MainWindow.YesWeCats.LeftTabControl.SelectedIndex = 2; // assets tab
        do
        {
            await Task.Delay(100);
            MainWindow.YesWeCats.AssetsListName.SelectedItem = assetItem;
            MainWindow.YesWeCats.AssetsListName.ScrollIntoView(assetItem);
        } while (MainWindow.YesWeCats.AssetsListName.SelectedItem == null);
    }

    private async void OnAssetExtract(object sender, RoutedEventArgs e)
    {
        if (SearchListView.SelectedItem is not AssetItem assetItem)
            return;

        WindowState = WindowState.Minimized;
        await _threadWorkerView.Begin(cancellationToken => _applicationView.CUE4Parse.Extract(cancellationToken, assetItem.FullPath, true));

        MainWindow.YesWeCats.Activate();
    }
    private async void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        if (MainWindow.YesWeCats.AssetsFolderName.SelectedItem is TreeItem folder)
        {
            await _threadWorkerView.Begin(cancellationToken => { _applicationView.CUE4Parse.SearchAsset(cancellationToken, folder, _applicationView.CUE4Parse.SearchVmProp); });

            _applicationView.CUE4Parse.SearchVmProp.RefreshFilter();
        }
    }

    private void OnStateChanged(object sender, EventArgs e)
    {
        switch (WindowState)
        {
            case WindowState.Normal:
                Activate();
                WpfLickMyAss.Focus();
                WpfLickMyAss.SelectAll();
                return;
        }
    }
}
