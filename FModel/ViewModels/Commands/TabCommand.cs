using System.Windows;
using AdonisUI.Controls;
using FModel.Framework;
using FModel.Services;
using FModel.Views.Resources.Controls;

namespace FModel.ViewModels.Commands;

public class TabCommand : ViewModelCommand<TabItem>
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public TabCommand(TabItem contextViewModel) : base(contextViewModel)
    {
    }

    public override void Execute(TabItem contextViewModel, object parameter)
    {
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