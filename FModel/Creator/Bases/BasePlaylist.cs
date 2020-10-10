using System;
using System.Windows;

using FModel.Creator.Texts;
using FModel.Utils;

using Fortnite_API.Objects;
using Fortnite_API.Objects.V1;

using PakReader.Parsers.Class;
using PakReader.Parsers.PropertyTagData;

using SkiaSharp;

namespace FModel.Creator.Bases
{
    public class BasePlaylist : IBase
    {
        public SKBitmap FallbackImage { get; }
        public SKBitmap IconImage { get; }
        public SKColor[] RarityBackgroundColors { get; }
        public SKColor[] RarityBorderColor { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int Width { get; }
        public int Height { get; }
        public int Margin { get; } = 2;

        public BasePlaylist(IUExport export)
        {
            FallbackImage = SKBitmap.Decode(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/T_Placeholder_Item_Image.png"))?.Stream);
            IconImage = FallbackImage;
            RarityBackgroundColors = new[] { SKColor.Parse("5EBC36"), SKColor.Parse("305C15") };
            RarityBorderColor = new[] { SKColor.Parse("74EF52"), SKColor.Parse("74EF52") };

            if (export.GetExport<TextProperty>("UIDisplayName", "DisplayName") is { } displayName)
                DisplayName = Text.GetTextPropertyBase(displayName);
            if (export.GetExport<TextProperty>("UIDescription", "Description") is { } description)
                Description = Text.GetTextPropertyBase(description);

            Width = Height = 512;

            if (export.GetExport<NameProperty>("PlaylistName") is { } playlistName && !playlistName.Value.IsNone)
            {
                ApiResponse<PlaylistV1> playlist = Endpoints.FortniteAPI.V1.Playlists.Get(playlistName.Value.String);

                if (playlist.IsSuccess && playlist.Data.Images.HasShowcase)
                {
                    byte[] imageBytes = Endpoints.GetRawData(playlist.Data.Images.Showcase);

                    if (imageBytes != null)
                    {
                        IconImage = SKBitmap.Decode(imageBytes);
                        Width = IconImage.Width;
                        Height = IconImage.Height;
                    }
                }
            }
        }
    }
}
