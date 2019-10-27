//////////////////////////////////////////////
// Apache 2.0  - 2016-2019
// Author : Derek Tremblay (derektremblay666@gmail.com)
// Contributor: Janus Tida
//////////////////////////////////////////////

using System;
using System.Windows;
using System.Windows.Input;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.Bytes;
using WpfHexaEditor.Core.MethodExtention;

namespace WpfHexaEditor
{
    internal class HexByte : BaseByte
    {
        #region Global class variables

        private KeyDownLabel _keyDownLabel = KeyDownLabel.FirstChar;

        #endregion global class variables

        #region Constructor

        public HexByte(HexEditor parent) : base(parent)
        {
            //Update width
            UpdateDataVisualWidth();
        }

        #endregion Contructor

        #region Methods

        /// <summary>
        /// Update the render of text derived bytecontrol from byte property
        /// </summary>
        public override void UpdateTextRenderFromByte()
        {
            if (Byte != null)
            {
                switch (_parent.DataStringVisual)
                {
                    case DataVisualType.Hexadecimal:
                        var chArr = ByteConverters.ByteToHexCharArray(Byte.Value);
                        Text = new string(chArr);
                        break;
                    case DataVisualType.Decimal:
                        Text = Byte.Value.ToString("d3");
                        break;
                }
            }
            else
                Text = string.Empty;
        }

        public override void Clear()
        {
            base.Clear();
            _keyDownLabel = KeyDownLabel.FirstChar;
        }

        public void UpdateDataVisualWidth()
        {
            switch (_parent.DataStringVisual)
            {
                case DataVisualType.Decimal:
                    Width = 25;
                    break;
                case DataVisualType.Hexadecimal:
                    Width = 20;
                    break;
            }
        }

        #endregion Methods

        #region Events delegate

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (IsFocused)
                {
                    //Is focused set editing to second char.
                    _keyDownLabel = KeyDownLabel.SecondChar;
                    UpdateCaret();
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Byte == null) return;

            if (KeyValidation(e)) return;

            //MODIFY BYTE
            if (!ReadOnlyMode && KeyValidator.IsHexKey(e.Key))
                switch (_parent.DataStringVisual)
                {
                    case DataVisualType.Hexadecimal:

                        #region Edit hexadecimal value 

                        string key;
                        key = KeyValidator.IsNumericKey(e.Key)
                            ? KeyValidator.GetDigitFromKey(e.Key).ToString()
                            : e.Key.ToString().ToLower();

                        //Update byte
                        var byteValueCharArray = ByteConverters.ByteToHexCharArray(Byte.Value);
                        switch (_keyDownLabel)
                        {
                            case KeyDownLabel.FirstChar:
                                byteValueCharArray[0] = key.ToCharArray()[0];
                                _keyDownLabel = KeyDownLabel.SecondChar;
                                Action = ByteAction.Modified;
                                Byte = ByteConverters.HexToByte(
                                    byteValueCharArray[0] + byteValueCharArray[1].ToString())[0];
                                break;
                            case KeyDownLabel.SecondChar:
                                byteValueCharArray[1] = key.ToCharArray()[0];
                                _keyDownLabel = KeyDownLabel.NextPosition;

                                Action = ByteAction.Modified;
                                Byte = ByteConverters.HexToByte(
                                    byteValueCharArray[0] + byteValueCharArray[1].ToString())[0];

                                //Insert byte at end of file
                                if (_parent.Length != BytePositionInStream + 1)
                                {
                                    _keyDownLabel = KeyDownLabel.NextPosition;
                                    OnMoveNext(new EventArgs());
                                }
                                break;
                            case KeyDownLabel.NextPosition:

                                //byte[] byteToAppend = { (byte)key.ToCharArray()[0] };
                                _parent.AppendByte(new byte[] { 0 });

                                OnMoveNext(new EventArgs());

                                break;
                        }

                        #endregion

                        break;
                    case DataVisualType.Decimal:

                        //Not editable at this moment, maybe in future

                        break;
                }

            UpdateCaret();

            base.OnKeyDown(e);
        }

        #endregion Events delegate

        #region Caret events/methods
        
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            _keyDownLabel = KeyDownLabel.FirstChar;
            UpdateCaret();

            base.OnGotFocus(e);
        }

        private void UpdateCaret()
        {
            if (ReadOnlyMode || Byte == null)
                _parent.HideCaret();
            else
            {
                //TODO: clear size and use BaseByte.TextFormatted property...
                var size = Text[1].ToString()
                    .GetScreenSize(_parent.FontFamily, _parent.FontSize, _parent.FontStyle, FontWeight,
                        _parent.FontStretch, _parent.Foreground);

                _parent.SetCaretSize(Width / 2, size.Height);
                _parent.SetCaretMode(CaretMode.Overwrite);

                switch (_keyDownLabel)
                {
                    case KeyDownLabel.FirstChar:
                        _parent.MoveCaret(TransformToAncestor(_parent).Transform(new Point(0, 0)));
                        break;
                    case KeyDownLabel.SecondChar:
                        _parent.MoveCaret(TransformToAncestor(_parent).Transform(new Point(size.Width + 1, 0)));
                        break;
                    case KeyDownLabel.NextPosition:
                        if (_parent.Length == BytePositionInStream + 1)
                            if (_parent.AllowExtend)
                            {
                                _parent.SetCaretMode(CaretMode.Insert);
                                _parent.MoveCaret(TransformToAncestor(_parent).Transform(new Point(size.Width * 2, 0)));
                            }
                            else
                                _parent.HideCaret();

                        break;
                }
            }
        }

        #endregion

    }
}
