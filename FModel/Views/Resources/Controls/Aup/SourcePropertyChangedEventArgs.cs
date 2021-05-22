using System;

namespace FModel.Views.Resources.Controls.Aup
{
    public enum ESourceProperty
    {
        FftData,
        PlaybackState,
        Length,
        Position,
        WaveformData,
        RecordingState
    }

    public class SourcePropertyChangedEventArgs : EventArgs
    {
        public ESourceProperty Property { get; }
        public object Value { get; }

        public SourcePropertyChangedEventArgs(ESourceProperty property, object value)
        {
            Property = property;
            Value = value;
        }
    }
}