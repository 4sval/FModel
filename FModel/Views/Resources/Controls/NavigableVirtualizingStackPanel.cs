using System.Windows.Controls;

namespace FModel.Views.Resources.Controls;

/// <summary>
/// Exposes container realization for programmatic navigation in a virtualized tree.
/// </summary>
public sealed class NavigableVirtualizingStackPanel : VirtualizingStackPanel
{
    public void BringItemIntoView(int index) => BringIndexIntoView(index);
}
