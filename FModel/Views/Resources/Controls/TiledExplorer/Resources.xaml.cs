using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using FModel.Extensions;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls.TiledExplorer;

public partial class ResourcesDictionary
{
    public ResourcesDictionary()
    {
        InitializeComponent();
    }

    private async void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem { DataContext: GameFileViewModel file })
            return;

        MainWindow.YesWeCats.LeftTabControl.SelectedIndex = 2;

        await file.ExtractAsync();

        // TODO: auto scroll on item selection just like folder view
        // AssetsListName.ScrollIntoView(file.Asset);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not ListBoxItem item || FindPopup(item) is not { IsOpen: true } popup)
            return;

        // var window = GetWindow(btn);
        // if (window == null) return;

        var point = item.PointToScreen(e.GetPosition(item));
        // var dpi = VisualTreeHelper.GetDpi(window);
        popup.HorizontalOffset = point.X /*/ dpi.DpiScaleX*/ + 12;
        popup.VerticalOffset = point.Y /*/ dpi.DpiScaleY*/ + 12;
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not ListBoxItem item || FindPopup(item) is not { } popup)
            return;

        if (popup.Child is Border { Child: StackPanel stack })
        {
            if (stack.Children[0] is not TextBlock fileInfoText) return;

            switch (item.DataContext)
            {
                case TreeItem folder:
                    fileInfoText.Inlines.Clear();
                    fileInfoText.Inlines.Add(new Run("Folders Count: "));
                    fileInfoText.Inlines.Add(new Run(folder.Folders.Count.ToString()) { FontWeight = FontWeights.Bold });
                    fileInfoText.Inlines.Add(new LineBreak());
                    fileInfoText.Inlines.Add(new Run("Assets Count: "));
                    fileInfoText.Inlines.Add(new Run(folder.AssetsList.Assets.Count.ToString()) { FontWeight = FontWeights.Bold });
                    break;
                case GameFileViewModel file:
                    fileInfoText.Inlines.Clear();
                    fileInfoText.Inlines.Add(new Run(file.Asset.Name) { FontWeight = FontWeights.Bold });
                    fileInfoText.Inlines.Add(new LineBreak());
                    var assetType = !string.IsNullOrEmpty(file.ResolvedAssetType) ? file.ResolvedAssetType : file.Asset.Extension;
                    fileInfoText.Inlines.Add(new Run($"Type: {assetType}") { Foreground = Brushes.LightGray });
                    fileInfoText.Inlines.Add(new LineBreak());
                    fileInfoText.Inlines.Add(new Run($"Size: {StringExtensions.GetReadableSize(file.Asset.Size)}") { Foreground = Brushes.LightGray });
                    break;
            }
        }

        popup.IsOpen = true;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not ListBoxItem item || FindPopup(item) is not { } popup)
            return;

        popup.IsOpen = false;
    }

    private Popup? FindPopup(ListBoxItem item)
    {
        var listBox = ItemsControl.ItemsControlFromItemContainer(item) as ListBox;
        return listBox?.Template?.FindName("AssetPopup", listBox) as Popup;
    }
}
