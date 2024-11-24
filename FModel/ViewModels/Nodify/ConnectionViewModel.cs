using FModel.Framework;

namespace FModel.ViewModels.Nodify;

public class ConnectionViewModel : ViewModel
{
    private ConnectorViewModel _input;
    public ConnectorViewModel Input
    {
        get => _input;
        set => SetProperty(ref _input, value);
    }

    private ConnectorViewModel _output;
    public ConnectorViewModel Output
    {
        get => _output;
        set => SetProperty(ref _output, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ConnectionViewModel()
    {

    }
}
