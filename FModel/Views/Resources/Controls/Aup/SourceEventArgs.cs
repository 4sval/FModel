using System;

namespace FModel.Views.Resources.Controls.Aup;

public enum ESourceEventType
{
    Loading
}

public class SourceEventArgs : EventArgs
{
    public ESourceEventType Event { get; }

    public SourceEventArgs(ESourceEventType e)
    {
        Event = e;
    }
}