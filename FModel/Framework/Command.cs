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