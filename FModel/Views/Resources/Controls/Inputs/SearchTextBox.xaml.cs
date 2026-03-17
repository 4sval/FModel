using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FModel.Views.Resources.Controls.Inputs;

public partial class SearchTextBox : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<SearchTextBox, string>(nameof(Text), defaultValue: string.Empty,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<SearchTextBox, string>(nameof(Watermark), defaultValue: "Search by name...");

    public static readonly RoutedEvent<RoutedEventArgs> ClearButtonClickEvent =
        RoutedEvent.Register<SearchTextBox, RoutedEventArgs>(nameof(ClearButtonClick), RoutingStrategies.Bubble);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public event EventHandler<RoutedEventArgs> ClearButtonClick
    {
        add => AddHandler(ClearButtonClickEvent, value);
        remove => RemoveHandler(ClearButtonClickEvent, value);
    }

    public SearchTextBox()
    {
        InitializeComponent();
    }

    private void OnClearButtonClick(object? sender, RoutedEventArgs e)
    {
        Text = string.Empty;
        RaiseEvent(new RoutedEventArgs(ClearButtonClickEvent, this));
    }
}
