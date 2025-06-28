using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace FModel.Views.Resources.Controls.Diff;

public partial class ExpandingNavbar
{
    private bool _isExpanded;
    private const double NavExpandedWidth = 200;
    private const double NavCollapsedWidth = 36;

    public ExpandingNavbar()
    {
        InitializeComponent();
        NavContent.Width = NavCollapsedWidth;
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleNavbar();
    }

    private void ToggleNavbar()
    {
        double from = _isExpanded ? NavExpandedWidth : NavCollapsedWidth;
        double to = _isExpanded ? NavCollapsedWidth : NavExpandedWidth;

        var anim = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(300)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };

        NavContent.BeginAnimation(WidthProperty, anim);

        double spacerFrom = _isExpanded ? 0 : 16;
        double spacerTo = _isExpanded ? 16 : 0;

        var spacerAnim = new DoubleAnimation(spacerFrom, spacerTo, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        Spacer.BeginAnimation(WidthProperty, spacerAnim);

        double fromAngle = _isExpanded ? 180 : 0;
        double toAngle = _isExpanded ? 0 : 180;

        var rotateAnim = new DoubleAnimation(fromAngle, toAngle, new Duration(TimeSpan.FromMilliseconds(300)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        ArrowRotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, rotateAnim);

        _isExpanded = !_isExpanded;
    }
}
