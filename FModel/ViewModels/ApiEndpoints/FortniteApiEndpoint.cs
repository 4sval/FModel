using System;
using FModel.ViewModels.ApiEndpoints.Models;
using RestSharp;
using System.Threading.Tasks;
using FModel.Framework;

namespace FModel.ViewModels.ApiEndpoints;

public class FortniteApiEndpoint : AbstractApiProvider
{
    public FortniteApiEndpoint(RestClient client) : base(client)
    {
    }

    public async Task<PlaylistResponse> GetPlaylistAsync(string playlistId)
    {
        var request = new FRestRequest($"https://fortnite-api.com/v1/playlists/{playlistId}");
        var response = await _client.ExecuteAsync<PlaylistResponse>(request).ConfigureAwait(false);
        return response.Data;
    }

    public PlaylistResponse GetPlaylist(string playlistId)
    {
        return GetPlaylistAsync(playlistId).GetAwaiter().GetResult();
    }

    public bool TryGetBytes(Uri link, out byte[] data)
    {
        var request = new FRestRequest(link);
        data = _client.DownloadData(request);
        return data != null;
    }
}
