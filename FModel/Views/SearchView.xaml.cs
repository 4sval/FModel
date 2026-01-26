using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CUE4Parse.FileProvider.Objects;
using FModel.Services;
using FModel.ViewModels;

namespace FModel.Views;

public enum ESearchViewTab
{
    SearchView,
    RefView
}

public partial class SearchView
{
    private ThreadWorkerViewModel _threadWorkerView => ApplicationService.ThreadWorkerView;
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
    private SearchViewModel _searchViewModel => _applicationView.CUE4Parse.SearchVm;
    private SearchViewModel _refViewModel => _applicationView.CUE4Parse.RefVm;

    private ESearchViewTab _currentTab = ESearchViewTab.SearchView;

    public SearchView()
    {
        DataContext = new
        {
            mainApplication = _applicationView,
            SearchTab = _searchViewModel,
            RefTab = _refViewModel,
        };
        InitializeComponent();

        Activate();
        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }

    public void FocusTab(ESearchViewTab view)
    {
        _currentTab = view;
        SearchTabControl.SelectedIndex = view switch
        {
            ESearchViewTab.SearchView => 0,
            ESearchViewTab.RefView => 1,
            _ => SearchTabControl.SelectedIndex
        };
        WindowState = WindowState.Normal;
        CurrentTextBox?.Focus();
        CurrentTextBox?.SelectAll();
    }

    public void ChangeCollection(ESearchViewTab view, IEnumerable<GameFile> files, GameFile refFile)
    {
        var vm = view switch
        {
            ESearchViewTab.SearchView => _searchViewModel,
            ESearchViewTab.RefView => _refViewModel,
            _ => null
        };
        vm?.ChangeCollection(files, refFile);
    }

    private async void OnFindRefs(object sender, RoutedEventArgs e)
    {
        if (CurrentListView?.SelectedItem is not GameFile entry)
            return;

        await _threadWorkerView.Begin(_ => _applicationView.CUE4Parse.FindReferences(entry));
    }

    private void OnTabItemChange(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource is not TabControl tabControl)
            return;

        _currentTab = tabControl.SelectedIndex switch
        {
            0 => ESearchViewTab.SearchView,
            1 => ESearchViewTab.RefView,
            _ => _currentTab
        };
        CurrentTextBox?.Focus();
        CurrentTextBox?.SelectAll();
    }

    private void OnDeleteSearchClick(object sender, RoutedEventArgs e)
    {
        var viewModel = CurrentViewModel;
        if (viewModel == null)
            return;
        viewModel.FilterText = string.Empty;
        viewModel.RefreshFilter();
    }

    private SearchViewModel CurrentViewModel => _currentTab switch
    {
        ESearchViewTab.SearchView => _applicationView.CUE4Parse.SearchVm,
        ESearchViewTab.RefView => _applicationView.CUE4Parse.RefVm,
        _ => null
    };

    private ListView CurrentListView => _currentTab switch
    {
        ESearchViewTab.SearchView => SearchListView,
        ESearchViewTab.RefView => RefListView,
        _ => null
    };

    private TextBox CurrentTextBox => _currentTab switch
    {
        ESearchViewTab.SearchView => SearchTextBox,
        ESearchViewTab.RefView => RefSearchTextBox,
        _ => null
    };

    private async void OnSearchSortClick(object sender, RoutedEventArgs e)
    {
        await CurrentViewModel?.CycleSortSizeMode();
    }

    private async void OnAssetDoubleClick(object sender, RoutedEventArgs e)
    {
        if (CurrentListView?.SelectedItem is not GameFile entry)
            return;

        await NavigateToAssetAndSelect(entry);
    }
    private async void OnGoToRefPackage(object sender, RoutedEventArgs e)
    {
        if (_refViewModel.RefFile is not GameFile entry)
            return;

        await NavigateToAssetAndSelect(entry);
    }

    private async Task NavigateToAssetAndSelect(GameFile entry)
    {
        WindowState = WindowState.Minimized;
        MainWindow.YesWeCats.AssetsListName.ItemsSource = null;
        var folder = _applicationView.CustomDirectories.GoToCommand.JumpTo(entry.Directory);
        if (folder == null)
            return;

        MainWindow.YesWeCats.Activate();

        do
        { await Task.Delay(100); } while (MainWindow.YesWeCats.AssetsListName.Items.Count < folder.AssetsList.Assets.Count);

        while (!folder.IsSelected || MainWindow.YesWeCats.AssetsFolderName.SelectedItem != folder)
            await Task.Delay(50); // stops assets tab from opening too early

        ApplicationService.ApplicationView.SelectedLeftTabIndex = 2; // assets tab
        do
        {
            await Task.Delay(100);
            var vm = MainWindow.YesWeCats.AssetsListName.Items
                .OfType<GameFileViewModel>()
                .FirstOrDefault(x => x.Asset == entry);
            MainWindow.YesWeCats.AssetsListName.SelectedItem = vm;
            MainWindow.YesWeCats.AssetsListName.ScrollIntoView(vm);
        } while (MainWindow.YesWeCats.AssetsListName.SelectedItem == null);
    }

    private async void OnAssetExtract(object sender, RoutedEventArgs e)
    {
        if (CurrentListView?.SelectedItem is not GameFile entry)
            return;

        WindowState = WindowState.Minimized;
        await _threadWorkerView.Begin(cancellationToken => _applicationView.CUE4Parse.Extract(cancellationToken, entry, true));

        MainWindow.YesWeCats.Activate();
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        CurrentViewModel?.RefreshFilter();
    }

    private void OnStateChanged(object sender, EventArgs e)
    {
        switch (WindowState)
        {
            case WindowState.Normal:
                Activate();
                CurrentTextBox?.Focus();
                CurrentTextBox?.SelectAll();
                return;
        }
    }
}
