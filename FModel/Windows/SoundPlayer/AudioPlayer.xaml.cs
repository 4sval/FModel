using CSCore.SoundOut;
using FModel.Chic.Windows;
using FModel.Discord;
using FModel.PakReader;
using FModel.ViewModels.ListBox;
using FModel.ViewModels.SoundPlayer;
using FModel.Windows.SoundPlayer.Visualization;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private string _oldPlayedSound = string.Empty;

        public AudioPlayer()
        {
            InitializeComponent();
            Startup();
            Show();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            output.Stop();
            ListBoxVm.soundFiles.Clear();
            DiscordIntegration.Restore();
            InputFileVm.inputFileViewModel.Reset();
        }
        private void Startup()
        {
            DiscordIntegration.SaveCurrentPresence();
            Sound_LstBox.ItemsSource = ListBoxVm.soundFiles;
            AudioPlayer_TabItm.DataContext = InputFileVm.inputFileViewModel;
            AudioDevices_CmbBox.ItemsSource = InputFileVm.inputFileViewModel.Devices;
            AudioDevices_CmbBox.SelectedItem = InputFileVm.inputFileViewModel.Devices.Where(x => x.DeviceId == Properties.Settings.Default.AudioPlayerDevice).FirstOrDefault();
            if (AudioDevices_CmbBox.SelectedIndex < 0) AudioDevices_CmbBox.SelectedIndex = 0;

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

        private void OnCreateVideoClick(object sender, RoutedEventArgs e)
        {
            if (Sound_LstBox.SelectedIndex > -1 && Sound_LstBox.SelectedItem is ListBoxViewModel2 selected)
            {
                VideoCreator creator = new VideoCreator(selected.FullPath);
                creator.Show();
            }
        }
        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = Properties.Resources.SelectFile,
                Filter = Properties.Resources.OggFilter,
                Multiselect = true,
                InitialDirectory = Properties.Settings.Default.OutputPath + "\\Sounds\\"
            };
            if ((bool)ofd.ShowDialog())
            {
                foreach (string file in ofd.FileNames)
                {
                    switch (Path.GetExtension(file))
                    {
                        case ".adpcm":
                        case ".wem":
                            Focus();
                            ListBoxVm.soundFiles.Add(new ListBoxViewModel2
                            {
                                Content = Path.GetFileName(file),
                                Data = File.ReadAllBytes(file),
                                FullPath = string.Empty,
                                Folder = string.Empty
                            });
                            break;
                        case ".bnk":
                        case ".pck":
                            Focus();
                            WwiseReader bnk = new WwiseReader(new BinaryReader(new FileInfo(file).Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite)));
                            LoadFiles(bnk.AudioFiles, "");
                            break;
                        default:
                            LoadFile(file);
                            break;
                    }
                }
            }
        }

        public void LoadFiles(Dictionary<string, byte[]> files, string gameFolder)
        {
            Focus();

            ListBoxVm.soundFiles.Clear();
            if (output.HasMedia) output.Stop();
            foreach (var (key, value) in files)
            {
                ListBoxVm.soundFiles.Add(new ListBoxViewModel2
                {
                    Content = key,
                    Data = value,
                    FullPath = string.Empty,
                    Folder = gameFolder
                });
            }
        }

        public void LoadFile(byte[] sound, string assetName, string gameFolder)
        {
            Focus();
            ListBoxVm.soundFiles.Add(new ListBoxViewModel2
            {
                Content = assetName,
                Data = sound,
                FullPath = string.Empty,
                Folder = gameFolder
            });
        }

        public void LoadFile(string filepath)
        {
            ListBoxViewModel2 item = ListBoxVm.soundFiles.Where(x => x.FullPath.Equals(filepath)).FirstOrDefault();
            if (item == null)
            {
                Focus();
                item = new ListBoxViewModel2
                {
                    Content = Path.GetFileName(filepath),
                    Data = null,
                    FullPath = filepath,
                    Folder = string.Empty
                };

                ListBoxVm.soundFiles.Add(item);

                if (ListBoxVm.soundFiles.Count == 1) // auto play if one in queue
                {
                    output.Stop();
                    output.Load(filepath);
                    output.Play();
                    Sound_LstBox.SelectedIndex = 0;

                    string name = Path.GetFileName(filepath);
                    InputFileVm.inputFileViewModel.Set(name, output);
                    DiscordIntegration.Update(string.Empty, string.Format(Properties.Resources.Listening, name));
                    PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/pause.png"));
                }
                else
                {
                    Sound_LstBox.SelectedIndex = ListBoxVm.soundFiles.IndexOf(item);
                    Sound_LstBox.ScrollIntoView(item);
                }
            }
            else
            {
                Focus();
                Sound_LstBox.SelectedIndex = ListBoxVm.soundFiles.IndexOf(item);
                Sound_LstBox.ScrollIntoView(item);
            }
        }

        private void UpdateVolume(object sender, RoutedEventArgs e)
        {
            if (output.HasMedia) output.Volume = InputFileVm.inputFileViewModel.Volume;
        }

        private void OnPlayPauseClick(object sender, RoutedEventArgs e)
        {
            if (output.HasMedia)
            {
                if (!output.FileName.Equals(_oldPlayedSound))
                {
                    output.Stop();
                    output.Load(_oldPlayedSound);
                    output.Play();

                    string name = Path.GetFileName(_oldPlayedSound);
                    InputFileVm.inputFileViewModel.Set(name, output);
                    DiscordIntegration.Update(string.Empty, string.Format(Properties.Resources.Listening, name));
                    PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/pause.png"));
                }
                else if (output.IsPlaying)
                {
                    output.Pause();
                    PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/play.png"));
                }
                else if (output.Paused)
                {
                    output.Resume();
                    PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/pause.png"));
                }
                else if (output.IsStopped)
                {
                    output.Play();
                    PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/pause.png"));
                }
            }
            else
            {
                if (Sound_LstBox.SelectedIndex > -1 && Sound_LstBox.SelectedItem is ListBoxViewModel2 selected)
                {
                    output.Stop();
                    output.Load(selected.FullPath);
                    output.Play();

                    InputFileVm.inputFileViewModel.Set(selected.Content, output);
                    DiscordIntegration.Update(string.Empty, string.Format(Properties.Resources.Listening, selected.Content));
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

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox c && c.SelectedItem is Device d)
            {
                Properties.Settings.Default.AudioPlayerDevice = d.DeviceId;
                Properties.Settings.Default.Save();

                if (output == null)
                {
                    output = new OutputSource(d);
                    output.SourcePropertyChangedEvent += Output_SourcePropertyChangedEvent;
                }
                else
                    output.SwapDevice(d);
            }
        }

        private void Output_SourcePropertyChangedEvent(object sender, SourcePropertyChangedEventArgs e)
        {
            switch (e.Property)
            {
                case ESourceProperty.PlaybackState:
                    if (output != null && output.Position == output.Length && (PlaybackState)e.Value == PlaybackState.Stopped)
                    {
                        output.Stop();
                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/play.png"));
                        });

                        var selected = ListBoxVm.soundFiles.Where(x => x.FullPath.Equals(output.FileName)).FirstOrDefault();
                        if (selected is ListBoxViewModel2 s)
                        {
                            int index = ListBoxVm.soundFiles.IndexOf(s);
                            if (index < ListBoxVm.soundFiles.Count - 1)
                            {
                                Application.Current.Dispatcher.Invoke(delegate
                                {
                                    Sound_LstBox.SelectedIndex = index + 1;
                                    OnPlayPauseClick(sender, null);
                                });
                            }
                        }
                    }
                    break;
            }
        }

        private void OnSelectedItemChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ListBoxViewModel2 selectedItem)
            {
                // vgmstream convert on select
                if (string.IsNullOrEmpty(selectedItem.FullPath) && selectedItem.Data != null)
                {
                    if (TryVGMStreamConvert(selectedItem, out string wavFile))
                    {
                        _oldPlayedSound = wavFile;
                        LoadFile(wavFile);
                    }
                }
                else if (!_oldPlayedSound.Equals(selectedItem.FullPath))
                    _oldPlayedSound = selectedItem.FullPath;

                if (output.HasMedia && output.FileName.Equals(_oldPlayedSound))
                    PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/pause.png"));
                else
                    PlayPauseImg.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/play.png"));
            }
        }

        /// <summary>
        /// this sucks honestly but i lost hope with all wav conversion types
        /// </summary>
        private bool TryVGMStreamConvert(ListBoxViewModel2 selectedItem, out string wavFilePath)
        {
            if (File.Exists(Properties.Settings.Default.OutputPath + "\\Vgm\\test.exe"))
            {
                Directory.CreateDirectory(Properties.Settings.Default.OutputPath + "\\Sounds\\" + selectedItem.Folder + "\\");
                File.WriteAllBytes(Properties.Settings.Default.OutputPath + "\\Sounds\\" + selectedItem.Folder + "\\" + selectedItem.Content, selectedItem.Data);

                wavFilePath = Path.ChangeExtension(Properties.Settings.Default.OutputPath + "\\Sounds\\" + selectedItem.Folder + "\\" + selectedItem.Content, ".wav");
                var vgmstream = Process.Start(new ProcessStartInfo
                {
                    FileName = Properties.Settings.Default.OutputPath + "\\Vgm\\test.exe",
                    Arguments = $"-o \"{wavFilePath}\" \"{Properties.Settings.Default.OutputPath + "\\Sounds\\" + selectedItem.Folder + "\\" + selectedItem.Content}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
                vgmstream.WaitForExit();

                ListBoxVm.soundFiles.Remove(selectedItem);
                File.Delete(Properties.Settings.Default.OutputPath + "\\Sounds\\" + selectedItem.Folder + "\\" + selectedItem.Content);

                return vgmstream.ExitCode == 0 && File.Exists(wavFilePath);
            }
            wavFilePath = string.Empty;
            return false;
        }
    }
}
