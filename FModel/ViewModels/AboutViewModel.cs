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
                DescriptionLabel = "FModel Linux is an unofficial Linux port of FModel (https://github.com/4sval/FModel), originally developed by Asval and contributors. It is an archive explorer for Unreal Engine games that uses CUE4Parse as its core parsing library, providing robust support for the latest UE4 and UE5 archive formats, along with a comprehensive set of tools for previewing and converting game packages.";
                ContributorsLabel = $"The Linux port is maintained by r6e. The underlying FModel project owes its continued existence to Asval and the passionate contributors who have generously given their time and expertise, including {string.Join(", ", "GMatrixGames", "amr", "LongerWarrior", "MinshuG", "InTheShade", "Officer")}, and many others. If you are benefiting from FModel and would like to support its continued improvements, please consider donating to the upstream project.";
                ReferencesLabel = string.Join(", ",
                    "Avalonia", "AvaloniaEdit", "CUE4Parse", "DiscordRichPresence",
                    "EpicManifestParser", "K4os.Compression.LZ4", "Newtonsoft.Json", "NVorbis", "Oodle.NET",
                    "OpenTK", "RestSharp", "Serilog", "SixLabors.ImageSharp", "SkiaSharp",
                    "Svg.Skia", "Twizzle.ImGui-Bundle.NET");
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
