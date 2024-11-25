using FModel.Framework;

namespace FModel.ViewModels.Nodify;

public class ConnectionViewModel : ViewModel
{
    private ConnectorViewModel _source;
    public ConnectorViewModel Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }

    private ConnectorViewModel _target;
    public ConnectorViewModel Target
    {
        get => _target;
        set => SetProperty(ref _target, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ConnectionViewModel(ConnectorViewModel connector)
    {
        switch (connector.Flow)
        {
            case ConnectorFlow.Input:
                Target = connector;
                break;
            case ConnectorFlow.Output:
                Source = connector;
                break;
        }
    }

    public ConnectionViewModel(ConnectorViewModel source, ConnectorViewModel target)
    {
        Source = source;
        Target = target;
    }

    public override string ToString()
    {
        return $"{Source.Title} -> {Target.Title}";
    }
}
