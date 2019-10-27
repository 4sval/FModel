//////////////////////////////////////////////
// Fork 2017 : Derek Tremblay (derektremblay666@gmail.com) 
// Part of Wpf HexEditor control : https://github.com/abbaye/WPFHexEditorControl
// Reference : https://www.codeproject.com/Tips/431000/Caret-for-WPF-User-Controls
// Reference license : The Code Project Open License (CPOL) 1.02
// Contributor : emes30
//////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Threading;

namespace WpfHexaEditor.Core
{
    /// <summary>
    /// This class represent a visual caret on editor
    /// </summary>
    public sealed class Caret : FrameworkElement, INotifyPropertyChanged
    {
        #region Global class variables
        private Timer _timer;
        private Point _position;
        private readonly Pen _pen = new Pen(Brushes.Black, 1);
        private readonly Brush _brush = new SolidColorBrush(Colors.Black);
        private int _blinkPeriod = 500;
        private double _caretHeight = 18;
        private double _caretWidth = 9;
        private bool _hide;
        private CaretMode _caretMode = CaretMode.Overwrite;
        #endregion

        #region Constructor
        public Caret()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _pen.Freeze();
                _brush.Opacity = .5;
                IsHitTestVisible = false;
                InitializeTimer();
                Hide();
            }
        }

        public Caret(Brush brush)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _pen.Brush = brush;
                _pen.Freeze();
                _brush.Opacity = .5;
                IsHitTestVisible = false;
                InitializeTimer();
                Hide();
            }
        }
        #endregion

        #region Properties

        private static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register(nameof(Visible), typeof(bool),
                typeof(Caret), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Get is caret is running
        /// </summary>
        public bool IsEnable => _timer != null;

        /// <summary>
        /// Propertie used when caret is blinking
        /// </summary>
        private bool Visible
        {
            get => (bool)GetValue(VisibleProperty);
            set => SetValue(VisibleProperty, value);
        }

        /// <summary>
        /// Height of the caret
        /// </summary>
        public double CaretHeight
        {
            get => _caretHeight;
            set
            {
                if (_caretHeight == value) return;

                _caretHeight = value;

                //InitializeTimer();

                OnPropertyChanged(nameof(CaretHeight));
            }
        }

        /// <summary>
        /// Width of the caret
        /// </summary>
        public double CaretWidth
        {
            get => _caretWidth;
            set
            {
                if (_caretWidth == value) return;

                _caretWidth = value;

                OnPropertyChanged(nameof(CaretWidth));
            }
        }

        /// <summary>
        /// Get the relative position of the caret
        /// </summary>
        public Point Position => _position;

        /// <summary>
        /// Left position of the caret
        /// </summary>
        public double Left
        {
            get => _position.X;
            private set
            {
                if (_position.X == value) return;

                _position.X = Math.Floor(value);
                if (Visible) Visible = false;

                OnPropertyChanged(nameof(Position));
                OnPropertyChanged(nameof(Left));
            }
        }

        /// <summary>
        /// Top position of the caret
        /// </summary>
        public double Top
        {
            get => _position.Y;
            private set
            {
                if (_position.Y == value) return;

                _position.Y = Math.Floor(value);
                if (Visible) Visible = false;

                OnPropertyChanged(nameof(Position));
                OnPropertyChanged(nameof(Top));
            }
        }

        /// <summary>
        /// Properties return true if caret is visible
        /// </summary>
        public bool IsVisibleCaret => Left >= 0 && Top > 0 && _hide == false;

        /// <summary>
        /// Blick period in millisecond
        /// </summary>
        public int BlinkPeriod
        {
            get => _blinkPeriod;
            set
            {
                _blinkPeriod = value;
                InitializeTimer();

                OnPropertyChanged(nameof(BlinkPeriod));
            }
        }

        /// <summary>
        /// Caret display mode. Line for Insert, Block for Overwrite
        /// </summary>
        public CaretMode CaretMode
        {
            get => _caretMode;
            set
            {
                if (_caretMode == value) return;

                _caretMode = value;

                OnPropertyChanged(nameof(CaretMode));
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Hide the caret
        /// </summary>
        public void Hide() => _hide = true;

        /// <summary>
        /// Method delegate for blink the caret
        /// </summary>
        private void BlinkCaret(Object state) => Dispatcher?.Invoke(() =>
        {
            Visible = !Visible && !_hide;
        });

        /// <summary>
        /// Initialise the timer
        /// </summary>
        private void InitializeTimer() => _timer = new Timer(BlinkCaret, null, 0, BlinkPeriod);

        /// <summary>
        /// Move the caret over the position defined by point parameter
        /// </summary>
        public void MoveCaret(Point point) => MoveCaret(point.X, point.Y);

        /// <summary>
        /// Move the caret over the position defined by point parameter
        /// </summary>
        public void MoveCaret(double x, double y)
        {
            _hide = false;
            Left = x;
            Top = y;
        }


        /// <summary>
        /// Start the caret
        /// </summary>
        public void Start()
        {
            InitializeTimer();

            _hide = false;

            OnPropertyChanged(nameof(IsEnable));
        }

        /// <summary>
        /// Stop the carret
        /// </summary>
        public void Stop()
        {
            Hide();
            _timer = null;

            OnPropertyChanged(nameof(IsEnable));
        }

        /// <summary>
        /// Render the caret
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            if (Visible)
                switch (_caretMode)
                {
                    case CaretMode.Insert:
                        dc.DrawLine(_pen, _position, new Point(Left, _position.Y + CaretHeight));
                        break;
                    case CaretMode.Overwrite:
                        dc.DrawRectangle(_brush, _pen, new Rect(Left, _position.Y, _caretWidth, CaretHeight));
                        break;
                }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
