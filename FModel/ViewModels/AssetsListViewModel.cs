using System.ComponentModel;
using Avalonia.Collections;
using CUE4Parse.FileProvider.Objects;
using FModel.Framework;

namespace FModel.ViewModels;

public class AssetsListViewModel
{
    public RangeObservableCollection<GameFileViewModel> Assets { get; } = [];

    private DataGridCollectionView _assetsView;
    public DataGridCollectionView AssetsView
    {
        get
        {
            _assetsView ??= new DataGridCollectionView(Assets)
            {
                SortDescriptions = { DataGridSortDescription.FromPath("Asset.Path", ListSortDirection.Ascending) }
            };
            return _assetsView;
        }
    }

    public void Add(GameFile gameFile) => Assets.Add(new GameFileViewModel(gameFile));
}
