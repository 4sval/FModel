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

    public override async void Execute(TabItem tabViewModel, object parameter)
    {
        switch (parameter)
        {
            case TabItem mdlClick:
                _applicationView.CUE4Parse.TabControl.RemoveTab(mdlClick);
                break;
            case "Close_Tab":
                _applicationView.CUE4Parse.TabControl.RemoveTab(tabViewModel);
                break;
            case "Close_All_Tabs":
                _applicationView.CUE4Parse.TabControl.RemoveAllTabs();
                break;
            case "Close_Other_Tabs":
                _applicationView.CUE4Parse.TabControl.RemoveOtherTabs(tabViewModel);
                break;
            case "Asset_Export_Data":
                await _threadWorkerView.Begin(_ => _applicationView.CUE4Parse.ExportData(tabViewModel.Entry));
                break;
            case "Asset_Save_Properties":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, tabViewModel.Entry, false, EBulkType.Properties);
                });
                break;
            case "Asset_Save_Textures":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, tabViewModel.Entry, false, EBulkType.Textures);
                });
                break;
            case "Asset_Save_Models":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, tabViewModel.Entry, false, EBulkType.Meshes);
                });
                break;
            case "Asset_Save_Animations":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, tabViewModel.Entry, false, EBulkType.Animations);
                });
                break;
            case "Open_Properties":
                if (tabViewModel.Header == "New Tab" || tabViewModel.Document == null) return;
                Helper.OpenWindow<AdonisWindow>(tabViewModel.Header + " (Properties)", () =>
                {
                    new PropertiesPopout(tabViewModel)
                    {
                        Title = tabViewModel.Header + " (Properties)"
                    }.Show();
                });
                break;
            case "Copy_Asset_Path":
                Clipboard.SetText(tabViewModel.Entry.Path);
                break;
        }
    }
}
