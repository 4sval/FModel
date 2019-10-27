//////////////////////////////////////////////
// Apache 2.0  - 2016-2018
// Base author  : Derek Tremblay (derektremblay666@gmail.com)
// Contributor  : emes30
// Notice       : The idea to make a base class with common bytecontrol code is taken 
//                from emes30 fork (https://github.com/emes30/WpfHexEditorControl)
//////////////////////////////////////////////

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.Interfaces;

namespace WpfHexaEditor
{
    /// <summary>
    /// Base class for bytecontrol
    /// </summary>
    abstract class BaseByte : FrameworkElement, IByteControl
    {
        #region Global class variables
        protected readonly HexEditor _parent;
        private bool _isSelected;
        private ByteAction _action = ByteAction.Nothing;
        private byte? _byte;
        private bool _isHighLight;
        #endregion global class variables

        #region Events

        public event EventHandler ByteModified;
        public event EventHandler MouseSelection;
        public event EventHandler Click;
        public event EventHandler RightClick;
        public event EventHandler DoubleClick;
        public event EventHandler MoveNext;
        public event EventHandler MovePrevious;
        public event EventHandler MoveRight;
        public event EventHandler MoveLeft;
        public event EventHandler MoveUp;
        public event EventHandler MoveDown;
        public event EventHandler MovePageDown;
        public event EventHandler MovePageUp;
        public event EventHandler ByteDeleted;
        public event EventHandler EscapeKey;
        public event EventHandler CtrlzKey;
        public event EventHandler CtrlvKey;
        public event EventHandler CtrlcKey;
        public event EventHandler CtrlaKey;
        public event EventHandler CtrlyKey;

        #endregion Events

        #region Constructor

        protected BaseByte(HexEditor parent)
        {
            //Parent hexeditor
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            
            #region Binding tooltip

            LoadDictionary("/FModel;component/Forms/HexViewer/ToolTipDictionary.xaml");
            var txtBinding = new Binding
            {
                Source = FindResource("ByteToolTip"),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Mode = BindingMode.OneWay
            };

            // Load ressources dictionnary
            void LoadDictionary(string url)
            {
                var ttRes = new ResourceDictionary { Source = new Uri(url, UriKind.Relative) };
                Resources.MergedDictionaries.Add(ttRes);
            }

            SetBinding(ToolTipProperty, txtBinding);
            
            #endregion
            
            //Default properties
            DataContext = this;
            Focusable = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Position in file
        /// </summary>
        public long BytePositionInStream { get; set; } = -1L;

        /// <summary>
        /// Used for selection coloring
        /// </summary>
        public bool FirstSelected { protected get; set; }

        /// <summary>
        /// Used to prevent ByteModified event occurc when we dont want! 
        /// </summary>
        public bool InternalChange { get; set; }

        /// <summary>
        /// Get or set if control as in read only mode
        /// </summary>
        public bool ReadOnlyMode { protected get; set; }

        /// <summary>
        /// Get or set the description to shown in tooltip
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Return true if mouse is over... (Used with traverse methods via IByteControl)
        /// </summary>
        public bool IsMouseOverMe { get; internal set; }

        /// <summary>
        /// Get or Set if control as selected
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;

                _isSelected = value;
                UpdateVisual();
            }
        }

        /// <summary>
        /// Get of Set if control as marked as highlighted
        /// </summary>   
        public bool IsHighLight
        {
            get => _isHighLight;
            set
            {
                if (value == _isHighLight) return;

                _isHighLight = value;
                UpdateVisual();
            }
        }

        /// <summary>
        /// Byte used for this instance
        /// </summary>
        public byte? Byte
        {
            get => _byte;
            set
            {
                _byte = value;

                if (Action != ByteAction.Nothing && InternalChange == false)
                    ByteModified?.Invoke(this, new EventArgs());

                UpdateTextRenderFromByte();
            }
        }

        /// <summary>
        /// Action with this byte
        /// </summary>
        public ByteAction Action
        {
            get => _action;
            set
            {
                _action = value != ByteAction.All ? value : ByteAction.Nothing;

                UpdateVisual();
            }
        }

        protected FormattedText TextFormatted { get; private set; }

        #endregion

        #region Private base properties

        /// <summary>
        /// Definie the foreground
        /// </summary>
        private static readonly DependencyProperty ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner(
                typeof(BaseByte));

        protected Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly DependencyProperty BackgroundProperty =
            TextElement.BackgroundProperty.AddOwner(typeof(BaseByte),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Defines the background
        /// </summary>
        protected Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        private static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(BaseByte),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Text to be displayed representation of Byte
        /// </summary>
        protected string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(BaseByte));

        /// <summary>
        /// The FontWeight property specifies the weight of the font.
        /// </summary>
        protected FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        #endregion Base properties

        #region Methods

        /// <summary>
        /// Update Background,foreground and font property
        /// </summary>
        public virtual void UpdateVisual()
        {
            if (IsSelected)
            {
                FontWeight = _parent.FontWeight;
                Foreground = _parent.ForegroundContrast;

                Background = FirstSelected
                    ? _parent.SelectionFirstColor
                    : _parent.SelectionSecondColor;
            }
            else if (IsHighLight)
            {
                FontWeight = _parent.FontWeight;
                Foreground = _parent.Foreground;
                Background = _parent.HighLightColor;
            }
            else if (Action != ByteAction.Nothing)
            {
                FontWeight = FontWeights.Bold;
                Foreground = _parent.Foreground;

                switch (Action)
                {
                    case ByteAction.Modified:
                        Background = _parent.ByteModifiedColor;
                        break;
                    case ByteAction.Deleted:
                        Background = _parent.ByteDeletedColor;
                        break;
                }
            }
            else
            {
                var cbb = _parent.GetCustomBackgroundBlock(BytePositionInStream);

                Description = cbb != null ? cbb.Description : "";

                Background = cbb != null ? cbb.Color : Brushes.Transparent;
                Foreground = _parent.GetColumnNumber(BytePositionInStream) % 2 == 0 
                    ? _parent.Foreground 
                    : _parent.ForegroundSecondColor;

                FontWeight = _parent.FontWeight;
            }

            UpdateAutoHighLiteSelectionByteVisual();

            InvalidateVisual();
        }

        /// <summary>
        /// Auto highlite SelectionByte
        /// </summary>
        protected void UpdateAutoHighLiteSelectionByteVisual()
        {
            if (_parent.AllowAutoHighLightSelectionByte && _parent.SelectionByte != null &&
                Byte == _parent.SelectionByte && !IsSelected)
                Background = _parent.AutoHighLiteSelectionByteBrush;
        }

        /// <summary>
        /// Update the render of text derived bytecontrol from byte property
        /// </summary>
        public abstract void UpdateTextRenderFromByte();

        /// <summary>
        /// Clear control
        /// </summary>
        public virtual void Clear()
        {
            InternalChange = true;
            BytePositionInStream = -1;
            Byte = null;
            Action = ByteAction.Nothing;
            IsSelected = false;
            InternalChange = false;
            Description = string.Empty;
        }

        #endregion

        #region Events delegate

        /// <summary>
        /// Render the control
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            //Draw background
            if (Background != null)
                dc.DrawRectangle(Background, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));

            //Draw text
            var typeface = new Typeface(_parent.FontFamily, _parent.FontStyle, FontWeight, _parent.FontStretch);

            var formattedText = new FormattedText(Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                typeface, _parent.FontSize, Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(formattedText, new Point(2, 2));

//#if DEBUG && NET47
//            //FOR HELP TO DEBUG HIDEBYTEDELETED ONLY...
//            if (_parent.HideByteDeleted && BytePositionInStream < 10000 && Core.Bytes.ByteConverters.IsHexValue(Text).success)
//            {
//                var formattedText2 = new FormattedText(BytePositionInStream.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
//                    typeface, 8, Brushes.Green, VisualTreeHelper.GetDpi(this).PixelsPerDip);

//                dc.DrawText(formattedText2, new Point(0, 10));
//            }
//#endif

            //Update properties
            TextFormatted = formattedText;
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            if (Byte != null && !IsSelected && !IsHighLight &&
                Action != ByteAction.Modified && 
                Action != ByteAction.Deleted &&
                Action != ByteAction.Added)
                Background = _parent.MouseOverColor;

            UpdateAutoHighLiteSelectionByteVisual();

            if (e.LeftButton == MouseButtonState.Pressed)
                MouseSelection?.Invoke(this, e);

            IsMouseOverMe = true;

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            var cbb = _parent.GetCustomBackgroundBlock(BytePositionInStream);
            
            if (Byte != null && !IsSelected && !IsHighLight &&
                Action != ByteAction.Modified &&
                Action != ByteAction.Deleted &&
                Action != ByteAction.Added)
                Background = Brushes.Transparent;

            if (cbb != null && !IsSelected && !IsHighLight &&
                Action != ByteAction.Modified &&
                Action != ByteAction.Deleted &&
                Action != ByteAction.Added)
                Background = cbb.Color;

            IsMouseOverMe = false;

            UpdateAutoHighLiteSelectionByteVisual();

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!IsFocused)
                    Focus();

                switch (e.ClickCount)
                {
                    case 1:                        
                        Click?.Invoke(this, e);
                        break;
                    case 2:
                        DoubleClick?.Invoke(this, e);
                        break;
                }

            }

            if (e.RightButton == MouseButtonState.Pressed)
                RightClick?.Invoke(this, e);

            base.OnMouseDown(e);
        }

        protected override void OnToolTipOpening(ToolTipEventArgs e)
        {
            if (Byte == null || !_parent.ShowByteToolTip)
                e.Handled = true;

            base.OnToolTipOpening(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            _parent.HideCaret();
            base.OnLostFocus(e);
        }

        protected void OnMoveNext(EventArgs e) => MoveNext?.Invoke(this, e);

        protected bool KeyValidation(KeyEventArgs e)
        {
            #region Key validation and launch event if needed

            if (KeyValidator.IsUpKey(e.Key))
            {
                e.Handled = true;
                MoveUp?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsDownKey(e.Key))
            {
                e.Handled = true;
                MoveDown?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsLeftKey(e.Key))
            {
                e.Handled = true;
                MoveLeft?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsRightKey(e.Key))
            {
                e.Handled = true;
                MoveRight?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsPageDownKey(e.Key))
            {
                e.Handled = true;
                MovePageDown?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsPageUpKey(e.Key))
            {
                e.Handled = true;
                MovePageUp?.Invoke(this, new EventArgs());

                return true;
            }
            if (KeyValidator.IsDeleteKey(e.Key))
            {
                if (!ReadOnlyMode)
                {
                    e.Handled = true;
                    ByteDeleted?.Invoke(this, new EventArgs());

                    return true;
                }
            }
            else if (KeyValidator.IsBackspaceKey(e.Key))
            {
                e.Handled = true;
                ByteDeleted?.Invoke(this, new EventArgs());

                MovePrevious?.Invoke(this, new EventArgs());

                return true;
            }
            else if (KeyValidator.IsEscapeKey(e.Key))
            {
                e.Handled = true;
                EscapeKey?.Invoke(this, new EventArgs());
                return true;
            }
            else if (KeyValidator.IsCtrlZKey(e.Key))
            {
                e.Handled = true;
                CtrlzKey?.Invoke(this, new EventArgs());
                return true;
            }
            else if (KeyValidator.IsCtrlYKey(e.Key))
            {
                e.Handled = true;
                CtrlyKey?.Invoke(this, new EventArgs());
                return true;
            }
            else if (KeyValidator.IsCtrlVKey(e.Key))
            {
                e.Handled = true;
                CtrlvKey?.Invoke(this, new EventArgs());
                return true;
            }
            else if (KeyValidator.IsCtrlCKey(e.Key))
            {
                e.Handled = true;
                CtrlcKey?.Invoke(this, new EventArgs());
                return true;
            }
            else if (KeyValidator.IsCtrlAKey(e.Key))
            {
                e.Handled = true;
                CtrlaKey?.Invoke(this, new EventArgs());
                return true;
            }

            return false;
            #endregion
        }
    }
    #endregion

}

