using System;
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
            case "Assets_Show_Metadata":
                _applicationView.CUE4Parse.ShowMetadata(tabViewModel.Entry);
                break;
            case "Find_References":
                _applicationView.CUE4Parse.FindReferences(tabViewModel.Entry);
                break;
            case "Assets_Decompile":
                _applicationView.CUE4Parse.Decompile(tabViewModel.Entry);
                break;
            case "Save_Data":
                await _threadWorkerView.Begin(_ => _applicationView.CUE4Parse.ExportData(tabViewModel.Entry));
                break;
            case "Save_Properties":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, tabViewModel.Entry, false, EBulkType.Properties);
                });
                break;
            case "Save_Textures":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, tabViewModel.Entry, false, EBulkType.Textures);
                });
                break;
            case "Save_Models":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, tabViewModel.Entry, false, EBulkType.Meshes);
                });
                break;
            case "Save_Worlds":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, tabViewModel.Entry, false, EBulkType.Worlds);
                });
                break;
            case "Save_Animations":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, tabViewModel.Entry, false, EBulkType.Animations);
                });
                break;
            case "Save_Audio":
                await _threadWorkerView.Begin(cancellationToken =>
                {
                    _applicationView.CUE4Parse.Extract(cancellationToken, tabViewModel.Entry, false, EBulkType.Audio);
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
            case "File_Path":
                Clipboard.SetText(tabViewModel.Entry.Path);
                break;
            case "File_Name":
                Clipboard.SetText(tabViewModel.Entry.Name);
                break;
            case "Directory_Path":
                Clipboard.SetText(tabViewModel.Entry.Directory);
                break;
            case "File_Path_No_Extension":
                Clipboard.SetText(tabViewModel.Entry.PathWithoutExtension);
                break;
            case "File_Name_No_Extension":
                Clipboard.SetText(tabViewModel.Entry.NameWithoutExtension);
                break;
        }

        if (parameter is string command && command.StartsWith("Save_", StringComparison.Ordinal)) // This is kinda bad
        {
            await ExportSessionViewModel.Instance.ExportAutomaticallyAsync();
        }
    }
}
