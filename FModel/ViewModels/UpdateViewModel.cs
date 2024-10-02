using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.Views.Resources.Converters;

namespace FModel.ViewModels;

public class UpdateViewModel : ViewModel
{
    private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;

    public RangeObservableCollection<GitHubCommit> Commits { get; }
    public ICollectionView CommitsView { get; }

    public UpdateViewModel()
    {
        Commits = new RangeObservableCollection<GitHubCommit>();
        CommitsView = new ListCollectionView(Commits)
        {
            GroupDescriptions = { new PropertyGroupDescription("Commit.Author.Date", new DateTimeToDateConverter()) }
        };
    }

    public async Task Load()
    {
#if DEBUG
        Commits.AddRange(Newtonsoft.Json.JsonConvert.DeserializeObject<GitHubCommit[]>(await System.IO.File.ReadAllTextAsync(@"C:\Users\valen\Downloads\history.json")));
        var qa = Newtonsoft.Json.JsonConvert.DeserializeObject<GitHubRelease>(await System.IO.File.ReadAllTextAsync(@"C:\Users\valen\Downloads\qa.json"));
#else
        Commits.AddRange(await _apiEndpointView.GitHubApi.GetCommitHistoryAsync());
        var qa = await _apiEndpointView.GitHubApi.GetReleaseAsync("qa");
#endif

        qa.Assets.OrderByDescending(x => x.CreatedAt).First().IsLatest = true;
        foreach (var asset in qa.Assets)
        {
            var commitSha = asset.Name.SubstringBeforeLast(".zip");
            var commit = Commits.FirstOrDefault(x => x.Sha == commitSha);
            if (commit != null)
            {
                commit.Asset = asset;
            }
        }
    }
}
