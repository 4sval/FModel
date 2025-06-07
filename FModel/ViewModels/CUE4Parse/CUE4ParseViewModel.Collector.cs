using System;
using AdonisUI.Controls;

namespace FModel.ViewModels.CUE4Parse;

public partial class CUE4ParseViewModel
{
    public void ClearProvider()
    {
        AssetsFolder.Folders.Clear();
        SearchVm.SearchResults.Clear();
        Helper.CloseWindow<AdonisWindow>("Search View");

        ForEachProvider(provider => provider.UnloadNonStreamedVfs());

        GC.Collect();
    }
}
