using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls.TiledExplorer;

public partial class FolderButton : UserControl
{
    public FolderButton()
    {
        InitializeComponent();
    }

    private void OnFolderClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: TreeItem folder })
            return;

        folder.IsSelected = true;
        folder.IsExpanded = true;
    }

    private void AssetsExplorerButton_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not Button { DataContext: TreeItem folder } btn || FindPopup(btn) is not Popup popup)
            return;

        if (popup.Child is Border { Child: StackPanel stack })
        {
            if (stack.Children[0] is not TextBlock fileInfoText) return;

            fileInfoText.Inlines.Clear();
            fileInfoText.Inlines.Add(new Run("Folders Count: "));
            fileInfoText.Inlines.Add(new Run(folder.Folders.Count.ToString()) { FontWeight = FontWeights.Bold });
            fileInfoText.Inlines.Add(new LineBreak());
            fileInfoText.Inlines.Add(new Run("Assets Count: "));
            fileInfoText.Inlines.Add(new Run(folder.AssetsList.Assets.Count.ToString()) { FontWeight = FontWeights.Bold });
        }

        popup.IsOpen = true;
    }

    private void AssetsExplorerButton_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not Button btn || FindPopup(btn) is not { } popup)
            return;

        popup.IsOpen = false;
    }

    private void AssetsExplorerButton_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not Button btn || FindPopup(btn) is not { IsOpen: true } popup)
            return;

        // var window = GetWindow(btn);
        // if (window == null) return;

        var point = btn.PointToScreen(e.GetPosition(btn));
        // var dpi = VisualTreeHelper.GetDpi(window);
        popup.HorizontalOffset = point.X /*/ dpi.DpiScaleX*/ + 12;
        popup.VerticalOffset = point.Y /*/ dpi.DpiScaleY*/ + 12;
    }

    private Popup? FindPopup(Button btn)
    {
        if (VisualTreeHelper.GetParent(btn) is Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is Popup popup)
                    return popup;
            }
        }
        return null;
    }
}

