using System.Threading.Tasks;
using FModel.Framework;
using FModel.ViewModels.ApiEndpoints.Models;
using RestSharp;

namespace FModel.ViewModels.ApiEndpoints;

public class GitHubApiEndpoint : AbstractApiProvider
{
    public GitHubApiEndpoint(RestClient client) : base(client) { }

    public async Task<GitHubCommit[]> GetCommitHistoryAsync(string branch = "dev", int page = 1, int limit = 20)
    {
        var request = new FRestRequest(Constants.GH_COMMITS_HISTORY);
        request.AddParameter("sha", branch);
        request.AddParameter("page", page);
        request.AddParameter("per_page", limit);
        var response = await _client.ExecuteAsync<GitHubCommit[]>(request).ConfigureAwait(false);
        return response.Data;
    }

    public async Task<GitHubRelease> GetReleaseAsync(string tag)
    {
        var request = new FRestRequest($"{Constants.GH_RELEASES}/tags/{tag}");
        var response = await _client.ExecuteAsync<GitHubRelease>(request).ConfigureAwait(false);
        return response.Data;
    }
}
