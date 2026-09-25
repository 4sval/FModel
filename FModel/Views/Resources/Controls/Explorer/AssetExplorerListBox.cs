using System.Collections;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FModel.Extensions;

namespace FModel.Views.Resources.Controls.Explorer;

public class AssetExplorerListBox : ListBox
{
    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        base.OnItemsSourceChanged(oldValue, newValue);

        // Always start at the top, otherwise inherited offset can cause items not to be visible when we change folder
        if (this.FindVisualChild<VirtualizingPanel>() is IScrollInfo scroll)
        {
            scroll.SetVerticalOffset(0);
        }
    }
}
