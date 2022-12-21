using System.Windows;
using AdonisUI.Controls;
using FModel.Framework;
using FModel.Services;
using FModel.Views.Resources.Controls;

namespace FModel.ViewModels.Commands;

public class TabCommand : ViewModelCommand<TabItem>
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;

    public TabCommand(TabItem contextViewModel) : base(contextViewModel)
    {
    }

    public override async void Execute(TabItem contextViewModel, object parameter)
    {
        var fullPath = contextViewModel.Directory + "/" + contextViewModel.Header;
        switch (parameter)
        {
            case TabItem mdlClick:
                _applicationView.CUE4Parse.TabControl.RemoveTab(mdlClick);
                break;
            case "Close_Tab":
                _applicationView.CUE4Parse.TabControl.RemoveTab(contextViewModel);
                break;
            case "Close_All_Tabs":
                _applicationView.CUE4Parse.TabControl.RemoveAllTabs();
                break;
            case "Close_Other_Tabs":
                _applicationView.CUE4Parse.TabControl.RemoveOtherTabs(contextViewModel);
                break;
            case "Asset_Export_Data":
                await _threadWorkerView.Begin(_ => _applicationView.CUE4Parse.ExportData(fullPath));
                break;
            case "Asset_Save_Properties":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, fullPath, false, EBulkType.Properties);
                    _applicationView.CUE4Parse.TabControl.SelectedTab.SaveProperty(false);
                });
                break;
            case "Asset_Save_Textures":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, fullPath, false, EBulkType.Textures);
                    _applicationView.CUE4Parse.TabControl.SelectedTab.SaveImages(false);
                });
                break;
            case "Asset_Save_Models":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, fullPath, false, EBulkType.Meshes);
                });
                break;
            case "Asset_Save_Animations":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, fullPath, false, EBulkType.Animations);
                });
                break;
            case "Open_Properties":
                if (contextViewModel.Header == "New Tab" || contextViewModel.Document == null) return;
                Helper.OpenWindow<AdonisWindow>(contextViewModel.Header + " (Properties)", () =>
                {
                    new PropertiesPopout(contextViewModel)
                    {
                        Title = contextViewModel.Header + " (Properties)"
                    }.Show();
                });
                break;
            case "Copy_Asset_Name":
                Clipboard.SetText(contextViewModel.Header);
                break;
        }
    }
}
