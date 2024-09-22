using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using FModel.Framework;
using FModel.Services;
using FModel.ViewModels.ApiEndpoints.Models;
using FModel.Views.Resources.Converters;

namespace FModel.ViewModels;

public class UpdateViewModel : ViewModel
{
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
        Commits.AddRange(await ApplicationService.ApiEndpointView.FModelApi.GetGitHubCommitHistoryAsync());
    }
}
