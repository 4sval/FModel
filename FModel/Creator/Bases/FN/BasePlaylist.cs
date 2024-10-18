using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FModel.Services;
using FModel.ViewModels;
using SkiaSharp;

namespace FModel.Creator.Bases.FN;

public class BasePlaylist : UCreator
{
    private ApiEndpointViewModel _apiEndpointView => ApplicationService.ApiEndpointView;
    private SKBitmap _missionIcon;

    public BasePlaylist(UObject uObject, EIconStyle style) : base(uObject, style)
    {
        Margin = 0;
        Width = 1024;
        Height = 512;
        Preview = Utils.GetBitmap("FortniteGame/Content/UI/Foundation/Textures/Tiles/T_Athena_Tile_Matchmaking_Default.T_Athena_Tile_Matchmaking_Default");
    }

    public override void ParseForInfo()
    {
        if (Object.TryGetValue(out FText displayName, "UIDisplayName", "DisplayName"))
            DisplayName = displayName.Text;
        if (Object.TryGetValue(out FText description, "UIDescription", "Description"))
            Description = description.Text;
        if (Object.TryGetValue(out UTexture2D missionIcon, "MissionIcon"))
            _missionIcon = Utils.GetBitmap(missionIcon).Resize(25);

        if (!Object.TryGetValue(out FName playlistName, "PlaylistName") || string.IsNullOrWhiteSpace(playlistName.Text))
            return;

        var playlist = _apiEndpointView.FortniteApi.GetPlaylist(playlistName.Text);
        if (!playlist.IsSuccess || playlist.Data.Images == null || !playlist.Data.Images.HasShowcase ||
            !_apiEndpointView.FortniteApi.TryGetBytes(playlist.Data.Images.Showcase, out var image))
            return;

        Preview = Utils.GetBitmap(image).ResizeWithRatio(1024, 512);
        Width = Preview.Width;
        Height = Preview.Height;
    }

    public override SKBitmap[] Draw()
    {
        var ret = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(ret);

        switch (Style)
        {
            case EIconStyle.NoBackground:
                DrawPreview(c);
                break;
            case EIconStyle.NoText:
                DrawPreview(c);
                DrawMissionIcon(c);
                break;
            default:
                DrawPreview(c);
                DrawTextBackground(c);
                DrawDisplayName(c);
                DrawDescription(c);
                DrawMissionIcon(c);
                break;
        }

        return new[] { ret };
    }

    private void DrawMissionIcon(SKCanvas c)
    {
        if (_missionIcon == null) return;
        c.DrawBitmap(_missionIcon, new SKPoint(5, 5), ImagePaint);
    }
}