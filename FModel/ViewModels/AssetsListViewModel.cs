using System.ComponentModel;
using System.Windows.Data;
using CUE4Parse.FileProvider.Objects;
using FModel.Framework;

namespace FModel.ViewModels;

public class AssetsListViewModel
{
    public RangeObservableCollection<GameFileViewModel> Assets { get; } = [];

    private ICollectionView _assetsView;
    public ICollectionView AssetsView
    {
        get
        {
            _assetsView ??= new ListCollectionView(Assets)
            {
                SortDescriptions = { new SortDescription("Asset.Path", ListSortDirection.Ascending) }
            };
            return _assetsView;
        }
    }

    public void Add(GameFile gameFile) => Assets.Add(new GameFileViewModel(gameFile));
}
