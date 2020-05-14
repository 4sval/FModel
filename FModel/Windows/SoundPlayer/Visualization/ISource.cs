using CSCore.DSP;
using System;

namespace FModel.Windows.SoundPlayer.Visualization
{
    public interface ISource
    {
        float[] FftData { get; }
        FftSize FftSize { get; set; }
        float[] WaveformData { get; }
        long SampleLength { get; }
        int BytesPerSecond { get; }
        TimeSpan Position { get; }
        BasicSpectrumProvider SpectrumProvider { get; }
        TimeSpan Length { get; }

        void GetData(int resolution = 2048);
        float[] GetDataRange(int start, int length, int resolution = 2048);
        void Dispose();

        event EventHandler<SourcePropertyChangedEventArgs> SourcePropertyChangedEvent;
        event EventHandler<SourceEventArgs> SourceEvent;
        void RaiseSourceEvent(ESourceEventType e);
        void RaiseSourcePropertyChangedEvent(ESourceProperty property, object value);
    }
}
