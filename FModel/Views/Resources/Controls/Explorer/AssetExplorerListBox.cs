using System.Windows.Controls;

namespace FModel.Views.Resources.Controls.Explorer;

public class AssetExplorerListBox : ListBox
{
    public AssetExplorerListBox()
    {
        AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));
    }

    // This is important, when we scroll and then change folder, scroll offset won't be adjusted and we will end up with missing items
    // Same applies to filtering assets as that also changes scroll offset
    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.OriginalSource is ScrollViewer scroll && scroll.VerticalOffset > scroll.ScrollableHeight)
        {
            scroll.ScrollToVerticalOffset(scroll.ScrollableHeight);
        }
    }
}
