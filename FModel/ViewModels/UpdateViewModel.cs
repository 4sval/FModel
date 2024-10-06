using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.ViewModels.Commands;
using FModel.Views.Resources.Converters;

namespace FModel.ViewModels;

public class UpdateViewModel : ViewModel
{
    private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;

    private RemindMeCommand _remindMeCommand;
    public RemindMeCommand RemindMeCommand => _remindMeCommand ??= new RemindMeCommand(this);

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
        Commits.AddRange(await _apiEndpointView.GitHubApi.GetCommitHistoryAsync());

        var qa = await _apiEndpointView.GitHubApi.GetReleaseAsync("qa");
        qa.Assets.OrderByDescending(x => x.CreatedAt).First().IsLatest = true;

        foreach (var asset in qa.Assets)
        {
            var commitSha = asset.Name.SubstringBeforeLast(".zip");
            var commit = Commits.FirstOrDefault(x => x.Sha == commitSha);
            if (commit != null)
            {
                commit.Asset = asset;
            }
            else
            {
                Commits.Add(new GitHubCommit
                {
                    Sha = commitSha,
                    Commit = new Commit
                    {
                        Message = "No commit message",
                        Author = new Author { Name = asset.Uploader.Login, Date = asset.CreatedAt }
                    },
                    Author = asset.Uploader,
                    Asset = asset
                });
            }
        }
    }

    public void DownloadLatest()
    {
        Commits.FirstOrDefault(x => x.Asset.IsLatest)?.Download();
    }
}
