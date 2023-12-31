using CSCore;
using CSCore.DSP;
using CSCore.SoundOut;
using CSCore.Streams;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using CSCore.CoreAudioAPI;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels.Commands;
using FModel.Views.Resources.Controls;
using FModel.Views.Resources.Controls.Aup;
using Microsoft.Win32;
using Serilog;

namespace FModel.ViewModels;

public class AudioFile : ViewModel
{
    private string _filePath;
    public string FilePath
    {
        get => _filePath;
        private set => SetProperty(ref _filePath, value);
    }

    private string _fileName;
    public string FileName
    {
        get => _fileName;
        private set => SetProperty(ref _fileName, value);
    }

    private long _length;
    public long Length
    {
        get => _length;
        private set => SetProperty(ref _length, value);
    }

    private TimeSpan _duration = TimeSpan.Zero;
    public TimeSpan Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    private TimeSpan _position = TimeSpan.Zero;
    public TimeSpan Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    private AudioEncoding _encoding = AudioEncoding.Unknown;
    public AudioEncoding Encoding
    {
        get => _encoding;
        set => SetProperty(ref _encoding, value);
    }

    private PlaybackState _playbackState = PlaybackState.Stopped;
    public PlaybackState PlaybackState
    {
        get => _playbackState;
        set => SetProperty(ref _playbackState, value);
    }

    private int _bytesPerSecond;
    public int BytesPerSecond
    {
        get => _bytesPerSecond;
        set => SetProperty(ref _bytesPerSecond, value);
    }

    public int Id { get; set; }
    public byte[] Data { get; set; }
    public string Extension { get; }

    public AudioFile(int id, byte[] data, string filePath)
    {
        Id = id;
        FilePath = filePath;
        FileName = filePath.SubstringAfterLast("/");
        Length = data.Length;
        Duration = TimeSpan.Zero;
        Position = TimeSpan.Zero;
        Encoding = AudioEncoding.Unknown;
        PlaybackState = PlaybackState.Stopped;
        BytesPerSecond = 0;
        Extension = filePath.SubstringAfterLast(".");
        Data = data;
    }

    public AudioFile(int id, string fileName)
    {
        Id = id;
        FilePath = string.Empty;
        FileName = fileName;
        Length = 0;
        Duration = TimeSpan.Zero;
        Position = TimeSpan.Zero;
        Encoding = AudioEncoding.Unknown;
        PlaybackState = PlaybackState.Stopped;
        BytesPerSecond = 0;
        Extension = string.Empty;
        Data = null;
    }

    public AudioFile(int id, FileInfo fileInfo)
    {
        Id = id;
        FilePath = fileInfo.FullName.Replace('\\', '/');
        FileName = fileInfo.Name;
        Length = fileInfo.Length;
        Duration = TimeSpan.Zero;
        Position = TimeSpan.Zero;
        Encoding = AudioEncoding.Unknown;
        PlaybackState = PlaybackState.Stopped;
        BytesPerSecond = 0;
        Extension = fileInfo.Extension[1..];
        Data = File.ReadAllBytes(fileInfo.FullName);
    }

    public AudioFile(AudioFile audioFile, IAudioSource wave)
    {
        Id = audioFile.Id;
        FilePath = audioFile.FilePath;
        FileName = audioFile.FileName;
        Length = audioFile.Length;
        Duration = wave.GetLength();
        Position = audioFile.Position;
        Encoding = wave.WaveFormat.WaveFormatTag;
        PlaybackState = audioFile.PlaybackState;
        BytesPerSecond = wave.WaveFormat.BytesPerSecond;
        Extension = audioFile.Extension;
        Data = audioFile.Data;
    }

    public override string ToString()
    {
        return $"{Id} | {FileName} | {Length}";
    }
}

public class AudioPlayerViewModel : ViewModel, ISource, IDisposable
{
    private DiscordHandler _discordHandler => DiscordService.DiscordHandler;
    private static IWaveSource _waveSource;
    private static ISoundOut _soundOut;
    private Timer _sourceTimer;

    private TimeSpan _length => _waveSource?.GetLength() ?? TimeSpan.Zero;
    private TimeSpan _position => _waveSource?.GetPosition() ?? TimeSpan.Zero;
    private PlaybackState _playbackState => _soundOut?.PlaybackState ?? PlaybackState.Stopped;
    private bool _hideToggle = false;

    public SpectrumProvider Spectrum { get; private set; }
    public float[] FftData { get; private set; }

    private AudioFile _playedFile = new(-1, "No audio file");
    public AudioFile PlayedFile
    {
        get => _playedFile;
        private set => SetProperty(ref _playedFile, value);
    }

    private AudioFile _selectedAudioFile;
    public AudioFile SelectedAudioFile
    {
        get => _selectedAudioFile;
        set => SetProperty(ref _selectedAudioFile, value);
    }

    private MMDevice _selectedAudioDevice;
    public MMDevice SelectedAudioDevice
    {
        get => _selectedAudioDevice;
        set => SetProperty(ref _selectedAudioDevice, value);
    }

    private AudioCommand _audioCommand;
    public AudioCommand AudioCommand => _audioCommand ??= new AudioCommand(this);

    public bool IsStopped => PlayedFile.PlaybackState == PlaybackState.Stopped;
    public bool IsPlaying => PlayedFile.PlaybackState == PlaybackState.Playing;
    public bool IsPaused => PlayedFile.PlaybackState == PlaybackState.Paused;

    private readonly ObservableCollection<AudioFile> _audioFiles;
    public ICollectionView AudioFilesView { get; }
    public ICollectionView AudioDevicesView { get; }

    public AudioPlayerViewModel()
    {
        _sourceTimer = new Timer(TimerTick, null, 0, 10);
        _audioFiles = new ObservableCollection<AudioFile>();
        AudioFilesView = new ListCollectionView(_audioFiles);

        var audioDevices = new ObservableCollection<MMDevice>(EnumerateDevices());
        AudioDevicesView = new ListCollectionView(audioDevices) { SortDescriptions = { new SortDescription("FriendlyName", ListSortDirection.Ascending) } };
        SelectedAudioDevice ??= audioDevices.FirstOrDefault();
    }

    public void Load()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!ConvertIfNeeded())
                return;

            _waveSource = new CustomCodecFactory().GetCodec(SelectedAudioFile.Data, SelectedAudioFile.Extension);
            if (_waveSource == null)
                return;

            PlayedFile = new AudioFile(SelectedAudioFile, _waveSource);
            Spectrum = new SpectrumProvider(_waveSource.WaveFormat.Channels, _waveSource.WaveFormat.SampleRate, FftSize.Fft4096);

            var notificationSource = new SingleBlockNotificationStream(_waveSource.ToSampleSource());
            notificationSource.SingleBlockRead += (s, a) => Spectrum.Add(a.Left, a.Right);
            _waveSource = notificationSource.ToWaveSource(16);

            RaiseSourceEvent(ESourceEventType.Loading);
            LoadSoundOut();
        });
    }

    public void Unload()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _waveSource = null;

            PlayedFile = new AudioFile(-1, "No audio file");
            Spectrum = null;

            RaiseSourceEvent(ESourceEventType.Clearing);
            ClearSoundOut();
        });
    }

    public void AddToPlaylist(byte[] data, string filePath)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _audioFiles.Add(new AudioFile(_audioFiles.Count, data, filePath));
            if (_audioFiles.Count > 1) return;

            SelectedAudioFile = _audioFiles.Last();
            Load();
            Play();
        });
    }

    public void AddToPlaylist(string filePath)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _audioFiles.Add(new AudioFile(_audioFiles.Count, new FileInfo(filePath)));
            if (_audioFiles.Count > 1) return;

            SelectedAudioFile = _audioFiles.Last();
            Load();
            Play();
        });
    }

    public void Remove()
    {
        if (_audioFiles.Count < 1) return;
        Application.Current.Dispatcher.Invoke(() =>
        {
            var removedPlaying = false;
            if (PlayedFile.Id == SelectedAudioFile.Id)
            {
                removedPlaying = true;
                Stop();
            }

            _audioFiles.RemoveAt(SelectedAudioFile.Id);
            for (var i = 0; i < _audioFiles.Count; i++)
            {
                _audioFiles[i].Id = i;
            }

            if (_audioFiles.Count < 1)
            {
                Unload();
                return;
            }

            SelectedAudioFile = _audioFiles[SelectedAudioFile.Id];

            if (!removedPlaying) return;
            Load();
            Play();
        });
    }

    public void Replace(AudioFile newAudio)
    {
        if (_audioFiles.Count < 1) return;
        Application.Current.Dispatcher.Invoke(() =>
        {
            _audioFiles.Insert(SelectedAudioFile.Id, newAudio);
            _audioFiles.RemoveAt(SelectedAudioFile.Id + 1);
            SelectedAudioFile = newAudio;
        });
    }

    public void SavePlaylist()
    {
        if (_audioFiles.Count < 1) return;
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var a in _audioFiles)
            {
                Save(a, true);
            }
        });
    }

    public void Save(AudioFile file = null, bool auto = false)
    {
        var fileToSave = file ?? SelectedAudioFile;
        if (_audioFiles.Count < 1 || fileToSave?.Data == null) return;
        var path = fileToSave.FilePath;

        if (!auto)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Audio",
                FileName = fileToSave.FileName,
                InitialDirectory = UserSettings.Default.AudioDirectory
            };
            if (!saveFileDialog.ShowDialog().GetValueOrDefault()) return;
            path = saveFileDialog.FileName;
        }
        else
        {
            Directory.CreateDirectory(path.SubstringBeforeLast('/'));
        }

        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(fileToSave.Data);
            writer.Flush();
        }

        if (File.Exists(path))
        {
            Log.Information("{FileName} successfully saved", fileToSave.FileName);
            FLogger.Append(ELog.Information, () =>
            {
                FLogger.Text("Successfully saved ", Constants.WHITE);
                FLogger.Link(fileToSave.FileName, path, true);
            });
        }
        else
        {
            Log.Error("{FileName} could not be saved", fileToSave.FileName);
            FLogger.Append(ELog.Error, () => FLogger.Text($"Could not save '{fileToSave.FileName}'", Constants.WHITE, true));
        }
    }

    public void PlayPauseOnStart()
    {
        if (IsStopped)
        {
            Load();
            Play();
        }
        else if (IsPaused)
        {
            Play();
        }
        else if (IsPlaying)
        {
            Pause();
        }
    }

    public void PlayPauseOnForce()
    {
        if (_audioFiles.Count < 1 || SelectedAudioFile.Id == PlayedFile.Id) return;

        Stop();
        Load();
        Play();
    }

    public void Next()
    {
        if (_audioFiles.Count < 1) return;

        Stop();
        SelectedAudioFile = _audioFiles.Next(PlayedFile.Id);
        Load();
        Play();
    }

    public void Previous()
    {
        if (_audioFiles.Count < 1) return;

        Stop();
        SelectedAudioFile = _audioFiles.Previous(PlayedFile.Id);
        Load();
        Play();
    }

    public void Play()
    {
        if (_soundOut == null || IsPlaying) return;
        _discordHandler.UpdateButDontSavePresence(null, $"Audio Player: {PlayedFile.FileName} ({PlayedFile.Duration:g})");
        _soundOut.Play();
    }

    public void Pause()
    {
        if (_soundOut == null || IsPaused) return;
        _soundOut.Pause();
    }

    public void Resume()
    {
        if (_soundOut == null || !IsPaused) return;
        _soundOut.Resume();
    }

    public void Stop()
    {
        if (_soundOut == null || IsStopped) return;
        _soundOut.Stop();
    }

    public void HideToggle()
    {
        if (!IsPlaying) return;
        _hideToggle = !_hideToggle;
        RaiseSourcePropertyChangedEvent(ESourceProperty.HideToggle, _hideToggle);
    }

    public void SkipTo(double percentage)
    {
        if (_soundOut == null || _waveSource == null) return;
        _waveSource.Position = (long) (_waveSource.Length * percentage);
    }

    public void Volume()
    {
        if (_soundOut == null) return;
        _soundOut.Volume = UserSettings.Default.AudioPlayerVolume / 100;
    }

    public void Device()
    {
        if (_soundOut == null) return;

        Pause();
        LoadSoundOut();
        Play();
    }

    public void Dispose()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_waveSource != null)
            {
                _waveSource.Dispose();
                _waveSource = null;
            }

            if (_soundOut != null)
            {
                _soundOut.Dispose();
                _soundOut = null;
            }

            if (Spectrum != null)
                Spectrum = null;

            foreach (var a in _audioFiles)
            {
                a.Data = null;
            }

            _audioFiles.Clear();
            PlayedFile = new AudioFile(-1, "No audio file");
        });
    }

    private void TimerTick(object state)
    {
        if (_waveSource == null || _soundOut == null) return;

        if (_position != PlayedFile.Position)
        {
            PlayedFile.Position = _position;
            RaiseSourcePropertyChangedEvent(ESourceProperty.Position, PlayedFile.Position);
        }

        if (_playbackState != PlayedFile.PlaybackState)
        {
            PlayedFile.PlaybackState = _playbackState;
            RaiseSourcePropertyChangedEvent(ESourceProperty.PlaybackState, PlayedFile.PlaybackState);
        }

        if (Spectrum != null && PlayedFile.PlaybackState == PlaybackState.Playing)
        {
            FftData = new float[4096];
            Spectrum.GetFftData(FftData);
            RaiseSourcePropertyChangedEvent(ESourceProperty.FftData, FftData);
        }
    }

    private void LoadSoundOut()
    {
        if (_waveSource == null) return;
        _soundOut = new WasapiOut(true, AudioClientShareMode.Shared, 100, ThreadPriority.Highest) { Device = SelectedAudioDevice };
        _soundOut.Initialize(_waveSource.ToSampleSource().ToWaveSource(16));
        _soundOut.Volume = UserSettings.Default.AudioPlayerVolume / 100;
    }

    private void ClearSoundOut()
    {
        _soundOut = null;
    }

    private IEnumerable<MMDevice> EnumerateDevices()
    {
        using var deviceEnumerator = new MMDeviceEnumerator();
        using var deviceCollection = deviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
        foreach (var device in deviceCollection)
        {
            if (device.DeviceID == UserSettings.Default.AudioDeviceId)
                SelectedAudioDevice = device;

            yield return device;
        }
    }

    public event EventHandler<SourceEventArgs> SourceEvent;

    public event EventHandler<SourcePropertyChangedEventArgs> SourcePropertyChangedEvent = (sender, args) =>
    {
        if (sender is not AudioPlayerViewModel viewModel) return;
        switch (args.Property)
        {
            case ESourceProperty.PlaybackState:
            {
                if (viewModel._position == viewModel._length && (PlaybackState) args.Value == PlaybackState.Stopped)
                    viewModel.Next();

                break;
            }
        }
    };

    private void RaiseSourceEvent(ESourceEventType e)
    {
        SourceEvent?.Invoke(this, new SourceEventArgs(e));
    }

    private void RaiseSourcePropertyChangedEvent(ESourceProperty property, object value)
    {
        SourcePropertyChangedEvent?.Invoke(this, new SourcePropertyChangedEventArgs(property, value));
    }

    private bool ConvertIfNeeded()
    {
        if (SelectedAudioFile?.Data == null)
            return false;

        switch (SelectedAudioFile.Extension)
        {
            case "adpcm":
            case "opus":
            case "wem":
            case "at9":
            case "raw":
            {
                if (TryConvert(out var wavFilePath))
                {
                    var newAudio = new AudioFile(SelectedAudioFile.Id, new FileInfo(wavFilePath));
                    Replace(newAudio);
                    return true;
                }

                return false;
            }
            case "binka":
            {
                if (TryDecode(out var rawFilePath))
                {
                    var newAudio = new AudioFile(SelectedAudioFile.Id, new FileInfo(rawFilePath));
                    Replace(newAudio);
                    return true;
                }

                return false;
            }
        }

        return true;
    }

    private bool TryConvert(out string wavFilePath) => TryConvert(SelectedAudioFile.FilePath, SelectedAudioFile.Data, out wavFilePath);
    private bool TryConvert(string inputFilePath, byte[] inputFileData, out string wavFilePath)
    {
        wavFilePath = string.Empty;
        var vgmFilePath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", "test.exe");
        if (!File.Exists(vgmFilePath))
        {
            vgmFilePath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", "vgmstream-cli.exe");
            if (!File.Exists(vgmFilePath)) return false;
        }

        Directory.CreateDirectory(inputFilePath.SubstringBeforeLast("/"));
        File.WriteAllBytes(inputFilePath, inputFileData);

        wavFilePath = Path.ChangeExtension(inputFilePath, ".wav");
        var vgmProcess = Process.Start(new ProcessStartInfo
        {
            FileName = vgmFilePath,
            Arguments = $"-o \"{wavFilePath}\" \"{inputFilePath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });
        vgmProcess?.WaitForExit(5000);

        File.Delete(inputFilePath);
        return vgmProcess?.ExitCode == 0 && File.Exists(wavFilePath);
    }

    private bool TryDecode(out string rawFilePath)
    {
        rawFilePath = string.Empty;
        var binkadecPath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", "binkadec.exe");
        if (!File.Exists(binkadecPath))
        {
            return false;
        }

        Directory.CreateDirectory(SelectedAudioFile.FilePath.SubstringBeforeLast("/"));
        File.WriteAllBytes(SelectedAudioFile.FilePath, SelectedAudioFile.Data);

        rawFilePath = Path.ChangeExtension(SelectedAudioFile.FilePath, ".wav");
        var binkadecProcess = Process.Start(new ProcessStartInfo
        {
            FileName = binkadecPath,
            Arguments = $"-i \"{SelectedAudioFile.FilePath}\" -o \"{rawFilePath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });
        binkadecProcess?.WaitForExit(5000);

        File.Delete(SelectedAudioFile.FilePath);
        return binkadecProcess?.ExitCode == 0 && File.Exists(rawFilePath);
    }
}
