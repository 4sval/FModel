using System;
using FModel.ViewModels.ApiEndpoints.Models;
using RestSharp;
using System.Threading.Tasks;

namespace FModel.ViewModels.ApiEndpoints;

public class FortniteApiEndpoint : AbstractApiProvider
{
    public FortniteApiEndpoint(IRestClient client) : base(client)
    {
    }

    public async Task<PlaylistResponse> GetPlaylistAsync(string playlistId)
    {
        var request = new RestRequest($"https://fortnite-api.com/v1/playlists/{playlistId}", Method.GET);
        var response = await _client.ExecuteAsync<PlaylistResponse>(request).ConfigureAwait(false);
        return response.Data;
    }

    public PlaylistResponse GetPlaylist(string playlistId)
    {
        return GetPlaylistAsync(playlistId).GetAwaiter().GetResult();
    }

    public bool TryGetBytes(Uri link, out byte[] data)
    {
        var request = new RestRequest(link, Method.GET);
        data = _client.DownloadData(request);
        return data != null;
    }
}