using System.Windows;
using System.Windows.Controls;
using FModel.Services;

namespace FModel.Views;

public partial class PubgKeyPrompt
{
    private PubgKeyPrompt(uint slot, bool replacing, string? previousValue = null)
    {
        InitializeComponent();
        Title = $"PUBG Mobile Dynamic Key — Slot {slot} (0x{slot:X})";
        SlotText.Text = $"{slot} (0x{slot:X})";
        if (replacing)
        {
            HeadingText.Text = "The saved dynamic key did not work";
            DescriptionText.Text = "FModel could not decrypt this PUBG Mobile archive with the saved key for this slot. Replace it below. Saving retries the archive immediately; no restart is needed.";
            KeyBox.Text = previousValue ?? string.Empty;
            KeyBox.SelectAll();
        }
        KeyBox.Focus();
    }

    public static string? Ask(uint slot)
    {
        var prompt = new PubgKeyPrompt(slot, replacing: false);
        if (Application.Current?.MainWindow is { IsLoaded: true } owner)
            prompt.Owner = owner;
        return prompt.ShowDialog() == true ? prompt.KeyBox.Text.Trim() : null;
    }

    public static string? Replace(uint slot, string? previousValue)
    {
        var prompt = new PubgKeyPrompt(slot, replacing: true, previousValue);
        if (Application.Current?.MainWindow is { IsLoaded: true } owner)
            prompt.Owner = owner;
        return prompt.ShowDialog() == true ? prompt.KeyBox.Text.Trim() : null;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (!PubgMobileKeyService.IsValid(KeyBox.Text))
        {
            ErrorText.Visibility = Visibility.Visible;
            KeyBox.Focus();
            KeyBox.SelectAll();
            return;
        }

        DialogResult = true;
    }

    private void OnKeyChanged(object sender, TextChangedEventArgs e)
    {
        if (ErrorText.Visibility == Visibility.Visible)
            ErrorText.Visibility = Visibility.Collapsed;
    }
}
