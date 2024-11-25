using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using FModel.Framework;

namespace FModel.ViewModels.Nodify;

public class IndexedNodeViewModel(List<FPropertyTagType> properties, FlowNodeViewModel parent) : BaseIndexedNodeViewModel(properties.Count)
{
    protected override void OnArrayIndexChanged()
    {
        for (int i = 1; i < Input.Count; i++) Input.RemoveAt(i);
        for (int i = 1; i < Output.Count; i++) Output.RemoveAt(i);

        ConnectOutput(properties[ArrayIndex]);
    }
}

public abstract class BaseIndexedNodeViewModel : FlowNodeViewModel
{
    private int _arrayIndex = -1;
    public int ArrayIndex
    {
        get => _arrayIndex;
        set
        {
            if (CanUpdateArrayIndex(value) && !SetProperty(ref _arrayIndex, value))
                return;

            RaisePropertyChanged(nameof(DisplayIndex));
            NextCommand.RaiseCanExecuteChanged();
            PreviousCommand.RaiseCanExecuteChanged();

            Children.Clear();
            OnArrayIndexChanged();
        }
    }
    public int ArrayMax { get; }

    public DelegateCommand NextCommand { get; }
    public DelegateCommand PreviousCommand { get; }

    public BaseIndexedNodeViewModel(int arrayMax)
    {
        NextCommand = new DelegateCommand(() => ArrayIndex++, () => ArrayIndex < ArrayMax - 1);
        PreviousCommand = new DelegateCommand(() => ArrayIndex--, () => ArrayIndex > 0);

        ArrayMax = arrayMax;
    }

    public void Initialize()
    {
        ArrayIndex = 0;
    }

    protected abstract void OnArrayIndexChanged();

    protected override void PostConnectOutput(FlowNodeViewModel parent)
    {
        if (Output.Count <= 1)
        {
            base.PostConnectOutput(parent);
            Initialize();
        }
    }

    protected bool CanUpdateArrayIndex(int value) => value >= 0 && value < ArrayMax;

    public int DisplayIndex => ArrayIndex + 1;
}
