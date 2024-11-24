using System.Windows;
using CUE4Parse.UE4.Assets.Objects.Properties;
using FModel.Framework;

namespace FModel.ViewModels.Nodify;

public enum ConnectorFlow
{
    Input,
    Output
}

public enum ConnectorShape
{
    Circle,
    Triangle,
    Square,
}

public class ConnectorViewModel : ViewModel
{
    private string? _title;
    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    private Point _anchor;
    public Point Anchor
    {
        get => _anchor;
        set => SetProperty(ref _anchor, value);
    }

    private ConnectorShape _shape;
    public ConnectorShape Shape
    {
        get => _shape;
        set => SetProperty(ref _shape, value);
    }

    public NodeViewModel Node { get; set; }
    public ConnectorFlow Flow { get; set; }

    public NodifyObservableCollection<ConnectionViewModel> Connections { get; } = new();

    public ConnectorViewModel()
    {
        Connections.WhenAdded(c => c.Input = this);
    }

    public ConnectorViewModel(string title) : this()
    {
        Title = title;
    }

    public ConnectorViewModel(FPropertyTagType? tag) : this(tag?.ToString())
    {

    }
}
