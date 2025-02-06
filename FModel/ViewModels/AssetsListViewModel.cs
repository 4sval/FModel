using System.ComponentModel;
using System.Windows.Data;
using CUE4Parse.FileProvider.Objects;
using FModel.Framework;

namespace FModel.ViewModels;

public class AssetsListViewModel
{
    public RangeObservableCollection<GameFile> Assets { get; }
    public ICollectionView AssetsView { get; }

    public AssetsListViewModel()
    {
        Assets = new RangeObservableCollection<GameFile>();
        AssetsView = new ListCollectionView(Assets)
        {
            SortDescriptions = { new SortDescription("Path", ListSortDirection.Ascending) }
        };
    }
}
