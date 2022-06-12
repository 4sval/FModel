﻿using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FModel.Services;

namespace FModel.Views.Resources.Controls;

public partial class Breadcrumb
{
    private const string _NAVIGATE_NEXT = "M9.31 6.71c-.39.39-.39 1.02 0 1.41L13.19 12l-3.88 3.88c-.39.39-.39 1.02 0 1.41.39.39 1.02.39 1.41 0l4.59-4.59c.39-.39.39-1.02 0-1.41L10.72 6.7c-.38-.38-1.02-.38-1.41.01z";

    public Breadcrumb()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not string pathAtThisPoint) return;
        InMeDaddy.Children.Clear();

        var folders = pathAtThisPoint.Split('/');
        for (var i = 0; i < folders.Length; i++)
        {
            var textBlock = new TextBlock
            {
                Text = folders[i],
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Tag = i + 1,
                Margin = new Thickness(0, 3, 0, 0)
            };
            textBlock.MouseUp += OnMouseClick;

            InMeDaddy.Children.Add(textBlock);
            if (i >= folders.Length - 1) continue;

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
                        new Path
                        {
                            Fill = Brushes.White,
                            Data = Geometry.Parse(_NAVIGATE_NEXT)
                        }
                    }
                }
            });
        }
    }

    private void OnMouseClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { DataContext: string pathAtThisPoint, Tag: int index }) return;

        var directory = string.Join('/', pathAtThisPoint.Split('/').Take(index));
        if (pathAtThisPoint.Equals(directory)) return;

        ApplicationService.ApplicationView.CustomDirectories.GoToCommand.JumpTo(directory);
    }
}