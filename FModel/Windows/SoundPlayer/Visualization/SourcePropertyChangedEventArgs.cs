using System;

namespace FModel.Windows.SoundPlayer.Visualization
{
    public class SourcePropertyChangedEventArgs : EventArgs
    {
        public ESourceProperty Property { get; set; }
        public object Value { get; set; }
    }
}
