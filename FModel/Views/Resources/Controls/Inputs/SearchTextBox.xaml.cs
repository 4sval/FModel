using System.Windows;
using System.Windows.Controls;

namespace FModel.Views.Resources.Controls.Inputs;

public partial class SearchTextBox : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(SearchTextBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty WatermarkProperty =
        DependencyProperty.Register(nameof(Watermark), typeof(string), typeof(SearchTextBox),
            new PropertyMetadata("Search by name..."));

    public static readonly RoutedEvent ClearButtonClickEvent =
        EventManager.RegisterRoutedEvent(nameof(ClearButtonClick), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(SearchTextBox));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Watermark
    {
        get => (string)GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public event RoutedEventHandler ClearButtonClick
    {
        add => AddHandler(ClearButtonClickEvent, value);
        remove => RemoveHandler(ClearButtonClickEvent, value);
    }

    public SearchTextBox()
    {
        InitializeComponent();
    }

    private void OnClearButtonClick(object sender, RoutedEventArgs e)
    {
        Text = string.Empty;
        RaiseEvent(new RoutedEventArgs(ClearButtonClickEvent, this));
    }
}
