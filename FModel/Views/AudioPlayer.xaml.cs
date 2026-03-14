using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
// TODO(P3-013): CSCore.CoreAudioAPI not available on Linux — swap out with cross-platform audio device API
// using CSCore.CoreAudioAPI;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;
// TODO(P4-004): Microsoft.Win32.OpenFileDialog not available on Linux — replace with StorageProvider
// using Microsoft.Win32;

namespace FModel.Views;

public partial class AudioPlayer : Window
{
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public AudioPlayer()
    {
        DataContext = _applicationView;
        InitializeComponent();
    }

    public void Load(byte[] data, string filePath)
    {
        _applicationView.AudioPlayer.AddToPlaylist(data, filePath);
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        _applicationView.AudioPlayer.Stop();
        _applicationView.AudioPlayer.Dispose();
        DiscordService.DiscordHandler.UpdateToSavedPresence();
    }

    private void OnDeviceSwap(object sender, SelectionChangedEventArgs e)
    {
        // TODO(P3-013): MMDevice (CSCore) not available on Linux — implement with cross-platform audio device API
        if (sender is not ComboBox comboBox || comboBox.SelectedItem is null)
            return;

        _applicationView.AudioPlayer.Device();
    }

    private void OnVolumeChange(object sender, RoutedEventArgs e)
    {
        _applicationView.AudioPlayer.Volume();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Source is TextBox)
            return;

        if (UserSettings.Default.AddAudio.IsTriggered(e.Key))
        {
            // TODO(P4-004): OpenFileDialog (Microsoft.Win32) not available on Linux — replace with StorageProvider
        }
        else if (UserSettings.Default.PlayPauseAudio.IsTriggered(e.Key))
            _applicationView.AudioPlayer.PlayPauseOnStart();
        else if (UserSettings.Default.PreviousAudio.IsTriggered(e.Key))
            _applicationView.AudioPlayer.Previous();
        else if (UserSettings.Default.NextAudio.IsTriggered(e.Key))
            _applicationView.AudioPlayer.Next();
    }

    private void OnAudioFileMouseDoubleClick(object sender, TappedEventArgs e)
    {
        _applicationView.AudioPlayer.PlayPauseOnForce();
    }

    private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        var filters = textBox.Text.Trim().Split(' ');
        _applicationView.AudioPlayer.AudioFilesView.Filter = o => { return o is AudioFile audio && filters.All(x => audio.FileName.Contains(x, StringComparison.OrdinalIgnoreCase)); };
    }

    private void OnActivatedDeactivated(object sender, EventArgs e)
    {
        _applicationView.AudioPlayer.HideToggle();
    }
}
