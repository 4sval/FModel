using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using FModel.Extensions;
using FModel.Framework;
using FModel.Views.Resources.Controls;

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
        FLogger.Append(ELog.Information, () =>
            FLogger.Text($"Fortnite has been loaded successfully in {contextViewModel.CUE4Parse.InternalGameName}ms", Constants.WHITE, true));
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
            case "Reference":
                foreach (var asset in assetItems) sb.AppendLine($"/Game/{asset.FullPath.SubstringBeforeLast('.').Substring(contextViewModel.CUE4Parse.InternalGameName.Length + ("Content/").Length + 1)}");
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
