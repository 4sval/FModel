using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using CUE4Parse.FileProvider.Objects;
using FModel.Framework;

namespace FModel.ViewModels.Commands;

public class CopyCommand : ViewModelCommand<ApplicationViewModel>
{
    public CopyCommand(ApplicationViewModel contextViewModel) : base(contextViewModel)
    {
    }

    public override void Execute(ApplicationViewModel contextViewModel, object parameter)
    {
        if (parameter is not object[] parameters || parameters[0] is not string trigger)
            return;

        var entries = ((IList) parameters[1]).Cast<GameFile>().ToArray();
        if (!entries.Any()) return;

        var sb = new StringBuilder();
        switch (trigger)
        {
            case "File_Path":
                foreach (var entry in entries) sb.AppendLine(entry.Path);
                break;
            case "File_Name":
                foreach (var entry in entries) sb.AppendLine(entry.Name);
                break;
            case "Directory_Path":
                foreach (var entry in entries) sb.AppendLine(entry.Directory);
                break;
            case "File_Path_No_Extension":
                foreach (var entry in entries) sb.AppendLine(entry.PathWithoutExtension);
                break;
            case "File_Name_No_Extension":
                foreach (var entry in entries) sb.AppendLine(entry.NameWithoutExtension);
                break;
        }

        Clipboard.SetText(sb.ToString().TrimEnd());
    }
}
