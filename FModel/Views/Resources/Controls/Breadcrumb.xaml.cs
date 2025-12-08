using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FModel.Services;

namespace FModel.Views.Resources.Controls;

public partial class Breadcrumb
{
    private const string NAVIGATE_NEXT = "M0,0 L6,6 L0,12 Z";

    public Breadcrumb()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not string pathAtThisPoint)
            return;
        InMeDaddy.Children.Clear();

        var folders = pathAtThisPoint.Split('/');

        for (int i = 0; i < folders.Length; i++)
        {
            var folderBorder = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 4, 0),
                Tag = i + 1
            };

            var folderText = new TextBlock
            {
                Text = folders[i],
                Foreground = i == folders.Length - 1 ? Brushes.White : Brushes.LightGray,
                FontWeight = i == folders.Length - 1 ? FontWeights.Bold : FontWeights.Normal
            };

            folderBorder.Child = folderText;

            if (i != folders.Length - 1)
            {
                folderBorder.Cursor = Cursors.Hand;
                folderBorder.MouseEnter += (_, _) =>
                {
                    folderBorder.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
                };
                folderBorder.MouseLeave += (_, _) => folderBorder.Background = Brushes.Transparent;
                folderBorder.MouseUp += OnFolderClick;
            }

            InMeDaddy.Children.Add(folderBorder);

            if (i < folders.Length - 1)
            {
                InMeDaddy.Children.Add(new Viewbox
                {
                    Width = 4,
                    Height = 8,
                    Margin = new Thickness(0, 0, 4, 0),
                    Child = new Path
                    {
                        Fill = Brushes.LightGray,
                        Data = Geometry.Parse(NAVIGATE_NEXT),
                        Stretch = Stretch.Uniform
                    }
                });
            }
        }
    }

    private void OnFolderClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: int index } folder)
            return;
        if (folder.Child is not TextBlock textBlock)
            return;

        var pathAtThisPoint = string.Join('/', InMeDaddy.Children
            .OfType<Border>()
            .Select(b => ((TextBlock) b.Child).Text));

        var directories = pathAtThisPoint.Split('/');
        var targetDirectory = string.Join('/', directories.Take(index));

        if (pathAtThisPoint.Equals(targetDirectory))
            return;

        ApplicationService.ApplicationView.CustomDirectories.GoToCommand.JumpTo(targetDirectory);
    }
}
