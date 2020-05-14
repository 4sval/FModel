using FModel.Discord;
using FModel.ViewModels.SoundPlayer;
using FModel.Windows.SoundPlayer.Visualization;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FModel.Windows.SoundPlayer
{
    /// <summary>
    /// Logique d'interaction pour AudioPlayer.xaml
    /// </summary>
    public partial class AudioPlayer : Window
    {
        private OutputSource output;
        private UserControls.SpectrumAnalyzer spectrumAnalyzer;
        private UserControls.Timeline timeline;
        private UserControls.Timeclock timeclock;

        public AudioPlayer()
        {
            InitializeComponent();
            Startup();
            Show();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            output.Stop();
            DiscordIntegration.Restore();
            InputFileVm.inputFileViewModel.Reset();
        }
        private void Startup()
        {
            DiscordIntegration.SaveCurrentPresence();
            AudioPlayer_TabItm.DataContext = InputFileVm.inputFileViewModel;
            output = new OutputSource(InputFileVm.inputFileViewModel.Device);
            if (spectrumAnalyzer == null)
                spectrumAnalyzer = new UserControls.SpectrumAnalyzer(output);
            if (timeline == null)
                timeline = new UserControls.Timeline(output);
            if (timeclock == null)
                timeclock = new UserControls.Timeclock(output);

            Clock.Content = timeclock;
            Spectrum.Content = spectrumAnalyzer;
            Time.Content = timeline;
        }

        private void OnOpenClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = Properties.Resources.SelectFile,
                Filter = Properties.Resources.OggFilter,
                InitialDirectory = Properties.Settings.Default.OutputPath + "\\Sounds\\"
            };
            if ((bool)ofd.ShowDialog())
                LoadFile(ofd.FileName);
        }

        public void LoadFile(string filepath)
        {
            Focus();

            output.Stop();
            output.Load(filepath);
            output.Play();

            string name = Path.GetFileName(filepath);
            InputFileVm.inputFileViewModel.Set(name, output);
            DiscordIntegration.Update(string.Empty, string.Format(Properties.Resources.Listening, name));
            PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/pause.png"));
        }

        private void UpdateVolume(object sender, RoutedEventArgs e)
        {
            if (output.HasMedia)
                output.Volume = InputFileVm.inputFileViewModel.Volume;
        }

        private void OnPlayPauseClick(object sender, RoutedEventArgs e)
        {
            if (output.HasMedia)
            {
                if (output.IsPlaying)
                {
                    output.Pause();
                    PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/play.png"));
                }
                else if (output.Paused)
                {
                    output.Resume();
                    PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/pause.png"));
                }
            }
        }

        private void OnStopClick(object sender, RoutedEventArgs e)
        {
            if (output.HasMedia)
            {
                output.Stop();
                InputFileVm.inputFileViewModel.Reset();
                PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/play.png"));
            }
        }
    }
}
