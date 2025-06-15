using System.Windows;
using System.Windows.Media;

namespace FModel.Views.Resources.Controls.Diff;

public partial class DiffHeader
{
    public static readonly DependencyProperty LeftIconProperty =
        DependencyProperty.Register(nameof(LeftIcon), typeof(Geometry), typeof(DiffHeader));
    public Geometry LeftIcon
    {
        get => (Geometry) GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }

    public static readonly DependencyProperty RightIconProperty =
        DependencyProperty.Register(nameof(RightIcon), typeof(Geometry), typeof(DiffHeader));
    public Geometry RightIcon
    {
        get => (Geometry) GetValue(RightIconProperty);
        set => SetValue(RightIconProperty, value);
    }

    public DiffHeader()
    {
        InitializeComponent();
    }
}
