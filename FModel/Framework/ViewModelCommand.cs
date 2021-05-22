using System;

namespace FModel.Framework
{
    public abstract class ViewModelCommand<TContextViewModel> : Command where TContextViewModel : ViewModel
    {
        private WeakReference _parent;

        public TContextViewModel ContextViewModel
        {
            get
            {
                if (_parent is {IsAlive: true})
                    return (TContextViewModel) _parent.Target;

                return null;
            }

            private set
            {
                if (ContextViewModel == value)
                    return;

                _parent = value != null ? new WeakReference(value) : null;
            }
        }

        protected ViewModelCommand(TContextViewModel contextViewModel)
        {
            ContextViewModel = contextViewModel /*?? throw new ArgumentNullException(nameof(contextViewModel))*/;
        }

        public sealed override void Execute(object parameter)
        {
            Execute(ContextViewModel, parameter);
        }

        public abstract void Execute(TContextViewModel contextViewModel, object parameter);

        public sealed override bool CanExecute(object parameter)
        {
            return CanExecute(ContextViewModel, parameter);
        }

        public virtual bool CanExecute(TContextViewModel contextViewModel, object parameter)
        {
            return true;
        }
    }
}