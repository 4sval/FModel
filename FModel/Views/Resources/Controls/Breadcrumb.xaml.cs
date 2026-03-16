using System.Linq;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using FModel.Services;

namespace FModel.Views.Resources.Controls;

public partial class Breadcrumb
{
    private const string NavigateNext = "M9.31 6.71c-.39.39-.39 1.02 0 1.41L13.19 12l-3.88 3.88c-.39.39-.39 1.02 0 1.41.39.39 1.02.39 1.41 0l4.59-4.59c.39-.39.39-1.02 0-1.41L10.72 6.7c-.38-.38-1.02-.38-1.41.01z";

    public Breadcrumb()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not string pathAtThisPoint)
            return;
        InMeDaddy.Children.Clear();

        var folders = pathAtThisPoint.Split('/');
        for (var i = 0; i < folders.Length; i++)
        {
            var capturedIndex = i + 1;
            var capturedPath = pathAtThisPoint;

            var border = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Transparent,
                Background = Brushes.Transparent,
                Padding = new Thickness(6, 3, 6, 3),
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = capturedIndex,
                IsEnabled = i < folders.Length - 1,
                Child = new TextBlock
                {
                    Text = folders[i],
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            border.PointerEntered += (_, _) =>
            {
                border.BorderBrush = new ImmutableSolidColorBrush(Color.FromRgb(127, 127, 144));
                border.Background = new ImmutableSolidColorBrush(Color.FromRgb(72, 73, 92));
            };
            border.PointerExited += (_, _) =>
            {
                border.BorderBrush = Brushes.Transparent;
                border.Background = Brushes.Transparent;
            };
            border.PointerReleased += (_, args) =>
            {
                if (args.InitialPressMouseButton != Avalonia.Input.MouseButton.Left)
                    return;
                var directory = string.Join('/', capturedPath.Split('/').Take(capturedIndex));
                if (capturedPath.Equals(directory))
                    return;
                ApplicationService.ApplicationView.CustomDirectories?.GoToCommand.JumpTo(directory);
            };

            InMeDaddy.Children.Add(border);
            if (i >= folders.Length - 1)
                continue;

            InMeDaddy.Children.Add(new Viewbox
            {
                Width = 16,
                Height = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = new Canvas
                {
                    Width = 24,
                    Height = 24,
                    Children =
                    {
                        new Avalonia.Controls.Shapes.Path
                        {
                            Fill = Brushes.White,
                            Data = Geometry.Parse(NavigateNext),
                            Opacity = 0.6
                        }
                    }
                }
            });
        }
    }
}
