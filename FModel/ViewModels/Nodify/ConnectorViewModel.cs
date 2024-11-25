using System.Windows;
using CUE4Parse.UE4.Assets.Objects;
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
        void Remove(ConnectionViewModel connection)
        {
            if (Flow is not ConnectorFlow.Input) return;
            Node.Graph.Connections.Remove(connection);
        }

        Connections.WhenAdded(c =>
        {
            switch (Flow)
            {
                case ConnectorFlow.Input when c.Target is null:
                    c.Target = this;
                    c.Source.Connections.Add(c);
                    break;
                case ConnectorFlow.Output when c.Source is null:
                    c.Source = this;
                    c.Target.Connections.Add(c);

                    // only connect outputs to inputs
                    Node.Graph.Connections.Add(c);
                    break;
            }

            c.Source.IsConnected = true;
            c.Target.IsConnected = true;
        })
        .WhenRemoved(Remove)
        .WhenCleared(c =>
        {
            foreach (var connection in c)
            {
                Remove(connection);
            }
        });
    }

    public ConnectorViewModel(string title) : this()
    {
        Title = title;
    }

    public ConnectorViewModel(FPropertyTagData? type) : this(type?.ToString())
    {

    }

    public override string ToString()
    {
        return $"{Title} ({Connections.Count} connection{(Connections.Count > 0 ? "s" : "")})";
    }
}
