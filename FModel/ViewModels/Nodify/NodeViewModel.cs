using System.Windows;
using System.Windows.Controls;
using FModel.Framework;

namespace FModel.ViewModels.Nodify;

public abstract class NodeViewModel : ViewModel
{
    public NodifyEditorViewModel Graph { get; set; }

    private Point _location;
    public Point Location
    {
        get => _location;
        set => SetProperty(ref _location, value);
    }

    public Orientation Orientation { get; protected set; }

    protected NodeViewModel()
    {

    }
}
