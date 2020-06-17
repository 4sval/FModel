using CSCore;
using CSCore.DSP;
using CSCore.SoundOut;
using CSCore.Streams;
using FModel.ViewModels.SoundPlayer;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FModel.Windows.SoundPlayer.Visualization
{
    public class OutputSource : ISource, IDisposable
    {
        private string _filename;
        private Uri _uri;
        private IWaveSource _waveSource;
        private ISampleSource _sampleSource;
        private ISoundOut _soundOut;

        private Timer _sourceTimer;
        private TimeSpan cachedPosition = TimeSpan.Zero;
        private PlaybackState cachedPlaybackState = PlaybackState.Stopped;

        private Device outputDevice;
        public Device OutputDevice
        {
            get { return outputDevice; }
            set { outputDevice = value; }
        }

        private BasicSpectrumProvider spectrumProvider;
        public BasicSpectrumProvider SpectrumProvider { get { return spectrumProvider; } }

        private float[] waveformData;
        public float[] WaveformData
        {
            get { return waveformData; }
        }

        private float[] fftData;
        public float[] FftData
        {
            get { return fftData; }
        }

        private FftSize fftSize = FftSize.Fft4096;
        public FftSize FftSize
        {
            get { return fftSize; }
            set
            {
                fftSize = value;
            }
        }

        private float volume = float.TryParse(Properties.Settings.Default.AudioPlayerVolume, out var v) ? v : 0.5f;
        public float Volume
        {
            get { return _soundOut == null ? 0.0f : _soundOut.Volume; }
            set
            {
                volume = value;
                Properties.Settings.Default.AudioPlayerVolume = volume.ToString();
                Properties.Settings.Default.Save();

                if (_soundOut != null)
                {
                    _soundOut.Volume = volume;
                }
            }
        }

        public static string SupportedFiles
        {
            get { return CSCore.Codecs.CodecFactory.SupportedFilesFilterEn; }
        }

        public TimeSpan Length
        {
            get { return _waveSource == null ? TimeSpan.Zero : _waveSource.GetLength(); }
        }

        public long SampleLength
        {
            get { return _sampleSource == null ? 0L : _sampleSource.Length; }
        }

        public int BytesPerSecond
        {
            get { return _sampleSource == null ? 0 : (_sampleSource.WaveFormat.Channels * _sampleSource.WaveFormat.SampleRate); }
        }

        public TimeSpan Position
        {
            get
            {
                try
                {
                    return _waveSource == null ? TimeSpan.Zero : _waveSource.GetPosition();
                }
                catch (Exception)
                {
                    return TimeSpan.Zero;
                }
            }
            set
            {
                if (_waveSource != null)
                {
                    _waveSource.SetPosition(value);
                }
            }
        }

        public PlaybackState PlaybackState
        {
            get { return _soundOut == null ? PlaybackState.Stopped : _soundOut.PlaybackState; }
        }

        public bool IsStopped
        {
            get { return PlaybackState == PlaybackState.Stopped; }
        }

        public bool IsPlaying
        {
            get { return PlaybackState == PlaybackState.Playing; }
        }

        public bool Paused
        {
            get { return PlaybackState == PlaybackState.Paused; }
        }

        private bool hasMedia;
        public bool HasMedia
        {
            get { return hasMedia; }
        }

        #region Constructors

        public OutputSource()
        {
            //Register the new codec.
            CSCore.Codecs.CodecFactory.Instance.Register("ogg-vorbis", new CSCore.Codecs.CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));

            outputDevice = Device.GetDefaultDevice();
            TimerSetup();
        }

        public OutputSource(Device device)
        {
            //Register the new codec.
            CSCore.Codecs.CodecFactory.Instance.Register("ogg-vorbis", new CSCore.Codecs.CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));

            outputDevice = device;
            TimerSetup();
        }

        public OutputSource(string filename)
        {
            //Register the new codec.
            CSCore.Codecs.CodecFactory.Instance.Register("ogg-vorbis", new CSCore.Codecs.CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));

            outputDevice = Device.GetDefaultDevice();
            Load(filename);
            TimerSetup();
        }

        public OutputSource(Device device, string filename)
        {
            //Register the new codec.
            CSCore.Codecs.CodecFactory.Instance.Register("ogg-vorbis", new CSCore.Codecs.CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));

            outputDevice = device;
            Load(filename);
            TimerSetup();
        }

        public OutputSource(Uri uri)
        {
            //Register the new codec.
            CSCore.Codecs.CodecFactory.Instance.Register("ogg-vorbis", new CSCore.Codecs.CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));

            outputDevice = Device.GetDefaultDevice();
            Load(uri);
            TimerSetup();
        }

        public OutputSource(Device device, Uri uri)
        {
            //Register the new codec.
            CSCore.Codecs.CodecFactory.Instance.Register("ogg-vorbis", new CSCore.Codecs.CodecFactoryEntry(s => new NVorbisSource(s).ToWaveSource(), ".ogg"));

            outputDevice = device;
            Load(uri);
            TimerSetup();
        }

        #endregion

        public void Dispose()
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

            if (_sampleSource != null)
            {
                _sampleSource.Dispose();
                _sampleSource = null;
            }
        }

        #region Private Methods

        private void TimerSetup()
        {
            _sourceTimer = new Timer(TimerTick, null, 0, 10);
        }

        private void TimerTick(object state)
        {
            // This is a loop for the life of the source that reports things like 
            // position back to a subscriber.
            if (_waveSource != null)
            {
                if (Position != cachedPosition)
                {
                    // position has changed
                    cachedPosition = Position;
                    RaiseSourcePropertyChangedEvent(ESourceProperty.Position, cachedPosition);
                }

                if (_soundOut != null)
                {
                    if (PlaybackState != cachedPlaybackState)
                    {
                        cachedPlaybackState = PlaybackState;
                        RaiseSourcePropertyChangedEvent(ESourceProperty.PlaybackState, cachedPlaybackState);
                    }
                }

                if (SpectrumProvider != null && cachedPlaybackState == PlaybackState.Playing)
                {
                    fftData = new float[(int)fftSize];
                    SpectrumProvider.GetFftData(fftData);
                    RaiseSourcePropertyChangedEvent(ESourceProperty.FftData, FftData);
                }
            }
        }

        private void LoadSoundOut()
        {
            if (_waveSource != null)
            {
                _soundOut = new CSCore.SoundOut.WasapiOut(true, CSCore.CoreAudioAPI.AudioClientShareMode.Shared,
                    100, System.Threading.ThreadPriority.Highest)
                {
                    Device = OutputDevice.ActualDevice
                };

                _soundOut.Initialize(_waveSource.ToSampleSource().ToWaveSource(16));
                // set defaults
                _soundOut.Volume = volume;
            }
        }

        private float GetPeak(float[] values)
        {
            float peak = 0;
            var length = values.Length;

            for (var i = 0; i < length; i++)
            {
                var value = Math.Abs(values[i]);

                if (peak < value)
                {
                    peak = value;
                }
            }

            return peak;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Asynchronously fetched data from an isolated SampleSource.
        /// </summary>
        public void GetData(int resolution = 2048)
        {
            Task.Factory.StartNew(() =>
            {
                var _source = CSCore.Codecs.CodecFactory.Instance.GetCodec(_filename).ToSampleSource();
                var blockSize = _sampleSource.Length / resolution;
                var buffer = new float[blockSize];
                var l = _sampleSource.Length / blockSize;

                if (l % 2 != 0)
                {
                    l++;
                }

                waveformData = new float[l];
                var waveformDataCount = 0;
                var flag = true;

                while (flag)
                {
                    var samplesToRead = buffer.Length;
                    var read = _source.Read(buffer, 0, samplesToRead);
                    waveformData[waveformDataCount] = GetPeak(buffer);

                    if (waveformDataCount % 2 != 0)
                    {
                        RaiseSourcePropertyChangedEvent(ESourceProperty.WaveformData, waveformData);
                    }

                    waveformDataCount++;

                    if (read == 0)
                        flag = false;

                    if (waveformDataCount >= waveformData.Length)
                        flag = false;
                }

                _source.Dispose();
                _source = null;

                RaiseSourceEvent(ESourceEventType.WaveformDataCompleted);
            });
        }

        /// <summary>
        /// Fetch data dynamically from the SampleSource.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public float[] GetDataRange(int start, int length, int resolution = 2048)
        {
            if (start + length > _sampleSource.Length)
                return null;

            if (resolution <= 0)
                return null;

            if (length < resolution)
                resolution = length;

            var blockSize = length / resolution;
            var samples = new float[resolution];

            _sampleSource.Position = start;
            var buffer = new float[blockSize];

            for (var i = 0; i < resolution; i++)
            {
                int read = _sampleSource.Read(buffer, 0, blockSize);

                if (read < buffer.Length)
                    Array.Clear(samples, read, buffer.Length - read);

                samples[i] = GetPeak(buffer);
            }

            return samples;
        }

        public void Load(string filename)
        {
            Dispose();
            _filename = filename;
            var codec = CSCore.Codecs.CodecFactory.Instance.GetCodec(_filename);
            _waveSource = codec.ToSampleSource().ToMono().ToWaveSource();

            spectrumProvider = new BasicSpectrumProvider(_waveSource.WaveFormat.Channels,
                _waveSource.WaveFormat.SampleRate,
                CSCore.DSP.FftSize.Fft4096);

            //the SingleBlockNotificationStream is used to intercept the played samples
            var notificationSource = new SingleBlockNotificationStream(_waveSource.ToSampleSource());
            //pass the intercepted samples as input data to the spectrumprovider (which will calculate a fft based on them)
            notificationSource.SingleBlockRead += (s, a) => SpectrumProvider.Add(a.Left, a.Right);
            _waveSource = notificationSource.ToWaveSource(16);
            // Load the sample source
            _sampleSource = codec.ToSampleSource();
            RaiseSourceEvent(ESourceEventType.Loaded);
            hasMedia = true;
            LoadSoundOut();
        }

        public void Load(Uri uri)
        {
            Dispose();
            _uri = uri;
            var codec = CSCore.Codecs.CodecFactory.Instance.GetCodec(_uri);
            _waveSource = codec.ToSampleSource().ToMono().ToWaveSource();

            spectrumProvider = new BasicSpectrumProvider(_waveSource.WaveFormat.Channels,
                _waveSource.WaveFormat.SampleRate,
                CSCore.DSP.FftSize.Fft4096);

            //the SingleBlockNotificationStream is used to intercept the played samples
            var notificationSource = new SingleBlockNotificationStream(_waveSource.ToSampleSource());
            //pass the intercepted samples as input data to the spectrumprovider (which will calculate a fft based on them)
            notificationSource.SingleBlockRead += (s, a) => SpectrumProvider.Add(a.Left, a.Right);
            _waveSource = notificationSource.ToWaveSource(16);
            // Load the sample source
            _sampleSource = codec.ToSampleSource();
            RaiseSourceEvent(ESourceEventType.Loaded);
            hasMedia = true;
            LoadSoundOut();
        }

        public bool Play()
        {
            if (_soundOut != null)
            {
                if (_soundOut.PlaybackState != CSCore.SoundOut.PlaybackState.Playing)
                {
                    _soundOut.Play();
                    return true;
                }
            }

            return false;
        }

        public bool Pause()
        {
            if (_soundOut != null)
            {
                if (_soundOut.PlaybackState != CSCore.SoundOut.PlaybackState.Paused)
                {
                    _soundOut.Pause();
                    return true;
                }
            }

            return false;
        }

        public bool Resume()
        {
            if (_soundOut != null)
            {
                if (_soundOut.PlaybackState == CSCore.SoundOut.PlaybackState.Paused)
                {
                    _soundOut.Resume();
                    return true;
                }
            }

            return false;
        }

        public bool Stop()
        {
            if (_soundOut != null)
            {
                _soundOut.Stop();
                _waveSource.Position = 0;
                hasMedia = false;
                Dispose();
                return true;
            }

            return false;
        }

        public bool SwapDevice(Device d)
        {
            if (_soundOut != null)
            {
                bool wasPlaying = IsPlaying;
                bool wasPaused = Paused;
                TimeSpan pos = Position;
                outputDevice = d;

                if (wasPlaying || wasPaused)
                {
                    _soundOut.Stop();
                }

                LoadSoundOut();
                _waveSource.SetPosition(pos);

                if (wasPlaying)
                    _soundOut.Play();
                else if (wasPaused)
                {
                    _soundOut.Play();
                    _soundOut.Pause();
                }
            }

            return true;
        }

        #endregion

        #region Events

        public event EventHandler<SourcePropertyChangedEventArgs> SourcePropertyChangedEvent;
        public event EventHandler<SourceEventArgs> SourceEvent;

        public void RaiseSourceEvent(ESourceEventType e)
        {
            SourceEvent?.Invoke(this, new SourceEventArgs
            {
                Event = e
            });
        }

        public void RaiseSourcePropertyChangedEvent(ESourceProperty property, object value)
        {
            SourcePropertyChangedEvent?.Invoke(this, new SourcePropertyChangedEventArgs
            {
                Property = property,
                Value = value
            });
        }

        #endregion
    }
}
