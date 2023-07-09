using System.Text;
using System.Threading.Tasks;
using FModel.Framework;
using FModel.Services;
using FModel.ViewModels.ApiEndpoints.Models;

namespace FModel.ViewModels;

public class AboutViewModel : ViewModel
{
    private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;

    private string _descriptionLabel;
    public string DescriptionLabel
    {
        get => _descriptionLabel;
        set => SetProperty(ref _descriptionLabel, value);
    }

    private string _contributorsLabel;
    public string ContributorsLabel
    {
        get => _contributorsLabel;
        set => SetProperty(ref _contributorsLabel, value);
    }

    private string _donatorsLabel;
    public string DonatorsLabel
    {
        get => _donatorsLabel;
        set => SetProperty(ref _donatorsLabel, value);
    }

    private string _referencesLabel;
    public string ReferencesLabel
    {
        get => _referencesLabel;
        set => SetProperty(ref _referencesLabel, value);
    }

    public AboutViewModel()
    {

    }

    public async Task Initialize()
    {
        await Task.WhenAll(
            Task.Run(() =>
            {
                DescriptionLabel = "FModel is an archive explorer for Unreal Engine games that uses CUE4Parse as its core parsing library, providing robust support for the latest UE4 and UE5 archive formats. It aims to deliver a modern and intuitive user interface, powerful features, and a comprehensive set of tools for previewing and converting game packages, empowering YOU to understand games' inner workings with ease.";
                ContributorsLabel = $"FModel owes its continued existence to the passionate individuals who have generously contributed their time and expertise. Contributions from individuals such as {string.Join(", ", "GMatrixGames", "amr", "LongerWarrior", "MinshuG", "InTheShade", "Officer")}, and countless others, both in the past and those yet to come, ensure the continuous development and success of this project. If you are benefiting from FModel and would like to support its continued improvements, please consider making a donation.";
                ReferencesLabel = string.Join(", ",
                    "Adonis UI", "AutoUpdater.NET", "AvalonEdit", "CSCore", "CUE4Parse", "DiscordRichPresence",
                    "EpicManifestParser", "ImGui.NET", "K4os.Compression.LZ4", "Newtonsoft.Json", "NVorbis", "Oodle.NET",
                    "Ookii.Dialogs.Wpf", "OpenTK", "RestSharp", "Serilog", "SixLabors.ImageSharp", "SkiaSharp");
            }),
            Task.Run(() =>
            {
                var donators = _apiEndpointView.FModelApi.GetDonators();
                if (donators == null) return;

                var sb = new StringBuilder();
                sb.AppendJoin<Donator>(", ", donators);
                sb.Append('.');
                DonatorsLabel = sb.ToString();
            })
        ).ConfigureAwait(false);
    }
}
