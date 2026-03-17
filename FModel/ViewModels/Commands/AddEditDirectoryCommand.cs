using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using FModel.Framework;
using FModel.Settings;
using FModel.Views;
using Serilog;

namespace FModel.ViewModels.Commands;

public class AddEditDirectoryCommand : ViewModelCommand<CustomDirectoriesViewModel>
{
    public AddEditDirectoryCommand(CustomDirectoriesViewModel contextViewModel) : base(contextViewModel)
    {
    }

    public override async void Execute(CustomDirectoriesViewModel contextViewModel, object parameter)
    {
        try
        {
            var sourceDir = parameter as CustomDirectory ?? new CustomDirectory();
            var editableDir = new CustomDirectory(sourceDir.Header, sourceDir.DirectoryPath);

            var index = contextViewModel.GetIndex(sourceDir);
            var input = new CustomDir(editableDir);
            var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (owner == null)
            {
                Log.Warning("AddEditDirectoryCommand: no owner window available, cannot show dialog");
                return;
            }

            var result = await input.ShowDialog<bool?>(owner);
            Apply(result);

            void Apply(bool? dialogResult)
            {
                if (dialogResult is not true || string.IsNullOrEmpty(editableDir.Header) && string.IsNullOrEmpty(editableDir.DirectoryPath))
                    return;

                // Sync edits back to sourceDir so menu CommandParameters stay in sync
                sourceDir.Header = editableDir.Header;
                sourceDir.DirectoryPath = editableDir.DirectoryPath;

                if (index > 1)
                    contextViewModel.Edit(index, sourceDir);
                else
                    contextViewModel.Add(sourceDir);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AddEditDirectoryCommand failed");
        }
    }
}
