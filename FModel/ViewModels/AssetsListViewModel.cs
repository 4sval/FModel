using System.ComponentModel;
using System.Windows.Data;
using CUE4Parse.FileProvider.Objects;
using FModel.Framework;

namespace FModel.ViewModels;

public class AssetsListViewModel
{
    public RangeObservableCollection<GameFileViewModel> Assets { get; }
    public ICollectionView AssetsView { get; }

    public AssetsListViewModel()
    {
        Assets = [];
        AssetsView = new ListCollectionView(Assets)
        {
            SortDescriptions = { new SortDescription("Asset.Path", ListSortDirection.Ascending) }
        };
    }

    public void Add(GameFile gameFile) => Assets.Add(new GameFileViewModel(gameFile));
}
