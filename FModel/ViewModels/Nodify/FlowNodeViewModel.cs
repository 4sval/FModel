using System.Windows.Controls;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Framework;

namespace FModel.ViewModels.Nodify;

public class FlowNodeViewModel : NodeViewModel
{
    private string? _title;
    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public NodifyObservableCollection<ConnectorViewModel> Input { get; } = new();
    public NodifyObservableCollection<ConnectorViewModel> Output { get; } = new();

    public FlowNodeViewModel()
    {
        Input.WhenAdded(c =>
        {
            c.Flow = ConnectorFlow.Input;
            c.Node = this;
        });
        Output.WhenAdded(c =>
        {
            c.Flow = ConnectorFlow.Output;
            c.Node = this;
        });

        Orientation = Orientation.Horizontal;
    }

    protected void ConnectOutput(ConnectorViewModel output, FPropertyTagType? tag)
    {
        switch (tag)
        {
            case FPropertyTagType<FScriptStruct> { Value: not null } s:
                switch (s.Value.StructType)
                {
                    case FStructFallback fallback:
                        break;
                }
                break;
            case FPropertyTagType<UScriptArray> { Value.Properties.Count: > 0 } a:
                break;
            case FPropertyTagType<FPackageIndex> { Value: not null } p:
                break;
            case FPropertyTagType<FSoftObjectPath> s:
                break;
            case { } t:
                output.Title = t.GenericValue?.ToString();
                break;
        }
    }
}
