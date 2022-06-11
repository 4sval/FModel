using System;
using System.Diagnostics;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using I = Newtonsoft.Json.JsonIgnoreAttribute;

namespace FModel.ViewModels.ApiEndpoints.Models;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public class PlaylistResponse
{
    [J] public int Status { get; private set; }
    [J] public Playlist Data { get; private set; }
    [J] public string Error { get; private set; }

    public bool IsSuccess => Status == 200;
    public bool HasError => Error != null;

    private object DebuggerDisplay => IsSuccess ? Data : $"Error: {Status} | {Error}";
}

[DebuggerDisplay("{" + nameof(Id) + "}")]
public class Playlist
{
    [J] public string Id { get; private set; }
    [J] public PlaylistImages Images { get; private set; }
}

public class PlaylistImages
{
    [J] public Uri Showcase { get; private set; }
    [J] public Uri MissionIcon { get; private set; }

    [I] public bool HasShowcase => Showcase != null;
    [I] public bool HasMissionIcon => MissionIcon != null;
}