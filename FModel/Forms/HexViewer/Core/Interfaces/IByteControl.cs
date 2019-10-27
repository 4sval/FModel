//////////////////////////////////////////////
// Apache 2.0  - 2017-2019
// Author       : Janus Tida 
// Contributor  : Derek Tremblay
//////////////////////////////////////////////

using System;

namespace WpfHexaEditor.Core.Interfaces
{
    /// <summary>
    /// All byte control inherit from this interface.
    /// This interface is used to reduce the code when manipulate byte control
    /// </summary>
    internal interface IByteControl
    {
        //Properties
        long BytePositionInStream { get; set; }
        ByteAction Action { get; set; }
        byte? Byte { get; set; }
        bool IsHighLight { get; set; }
        bool IsSelected { get; set; }
        bool InternalChange { get; set; }
        bool IsMouseOverMe { get; }

        //Methods
        void UpdateVisual();
        void Clear();

        //Events
        event EventHandler ByteModified;
        event EventHandler MouseSelection;
        event EventHandler Click;
        event EventHandler DoubleClick;
        event EventHandler RightClick;
        event EventHandler MoveNext;
        event EventHandler MovePrevious;
        event EventHandler MoveRight;
        event EventHandler MoveLeft;
        event EventHandler MoveUp;
        event EventHandler MoveDown;
        event EventHandler MovePageDown;
        event EventHandler MovePageUp;
        event EventHandler ByteDeleted;
        event EventHandler EscapeKey;
        event EventHandler CtrlzKey;
        event EventHandler CtrlvKey;
        event EventHandler CtrlcKey;
        event EventHandler CtrlaKey;
        event EventHandler CtrlyKey;
    }
}