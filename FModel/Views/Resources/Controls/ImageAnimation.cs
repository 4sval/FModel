using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CUE4Parse.UE4.Assets.Exports.Paper2D;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.Textures.Paper2D;

namespace FModel.Views.Resources.Controls;

public static class ImageAnimation
{
    public sealed record Frame(BitmapSource Image, TimeSpan Duration);

    public static readonly DependencyProperty FramesProperty = DependencyProperty.RegisterAttached("Frames", typeof(IReadOnlyList<Frame>), typeof(ImageAnimation), new PropertyMetadata(null, OnFramesChanged));

    private static readonly DependencyProperty _playbackProperty = DependencyProperty.RegisterAttached("Playback", typeof(Playback), typeof(ImageAnimation));

    public static IReadOnlyList<Frame> GetFrames(DependencyObject image) => (IReadOnlyList<Frame>) image.GetValue(FramesProperty);
    public static void SetFrames(DependencyObject image, IReadOnlyList<Frame> frames) => image.SetValue(FramesProperty, frames);

    public static BitmapSource CreateSource(IReadOnlyList<Frame> frames)
    {
        if (frames is not { Count: > 0 })
            return null;
        if (frames.Count == 1)
            return frames[0].Image;

        var source = frames[0].Image.Clone();
        SetFrames(source, frames);
        source.Freeze();
        return source;
    }

    internal static Frame[] CreateFrames(CFlipbook flipbook, int maxSize = 0, CancellationToken cancellationToken = default)
    {
        using var animation = flipbook.ToImage(new ExportOptions(), maxSize: maxSize, cancellationToken: cancellationToken);
        var frames = new Frame[animation.Frames.Count];
        var minimumDuration = maxSize > 0 ? 1.0 / 15 : 0.001;
        for (var i = 0; i < frames.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var frame = animation.Frames[i];
            var pixels = new byte[frame.Width * frame.Height * 4];
            frame.CopyPixelDataTo(pixels);
            for (var pixel = 0; pixel < pixels.Length; pixel += 4)
            {
                (pixels[pixel], pixels[pixel + 2]) = (pixels[pixel + 2], pixels[pixel]);
            }

            var image = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null, pixels, frame.Width * 4);
            image.Freeze();

            var seconds = flipbook.FramesPerSecond > 0 ? flipbook.Frames[i].FrameRun / (double) flipbook.FramesPerSecond : 0;
            frames[i] = new Frame(image, TimeSpan.FromSeconds(Math.Max(minimumDuration, seconds)));
        }

        return frames;
    }

    // Only light animations, otherwise use the first frame as a thumbnail
    internal static BitmapSource CreateThumbnail(UPaperFlipbook flipbook, ExportOptions options, int maxSize)
    {
        var cache = new Dictionary<string, CTexture>();
        if (flipbook.FramesPerSecond > 0 && flipbook.KeyFrames.Length is > 1 and <= 64)
        {
            var pixels = flipbook.KeyFrames.Where(frame => frame.FrameRun > 0).Select(frame => frame.LoadSprite()).OfType<UPaperSprite>().Distinct()
                .Sum(sprite => (double) sprite.BakedSourceDimension.X * sprite.BakedSourceDimension.Y);
            if (pixels <= 16 * 1024 * 1024 && flipbook.Decode(options, cache) is { } decoded)
            {
                var minimumRun = Math.Max(1, (int) Math.Ceiling(decoded.FramesPerSecond / 15));
                var frames = new List<CFlipbookFrame>();
                foreach (var frame in decoded.Frames)
                {
                    if (frames.Count > 0 && frames[^1].FrameRun < minimumRun)
                    {
                        var previous = frames[^1];
                        frames[^1] = new CFlipbookFrame(previous.Texture, previous.FrameRun + frame.FrameRun, previous.Offset);
                    }
                    else
                    {
                        frames.Add(frame);
                    }
                }

                return CreateSource(CreateFrames(new CFlipbook(frames, decoded.FramesPerSecond), maxSize));
            }
        }

        var first = flipbook.KeyFrames.Where(frame => frame.FrameRun > 0).Select(frame => frame.LoadSprite()?.Decode(options, cache)).FirstOrDefault(texture => texture != null);
        return first == null ? null : CreateSource(CreateFrames(new CFlipbook([new CFlipbookFrame(first)], 1), maxSize));
    }

    private static void OnFramesChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not Image image)
            return;

        (image.GetValue(_playbackProperty) as Playback)?.Dispose();
        image.ClearValue(_playbackProperty);
        if (args.NewValue is not IReadOnlyList<Frame> { Count: > 0 } frames)
            return;

        image.SetCurrentValue(Image.SourceProperty, frames[0].Image);
        if (frames.Count > 1)
        {
            image.SetValue(_playbackProperty, new Playback(image, frames));
        }
    }

    private sealed class Playback : IDisposable
    {
        private readonly Image _image;
        private readonly IReadOnlyList<Frame> _frames;
        private readonly DispatcherTimer _timer;
        private int _index;

        public Playback(Image image, IReadOnlyList<Frame> frames)
        {
            _image = image;
            _frames = frames;
            _timer = new DispatcherTimer(DispatcherPriority.Render, image.Dispatcher) { Interval = frames[0].Duration };
            _timer.Tick += (_, _) =>
            {
                _index = (_index + 1) % _frames.Count;
                _image.SetCurrentValue(Image.SourceProperty, _frames[_index].Image);
                _timer.Interval = _frames[_index].Duration;
            };
            image.Loaded += UpdatePlayback;
            image.Unloaded += OnUnloaded;
            image.IsVisibleChanged += OnIsVisibleChanged;
            UpdatePlayback(image, null);
        }

        private void UpdatePlayback(object sender, RoutedEventArgs args)
        {
            if (_image.IsLoaded && _image.IsVisible)
            {
                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs args) => _timer.Stop();
        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args) => UpdatePlayback(sender, null);

        public void Dispose()
        {
            _timer.Stop();
            _image.Loaded -= UpdatePlayback;
            _image.Unloaded -= OnUnloaded;
            _image.IsVisibleChanged -= OnIsVisibleChanged;
        }
    }
}
