using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace FModel.Views.Resources.Converters;

public class AssetExtensionToIconConverter : IValueConverter
{
  public static readonly AssetExtensionToIconConverter Instance = new();

  private static readonly Lazy<IReadOnlyDictionary<string, Bitmap>> CachedIcons = new(CreateIcons);

  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    var extension = value as string ?? string.Empty;
    return CachedIcons.Value.TryGetValue(extension, out var icon)
        ? icon
        : CachedIcons.Value[string.Empty];
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
      => throw new NotImplementedException();

  private static IReadOnlyDictionary<string, Bitmap> CreateIcons()
  {
    return new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase)
    {
      [string.Empty] = LoadBitmap("unknown_asset.png"),
      ["uasset"] = LoadBitmap("asset.png"),
      ["ini"] = LoadBitmap("asset_ini.png"),
      ["png"] = LoadBitmap("asset_png.png"),
      ["psd"] = LoadBitmap("asset_psd.png")
    };
  }

  private static Bitmap LoadBitmap(string fileName)
  {
    using var stream = AssetLoader.Open(new Uri($"avares://FModel/Resources/{fileName}"));
    return new Bitmap(stream);
  }
}
