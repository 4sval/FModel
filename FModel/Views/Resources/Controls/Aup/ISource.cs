using System;
using FModel.ViewModels;

namespace FModel.Views.Resources.Controls.Aup;

public interface ISource
{
    AudioFile PlayedFile { get; }
    float[] FftData { get; }
    SpectrumProvider Spectrum { get; }

    void Play();
    void Pause();
    void Resume();
    void Stop();
    void SkipTo(double percentage);
    void Dispose();

    event EventHandler<SourceEventArgs> SourceEvent;
    event EventHandler<SourcePropertyChangedEventArgs> SourcePropertyChangedEvent;
}