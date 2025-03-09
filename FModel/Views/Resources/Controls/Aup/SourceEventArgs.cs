using System;

namespace FModel.Views.Resources.Controls.Aup;

public enum ESourceEventType
{
    Loading,
    Clearing
}

public class SourceEventArgs : EventArgs
{
    public ESourceEventType Event { get; }

    public SourceEventArgs(ESourceEventType e)
    {
        Event = e;
    }
}
