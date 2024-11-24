using System;
using System.Windows.Input;

namespace FModel.Framework;

public abstract class Command : ICommand
{
    public abstract void Execute(object parameter);

    public abstract bool CanExecute(object parameter);

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler CanExecuteChanged;
}

public class DelegateCommand : Command
{
    private readonly Action _action;
    private readonly Func<bool>? _condition;

    public DelegateCommand(Action action, Func<bool>? executeCondition = default)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _condition = executeCondition;
    }

    public override void Execute(object parameter) => _action();
    public override bool CanExecute(object parameter) => _condition?.Invoke() ?? true;
}
