using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects.Properties;
using FModel.Framework;

namespace FModel.ViewModels.Nodify;

public class IndexedNodeViewModel : BaseIndexedNodeViewModel
{
    public IndexedNodeViewModel(List<FPropertyTagType> properties) : base(properties.Count) { }

    protected override void OnArrayIndexChanged()
    {

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
            if (!SetProperty(ref _arrayIndex, value)) return;

            RaisePropertyChanged(nameof(DisplayIndex));
            NextCommand.RaiseCanExecuteChanged();
            PreviousCommand.RaiseCanExecuteChanged();

            Input.Clear();
            Output.Clear();
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
        ArrayIndex = 0;
    }

    protected abstract void OnArrayIndexChanged();

    public int DisplayIndex => ArrayIndex + 1;
}
