using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using CUE4Parse.Utils;
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

        var assetItems = ((IList) parameters[1]).Cast<AssetItem>().ToArray();
        if (!assetItems.Any()) return;

        var sb = new StringBuilder();
        switch (trigger)
        {
            case "File_Path":
                foreach (var asset in assetItems) sb.AppendLine(asset.FullPath);
                break;
            case "File_Name":
                foreach (var asset in assetItems) sb.AppendLine(asset.FullPath.SubstringAfterLast('/'));
                break;
            case "Directory_Path":
                foreach (var asset in assetItems) sb.AppendLine(asset.FullPath.SubstringBeforeLast('/'));
                break;
            case "File_Path_No_Extension":
                foreach (var asset in assetItems) sb.AppendLine(asset.FullPath.SubstringBeforeLast('.'));
                break;
            case "File_Name_No_Extension":
                foreach (var asset in assetItems) sb.AppendLine(asset.FullPath.SubstringAfterLast('/').SubstringBeforeLast('.'));
                break;
        }

        Clipboard.SetText(sb.ToString().TrimEnd());
    }
}
