using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CSCore.CoreAudioAPI;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels;
using Microsoft.Win32;

namespace FModel.Views;

public partial class AudioPlayer
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
        if (sender is not ComboBox { SelectedItem: MMDevice selectedDevice })
            return;

        UserSettings.Default.AudioDeviceId = selectedDevice.DeviceID;
        _applicationView.AudioPlayer.Device();
    }

    private void OnVolumeChange(object sender, RoutedEventArgs e)
    {
        _applicationView.AudioPlayer.Volume();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is TextBox)
            return;

        if (UserSettings.Default.AddAudio.IsTriggered(e.Key))
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select an audio file",
                InitialDirectory = UserSettings.Default.AudioDirectory,
                Filter = "OGG Files (*.ogg)|*.ogg|WAV Files (*.wav)|*.wav|WEM Files (*.wem)|*.wem|ADPCM Files (*.adpcm)|*.adpcm|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (!openFileDialog.ShowDialog().GetValueOrDefault()) return;
            foreach (var file in openFileDialog.FileNames)
            {
                _applicationView.AudioPlayer.AddToPlaylist(file);
            }
        }
        else if (UserSettings.Default.PlayPauseAudio.IsTriggered(e.Key))
            _applicationView.AudioPlayer.PlayPauseOnStart();
        else if (UserSettings.Default.PreviousAudio.IsTriggered(e.Key))
            _applicationView.AudioPlayer.Previous();
        else if (UserSettings.Default.NextAudio.IsTriggered(e.Key))
            _applicationView.AudioPlayer.Next();
        else if (UserSettings.Default.RemoveAudio.IsTriggered(e.Key))
            _applicationView.AudioPlayer.Remove();
    }

    private void OnAudioFileMouseDoubleClick(object sender, MouseButtonEventArgs e)
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
