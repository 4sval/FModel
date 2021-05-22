using FModel.Framework;
using FModel.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FModel.Settings;

namespace FModel.ViewModels
{
    public class LoadingModesViewModel : ViewModel
    {
        private LoadCommand _loadCommand;
        public LoadCommand LoadCommand => _loadCommand ??= new LoadCommand(this);

        public ReadOnlyObservableCollection<ELoadingMode> Modes { get; }

        public LoadingModesViewModel()
        {
            Modes = new ReadOnlyObservableCollection<ELoadingMode>(new ObservableCollection<ELoadingMode>(EnumerateLoadingModes()));
        }

        private IEnumerable<ELoadingMode> EnumerateLoadingModes() => Enum.GetValues(UserSettings.Default.LoadingMode.GetType()).Cast<ELoadingMode>();
    }
}