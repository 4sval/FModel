//////////////////////////////////////////////
// Apache 2.0  - 2016-2018
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfHexaEditor
{
    /// <summary>
    /// Light Label like control
    /// </summary>
    internal class FastTextLine: FrameworkElement
    {
        private readonly HexEditor _parent;

        #region Constructor

        public FastTextLine(HexEditor parent)
        {
            //Parent hexeditor
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));

            //Default properties
            DataContext = this;
        }

        #endregion Contructor
        
        #region Base properties

        /// <summary>
        /// Definie the foreground
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner(
                typeof(FastTextLine));

        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly DependencyProperty BackgroundProperty =
            TextElement.BackgroundProperty.AddOwner(typeof(FastTextLine),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Defines the background
        /// </summary>
        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(FastTextLine),
                new FrameworkPropertyMetadata(string.Empty,
                    FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// Text to be displayed representation of Byte
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(FastTextLine));

        /// <summary>
        /// The FontWeight property specifies the weight of the font.
        /// </summary>
        public FontWeight FontWeight
        {
            get => (FontWeight)GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        #endregion Base properties

        #region Properties

        public bool AutoWidth { get; set; } = true;
        
        public Point RenderPoint
        {
            get => (Point)GetValue(RenderPointProperty);
            set => SetValue(RenderPointProperty, value);
        }

        // Using a DependencyProperty as the backing store for RenderPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RenderPointProperty =
            DependencyProperty.Register(nameof(RenderPoint), typeof(Point), typeof(FastTextLine),
                new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsMeasure));
        
        #endregion

        /// <summary>
        /// Render the control
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            //Draw background
            if (Background != null)
                dc.DrawRectangle(Background, null, new Rect(0, 0, RenderSize.Width, RenderSize.Height));

            //Draw text
            var formatedText = new FormattedText(Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(_parent.FontFamily, _parent.FontStyle, FontWeight, _parent.FontStretch),
                    _parent.FontSize, Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                                   
            dc.DrawText(formatedText, new Point(RenderPoint.X, RenderPoint.Y));

            if (AutoWidth)
                Width = formatedText.Width + RenderPoint.X;
        }

    }
}