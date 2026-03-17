using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace FModel.Views.Resources.Converters;

/// <summary>
/// Converts an HTTP(S) URL string to an Avalonia <see cref="Bitmap"/>.
/// Converts only from cached images and never performs blocking network I/O
/// during binding evaluation.
/// </summary>
/// <remarks>
/// Use <see cref="LoadAsync"/> from ViewModel code to preload and cache images.
/// </remarks>
public class UrlToBitmapConverter : IValueConverter
{
    public static readonly UrlToBitmapConverter Instance = new();
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly ConcurrentDictionary<string, Bitmap?> _cache = new();
    private static readonly ConcurrentDictionary<string, Task<Bitmap?>> _inFlight = new();

    public static bool TryGetCached(string url, out Bitmap? bitmap)
    {
        return _cache.TryGetValue(url, out bitmap);
    }

    public static async Task<Bitmap?> LoadAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (_cache.TryGetValue(url, out var cached))
            return cached;

        try
        {
            return await _inFlight.GetOrAdd(url, DownloadAsync);
        }
        finally
        {
            _inFlight.TryRemove(url, out _);
        }
    }

    private static async Task<Bitmap?> DownloadAsync(string url)
    {
        try
        {
            using var stream = await _http.GetStreamAsync(url);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            var bitmap = new Bitmap(ms);
            _cache[url] = bitmap;
            return bitmap;
        }
        catch
        {
            _cache.TryRemove(url, out _);
            return null;
        }
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;

        return _cache.TryGetValue(url, out var cached) ? cached : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
