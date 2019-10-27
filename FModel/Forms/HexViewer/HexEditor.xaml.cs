//////////////////////////////////////////////
// Apache 2.0  - 2016-2019
// Author : Derek Tremblay (derektremblay666@gmail.com)
//////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.Bytes;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.Core.Interfaces;
using WpfHexaEditor.Core.MethodExtention;
using WpfHexaEditor.Core.Xcbb;
using WpfHexaEditor.Dialog;
using static System.Windows.Forms.SystemInformation;
using static WpfHexaEditor.Core.Bytes.ByteConverters;
using static WpfHexaEditor.Core.Bytes.ByteProvider;
using Path = System.IO.Path;

namespace WpfHexaEditor
{
    /// <summary> 
    /// Wpf HexEditor control implementation
    /// </summary>
    /// <remarks>
    /// A fast, fully customisable Wpf user control for editing file or stream as hexadecimal. 
    /// Can be used in Wpf or WinForm application.
    /// </remarks>
    public partial class HexEditor : IDisposable
    {
        #region Global class variables

        /// <summary>
        /// Byte provider for work with file or stream currently loaded in control.
        /// </summary>
        private ByteProvider _provider;

        /// <summary>
        /// The large change of scroll when clicked on scrollbar
        /// </summary>
        private double _scrollLargeChange = 100;

        /// <summary>
        /// List of byte are highlighted
        /// </summary>
        private Dictionary<long, long> _markedPositionList = new Dictionary<long, long>();

        /// <summary>
        /// Byte position in file when mouse right click occurs.
        /// </summary>
        private long _rightClickBytePosition = -1;

        /// <summary>
        /// Custom character table loaded. Used for show byte as texte.
        /// </summary>
        private TblStream _tblCharacterTable;

        /// <summary>
        /// Hold the count of all byte in file.
        /// </summary>
        private long[] _bytecount;

        /// <summary>
        /// Save the view byte buffer as a field. 
        /// To save the time when Scolling i do not building them every time when scolling.
        /// </summary>
        private byte[] _viewBuffer;

        /// <summary>
        /// Save the view byte buffer position as a field. 
        /// To save the time when Scolling i do not building them every time when scolling.
        /// </summary>
        private long[] _viewBufferBytePosition;

        /// <summary>
        /// Used for control the view on refresh
        /// </summary>
        private long _priLevel;

        /// <summary>
        /// Used with VerticalMoveByTime methods/events to move the scrollbar.
        /// </summary>
        private bool _mouseOnBottom, _mouseOnTop;

        /// <summary>
        /// Used with VerticalMoveByTime methods/events to move the scrollbar.
        /// </summary>
        private long _bottomEnterTimes, _topEnterTimes;

        /// <summary>
        /// Caret used in control to view position
        /// </summary>
        private readonly Caret _caret = new Caret();

        /// <summary>
        /// For detect redondants call when disposing control
        ///  </summary>
        private bool _disposedValue;

        /// <summary>
        /// Highlight the header and offset on SelectionStart property
        /// </summary>
        private bool _highLightSelectionStart = true;

        /// <summary>
        /// Get is the first color...
        /// </summary>
        private FirstColor _firstColor = FirstColor.HexByteData;

        #endregion Global Class variables

        #region Events

        /// <summary>
        /// Occurs when selection start are changed.
        /// </summary>
        public event EventHandler SelectionStartChanged;

        /// <summary>
        /// Occurs when selection stop are changed.
        /// </summary>
        public event EventHandler SelectionStopChanged;

        /// <summary>
        /// Occurs when the length of selection are changed.
        /// </summary>
        public event EventHandler SelectionLengthChanged;

        /// <summary>
        /// Occurs when data are copie to clipboard.
        /// </summary>
        public event EventHandler DataCopied;

        /// <summary>
        /// Occurs when the type of character table are changed.
        /// </summary>
        public event EventHandler TypeOfCharacterTableChanged;

        /// <summary>
        /// Occurs when a long process percent changed.
        /// </summary>
        public event EventHandler LongProcessProgressChanged;

        /// <summary>
        /// Occurs when a long process are started.
        /// </summary>
        public event EventHandler LongProcessProgressStarted;

        /// <summary>
        /// Occurs when a long process are completed.
        /// </summary>
        public event EventHandler LongProcessProgressCompleted;

        /// <summary>
        /// Occurs when readonly property are changed.
        /// </summary>
        public event EventHandler ReadOnlyChanged;

        /// <summary>
        /// Occurs when data are saved to stream/file.
        /// </summary>
        public event EventHandler ChangesSubmited;

        /// <summary>
        /// Occurs when the replace byte by byte are completed
        /// </summary>
        public event EventHandler ReplaceByteCompleted;

        /// <summary>
        /// Occurs when the fill with byte method are completed
        /// </summary>
        public event EventHandler FillWithByteCompleted;

        /// <summary>
        /// Occurs when bytes as deleted in control
        /// </summary>
        public event EventHandler BytesDeleted;

        /// <summary>
        /// Occurs when byte as modified in control
        /// </summary>
        public event EventHandler BytesModified;

        /// <summary>
        /// Occurs when undo are completed
        /// </summary>
        public event EventHandler Undone;

        /// <summary>
        /// Occurs when redo are completed
        /// </summary>
        public event EventHandler Redone;

        /// <summary>
        /// Occurs on byte click completed
        /// </summary>
        public event EventHandler ByteClick;

        /// <summary>
        /// Occurs on byte double click completed
        /// </summary>
        public event EventHandler ByteDoubleClick;



        #endregion Events

        #region Constructor

        public HexEditor()
        {
            InitializeComponent();

            //Refresh view
            UpdateScrollBar();
            InitializeCaret();
            RefreshView(true);
        }
        #endregion Contructor

        #region Build-in CTRL key support

        public bool AllowBuildinCtrlc
        {
            get => (bool)GetValue(AllowBuildinCtrlcProperty);
            set => SetValue(AllowBuildinCtrlcProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLC.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCtrlcProperty =
            DependencyProperty.Register(nameof(AllowBuildinCtrlc), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, Control_AllowBuildinCTRLPropertyChanged));

        public bool AllowBuildinCtrlv
        {
            get => (bool)GetValue(AllowBuildinCtrlvProperty);
            set => SetValue(AllowBuildinCtrlvProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLV.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCtrlvProperty =
            DependencyProperty.Register(nameof(AllowBuildinCtrlv), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, Control_AllowBuildinCTRLPropertyChanged));

        public bool AllowBuildinCtrla
        {
            get => (bool)GetValue(AllowBuildinCtrlaProperty);
            set => SetValue(AllowBuildinCtrlaProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLA.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCtrlaProperty =
            DependencyProperty.Register(nameof(AllowBuildinCtrla), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, Control_AllowBuildinCTRLPropertyChanged));

        public bool AllowBuildinCtrlz
        {
            get => (bool)GetValue(AllowBuildinCtrlzProperty);
            set => SetValue(AllowBuildinCtrlzProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCtrlzProperty =
            DependencyProperty.Register(nameof(AllowBuildinCtrlz), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, Control_AllowBuildinCTRLPropertyChanged));

        public bool AllowBuildinCtrly
        {
            get => (bool)GetValue(AllowBuildinCtrlyProperty);
            set => SetValue(AllowBuildinCtrlyProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowBuildinCTRLY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowBuildinCtrlyProperty =
            DependencyProperty.Register(nameof(AllowBuildinCtrly), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, Control_AllowBuildinCTRLPropertyChanged));

        private static void Control_AllowBuildinCTRLPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue) ctrl.RefreshView();
        }

        private void Control_CTRLZKey(object sender, EventArgs e)
        {
            if (AllowBuildinCtrlz) Undo();
        }

        private void Control_CTRLYKey(object sender, EventArgs e)
        {
            if (AllowBuildinCtrly) Redo();
        }

        private void Control_CTRLCKey(object sender, EventArgs e)
        {
            if (AllowBuildinCtrlc) CopyToClipboard();
        }

        private void Control_CTRLAKey(object sender, EventArgs e)
        {
            if (AllowBuildinCtrla) SelectAll();
        }

        private void Control_CTRLVKey(object sender, EventArgs e)
        {
            if (AllowBuildinCtrlv) Paste(AllowExtend);
        }

        #endregion Build-in CTRL key support

        #region Colors/fonts property and methods

        public Brush SelectionFirstColor
        {
            get => (Brush)GetValue(SelectionFirstColorProperty);
            set => SetValue(SelectionFirstColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectionFirstColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionFirstColorProperty =
            DependencyProperty.Register(nameof(SelectionFirstColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.CornflowerBlue, Control_ColorPropertyChanged));

        public Brush SelectionSecondColor
        {
            get => (Brush)GetValue(SelectionSecondColorProperty);
            set => SetValue(SelectionSecondColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectionFirstColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionSecondColorProperty =
            DependencyProperty.Register(nameof(SelectionSecondColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.LightSteelBlue, Control_ColorPropertyChanged));

        public Brush ByteModifiedColor
        {
            get => (Brush)GetValue(ByteModifiedColorProperty);
            set => SetValue(ByteModifiedColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteModifiedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteModifiedColorProperty =
            DependencyProperty.Register(nameof(ByteModifiedColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.DarkGray, Control_ColorPropertyChanged));

        public Brush MouseOverColor
        {
            get => (Brush)GetValue(MouseOverColorProperty);
            set => SetValue(MouseOverColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for MouseOverColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MouseOverColorProperty =
            DependencyProperty.Register(nameof(MouseOverColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.LightSkyBlue, Control_ColorPropertyChanged));

        public Brush ByteDeletedColor
        {
            get => (Brush)GetValue(ByteDeletedColorProperty);
            set => SetValue(ByteDeletedColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteDeletedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteDeletedColorProperty =
            DependencyProperty.Register(nameof(ByteDeletedColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Red, Control_ColorPropertyChanged));

        public Brush HighLightColor
        {
            get => (Brush)GetValue(HighLightColorProperty);
            set => SetValue(HighLightColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for HighLightColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighLightColorProperty =
            DependencyProperty.Register(nameof(HighLightColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Gold, Control_ColorPropertyChanged));

        public Brush ForegroundHighLightOffSetHeaderColor
        {
            get => (Brush)GetValue(ForegroundHighLightOffSetHeaderColorProperty);
            set => SetValue(ForegroundHighLightOffSetHeaderColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ForegroundOffSetHeaderColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundHighLightOffSetHeaderColorProperty =
            DependencyProperty.Register(nameof(ForegroundHighLightOffSetHeaderColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Black, Control_ForegroundOffSetHeaderColorPropertyChanged));

        public Brush ForegroundOffSetHeaderColor
        {
            get => (Brush)GetValue(ForegroundOffSetHeaderColorProperty);
            set => SetValue(ForegroundOffSetHeaderColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ForegroundOffSetHeaderColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundOffSetHeaderColorProperty =
            DependencyProperty.Register(nameof(ForegroundOffSetHeaderColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Gray, Control_ForegroundOffSetHeaderColorPropertyChanged));

        private static void Control_ForegroundOffSetHeaderColorPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl) || e.NewValue == e.OldValue) return;

            ctrl.UpdateHeader();
            ctrl.UpdateLinesInfo();
        }

        public new Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public new Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        // Using a DependencyProperty as the backing store for Foreground.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Black, Control_ColorPropertyChanged));

        /// <summary>
        /// Second foreground colors used in hexbyte
        /// </summary>
        public Brush ForegroundSecondColor
        {
            get => (Brush)GetValue(ForegroundSecondColorProperty);
            set => SetValue(ForegroundSecondColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for ForegroundSecond.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundSecondColorProperty =
            DependencyProperty.Register(nameof(ForegroundSecondColor), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Blue, Control_ColorPropertyChanged));


        public Brush ForegroundContrast
        {
            get => (Brush)GetValue(ForegroundContrastProperty);
            set => SetValue(ForegroundContrastProperty, value);
        }

        // Using a DependencyProperty as the backing store for ForegroundContrastColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForegroundContrastProperty =
            DependencyProperty.Register(nameof(ForegroundContrast), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.White, Control_ColorPropertyChanged));

        // Using a DependencyProperty as the backing store for  Background.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.White, Control_BackgroundColorPropertyChanged));

        private static void Control_BackgroundColorPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
                ctrl.BaseGrid.Background = (Brush)e.NewValue;
        }

        private static void Control_ColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
                ctrl.UpdateVisual();
        }

        public new FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        // Using a DependencyProperty as the backing store for FontFamily.  This enables animation, styling, binding, etc...
        public new static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(nameof(FontFamily), typeof(FontFamily), typeof(HexEditor),
                new FrameworkPropertyMetadata(new FontFamily("Courier New"),
                    FontFamily_Changed));

        private static void FontFamily_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
                ctrl.RefreshView(true);
        }

        /// <summary>
        /// Call Updatevisual methods for all IByteControl
        /// </summary>
        public void UpdateVisual() => TraverseHexAndStringBytes(ctrl => { ctrl.UpdateVisual(); });

        #endregion Colors/fonts property and methods

        #region Miscellaneous property/methods

        /// <summary>
        /// The name of your application to be showing in messagebox title
        /// </summary>
        public string ApplicationName { get; set; } = "Wpf HexEditor";

        /// <summary>
        /// Height of data line. 
        /// </summary>
        public double LineHeight
        {
            get => (double)GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        // Using a DependencyProperty as the backing store for LineHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LineHeightProperty =
            DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(HexEditor),
                new FrameworkPropertyMetadata(18D, LineHeight_PropertyChanged));

        private static void LineHeight_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.RefreshView();
        }

        public OffSetPanelType OffSetPanelVisual
        {
            get { return (OffSetPanelType)GetValue(OffSetPanelVisualProperty); }
            set { SetValue(OffSetPanelVisualProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OffSetPanelVisual.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OffSetPanelVisualProperty =
            DependencyProperty.Register(nameof(OffSetPanelVisual), typeof(OffSetPanelType), typeof(HexEditor),
                new FrameworkPropertyMetadata(OffSetPanelType.OffsetOnly, OffSetPanelVisual_PropertyChanged));

        private static void OffSetPanelVisual_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.UpdateLinesInfo();
        }

        public OffSetPanelFixedWidth OffSetPanelFixedWidthVisual
        {
            get { return (OffSetPanelFixedWidth)GetValue(OffSetPanelFixedWidthVisualProperty); }
            set { SetValue(OffSetPanelFixedWidthVisualProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OffSetPanelFixedWidthVisual.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OffSetPanelFixedWidthVisualProperty =
            DependencyProperty.Register(nameof(OffSetPanelFixedWidthVisual), typeof(OffSetPanelFixedWidth), typeof(HexEditor),
                new FrameworkPropertyMetadata(OffSetPanelFixedWidth.Dynamic, OffSetPanelVisual_PropertyChanged));


        /// <summary>
        /// Show or not the tooltip of byte in hexeditor
        /// </summary>
        public bool ShowByteToolTip
        {
            get { return (bool)GetValue(ShowByteToolTipProperty); }
            set { SetValue(ShowByteToolTipProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowByteToolTip.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowByteToolTipProperty =
            DependencyProperty.Register(nameof(ShowByteToolTip), typeof(bool), typeof(HexEditor),
                new PropertyMetadata(false));

        #endregion Miscellaneous property/methods

        #region Data visual type support

        /// <summary>
        /// Set or get the visual of line offset header
        /// </summary>
        public DataVisualType OffSetStringVisual
        {
            get => (DataVisualType)GetValue(OffSetStringVisualProperty);
            set => SetValue(OffSetStringVisualProperty, value);
        }

        // Using a DependencyProperty as the backing store for OffSetStringVisual.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OffSetStringVisualProperty =
            DependencyProperty.Register(nameof(OffSetStringVisual), typeof(DataVisualType), typeof(HexEditor),
                new FrameworkPropertyMetadata(DataVisualType.Hexadecimal, DataVisualTypeProperty_PropertyChanged));


        private static void DataVisualTypeProperty_PropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
                ctrl.UpdateLinesInfo();
        }

        public DataVisualType DataStringVisual
        {
            get => (DataVisualType)GetValue(DataStringVisualProperty);
            set => SetValue(DataStringVisualProperty, value);
        }

        // Using a DependencyProperty as the backing store for HexByteStringVisual.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataStringVisualProperty =
            DependencyProperty.Register(nameof(DataStringVisual), typeof(DataVisualType), typeof(HexEditor),
                new FrameworkPropertyMetadata(DataVisualType.Hexadecimal, DataStringVisualTypeProperty_PropertyChanged));

        private static void DataStringVisualTypeProperty_PropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl) || e.NewValue == e.OldValue) return;

            ctrl.UpdateHeader();

            ctrl.TraverseHexBytes(hctrl =>
            {
                hctrl.UpdateDataVisualWidth();
                hctrl.UpdateTextRenderFromByte();
            });
        }

        #endregion Data visual type support

        #region Characters tables property/methods

        /// <summary>
        /// Type of caracter table are used un hexacontrol.
        /// For now, somes character table can be readonly but will change in future
        /// </summary>
        public CharacterTableType TypeOfCharacterTable
        {
            get => (CharacterTableType)GetValue(TypeOfCharacterTableProperty);
            set => SetValue(TypeOfCharacterTableProperty, value);
        }

        // Using a DependencyProperty as the backing store for TypeOfCharacterTable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TypeOfCharacterTableProperty =
            DependencyProperty.Register(nameof(TypeOfCharacterTable), typeof(CharacterTableType), typeof(HexEditor),
                new FrameworkPropertyMetadata(CharacterTableType.Ascii,
                    TypeOfCharacterTable_PropertyChanged));

        private static void TypeOfCharacterTable_PropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl)) return;

            ctrl.RefreshView(true);
            ctrl.TypeOfCharacterTableChanged?.Invoke(ctrl, new EventArgs());
        }

        /// <summary>
        /// Show or not Multi Title Enconding (MTE) are loaded in TBL file
        /// </summary>
        public bool TblShowMte
        {
            get => (bool)GetValue(TblShowMteProperty);
            set => SetValue(TblShowMteProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLShowMTE.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TblShowMteProperty =
            DependencyProperty.Register(nameof(TblShowMte), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true,
                    TBLShowMTE_PropetyChanged));

        private static void TBLShowMTE_PropetyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.RefreshView();
        }

        /// <summary>
        /// Load TBL Character table file in control. (Used for ROM reverse engineering)
        /// Load TBL Bookmark into control.
        /// Change CharacterTable property for use.
        /// </summary>
        public void LoadTblFile(string fileName)
        {
            if (!File.Exists(fileName)) return;

            _tblCharacterTable = new TblStream(fileName);

            TblLabel.Visibility = Visibility.Visible;
            TblLabel.ToolTip = $"TBL file : {fileName}";

            UpdateTblBookMark();

            BuildDataLines(MaxVisibleLine, true);
            RefreshView(true);
        }

        /// <summary>
        /// Load TBL Character table file in control. (Used for ROM reverse engineering)
        /// Load TBL Bookmark into control.
        /// Change CharacterTable property for use.
        /// </summary>
        public void LoadDefaultTbl(DefaultCharacterTableType type = DefaultCharacterTableType.Ascii)
        {
            _tblCharacterTable = TblStream.CreateDefaultTbl(type);
            TblShowMte = false;

            TblLabel.Visibility = Visibility.Visible;
            TblLabel.ToolTip = $"{FModel.Properties.Resources.DefaultTBLString} : {type}";

            RefreshView();
        }

        /// <summary>
        /// Update TBL bookmark in control
        /// </summary>
        private void UpdateTblBookMark()
        {
            //Load from loaded TBL bookmark
            if (_tblCharacterTable == null) return;

            foreach (var mark in _tblCharacterTable.BookMarks)
                SetScrollMarker(mark);
        }

        /// <summary>
        /// Get or set the color of DTE in string panel.
        /// </summary>
        public SolidColorBrush TbldteColor
        {
            get => (SolidColorBrush)GetValue(TbldteColorProperty);
            set => SetValue(TbldteColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TbldteColorProperty =
            DependencyProperty.Register(nameof(TbldteColor), typeof(SolidColorBrush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Red,
                    TBLColor_Changed));

        private static void TBLColor_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.RefreshView();
        }

        /// <summary>
        /// Get or set the color of MTE in string panel.
        /// </summary>
        public SolidColorBrush TblmteColor
        {
            get => (SolidColorBrush)GetValue(TblmteColorProperty);
            set => SetValue(TblmteColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TblmteColorProperty =
            DependencyProperty.Register(nameof(TblmteColor), typeof(SolidColorBrush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.DarkSlateGray,
                    TBLColor_Changed));

        /// <summary>
        /// Get or set the color of EndBlock in string panel.
        /// </summary>
        public SolidColorBrush TblEndBlockColor
        {
            get => (SolidColorBrush)GetValue(TblEndBlockColorProperty);
            set => SetValue(TblEndBlockColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TblEndBlockColorProperty =
            DependencyProperty.Register(nameof(TblEndBlockColor), typeof(SolidColorBrush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Blue,
                    TBLColor_Changed));

        /// <summary>
        /// Get or set the color of EndBlock in string panel.
        /// </summary>
        public SolidColorBrush TblEndLineColor
        {
            get => (SolidColorBrush)GetValue(TblEndLineColorProperty);
            set => SetValue(TblEndLineColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TblEndLineColorProperty =
            DependencyProperty.Register(nameof(TblEndLineColor), typeof(SolidColorBrush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Blue,
                    TBLColor_Changed));

        /// <summary>
        /// Get or set the color of EndBlock in string panel.
        /// </summary>
        public SolidColorBrush TblDefaultColor
        {
            get => (SolidColorBrush)GetValue(TblDefaultColorProperty);
            set => SetValue(TblDefaultColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for TBLDTEColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TblDefaultColorProperty =
            DependencyProperty.Register(nameof(TblDefaultColor), typeof(SolidColorBrush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.Black,
                    TBLColor_Changed));

        #endregion Characters tables property/methods

        #region ReadOnly property/event

        /// <summary>
        /// Put the control on readonly mode.
        /// </summary>
        public bool ReadOnlyMode
        {
            get => (bool)GetValue(ReadOnlyModeProperty);
            set => SetValue(ReadOnlyModeProperty, value);
        }

        public static readonly DependencyProperty ReadOnlyModeProperty =
            DependencyProperty.Register("ReadOnlyMode", typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(false,
                    ReadOnlyMode_PropertyChanged));

        private static void ReadOnlyMode_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
                ctrl.RefreshView(true);
        }

        private void Provider_ReadOnlyChanged(object sender, EventArgs e)
        {
            if (!CheckIsOpen(_provider)) return;

            ReadOnlyMode = _provider.ReadOnlyMode;
            ReadOnlyChanged?.Invoke(this, new EventArgs());
        }

        #endregion ReadOnly property/event

        #region ByteModified methods/event/property
        /// <summary>
        /// Stream or file are modified when IsModified are set to true.
        /// </summary>
        public bool IsModified { get; set; } = false;

        private void Control_ByteModified(object sender, EventArgs e)
        {
            if (sender is IByteControl ctrl)
            {
                _provider.AddByteModified(ctrl.Byte, ctrl.BytePositionInStream);
                SetScrollMarker(ctrl.BytePositionInStream, ScrollMarker.ByteModified);
                UpdateByteModified();

                BytesModified?.Invoke(this, new EventArgs());
            }

            UpdateStatusBar();
        }

        private void Control_ByteDeleted(object sender, EventArgs e) => DeleteSelection();

        #endregion ByteModified methods/event/methods

        #region Lines methods
        /// <summary>
        /// Obtain the max line for vertical scrollbar
        /// </summary>
        private long MaxLine
        {
            get
            {
                long byteDeletedCount = 0;
                if (CheckIsOpen(_provider) && HideByteDeleted)
                    byteDeletedCount = _provider.GetByteModifieds(ByteAction.Deleted).Count;

                return AllowVisualByteAddress
                          ? CheckIsOpen(_provider) ? (VisualByteAdressLength - byteDeletedCount) / BytePerLine : 0
                          : CheckIsOpen(_provider) ? (_provider.Length - byteDeletedCount) / BytePerLine : 0;
            }
        }

        /// <summary>
        /// Get the number of row visible in control
        /// </summary>
        private int MaxVisibleLine
        {
            get
            {
                var actualheight = BaseGrid.RowDefinitions[1].ActualHeight;

                if (actualheight < 0) actualheight = 0;

                return (int)(actualheight / (LineHeight * ZoomScale)) + 1;
            }
        }

        #endregion Lines methods

        #region Selection Property/Methods/Event

        /// <summary>
        /// Get the selected line of focus control
        /// </summary>
        public long SelectionLine
        {
            get => (long)GetValue(SelectionLineProperty);
            internal set => SetValue(SelectionLineProperty, value);
        }

        public static readonly DependencyProperty SelectionLineProperty =
            DependencyProperty.Register(nameof(SelectionLine), typeof(long), typeof(HexEditor),
                new FrameworkPropertyMetadata(0L));

        private void LinesOffSetLabel_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is FastTextLine line && e.LeftButton == MouseButtonState.Pressed)
                SelectionStop = HexLiteralToLong((string)line.Tag).position + BytePerLine - 1;
        }

        private void LinesOffSetLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is FastTextLine line)) return;

            SelectionStart = HexLiteralToLong((string)line.Tag).position;
            SelectionStop = SelectionStart + BytePerLine - 1 - GetColumnNumber(SelectionStart);

            HideCaret();
        }

        private void Control_EscapeKey(object sender, EventArgs e)
        {
            UnSelectAll();
            UnHighLightAll();
            Focus();
        }

        private void Control_KeyDown(object sender, KeyEventArgs e)
        {
            //TODO: need to fix... not occurs all times as needed.

            switch (e.Key)
            {
                case Key.Delete:
                    DeleteSelection();
                    break;
                case Key.Escape:
                    UnSelectAll();
                    UnHighLightAll();
                    Focus();
                    break;
            }
        }

        private void Control_MovePageUp(object sender, EventArgs e)
        {
            //Prevent infinite loop
            _setFocusTest = false;

            //Get the new position from SelectionStart down one page
            var newPosition = GetValidPositionFrom(SelectionStart, -(BytePerLine * MaxVisibleLine));

            if (Keyboard.Modifiers == ModifierKeys.Shift)
                SelectionStart = newPosition < _provider.Length ? newPosition : 0;
            else
            {
                FixSelectionStartStop();

                if (newPosition > -1)
                    SelectionStart = SelectionStop = newPosition;
            }

            if (AllowVisualByteAddress && SelectionStart > VisualByteAdressStart)
                SelectionStart = VisualByteAdressStart;

            if (SelectionStart < FirstVisibleBytePosition)
                VerticalScrollBar.Value--;

            if (sender is HexByte || sender is StringByte)
            {
                VerticalScrollBar.Value -= MaxVisibleLine - 1;
                SetFocusAtSelectionStart();
            }
        }

        private void Control_MovePageDown(object sender, EventArgs e)
        {
            //Prevent infinite loop
            _setFocusTest = false;

            //Get the new position from SelectionStart down one page
            var newPosition = GetValidPositionFrom(SelectionStart, BytePerLine * MaxVisibleLine);

            if (Keyboard.Modifiers == ModifierKeys.Shift)
                SelectionStart = newPosition < _provider.Length ? newPosition : _provider.Length;
            else
            {
                FixSelectionStartStop();

                if (newPosition < _provider.Length)
                    SelectionStart = SelectionStop = newPosition;
            }

            if (AllowVisualByteAddress && SelectionStart > VisualByteAdressStop)
                SelectionStart = VisualByteAdressStop;

            if (SelectionStart > LastVisibleBytePosition)
                VerticalScrollBar.Value++;

            if (sender is HexByte || sender is StringByte)
            {
                VerticalScrollBar.Value += MaxVisibleLine - 1;
                SetFocusAtSelectionStart();
            }
        }

        private void Control_MoveDown(object sender, EventArgs e)
        {
            _setFocusTest = false;

            var newPosition = GetValidPositionFrom(SelectionStart, BytePerLine);

            if (Keyboard.Modifiers == ModifierKeys.Shift)
                SelectionStart = newPosition < _provider.Length ? newPosition : _provider.Length;
            else
            {
                FixSelectionStartStop();

                if (newPosition < _provider.Length)
                    SelectionStart = SelectionStop = newPosition;
            }

            if (AllowVisualByteAddress && SelectionStart > VisualByteAdressStop)
                SelectionStart = VisualByteAdressStop;

            if (SelectionStart > LastVisibleBytePosition)
                VerticalScrollBar.Value++;

            SetFocusAtSelectionStart();
        }

        private void Control_MoveUp(object sender, EventArgs e)
        {
            //Prevent infinite loop
            _setFocusTest = false;

            //Get the new position from SelectionStart
            var newPosition = GetValidPositionFrom(SelectionStart, -BytePerLine);

            if (Keyboard.Modifiers == ModifierKeys.Shift)
                SelectionStart = newPosition > -1 ? newPosition : 0;
            else
            {
                FixSelectionStartStop();

                if (newPosition > -1)
                    SelectionStart = SelectionStop = newPosition;
            }

            if (AllowVisualByteAddress && SelectionStart < VisualByteAdressStart)
                SelectionStart = VisualByteAdressStart;

            if (SelectionStart < FirstVisibleBytePosition)
                VerticalScrollBar.Value--;

            SetFocusAtSelectionStart();
        }

        private void Control_MouseSelection(object sender, EventArgs e)
        {
            //Prevent false mouse selection on file open
            if (SelectionStart == -1) return;

            if (!(sender is IByteControl bCtrl)) return;

            var focusedControl = Keyboard.FocusedElement;

            //update selection
            SelectionStop = bCtrl.BytePositionInStream != -1 ? bCtrl.BytePositionInStream : LastVisibleBytePosition;

            UpdateSelectionColor(focusedControl is HexByte ? FirstColor.HexByteData : FirstColor.StringByteData);
            UpdateSelection();
        }

        /// <summary>
        /// Set the start byte position of selection
        /// </summary>
        public long SelectionStart
        {
            get => (long)GetValue(SelectionStartProperty);
            set => SetValue(SelectionStartProperty, value);
        }

        public static readonly DependencyProperty SelectionStartProperty =
            DependencyProperty.Register("SelectionStart", typeof(long), typeof(HexEditor),
                new FrameworkPropertyMetadata(-1L, SelectionStart_ChangedCallBack,
                    SelectionStart_CoerceValueCallBack));

        private static object SelectionStart_CoerceValueCallBack(DependencyObject d, object baseValue)
        {
            if (!(d is HexEditor ctrl)) return -1L;
            if (!CheckIsOpen(ctrl._provider)) return -1L;
            if ((long)baseValue < -1) return -1L;

            return baseValue;
        }

        private static void SelectionStart_ChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl)) return;
            if (e.NewValue == e.OldValue) return;
            if (!CheckIsOpen(ctrl._provider)) return;

            ctrl.SelectionByte = ctrl._provider.GetByte(ctrl.SelectionStart).singleByte;

            ctrl.UpdateSelection();
            ctrl.UpdateSelectionLine();
            ctrl.UpdateVisual();
            ctrl.UpdateStatusBar();
            ctrl.UpdateLinesInfo();
            ctrl.UpdateHeader(true);
            ctrl.SetScrollMarker(0, ScrollMarker.SelectionStart);

            ctrl.SelectionStartChanged?.Invoke(ctrl, new EventArgs());
            ctrl.SelectionLengthChanged?.Invoke(ctrl, new EventArgs());
        }

        /// <summary>
        /// Set the start byte position of selection
        /// </summary>
        public long SelectionStop
        {
            get => (long)GetValue(SelectionStopProperty);
            set => SetValue(SelectionStopProperty, value);
        }

        public static readonly DependencyProperty SelectionStopProperty =
            DependencyProperty.Register("SelectionStop", typeof(long), typeof(HexEditor),
                new FrameworkPropertyMetadata(-1L, SelectionStop_ChangedCallBack,
                    SelectionStop_CoerceValueCallBack));

        private static object SelectionStop_CoerceValueCallBack(DependencyObject d, object baseValue)
        {
            if (!(d is HexEditor ctrl)) return baseValue;

            var value = (long)baseValue;

            if (value < -1 || !CheckIsOpen(ctrl._provider)) return -1L;

            return value >= ctrl._provider.Length ? ctrl._provider.Length : baseValue;
        }

        private static void SelectionStop_ChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl) || e.NewValue == e.OldValue) return;

            ctrl.UpdateSelection();
            ctrl.UpdateSelectionLine();

            ctrl.SelectionStopChanged?.Invoke(ctrl, new EventArgs());
            ctrl.SelectionLengthChanged?.Invoke(ctrl, new EventArgs());
        }

        /// <summary>
        /// Fix the selection start and stop when needed
        /// </summary>
        private void FixSelectionStartStop()
        {
            if (SelectionStart > SelectionStop)
                SelectionStart = SelectionStop;
            else
                SelectionStop = SelectionStart;
        }

        /// <summary>
        /// Reset selection to -1
        /// </summary>
        public void UnSelectAll(bool cleanFocus = false)
        {
            SelectionStart = SelectionStop = -1;

            if (cleanFocus)
            {
                HideCaret();
                Focus();
            }
        }

        /// <summary>
        /// Select the entire file
        /// If file are closed the selection will be set to -1
        /// </summary>
        public void SelectAll()
        {
            if (CheckIsOpen(_provider))
            {
                SelectionStart = 0;
                SelectionStop = _provider.Length;
            }
            else
            {
                SelectionStart = -1;
                SelectionStop = -1;
            }

            UpdateSelection();
        }

        /// <summary>
        /// Get the length of byte are selected (base 1)
        /// </summary>
        public long SelectionLength => GetSelectionLength(SelectionStart, SelectionStop);

        /// <summary>
        /// Get byte array from current selection
        /// </summary>
        public byte[] SelectionByteArray
        {
            get
            {
                using (var ms = new MemoryStream())
                {
                    CopyToStream(ms, true);
                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Get string from current selection
        /// </summary>
        public string SelectionString
        {
            get
            {
                using (var ms = new MemoryStream())
                {
                    CopyToStream(ms, true);
                    return BytesToString(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Get Hexadecimal from current selection
        /// </summary>
        public string SelectionHex
        {
            get
            {
                using (var ms = new MemoryStream())
                {
                    CopyToStream(ms, true);
                    return ByteToHex(ms.ToArray());
                }
            }
        }

        private void Control_Unloaded(object sender, RoutedEventArgs e)
        {
            //CloseProvider();
        }

        private void Control_MoveRight(object sender, EventArgs e)
        {
            //Prevent infinite loop
            _setFocusTest = false;

            //Get the new position from SelectionStart down one page
            var newPosition = GetValidPositionFrom(SelectionStart, 1);

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (newPosition <= _provider.Length)
                    SelectionStop = GetValidPositionFrom(SelectionStop, 1);
                else
                    SelectionStart = _provider.Length;
            }
            else
            {
                FixSelectionStartStop();

                if (newPosition < _provider.Length)
                    SelectionStart = SelectionStop = newPosition;
            }

            if (SelectionStart >= _provider.Length)
                SelectionStart = _provider.Length - 1;

            if (AllowVisualByteAddress && SelectionStart > VisualByteAdressStop)
                SelectionStart = VisualByteAdressStop;

            if (SelectionStart > LastVisibleBytePosition)
                VerticalScrollBar.Value++;

            SetFocusAtSelectionStart();
        }

        private void Control_MoveLeft(object sender, EventArgs e)
        {
            //Prevent infinite loop
            _setFocusTest = false;

            //Get the new position from SelectionStart down one page
            var newPosition = GetValidPositionFrom(SelectionStart, -1);

            if (Keyboard.Modifiers == ModifierKeys.Shift)
                SelectionStart = newPosition > -1 ? newPosition : 0;
            else
            {
                FixSelectionStartStop();

                if (newPosition > -1)
                    SelectionStart = SelectionStop = newPosition;
            }

            if (SelectionStart < 0)
                SelectionStart = 0;

            if (AllowVisualByteAddress && SelectionStart < VisualByteAdressStart)
                SelectionStart = VisualByteAdressStart;

            if (SelectionStart < FirstVisibleBytePosition)
                VerticalScrollBar.Value--;

            SetFocusAtSelectionStart();
        }

        private void Control_MovePrevious(object sender, EventArgs e)
        {
            UpdateByteModified();

            //Call move left event
            Control_MoveLeft(sender, new EventArgs());
        }

        private void Control_MoveNext(object sender, EventArgs e)
        {
            UpdateByteModified();

            //Call moveright event
            Control_MoveRight(sender, new EventArgs());
        }

        #endregion Selection Property/Methods/Event

        #region Copy/Paste/Cut Methods

        /// <summary>
        /// Set or get the default copy to clipboard mode
        /// </summary>
        public CopyPasteMode DefaultCopyToClipboardMode { get; set; } = CopyPasteMode.HexaString;

        /// <summary>
        /// Paste clipboard string without inserting byte at selection start
        /// </summary>
        /// <param name="expendIfneeded">Set AllowExpend to true for working</param>
        private void Paste(bool expendIfneeded)
        {
            if (!CheckIsOpen(_provider) || SelectionStart <= -1) return;

            var clipBoardText = Clipboard.GetText();
            var (success, byteArray) = IsHexaByteStringValue(clipBoardText);

            #region Expend stream if needed

            var pastelength = success ? byteArray.Length : clipBoardText.Length;
            var needToBeExtent = _provider.Position + pastelength > _provider.Length;
            var expend = false;
            if (expendIfneeded && AllowExtend && needToBeExtent)
                if (AppendNeedConfirmation)
                {
                    if (MessageBox.Show(FModel.Properties.Resources.PasteExtendByteConfirmationString, ApplicationName,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                        expend = true;
                }
                else
                    expend = true;

            #endregion

            if (success)
                _provider.Paste(SelectionStart, byteArray, expend);
            else
                _provider.Paste(SelectionStart, clipBoardText, expend);

            SetScrollMarker(SelectionStart, ScrollMarker.ByteModified, FModel.Properties.Resources.PasteFromClipboardString);
            RefreshView();

            BytesModified?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Fill the selection with a Byte at selection start
        /// </summary>
        public void FillWithByte(byte val) => FillWithByte(SelectionStart, SelectionLength, val);

        /// <summary>
        /// Fill with a Byte at start position
        /// </summary>
        public void FillWithByte(long startPosition, long length, byte val)
        {
            if (!CheckIsOpen(_provider) || (startPosition <= -1 || length <= 0)) return;

            _provider.FillWithByte(startPosition, length, val);
            SetScrollMarker(SelectionStart, ScrollMarker.ByteModified, FModel.Properties.Resources.FillSelectionAloneString);
            RefreshView();
        }

        /// <summary>
        /// Get all bytes from file or stream opened
        /// </summary>
        public byte[] GetAllBytes(bool copyChange)
        {
            if (!CheckIsOpen(_provider)) return null;

            return _provider.GetAllBytes(copyChange);
        }

        /// <summary>
        /// Get all bytes from file or stream opened and copy change
        /// </summary>
        public byte[] GetAllBytes() => GetAllBytes(true);

        /// <summary>
        /// Return true if Copy method could be invoked.
        /// </summary>
        public bool CanCopy => SelectionLength >= 1 && CheckIsOpen(_provider);

        /// <summary>
        /// Return true if delete method could be invoked.
        /// </summary>
        public bool CanDelete => CanCopy && !ReadOnlyMode && AllowDeleteByte;

        /// <summary>
        /// Copy to clipboard with default CopyPasteMode.ASCIIString
        /// </summary>
        public void CopyToClipboard() => CopyToClipboard(DefaultCopyToClipboardMode);

        /// <summary>
        /// Copy to clipboard the current selection with actual change in control
        /// </summary>
        public void CopyToClipboard(CopyPasteMode copypastemode) => CopyToClipboard(copypastemode, SelectionStart,
            SelectionStop, true, _tblCharacterTable);

        /// <summary>
        /// Copy to clipboard
        /// </summary>
        public void CopyToClipboard(CopyPasteMode copypastemode, long selectionStart, long selectionStop,
            bool copyChange, TblStream tbl)
        {
            if (!CanCopy) return;
            if (!CheckIsOpen(_provider)) return;

            _provider.CopyToClipboard(copypastemode, selectionStart, selectionStop, copyChange, tbl);
        }

        /// <summary>
        /// Copy selection to a stream
        /// </summary>
        /// <param name="output">Output stream is not closed after copy</param>
        public void CopyToStream(Stream output, bool copyChange) =>
            CopyToStream(output, SelectionStart, SelectionStop, copyChange);

        /// <summary>
        /// Copy selection to a stream
        /// </summary>
        /// <param name="output">Output stream is not closed after copy</param>
        public void CopyToStream(Stream output, long selectionStart, long selectionStop, bool copyChange)
        {
            if (!CanCopy) return;
            if (!CheckIsOpen(_provider)) return;

            _provider.CopyToStream(output, selectionStart, selectionStop, copyChange);
        }

        /// <summary>
        /// Return a byte array with the copy of data defined by selection start/stop
        /// </summary>
        public byte[] GetCopyData(long selectionStart, long selectionStop, bool copyChange)
        {
            if (!CanCopy) return null;
            if (!CheckIsOpen(_provider)) return null;

            return _provider.GetCopyData(selectionStart, selectionStop, copyChange);
        }

        /// <summary>
        /// Occurs when data is copied in byteprovider instance
        /// </summary>
        private void Provider_DataCopied(object sender, EventArgs e) => DataCopied?.Invoke(sender, e);

        #endregion Copy/Paste/Cut Methods

        #region Position methods

        /// <summary>
        /// Set position of cursor
        /// </summary>
        public void SetPosition(long position, long byteLength)
        {
            SelectionStart = position;
            SelectionStop = position + byteLength - 1;

            VerticalScrollBar.Value = CheckIsOpen(_provider) ? GetLineNumber(position) : 0;
        }

        /// <summary>
        /// Get the line number of position in parameter
        /// </summary>
        public long GetLineNumber(long position) => (position - ByteShiftLeft) / BytePerLine;

        /// <summary>
        /// Get the column number of the position
        /// </summary>
        public int GetColumnNumber(long position) =>
            AllowVisualByteAddress
                ? (int)(position - VisualByteAdressStart - ByteShiftLeft) % BytePerLine
                : (int)(position - ByteShiftLeft) % BytePerLine;

        /// <summary>
        /// Set position in control at position in parameter
        /// </summary>
        public void SetPosition(long position) => SetPosition(position, 0);

        /// <summary>
        /// Set position in control at position in parameter
        /// </summary>
        public void SetPosition(string hexLiteralPosition) =>
            SetPosition(HexLiteralToLong(hexLiteralPosition).position);

        /// <summary>
        /// Set position in control at position in parameter with specified selected length
        /// </summary>
        public void SetPosition(string hexLiteralPosition, long byteLength) =>
            SetPosition(HexLiteralToLong(hexLiteralPosition).position, byteLength);

        /// <summary>
        /// Give a next valid position
        /// </summary>
        /// <param name="position">Start position for compute the correction</param>
        /// <param name="positionCorrection">Positive or negative position number to add/substract from position</param>
        private long GetValidPositionFrom(long position, long positionCorrection)
        {
            if (!CheckIsOpen(_provider)) return -1;

            long validPosition = position;
            long gap = positionCorrection >= 0 ? positionCorrection : -positionCorrection;

            long cnt = 0;
            for (long i = 0; i < gap; i++)
            {
                cnt++;

                if (_provider.CheckIfIsByteModified(position + (positionCorrection > 0 ? cnt : -cnt), ByteAction.Deleted).success)
                {
                    validPosition += positionCorrection > 0 ? 1 : -1;
                    i--;
                }
                else
                    validPosition += positionCorrection > 0 ? 1 : -1;
            }

            return validPosition >= 0 ? validPosition : -1;
        }

        #endregion position methods

        #region Visibility property

        /// <summary>
        /// Set or Get value for change visibility of hexadecimal panel
        /// </summary>
        public Visibility HexDataVisibility
        {
            get => (Visibility)GetValue(HexDataVisibilityProperty);
            set => SetValue(HexDataVisibilityProperty, value);
        }

        public static readonly DependencyProperty HexDataVisibilityProperty =
            DependencyProperty.Register("HexDataVisibility", typeof(Visibility), typeof(HexEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    HexDataVisibility_PropertyChanged,
                    Visibility_CoerceValue));

        private static object Visibility_CoerceValue(DependencyObject d, object baseValue) =>
            (Visibility)baseValue == Visibility.Hidden ? Visibility.Collapsed : (Visibility)baseValue;

        private static void HexDataVisibility_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl)) return;

            switch ((Visibility)e.NewValue)
            {
                case Visibility.Visible:
                    ctrl.HexDataStackPanel.Visibility = Visibility.Visible;

                    if (ctrl.HeaderVisibility == Visibility.Visible)
                        ctrl.HexHeaderStackPanel.Visibility = Visibility.Visible;
                    break;

                case Visibility.Collapsed:
                    ctrl.HexDataStackPanel.Visibility = Visibility.Collapsed;
                    ctrl.HexHeaderStackPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        /// <summary>
        /// Set or Get value for change visibility of hexadecimal header
        /// </summary>
        public Visibility HeaderVisibility
        {
            get => (Visibility)GetValue(HeaderVisibilityProperty);
            set => SetValue(HeaderVisibilityProperty, value);
        }

        public static readonly DependencyProperty HeaderVisibilityProperty =
            DependencyProperty.Register("HeaderVisibility", typeof(Visibility), typeof(HexEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    HeaderVisibility_PropertyChanged,
                    Visibility_CoerceValue));

        private static void HeaderVisibility_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl)) return;

            switch ((Visibility)e.NewValue)
            {
                case Visibility.Visible:
                    if (ctrl.HexDataVisibility == Visibility.Visible)
                    {
                        ctrl.HexHeaderStackPanel.Visibility = Visibility.Visible;
                        ctrl.TopRectangle.Visibility = Visibility.Visible;
                    }
                    break;

                case Visibility.Collapsed:
                    ctrl.HexHeaderStackPanel.Visibility = Visibility.Collapsed;
                    ctrl.TopRectangle.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        /// <summary>
        /// Set or Get value for change visibility of string panel
        /// </summary>
        public Visibility StringDataVisibility
        {
            get => (Visibility)GetValue(StringDataVisibilityProperty);
            set => SetValue(StringDataVisibilityProperty, value);
        }

        public static readonly DependencyProperty StringDataVisibilityProperty =
            DependencyProperty.Register("StringDataVisibility", typeof(Visibility), typeof(HexEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    StringDataVisibility_ValidateValue,
                    Visibility_CoerceValue));

        private static void StringDataVisibility_ValidateValue(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl)) return;

            switch ((Visibility)e.NewValue)
            {
                case Visibility.Visible:
                    ctrl.StringDataStackPanel.Visibility = Visibility.Visible;
                    break;
                case Visibility.Collapsed:
                    ctrl.StringDataStackPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        /// <summary>
        /// Set or Get value for change visibility of status bar
        /// </summary>
        public Visibility StatusBarVisibility
        {
            get => (Visibility)GetValue(StatusBarVisibilityProperty);
            set => SetValue(StatusBarVisibilityProperty, value);
        }

        public static readonly DependencyProperty StatusBarVisibilityProperty =
            DependencyProperty.Register("StatusBarVisibility", typeof(Visibility), typeof(HexEditor),
                new FrameworkPropertyMetadata(Visibility.Visible,
                    StatusBarVisibility_ValueChange,
                    Visibility_CoerceValue));

        private static void StatusBarVisibility_ValueChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl)) return;

            switch ((Visibility)e.NewValue)
            {
                case Visibility.Visible:
                    ctrl.StatusBarGrid.Visibility = Visibility.Visible;
                    ctrl.BottomRectangle.Visibility = Visibility.Visible;
                    break;

                case Visibility.Collapsed:
                    ctrl.StatusBarGrid.Visibility = Visibility.Collapsed;
                    ctrl.BottomRectangle.Visibility = Visibility.Collapsed;
                    break;
            }

            ctrl.RefreshView(true);
        }

        #endregion Visibility property

        #region Undo / Redo

        /// <summary>
        /// Clear undo and change
        /// </summary>
        public void ClearAllChange()
        {
            if (!CheckIsOpen(_provider)) return;

            _provider.ClearUndoChange();
            _provider.ClearRedoChange();
        }

        /// <summary>
        /// Make undo of last the last bytemodified
        /// TODO: Fixe when HideByteDeleted
        /// </summary>
        public void Undo(int repeat = 1)
        {
            UnSelectAll();

            if (!CheckIsOpen(_provider)) return;

            for (var i = 0; i < repeat; i++)
                _provider.Undo();

            RefreshView();

            //Update focus
            if (UndoStack.Count == 0) return;

            var position = UndoStack.ElementAt(0).BytePositionInStream;
            if (!IsBytePositionAreVisible(position))
                SetPosition(position);

            SetFocusAt(position);

            Undone?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Make Redo of the last bytemodified
        /// </summary>
        public void Redo(int repeat = 1)
        {
            UnSelectAll();

            if (!CheckIsOpen(_provider)) return;

            for (var i = 0; i < repeat; i++)
                _provider.Redo();

            RefreshView();

            //Update focus
            if (RedoStack.Count == 0) return;

            var position = RedoStack.ElementAt(0).BytePositionInStream;
            if (!IsBytePositionAreVisible(position))
                SetPosition(position);

            SetFocusAt(position);

            Redone?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Clear the scroll marker when undone 
        /// </summary>
        /// <param name="sender">List of long representing position in file are undone</param>
        private void Provider_Undone(object sender, EventArgs e)
        {
            switch (sender)
            {
                case List<long> bytePosition:
                    foreach (var position in bytePosition)
                        ClearScrollMarker(position);
                    break;
            }

            IsModified = _provider.UndoCount > 0;
        }

        /// <summary>
        /// Get the undo count
        /// </summary>
        public long UndoCount => CheckIsOpen(_provider) ? _provider.UndoCount : 0;

        /// <summary>
        /// Get the undo count
        /// </summary>
        public long RedoCount => CheckIsOpen(_provider) ? _provider.RedoCount : 0;

        /// <summary>
        /// Get the undo stack
        /// </summary>
        public Stack<ByteModified> UndoStack => CheckIsOpen(_provider) ? _provider.UndoStack : null;

        /// <summary>
        /// Get the Redo stack
        /// </summary>
        public Stack<ByteModified> RedoStack => CheckIsOpen(_provider) ? _provider.RedoStack : null;

        #endregion Undo / Redo

        #region Open, Close, Save, byte provider ...

        private void Provider_ChangesSubmited(object sender, EventArgs e)
        {
            if (!(sender is ByteProvider bp)) return;

            //Refresh filename
            var filename = bp.FileName;
            CloseProvider();
            FileName = filename;

            ChangesSubmited?.Invoke(this, new EventArgs());
        }

        private void ProviderStream_ChangesSubmited(object sender, EventArgs e)
        {
            //Refresh stream
            if (!CheckIsOpen(_provider)) return;

            using (var stream = new MemoryStream())
            {
                _provider.Position = 0;
                _provider.Stream.CopyTo(stream);

                CloseProvider();
                OpenStream(stream);
            }

            ChangesSubmited?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Set or Get the file with the control will show hex
        /// </summary>
        public string FileName
        {
            get => (string)GetValue(FileNameProperty);
            set => SetValue(FileNameProperty, value);
        }

        // Using a DependencyProperty as the backing store for FileName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register(nameof(FileName), typeof(string), typeof(HexEditor),
                new FrameworkPropertyMetadata(string.Empty,
                    FileName_PropertyChanged));

        private static void FileName_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.OpenFile((string)e.NewValue);
        }

        /// <summary>
        /// Set the MemoryStream are used by ByteProvider
        /// </summary>
        public MemoryStream Stream
        {
            get => (MemoryStream)GetValue(StreamProperty);
            set => SetValue(StreamProperty, value);
        }

        // Using a DependencyProperty as the backing store for Stream.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StreamProperty =
            DependencyProperty.Register(nameof(Stream), typeof(MemoryStream), typeof(HexEditor),
                new FrameworkPropertyMetadata(null,
                    Stream_PropertyChanged));

        private static void Stream_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl)) return;

            ctrl.CloseProvider();
            ctrl.OpenStream((MemoryStream)e.NewValue);
        }

        /// <summary>
        /// Get the length of byteprovider are opened in control
        /// </summary>
        public long Length => CheckIsOpen(_provider) ? _provider.Length : -1;

        /// <summary>
        /// Close file and clear control
        /// ReadOnlyMode is reset to false
        /// </summary>
        public void CloseProvider()
        {
            if (CheckIsOpen(_provider))
            {
                FileName = string.Empty;
                ReadOnlyMode = false;
                VerticalScrollBar.Value = 0;

                _provider.Close();
            }

            UnHighLightAll();
            ClearAllScrollMarker();
            UnSelectAll();
            RefreshView();
            UpdateHeader(true);
            UpdateScrollBar();

            //Debug
            Debug.Print("PROVIDER CLOSED");
        }

        /// <summary>
        /// Save to the current stream/file
        /// </summary>
        public void SubmitChanges()
        {
            if (!CheckIsOpen(_provider) || _provider.ReadOnlyMode || !IsModified) return;

            _provider.SubmitChanges();
        }

        /// <summary>
        /// Save as to another file
        /// </summary>
        public void SubmitChanges(string newfilename, bool overwrite = false)
        {
            if (!CheckIsOpen(_provider) || _provider.ReadOnlyMode) return;

            _provider.SubmitChanges(newfilename, overwrite);
        }

        /// <summary>
        /// Open file name
        /// </summary>
        /// <param name="filename"></param>
        private void OpenFile(string filename)
        {
            if (string.IsNullOrEmpty(FileName))
            {
                CloseProvider();
                return;
            }

            if (File.Exists(filename))
            {
                CloseProvider();

                _provider = new ByteProvider(filename);

                if (_provider.IsEmpty)
                {
                    CloseProvider();
                    return;
                }

                _provider.ReadOnlyChanged += Provider_ReadOnlyChanged;
                _provider.DataCopiedToClipboard += Provider_DataCopied;
                _provider.ChangesSubmited += Provider_ChangesSubmited;
                _provider.Undone += Provider_Undone;
                _provider.LongProcessChanged += Provider_LongProcessProgressChanged;
                _provider.LongProcessStarted += Provider_LongProcessProgressStarted;
                _provider.LongProcessCompleted += Provider_LongProcessProgressCompleted;
                _provider.LongProcessCanceled += Provider_LongProcessProgressCompleted;
                _provider.FillWithByteCompleted += Provider_FillWithByteCompleted;
                _provider.ReplaceByteCompleted += Provider_ReplaceByteCompleted;
                _provider.BytesAppendCompleted += Provider_BytesAppendCompleted;

                UpdateScrollBar();
                UpdateHeader();

                //Load file with ASCII character table;
                var previousTable = TypeOfCharacterTable;
                TypeOfCharacterTable = CharacterTableType.Ascii;

                ////TEMPS : CUSTOMBACKGROUNDBLOCK (CBB) /////////
                ////TODO: Add autodetect file type and create external CBB...
                //if (UseCustomBackGroudBlock)
                //    _cbbList = new ExeFile().GetCustomBackgroundBlock(_provider);
                /////////////////////////////////////////////////

                RefreshView(true);

                //Replace previous character table
                TypeOfCharacterTable = previousTable;

                UnSelectAll();

                UpdateTblBookMark();
                UpdateSelectionColor(FirstColor.HexByteData);

                //Update count of byte on file open
                UpdateByteCount();

                //Debug
                Debug.Print("FILE OPENED");
            }
            else
                throw new FileNotFoundException();
        }

        /// <summary>
        /// Open file name
        /// </summary>
        private void OpenStream(MemoryStream stream)
        {
            if (!stream.CanRead) return;

            CloseProvider();

            _provider = new ByteProvider(stream);

            if (_provider.IsEmpty)
            {
                CloseProvider();
                return;
            }

            _provider.ReadOnlyChanged += Provider_ReadOnlyChanged;
            _provider.DataCopiedToClipboard += Provider_DataCopied;
            _provider.ChangesSubmited += ProviderStream_ChangesSubmited;
            _provider.Undone += Provider_Undone;
            _provider.LongProcessChanged += Provider_LongProcessProgressChanged;
            _provider.LongProcessStarted += Provider_LongProcessProgressStarted;
            _provider.LongProcessCompleted += Provider_LongProcessProgressCompleted;
            _provider.LongProcessCanceled += Provider_LongProcessProgressCompleted;
            _provider.FillWithByteCompleted += Provider_FillWithByteCompleted;
            _provider.ReplaceByteCompleted += Provider_ReplaceByteCompleted;
            _provider.BytesAppendCompleted += Provider_BytesAppendCompleted;

            UpdateScrollBar();
            UpdateHeader();

            RefreshView(true);

            UnSelectAll();

            UpdateTblBookMark();
            UpdateSelectionColor(FirstColor.HexByteData);

            //Update count of byte
            UpdateByteCount();

            //Debug
            Debug.Print("STREAM OPENED");
        }

        private void Provider_LongProcessProgressCompleted(object sender, EventArgs e)
        {
            LongProgressProgressBar.Visibility = Visibility.Collapsed;
            CancelLongProcessButton.Visibility = Visibility.Collapsed;

            #region Enable controls

            TraverseHexBytes(ctrl => ctrl.IsEnabled = true);
            TraverseStringBytes(ctrl => ctrl.IsEnabled = true);
            TraverseLineInfo(ctrl => ctrl.IsEnabled = true);
            TraverseHexHeader(ctrl => ctrl.IsEnabled = true);
            TopRectangle.IsEnabled = BottomRectangle.IsEnabled = true;
            VerticalScrollBar.IsEnabled = true;

            #endregion

            LongProcessProgressCompleted?.Invoke(this, new EventArgs());
        }

        private void Provider_LongProcessProgressStarted(object sender, EventArgs e)
        {
            LongProgressProgressBar.Visibility = Visibility.Visible;
            CancelLongProcessButton.Visibility = Visibility.Visible;

            #region Disable controls

            TraverseHexBytes(ctrl => ctrl.IsEnabled = false);
            TraverseStringBytes(ctrl => ctrl.IsEnabled = false);
            TraverseLineInfo(ctrl => ctrl.IsEnabled = false);
            TraverseHexHeader(ctrl => ctrl.IsEnabled = false);
            TopRectangle.IsEnabled = BottomRectangle.IsEnabled = false;
            VerticalScrollBar.IsEnabled = false;

            #endregion

            LongProcessProgressStarted?.Invoke(this, new EventArgs());
        }

        private void Provider_LongProcessProgressChanged(object sender, EventArgs e)
        {
            //Update progress bar
            LongProgressProgressBar.Value = (double)sender;
            Application.Current.DoEvents();

            LongProcessProgressChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Update scrollbar when append are completed
        /// </summary>
        private void Provider_BytesAppendCompleted(object sender, EventArgs e) =>
            VerticalScrollBar.Maximum = MaxLine - 1;

        private void Provider_ReplaceByteCompleted(object sender, EventArgs e) =>
            ReplaceByteCompleted?.Invoke(this, new EventArgs());

        private void Provider_FillWithByteCompleted(object sender, EventArgs e) =>
            FillWithByteCompleted?.Invoke(this, new EventArgs());

        private void CancelLongProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckIsOpen(_provider)) return;

            _provider.IsOnLongProcess = false;
        }

        /// <summary>
        /// Check if byteprovider is on long progress and update control
        /// </summary>
        private void CheckProviderIsOnProgress()
        {
            if (CheckIsOpen(_provider))
            {
                if (_provider.IsOnLongProcess) return;
                CancelLongProcessButton.Visibility = Visibility.Collapsed;
                LongProgressProgressBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                CancelLongProcessButton.Visibility = Visibility.Collapsed;
                LongProgressProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        #endregion Open, Close, Save, byte provider ...

        #region Easy powerful traverses methods

        /// <summary>
        /// Used to make action on all visible hexbyte
        /// </summary>
        private void TraverseHexBytes(Action<HexByte> act, ref bool exit, bool force = false)
        {
            var visibleLine = MaxVisibleLine;
            var cnt = 0;

            //HexByte panel
            foreach (StackPanel hexDataStack in HexDataStackPanel.Children)
            {
                if (cnt++ == visibleLine && !force) break;

                foreach (var ctrl in hexDataStack.Children)
                    if (ctrl is HexByte hexCtrl)
                        act(hexCtrl);

                if (exit) return;
            }
        }

        /// <summary>
        /// Used to make action on all visible hexbyte
        /// </summary>
        private void TraverseHexBytes(Action<HexByte> act)
        {
            var exit = false;
            TraverseHexBytes(act, ref exit);
        }

        /// <summary>
        /// Used to make action on all visible stringbyte
        /// </summary>
        private void TraverseStringBytes(Action<StringByte> act, ref bool exit, bool force = false)
        {
            var visibleLine = MaxVisibleLine;
            var cnt = 0;

            //Stringbyte panel
            foreach (StackPanel stringDataStack in StringDataStackPanel.Children)
            {
                if (cnt++ == visibleLine && !force)
                    break;

                foreach (var ctrl in stringDataStack.Children)
                    if (ctrl is StringByte sbControl)
                        act(sbControl);

                if (exit) return;
            }
        }

        /// <summary>
        /// Used to make action on all visible stringbyte
        /// </summary>
        private void TraverseStringBytes(Action<StringByte> act)
        {
            var exit = false;
            TraverseStringBytes(act, ref exit);
        }

        /// <summary>
        /// Used to make action on all visible hexbyte and stringbyte.
        /// </summary>
        private void TraverseHexAndStringBytes(Action<IByteControl> act, ref bool exit, bool force = false)
        {
            TraverseStringBytes(act, ref exit, force);
            TraverseHexBytes(act, ref exit, force);
        }

        /// <summary>
        /// Used to make action on all visible hexbyte and stringbyte.
        /// </summary>
        private void TraverseHexAndStringBytes(Action<IByteControl> act, bool force = false)
        {
            var exit = false;
            TraverseHexAndStringBytes(act, ref exit, force);
        }

        /// <summary>
        /// Used to make action on all visible lineinfos
        /// </summary>
        private void TraverseLineInfo(Action<FastTextLine> act)
        {
            var visibleLine = MaxVisibleLine;
            var cnt = 0;

            //lines infos panel
            foreach (var ctrl in LinesInfoStackPanel.Children)
            {
                if (cnt++ == visibleLine) break;

                if (ctrl is FastTextLine lineInfo)
                    act(lineInfo);
            }
        }

        /// <summary>
        /// Used to make action on all visible header
        /// </summary>
        private void TraverseHexHeader(Action<FastTextLine> act)
        {
            var visibleLine = MaxVisibleLine;
            var cnt = 0;

            //header panel
            foreach (var ctrl in HexHeaderStackPanel.Children)
            {
                if (cnt++ == visibleLine) break;

                if (ctrl is FastTextLine column)
                    act(column);
            }
        }

        /// <summary>
        /// Used to make action on ScrollMarker
        /// </summary>
        private void TraverseScrollMarker(Action<Rectangle> act, ref bool exit)
        {
            for (var i = MarkerGrid.Children.Count - 1; i >= 0; i--)
            {
                if (MarkerGrid.Children[i] is Rectangle rect)
                    act(rect);

                if (exit) return;
            }
        }

        /// <summary>
        /// Used to make action on ScrollMarker
        /// </summary>
        private void TraverseScrollMarker(Action<Rectangle> act)
        {
            var exit = false;
            TraverseScrollMarker(act, ref exit);
        }

        #endregion Traverse methods

        #region BytePerLine property/methods

        /// <summary>
        /// Get or set the number of byte are show in control
        /// </summary>
        public int BytePerLine
        {
            get => (int)GetValue(BytePerLineProperty);
            set => SetValue(BytePerLineProperty, value);
        }

        public static readonly DependencyProperty BytePerLineProperty =
            DependencyProperty.Register("BytePerLine", typeof(int), typeof(HexEditor),
                new FrameworkPropertyMetadata(24, BytePerLine_PropertyChanged,
                    BytePerLine_CoerceValue));

        private static object BytePerLine_CoerceValue(DependencyObject d, object baseValue) =>
            (int)baseValue < 1 ? 1 : ((int)baseValue > 64 ? 64 : baseValue);

        private static void BytePerLine_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl) || e.NewValue == e.OldValue) return;

            //Get previous state
            var firstPos = ctrl.FirstVisibleBytePosition;
            var startPos = ctrl.SelectionStart;
            var stopPos = ctrl.SelectionStop;

            //refresh
            ctrl.UpdateScrollBar();
            ctrl.BuildDataLines(ctrl.MaxVisibleLine, true);
            ctrl.RefreshView(true);
            ctrl.UpdateHeader(true);

            //Set previous state
            ctrl.SetPosition(firstPos);
            ctrl.SelectionStart = startPos;
            ctrl.SelectionStop = stopPos;
        }

        #endregion

        #region vertical scrollbar property/methods

        /// <summary>
        /// Vertical scrollbar large change on click
        /// </summary>
        public double ScrollLargeChange
        {
            get => _scrollLargeChange;
            set
            {
                _scrollLargeChange = value;
                UpdateScrollBar();
            }
        }

        private void VerticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
            RefreshView(true);

        /// <summary>
        /// Update vertical scrollbar with file info
        /// </summary>
        private void UpdateScrollBar()
        {
            VerticalScrollBar.Visibility = Visibility.Collapsed;

            if (CheckIsOpen(_provider))
            {
                VerticalScrollBar.Visibility = Visibility.Visible;
                VerticalScrollBar.SmallChange = 1;
                VerticalScrollBar.LargeChange = ScrollLargeChange;
                VerticalScrollBar.Maximum = MaxLine - MaxVisibleLine + 1;
            }
        }
        #endregion

        #region General update methods / Refresh view

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged) RefreshView(true);
        }

        /// <summary>
        /// Clear all IByteControl in hexeditor
        /// </summary>
        /// <param name="force">Set to true for clear all byte visible (set by MaxVisibleLine) or not visible in control.</param>
        private void ClearAllBytes(bool force = false) => TraverseHexAndStringBytes(ctrl => { ctrl.Clear(); }, force);

        /// <summary>
        /// Clear all lines offset...
        /// </summary>
        private void ClearLineInfo() => TraverseLineInfo(ctrl => { ctrl.Tag = ctrl.Text = string.Empty; });

        /// <summary>
        /// Refresh currentview of hexeditor
        /// </summary>
        public void RefreshView(bool controlResize = false, bool refreshData = true)
        {
#if DEBUG
            var watch = new Stopwatch();
            watch.Start();
#endif
            UpdateLinesInfo();

            if (refreshData)
                UpdateViewers(controlResize);

            //Update visual of byte control
            UpdateByteModified();
            UpdateSelection();
            UpdateHighLight();
            UpdateStatusBar();
            UpdateVisual();
            UpdateFocus();

            CheckProviderIsOnProgress();

            if (controlResize)
            {
                UpdateScrollMarkerPosition();
                UpdateHeader(true);
            }

#if DEBUG
            watch.Stop();
            Debug.Print($"REFRESH TIME: {watch.Elapsed.Milliseconds} ms");
#endif
        }

        /// <summary>
        /// Update the selection of byte in hexadecimal panel
        /// </summary>
        private void UpdateSelectionColor(FirstColor coloring)
        {
            switch (coloring)
            {
                case FirstColor.HexByteData:
                    TraverseHexBytes(ctrl => { ctrl.FirstSelected = true; });
                    TraverseStringBytes(ctrl => { ctrl.FirstSelected = false; });
                    _firstColor = FirstColor.HexByteData;
                    break;
                case FirstColor.StringByteData:
                    TraverseHexBytes(ctrl => { ctrl.FirstSelected = false; });
                    TraverseStringBytes(ctrl => { ctrl.FirstSelected = true; });
                    _firstColor = FirstColor.StringByteData;
                    break;
            }
        }

        /// <summary>
        /// Build the StringByte and HexByte control used byte hexeditor
        /// </summary>
        /// <param name="maxline">Number of line to build</param>
        /// <param name="rebuild">Rebuild data line</param>
        private void BuildDataLines(int maxline, bool rebuild = false)
        {
            var reAttachEvents = false;

            if (rebuild)
            {
                reAttachEvents = true;

                StringDataStackPanel.Children.Clear();
                HexDataStackPanel.Children.Clear();
            }

            for (var lineIndex = StringDataStackPanel.Children.Count; lineIndex < maxline; lineIndex++)
            {
                #region Build StringByte

                var dataLineStack = new StackPanel
                {
                    Height = LineHeight,
                    Orientation = Orientation.Horizontal
                };

                for (var i = 0; i < BytePerLine; i++)
                {
                    if (_tblCharacterTable == null && (ByteSpacerPositioning == ByteSpacerPosition.Both ||
                                                       ByteSpacerPositioning == ByteSpacerPosition.StringBytePanel))
                        AddByteSpacer(dataLineStack, i);

                    var sbCtrl = new StringByte(this);
                    sbCtrl.Clear();

                    dataLineStack.Children.Add(sbCtrl);
                }
                StringDataStackPanel.Children.Add(dataLineStack);

                #endregion

                #region Build HexByte

                var hexaDataLineStack = new StackPanel
                {
                    Height = LineHeight,
                    Orientation = Orientation.Horizontal
                };

                for (var i = 0; i < BytePerLine; i++)
                {
                    if (ByteSpacerPositioning == ByteSpacerPosition.Both ||
                        ByteSpacerPositioning == ByteSpacerPosition.HexBytePanel)
                        AddByteSpacer(hexaDataLineStack, i);

                    var byteControl = new HexByte(this);
                    byteControl.Clear();

                    hexaDataLineStack.Children.Add(byteControl);
                }

                HexDataStackPanel.Children.Add(hexaDataLineStack);

                #endregion

                reAttachEvents = true;
            }

            #region Attach/detach events to each IByteControl

            if (reAttachEvents)
                TraverseHexAndStringBytes(ctrl =>
                {
                    #region Detach events

                    ctrl.ByteModified -= Control_ByteModified;
                    ctrl.MoveNext -= Control_MoveNext;
                    ctrl.MovePrevious -= Control_MovePrevious;
                    ctrl.MouseSelection -= Control_MouseSelection;
                    ctrl.Click -= Control_Click;
                    ctrl.DoubleClick -= Control_DoubleClick;
                    ctrl.RightClick -= Control_RightClick;
                    ctrl.MoveUp -= Control_MoveUp;
                    ctrl.MoveDown -= Control_MoveDown;
                    ctrl.MoveLeft -= Control_MoveLeft;
                    ctrl.MoveRight -= Control_MoveRight;
                    ctrl.MovePageDown -= Control_MovePageDown;
                    ctrl.MovePageUp -= Control_MovePageUp;
                    ctrl.ByteDeleted -= Control_ByteDeleted;
                    ctrl.EscapeKey -= Control_EscapeKey;
                    ctrl.CtrlaKey -= Control_CTRLAKey;
                    ctrl.CtrlzKey -= Control_CTRLZKey;
                    ctrl.CtrlcKey -= Control_CTRLCKey;
                    ctrl.CtrlvKey -= Control_CTRLVKey;
                    ctrl.CtrlyKey -= Control_CTRLYKey;

                    #endregion

                    #region Attach events

                    ctrl.ByteModified += Control_ByteModified;
                    ctrl.MoveNext += Control_MoveNext;
                    ctrl.MovePrevious += Control_MovePrevious;
                    ctrl.MouseSelection += Control_MouseSelection;
                    ctrl.Click += Control_Click;
                    ctrl.DoubleClick += Control_DoubleClick;
                    ctrl.RightClick += Control_RightClick;
                    ctrl.MoveUp += Control_MoveUp;
                    ctrl.MoveDown += Control_MoveDown;
                    ctrl.MoveLeft += Control_MoveLeft;
                    ctrl.MoveRight += Control_MoveRight;
                    ctrl.MovePageDown += Control_MovePageDown;
                    ctrl.MovePageUp += Control_MovePageUp;
                    ctrl.ByteDeleted += Control_ByteDeleted;
                    ctrl.EscapeKey += Control_EscapeKey;
                    ctrl.CtrlaKey += Control_CTRLAKey;
                    ctrl.CtrlzKey += Control_CTRLZKey;
                    ctrl.CtrlcKey += Control_CTRLCKey;
                    ctrl.CtrlvKey += Control_CTRLVKey;
                    ctrl.CtrlyKey += Control_CTRLYKey;

                    #endregion

                });

            #endregion
        }

        /// <summary>
        /// Update the data and string panels to current view.
        /// Only load what is needed in the view
        /// </summary>
        private void UpdateViewers(bool controlResize)
        {
            var curLevel = ++_priLevel;
            if (CheckIsOpen(_provider))
            {
                var bufferlength = MaxVisibleLine * BytePerLine + 1 + ByteShiftLeft;

                #region Build the buffer lenght if needed

                if (controlResize)
                {
                    if (_viewBuffer != null)
                    {
                        if (_viewBuffer.Length < bufferlength)
                        {
                            BuildDataLines(MaxVisibleLine);
                            _viewBuffer = new byte[bufferlength];
                            _viewBufferBytePosition = new long[bufferlength];
                        }
                    }
                    else
                    {
                        _viewBuffer = new byte[bufferlength];
                        _viewBufferBytePosition = new long[bufferlength];
                        BuildDataLines(MaxVisibleLine);
                    }
                }
                #endregion

                if (LinesInfoStackPanel.Children.Count == 0) return;

                var startPosition = FirstVisibleBytePosition;

                if (AllowVisualByteAddress && startPosition < VisualByteAdressStart)
                    startPosition = VisualByteAdressStart;

                #region read the data from the provider and warns if necessary to load the bytes that have been deleted
                _provider.Position = startPosition;
                var readSize = 0;
                if (HideByteDeleted)
                    for (int i = 0; i < _viewBuffer.Count(); i++)
                    {
                        if (!_provider.CheckIfIsByteModified(_provider.Position, ByteAction.Deleted).success)
                        {
                            if (!_provider.Eof)
                            {
                                _viewBuffer[readSize] = (byte)_provider.ReadByte();
                                _viewBufferBytePosition[readSize] = _provider.Position - 1;
                                readSize++;
                            }
                        }
                        else
                        {
                            _viewBufferBytePosition[readSize] = -1;
                            _provider.Position++;
                            i--;
                        }
                    }
                else
                {
                    readSize = _provider.Read(_viewBuffer, 0, bufferlength <= _viewBuffer.Count()
                                                                    ? bufferlength
                                                                    : _viewBuffer.Count());
                }
                #endregion

                var index = 0;

                #region HexByte panel refresh

                TraverseHexBytes(ctrl =>
                {
                    ctrl.Action = ByteAction.Nothing;
                    ctrl.ReadOnlyMode = ReadOnlyMode;

                    ctrl.InternalChange = true;

                    var nextPos = startPosition + index;

                    //Prevent load if byte are deleted from file
                    if (HideByteDeleted)
                        while (_provider.CheckIfIsByteModified(nextPos, ByteAction.Deleted).success)
                            nextPos++;

                    if (index < readSize && _priLevel == curLevel)
                    {
                        ctrl.Byte = _viewBuffer[index];
                        ctrl.BytePositionInStream = !HideByteDeleted ? nextPos : _viewBufferBytePosition[index];

                        if (AllowVisualByteAddress && nextPos > VisualByteAdressStop)
                            ctrl.Clear();
                    }
                    else
                        ctrl.Clear();

                    ctrl.InternalChange = false;
                    index++;
                });

                #endregion

                index = 0;

                #region StringByte panel refresh

                TraverseStringBytes(ctrl =>
                {
                    ctrl.Action = ByteAction.Nothing;
                    ctrl.ReadOnlyMode = ReadOnlyMode;

                    ctrl.InternalChange = true;
                    ctrl.TblCharacterTable = _tblCharacterTable;
                    ctrl.TypeOfCharacterTable = TypeOfCharacterTable;

                    var nextPos = startPosition + index;

                    //Prevent load if byte are deleted from file
                    if (HideByteDeleted)
                        while (_provider.CheckIfIsByteModified(nextPos, ByteAction.Deleted).success)
                            nextPos++;

                    if (index < readSize)
                    {
                        ctrl.Byte = _viewBuffer[index];
                        ctrl.BytePositionInStream = !HideByteDeleted ? nextPos : _viewBufferBytePosition[index];

                        ctrl.ByteNext = index < readSize - 1 ? (byte?)_viewBuffer[index + 1] : null;

                        if (AllowVisualByteAddress && nextPos > VisualByteAdressStop)
                            ctrl.Clear();
                    }
                    else
                        ctrl.Clear();

                    ctrl.InternalChange = false;
                    index++;
                });

                #endregion
            }
            else
            {
                #region Clear IByteControl

                _viewBuffer = null;
                ClearAllBytes();

                #endregion
            }
        }

        /// <summary>
        /// Update byte are modified
        /// </summary>
        private void UpdateByteModified()
        {
            if (!CheckIsOpen(_provider)) return;

            var modifiedBytesDictionary =
                _provider.GetByteModifieds(ByteAction.All);

            TraverseHexAndStringBytes(ctrl =>
            {
                if (!modifiedBytesDictionary.TryGetValue(ctrl.BytePositionInStream, out var byteModified)) return;

                ctrl.InternalChange = true;
                ctrl.Byte = byteModified.Byte;

                if (byteModified.Action == ByteAction.Modified || byteModified.Action == ByteAction.Deleted)
                    ctrl.Action = byteModified.Action;

                ctrl.InternalChange = false;
            });

            IsModified = _provider.UndoCount > 0;
        }

        /// <summary>
        /// Update the selection of byte
        /// </summary>
        private void UpdateSelection()
        {
            var minSelect = SelectionStart <= SelectionStop ? SelectionStart : SelectionStop;
            var maxSelect = SelectionStart <= SelectionStop ? SelectionStop : SelectionStart;

            TraverseHexAndStringBytes(ctrl =>
            {
                ctrl.IsSelected = ctrl.BytePositionInStream >= minSelect &&
                                  ctrl.BytePositionInStream <= maxSelect &&
                                  ctrl.BytePositionInStream != -1
                    ? ctrl.Action != ByteAction.Deleted
                    : false;
            });
        }
        /// <summary>
        /// Update de SelectionLine property
        /// </summary>
        private void UpdateSelectionLine() =>
            SelectionLine = CheckIsOpen(_provider)
                ? GetLineNumber(SelectionStart)
                : 0;


        /// <summary>
        /// Update bytes as marked on findall()
        /// </summary>
        private void UpdateHighLight()
        {
            if (_markedPositionList.Count > 0)
                TraverseHexAndStringBytes(ctrl => ctrl.IsHighLight = _markedPositionList.ContainsKey(ctrl.BytePositionInStream));
            else //Un highlight all            
                TraverseHexAndStringBytes(ctrl => ctrl.IsHighLight = false);
        }

        /// <summary>
        /// Update the position info panel at left of the control
        /// </summary>
        private void UpdateHeader(bool clear = false)
        {
            //Clear before refresh
            if (clear) HexHeaderStackPanel.Children.Clear();

            if (!CheckIsOpen(_provider)) return;

            for (var i = HexHeaderStackPanel.Children.Count; i < BytePerLine; i++)
            {
                if (ByteSpacerPositioning == ByteSpacerPosition.Both ||
                    ByteSpacerPositioning == ByteSpacerPosition.HexBytePanel)
                    AddByteSpacer(HexHeaderStackPanel, i, true);

                var hlHeader = HighLightSelectionStart &&
                               GetColumnNumber(SelectionStart) == i &&
                               SelectionStart > -1;

                //Create control
                var headerLabel = new FastTextLine(this)
                {
                    Height = LineHeight,
                    AutoWidth = false,
                    FontWeight = hlHeader && !HideByteDeleted ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = hlHeader && !HideByteDeleted ? ForegroundHighLightOffSetHeaderColor : ForegroundOffSetHeaderColor,
                    RenderPoint = new Point(2, 2),
                };

                #region Set text visual of header

                switch (DataStringVisual)
                {
                    case DataVisualType.Hexadecimal:
                        headerLabel.Text = ByteToHex((byte)i);
                        headerLabel.Width = 20;
                        break;
                    case DataVisualType.Decimal:
                        headerLabel.Text = i.ToString("d3");
                        headerLabel.Width = 25;
                        break;
                }

                #endregion

                //Add to stackpanel
                HexHeaderStackPanel.Children.Add(headerLabel);
            }
        }

        /// <summary>
        /// Update the position info panel at left of the control
        /// </summary>
        private void UpdateLinesInfo()
        {
            var maxVisibleLine = MaxVisibleLine;

            #region If the lines are less than "visible lines" create them

            var linesCount = LinesInfoStackPanel.Children.Count;

            if (linesCount < maxVisibleLine)
            {
                for (var i = 0; i < maxVisibleLine - linesCount; i++)
                {
                    var lineInfoLabel = new FastTextLine(this)
                    {
                        Height = LineHeight,
                        Foreground = ForegroundOffSetHeaderColor,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        RenderPoint = new Point(2, 2)
                    };

                    //Events
                    lineInfoLabel.MouseDown += LinesOffSetLabel_MouseDown;
                    lineInfoLabel.MouseMove += LinesOffSetLabel_MouseMove;

                    LinesInfoStackPanel.Children.Add(lineInfoLabel);
                }
            }

            #endregion

            ClearLineInfo();

            if (!CheckIsOpen(_provider)) return;

            long firstByteInLine = FirstVisibleBytePosition;
            long actualPosition = 0;

            for (var i = 0; i < maxVisibleLine; i++)
            {
                var lineOffsetLabel = (FastTextLine)LinesInfoStackPanel.Children[i];

                if (i > 0) firstByteInLine += BytePerLine;

                #region Set text visual
                if (!HideByteDeleted &&
                    HighLightSelectionStart &&
                    SelectionStart > -1 &&
                    SelectionStart >= firstByteInLine &&
                    SelectionStart <= firstByteInLine + BytePerLine - 1)
                {
                    lineOffsetLabel.FontWeight = FontWeights.Bold;
                    lineOffsetLabel.Foreground = ForegroundHighLightOffSetHeaderColor;
                    lineOffsetLabel.ToolTip = $"{FModel.Properties.Resources.FirstByteString} : {SelectionStart}";
                    lineOffsetLabel.Tag = $"0x{LongToHex(SelectionStart).ToUpper()}";
                    actualPosition = SelectionStart;
                }
                else
                {
                    lineOffsetLabel.FontWeight = FontWeights.Normal;
                    lineOffsetLabel.Foreground = ForegroundOffSetHeaderColor;
                    lineOffsetLabel.ToolTip = $"{FModel.Properties.Resources.FirstByteString} : {firstByteInLine}";
                    lineOffsetLabel.Tag = $"0x{LongToHex(firstByteInLine).ToUpper()}";
                    actualPosition = firstByteInLine;
                }

                //update the visual
                switch (OffSetStringVisual)
                {
                    case DataVisualType.Hexadecimal:
                        #region Hexadecimal
                        switch (OffSetPanelVisual)
                        {
                            case OffSetPanelType.OffsetOnly:
                                lineOffsetLabel.Text = $"0x{LongToHex(actualPosition, OffSetPanelFixedWidthVisual).ToUpper()}";
                                break;
                            case OffSetPanelType.LineOnly:
                                lineOffsetLabel.Text = $"ln {LongToHex(GetLineNumber(actualPosition), OffSetPanelFixedWidthVisual).ToUpper()}";
                                break;
                            case OffSetPanelType.Both:
                                lineOffsetLabel.Text = $"ln {LongToHex(GetLineNumber(actualPosition), OffSetPanelFixedWidthVisual)} 0x{LongToHex(actualPosition, OffSetPanelFixedWidthVisual).ToUpper()}";
                                break;
                        }
                        #endregion
                        break;
                    case DataVisualType.Decimal:

                        var format = OffSetPanelFixedWidthVisual == OffSetPanelFixedWidth.Dynamic ? "G" : "D8";

                        #region Decimal
                        switch (OffSetPanelVisual)
                        {
                            case OffSetPanelType.OffsetOnly:
                                lineOffsetLabel.Text = $"d{actualPosition.ToString(format)}";
                                break;
                            case OffSetPanelType.LineOnly:
                                lineOffsetLabel.Text = $"ln {GetLineNumber(actualPosition).ToString(format)}";
                                break;
                            case OffSetPanelType.Both:
                                lineOffsetLabel.Text = $"ln {GetLineNumber(actualPosition).ToString(format)} d{actualPosition.ToString(format)}";
                                break;
                        }
                        #endregion
                        break;
                }

                if (AllowVisualByteAddress && firstByteInLine > VisualByteAdressStop)
                    lineOffsetLabel.Tag = lineOffsetLabel.Text = string.Empty;

                #endregion
            }
        }
        #endregion Update view

        #region First/Last visible byte methods
        /// <summary>
        /// Get first visible byte position in control
        /// TODO: fix the first visible byte when HideByteDeleted are activated... 90% completed
        /// </summary>
        private long FirstVisibleBytePosition
        {
            get
            {
                //compute the cibled position for the first visible byte position
                long cibledPosition = AllowVisualByteAddress
                    ? ((long)VerticalScrollBar.Value) * BytePerLine + ByteShiftLeft + VisualByteAdressStart
                    : ((long)VerticalScrollBar.Value) * BytePerLine + ByteShiftLeft;

                if (HideByteDeleted)
                    //Count the byte are deleted before the cibled position
                    return cibledPosition +
                        (CheckIsOpen(_provider)
                            ? _provider.GetByteModifieds(ByteAction.Deleted).Count(b => b.Value.BytePositionInStream < cibledPosition)
                            : 0);
                else
                    return cibledPosition;
            }
        }

        /// <summary>
        /// Return True if SelectionStart are visible in control
        /// </summary>
        public bool SelectionStartIsVisible => IsBytePositionAreVisible(SelectionStart);

        public bool IsBytePositionAreVisible(long bytePosition) =>
            bytePosition >= FirstVisibleBytePosition && bytePosition <= LastVisibleBytePosition;

        /// <summary>
        /// Get last visible byte position in control
        /// </summary>
        private long LastVisibleBytePosition
        {
            get
            {
                long lastByte = 0;

                TraverseHexBytes(ctrl => { lastByte = ctrl.BytePositionInStream; });

                return lastByte;
            }
        }
        #endregion First/Last visible byte methods

        #region Focus Methods

        /// <summary>
        /// Prevent infinite loop on SetFocusHexDataPanel and SetFocusStringDataPanel
        /// </summary>
        bool _setFocusTest = false;

        /// <summary>
        /// Update the focus to selection start
        /// </summary>
        public void UpdateFocus()
        {
            if (SelectionStartIsVisible)
                SetFocusAtSelectionStart();
            else
                Focus();
        }

        /// <summary>
        /// Set the focus to the selection start
        /// </summary>
        public void SetFocusAtSelectionStart() => SetFocusAt(SelectionStart);

        /// <summary>
        /// Set the focus to the bytePosition
        /// </summary>
        public void SetFocusAt(long bytePosition)
        {
            switch (_firstColor)
            {
                case FirstColor.HexByteData:
                    SetFocusHexDataPanel(bytePosition);
                    break;
                case FirstColor.StringByteData:
                    SetFocusStringDataPanel(bytePosition);
                    break;
            }
        }

        /// <summary>
        /// Set focus on byte in the hex panel
        /// </summary>
        private void SetFocusHexDataPanel(long bytePositionInStream)
        {
            if (!CheckIsOpen(_provider)) return;
            if (bytePositionInStream >= _provider.Length) return;

            var rtn = false;
            TraverseHexBytes(ctrl =>
            {
                if (ctrl.BytePositionInStream == bytePositionInStream)
                {
                    ctrl.Focus();
                    rtn = true;
                }
            }, ref rtn);

            if (rtn) return;

            if (VerticalScrollBar.Value < VerticalScrollBar.Maximum && !_setFocusTest)
            {
                _setFocusTest = true;
                VerticalScrollBar.Value++;
            }

            if (!SelectionStartIsVisible && SelectionLength == 1)
                SetPosition(SelectionStart, 1);
        }

        /// <summary>
        /// Set focus on byte in the string panel
        /// </summary>
        private void SetFocusStringDataPanel(long bytePositionInStream)
        {
            if (!CheckIsOpen(_provider)) return;
            if (bytePositionInStream >= _provider.Length) return;

            var rtn = false;
            TraverseStringBytes(ctrl =>
            {
                if (ctrl.BytePositionInStream == bytePositionInStream)
                {
                    ctrl.Focus();
                    rtn = true;
                }
            }, ref rtn);

            if (rtn) return;

            if (VerticalScrollBar.Value < VerticalScrollBar.Maximum && !_setFocusTest)
            {
                _setFocusTest = true;
                VerticalScrollBar.Value++;
            }

            if (!SelectionStartIsVisible && SelectionLength == 1)
                SetPosition(SelectionStart, 1);
        }

        #endregion Focus Methods

        #region Find/replace methods

        /// <summary>
        /// Find first occurence of string in stream. Search start as startPosition.
        /// </summary>
        public long FindFirst(string text, long startPosition = 0) =>
            FindFirst(StringToByte(text), startPosition);

        /// <summary>
        /// Find first occurence of byte[] in stream. Search start as startPosition.
        /// </summary>
        public long FindFirst(byte[] data, long startPosition = 0, bool highLight = false)
        {
            if (data == null) return -1;
            if (!CheckIsOpen(_provider)) return -1;

            UnHighLightAll();

            try
            {
                var position = _provider.FindIndexOf(data, startPosition).ToList().First();

                SetPosition(position, data.Length);

                if (!highLight) return position;

                AddHighLight(position, data.Length, false);

                SetScrollMarker(position, ScrollMarker.SearchHighLight);

                SelectionStart = position;
                UpdateHighLight();
                return position;
            }
            catch
            {
                UnSelectAll();
                UnHighLightAll();
                return -1;
            }
        }

        /// <summary>
        /// Find next occurence of string in stream search start at SelectionStart.
        /// </summary>
        public long FindNext(string text) =>
            FindNext(StringToByte(text));

        /// <summary>
        /// Find next occurence of byte[] in stream search start at SelectionStart.
        /// </summary>
        public long FindNext(byte[] data, bool highLight = false) =>
            FindFirst(data, SelectionStart + 1, highLight);

        /// <summary>
        /// Find last occurence of string in stream search start at SelectionStart.
        /// </summary>
        public long FindLast(string text) => FindLast(StringToByte(text));

        /// <summary>
        /// Find first occurence of byte[] in stream.
        /// </summary>
        /// <returns>Return the position</returns>
        public long FindLast(byte[] data, bool highLight = false)
        {
            if (data == null) return -1;
            if (!CheckIsOpen(_provider)) return -1;

            UnHighLightAll();

            try
            {
                var position = _provider.FindIndexOf(data, SelectionStart + 1).ToList().Last();

                SetPosition(position, data.Length);

                if (!highLight) return position;

                AddHighLight(position, data.Length, false);

                SetScrollMarker(position, ScrollMarker.SearchHighLight);

                SelectionStart = position;
                UpdateHighLight();
                return position;
            }
            catch
            {
                UnSelectAll();
                UnHighLightAll();
                return -1;
            }
        }

        /// <summary>
        /// Find all occurence of string in stream.
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(string text) => FindAll(StringToByte(text));

        /// <summary>
        /// Find all occurence of byte[] in stream.
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(byte[] data)
        {
            if (data == null) return null;

            UnHighLightAll();

            return CheckIsOpen(_provider) ? _provider.FindIndexOf(data) : null;
        }

        /// <summary>
        /// Find all occurence of string in stream.
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(string text, bool highLight) =>
            FindAll(StringToByte(text), highLight);

        /// <summary>
        /// Find all occurence of string in stream. Highlight occurance in stream is MarcAll as true
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAll(byte[] data, bool highLight)
        {
            if (data == null) return null;

            ClearScrollMarker(ScrollMarker.SearchHighLight);

            if (highLight)
            {
                var positions = FindAll(data);

                if (positions == null) return null;

                var findAll = positions as IList<long> ?? positions.ToList();
                foreach (var position in findAll)
                {
                    AddHighLight(position, data.Length, false);

                    SetScrollMarker(position, ScrollMarker.SearchHighLight);
                }


                UnSelectAll();
                UpdateHighLight();

                return findAll;
            }

            return FindAll(data);
        }

        /// <summary>
        /// Find all occurence of SelectionByteArray in stream. Highlight byte finded
        /// </summary>
        /// <returns>Return null if no occurence found</returns>
        public IEnumerable<long> FindAllSelection(bool highLight) =>
            SelectionLength > 0
                ? FindAll(SelectionByteArray, highLight)
                : null;

        /// <summary>
        /// Replace byte with another at selection position
        /// </summary>
        public void ReplaceByte(byte original, byte replace) =>
            ReplaceByte(SelectionStart, SelectionLength, original, replace);

        /// <summary>
        /// Replace byte with another at start position
        /// </summary>
        public void ReplaceByte(long startPosition, long length, byte original, byte replace)
        {
            if (!CheckIsOpen(_provider) || startPosition <= -1 || length <= 0) return;

            _provider.ReplaceByte(startPosition, length, original, replace);
            SetScrollMarker(SelectionStart, ScrollMarker.ByteModified, FModel.Properties.Resources.ReplaceWithByteString);
            RefreshView();
        }

        /// <summary>
        /// Replace the first byte array define by findData in byteprovider at start position. 
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>
        public long ReplaceFirst(byte[] findData, byte[] replaceData, bool truckLength = true, long startPosition = 0, bool hightlight = false)
        {
            if (findData == null || replaceData == null) return -1;
            if (!CheckIsOpen(_provider)) return -1;

            var position = FindFirst(findData, startPosition, hightlight);

            if (position > -1)
            {
                byte[] finalReplaceData = truckLength
                    ? replaceData.Take(findData.Length).ToArray()
                    : replaceData;

                _provider.Paste(position, finalReplaceData, false);

                SetScrollMarker(position, ScrollMarker.ByteModified);

                UnSelectAll();
                RefreshView();

                return position;
            }
            else
                return -1;
        }

        /// <summary>
        /// Replace the first byte array define by findData in byteprovider at SelectionStart. Start the search at SelectionStart. 
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>
        public long ReplaceFirst(byte[] findData, byte[] replaceData, bool truckLength = true, bool hightlight = false) =>
            ReplaceFirst(findData, replaceData, truckLength, SelectionStart, hightlight);

        /// <summary>
        /// Replace the first byte array define by findData in byteprovider at SelectionStart. 
        /// Start the search at SelectionStart. 
        /// No highlight
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>
        public long ReplaceFirst(byte[] findData, byte[] replaceData, bool truckLength = true) =>
            ReplaceFirst(findData, replaceData, truckLength, SelectionStart, false);

        /// <summary>
        /// Replace the first byte array define by findData in byteprovider at SelectionStart. 
        /// Start the search at SelectionStart. 
        /// No highlight
        /// Truck replace data to length of findData
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>
        public long ReplaceFirst(byte[] findData, byte[] replaceData) =>
            ReplaceFirst(findData, replaceData, true, SelectionStart, false);

        /// <summary>
        /// Replace the first string define by find in byteprovider at SelectionStart. Start the search at SelectionStart. 
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>
        public long ReplaceFirst(string find, string replace, bool truckLength = true, bool hightlight = false) =>
            ReplaceFirst(StringToByte(find), StringToByte(replace), truckLength, SelectionStart, hightlight);

        /// <summary>
        /// Replace the first string define by find in byteprovider at SelectionStart. 
        /// Start the search at SelectionStart. 
        /// No highlight
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>        
        public long ReplaceFirst(string find, string replace, bool truckLength = true) =>
            ReplaceFirst(StringToByte(find), StringToByte(replace), truckLength, SelectionStart, false);

        /// <summary>
        /// Replace the first string define by find in byteprovider at start position. 
        /// Start the search at SelectionStart. 
        /// No highlight
        /// Truck replace data to length of findData
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>        
        public long ReplaceFirst(string find, string replace) =>
            ReplaceFirst(StringToByte(find), StringToByte(replace), true, SelectionStart, false);

        /// <summary>
        /// Replace the next byte array define by findData in byteprovider at SelectionStart. 
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>
        public long ReplaceNext(byte[] findData, byte[] replaceData, bool truckLength = true, bool hightlight = false) =>
            ReplaceFirst(findData, replaceData, truckLength, SelectionStart + 1, hightlight);

        /// <summary>
        /// Replace the next byte array define by findData in byteprovider at SelectionStart. 
        /// No highlight
        /// Truck replace data to length of findData
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>
        public long ReplaceNext(byte[] findData, byte[] replaceData) =>
            ReplaceFirst(findData, replaceData, true, SelectionStart + 1, false);

        /// <summary>
        /// Replace the next byte array define by findData in byteprovider at SelectionStart. 
        /// No highlight
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>
        public long ReplaceNext(byte[] findData, byte[] replaceData, bool truckLength = true) =>
            ReplaceFirst(findData, replaceData, truckLength, SelectionStart + 1, false);

        /// <summary>
        /// Replace the next string define by find in byteprovider at SelectionStart. 
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>
        public long ReplaceNext(string find, string replace, bool truckLength = true, bool hightlight = false) =>
            ReplaceFirst(StringToByte(find), StringToByte(replace), truckLength, SelectionStart + 1, hightlight);

        /// <summary>
        /// Replace the next string define by find in byteprovider at SelectionStart. 
        /// No highlight
        /// Truck replace data to length of findData
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>
        public long ReplaceNext(string find, string replace) =>
            ReplaceFirst(StringToByte(find), StringToByte(replace), true, SelectionStart + 1, false);

        /// <summary>
        /// Replace the next string define by find in byteprovider at SelectionStart. 
        /// No highlight
        /// </summary>
        /// <returns>Return the position of replace. Return -1 on error/no replace</returns>
        public long ReplaceNext(string find, string replace, bool truckLength = true) =>
            ReplaceFirst(StringToByte(find), StringToByte(replace), truckLength, SelectionStart + 1, false);

        /// <summary>
        /// Replace the all byte array define by findData in byteprovider. 
        /// </summary>
        /// <returns>Return the an IEnumerable contains all positions are replaced. Return null on error/no replace</returns>
        public IEnumerable<long> ReplaceAll(byte[] findData, byte[] replaceData, bool truckLength = true, bool hightlight = false)
        {
            if (findData == null || replaceData == null) return null;
            if (!CheckIsOpen(_provider)) return null;

            var positions = FindAll(findData, hightlight);

            if (positions.Any())
            {
                byte[] finalReplaceData = truckLength
                    ? replaceData.Take(findData.Length).ToArray()
                    : replaceData;

                foreach (var position in positions)
                {
                    _provider.Paste(position, finalReplaceData, false);
                    SetScrollMarker(position, ScrollMarker.ByteModified);
                }

                UnSelectAll();
                RefreshView();

                return positions;
            }
            else
                return null;
        }

        /// <summary>
        /// Replace the all byte array define by findData in byteprovider. 
        /// No highlight
        /// </summary>
        /// <returns>Return the position of replace. Return null on error/no replace</returns>
        public IEnumerable<long> ReplaceAll(byte[] findData, byte[] replaceData, bool truckLength = true) =>
            ReplaceAll(findData, replaceData, truckLength, false);

        /// <summary>
        /// Replace the all string define by find in byteprovider. 
        /// </summary>
        /// <returns>Return the position of replace. Return null on error/no replace</returns>
        public IEnumerable<long> ReplaceAll(string find, string replace, bool truckLength = true, bool hightlight = false) =>
            ReplaceAll(StringToByte(find), StringToByte(replace), truckLength, hightlight);

        /// <summary>
        /// Replace the all string define by find in byteprovider.  
        /// No highlight
        /// Truck replace data to length of find
        /// </summary>
        /// <returns>Return the position of replace. Return null on error/no replace</returns>
        public IEnumerable<long> ReplaceAll(string find, string replace) =>
            ReplaceAll(StringToByte(find), StringToByte(replace), true, false);

        /// <summary>
        /// Replace the all byte array define by findData in byteprovider.  
        /// No highlight
        /// </summary>
        /// <returns>Return the position of replace. Return null on error/no replace</returns>
        public IEnumerable<long> ReplaceAll(string find, string replace, bool truckLength = true) =>
            ReplaceAll(StringToByte(find), StringToByte(replace), truckLength, false);

        #endregion Find/replace methods

        #region Statusbar

        /// <summary>
        /// Update statusbar for somes property dont support dependency property
        /// </summary>
        private void UpdateStatusBar()
        {
            if (StatusBarVisibility == Visibility.Visible)
                if (CheckIsOpen(_provider))
                {
                    #region Show length  TODO:REFRESH ONLY WHEN NEEDED

                    var mb = false;
                    long deletedBytesCount = _provider.GetByteModifieds(ByteAction.Deleted).Count;
                    long addedBytesCount = _provider.GetByteModifieds(ByteAction.Added).Count;

                    //is mega bytes ?
                    double length = (_provider.Length - deletedBytesCount + addedBytesCount) / 1024;

                    if (length > 1024)
                    {
                        length = length / 1024;
                        mb = true;
                    }

                    FileLengthKbLabel.Content = Math.Round(length, 2) +
                                                (mb
                                                    ? $" {FModel.Properties.Resources.MBTagString}"
                                                    : $" {FModel.Properties.Resources.KBTagString}");
                    //FileLengthKbLabel.ToolTip = $" {_provider.Length - deletedBytesCount} {Properties.Resources.ByteString}";

                    #endregion

                    #region Byte count of selectionStart

                    if (AllowByteCount && _bytecount != null && SelectionStart > -1)
                    {
                        ByteCountPanel.Visibility = Visibility.Visible;

                        var val = _provider.GetByte(SelectionStart).singleByte.Value;
                        CountOfByteSumLabel.Content = _bytecount[val];
                        CountOfByteLabel.Content = $"0x{LongToHex(val)}";
                    }
                    else
                        ByteCountPanel.Visibility = Visibility.Collapsed;

                    #endregion
                }
                else
                {
                    FileLengthKbLabel.Content = 0;
                    CountOfByteLabel.Content = 0;
                }
        }

        #endregion Statusbar

        #region Bookmark and other scrollmarker

        /// <summary>
        /// Get all bookmark are currently set
        /// </summary>
        public IEnumerable<BookMark> BookMarks
        {
            get
            {
                var bmList = new List<BookMark>();

                TraverseScrollMarker(sm =>
                {
                    if (sm.Tag is BookMark bm && bm.Marker == ScrollMarker.Bookmark)
                        bmList.Add(bm);
                });

                foreach (var bm in bmList)
                    yield return bm;
            }
        }

        /// <summary>
        /// Set bookmark at specified position
        /// </summary>
        /// <param name="position"></param>
        public void SetBookMark(long position) => SetScrollMarker(position, ScrollMarker.Bookmark);

        /// <summary>
        /// Set bookmark at selection start
        /// </summary>
        public void SetBookMark() => SetScrollMarker(SelectionStart, ScrollMarker.Bookmark);

        /// <summary>
        /// Set marker at position using bookmark object
        /// </summary>
        /// <param name="mark"></param>
        private void SetScrollMarker(BookMark mark) =>
            SetScrollMarker(mark.BytePositionInStream, mark.Marker, mark.Description);

        /// <summary>
        /// Set marker at position
        /// </summary>
        private void SetScrollMarker(long position, ScrollMarker marker, string description = "")
        {
            if (CheckIsOpen(_provider))
            {
                double rightPosition = 0;
                var exit = false;

                //create bookmark
                var bookMark = new BookMark
                {
                    Marker = marker,
                    BytePositionInStream = position,
                    Description = description
                };

                #region Remove selection start marker and set position

                if (marker == ScrollMarker.SelectionStart)
                {
                    TraverseScrollMarker(sm =>
                    {
                        if (sm.Tag is BookMark mark && mark.Marker == ScrollMarker.SelectionStart)
                        {
                            MarkerGrid.Children.Remove(sm);
                            exit = true;
                        }
                    }, ref exit);

                    bookMark.BytePositionInStream = SelectionStart;
                }

                #endregion

                #region Set position in scrollbar

                var topPosition =
                    (GetLineNumber(bookMark.BytePositionInStream) * VerticalScrollBar.Track.TickHeight(MaxLine) - 1).Round(1);

                if (double.IsNaN(topPosition))
                    topPosition = 0;

                #endregion

                #region Check if position already exist and exit if exist                

                if (marker != ScrollMarker.SelectionStart)
                {
                    exit = false;

                    TraverseScrollMarker(sm =>
                    {
                        if (sm.Tag is BookMark mark && mark.Marker == marker &&
                            (int)sm.Margin.Top == (int)topPosition)
                            exit = true;
                    }, ref exit);

                    if (exit) return;
                }

                #endregion

                #region Build rectangle

                var rect = new Rectangle
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Tag = bookMark,
                    Width = 5,
                    Height = 3,
                    DataContext = bookMark
                };

                #endregion

                #region Set somes properties for different marker

                switch (marker)
                {
                    case ScrollMarker.TblBookmark:
                    case ScrollMarker.Bookmark:
                        rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                        rect.Fill = (SolidColorBrush)TryFindResource("BookMarkColor");
                        break;
                    case ScrollMarker.SearchHighLight:
                        rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                        rect.Fill = (SolidColorBrush)TryFindResource("SearchBookMarkColor");
                        rect.HorizontalAlignment = HorizontalAlignment.Center;
                        break;
                    case ScrollMarker.SelectionStart:
                        rect.Fill = (SolidColorBrush)TryFindResource("SelectionStartBookMarkColor");
                        rect.Width = VerticalScrollBar.ActualWidth;
                        rect.Height = 3;
                        break;
                    case ScrollMarker.ByteModified:
                        rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                        rect.Fill = (SolidColorBrush)TryFindResource("ByteModifiedMarkColor");
                        rect.HorizontalAlignment = HorizontalAlignment.Right;
                        break;
                    case ScrollMarker.ByteDeleted:
                        rect.ToolTip = TryFindResource("ScrollMarkerSearchToolTip");
                        rect.Fill = (SolidColorBrush)TryFindResource("ByteDeletedMarkColor");
                        rect.HorizontalAlignment = HorizontalAlignment.Right;
                        rightPosition = 4;
                        break;
                }

                rect.MouseDown += Rect_MouseDown;
                //rect.DataContext = new ByteModified {BytePositionInStream = position};
                rect.Margin = new Thickness(0, topPosition, rightPosition, 0);

                #endregion

                //Add to grid
                MarkerGrid.Children.Add(rect);
            }
        }

        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle rect && rect.Tag is BookMark bm)
                SetPosition(bm.Marker != ScrollMarker.SelectionStart ? bm.BytePositionInStream : SelectionStart, 1);
        }

        /// <summary>
        /// Update all scroll marker position
        /// </summary>
        private void UpdateScrollMarkerPosition() =>
            TraverseScrollMarker(ctrl =>
            {
                if (!(ctrl.Tag is BookMark bm)) return;

                try
                {
                    ctrl.Margin = new Thickness
                    (
                        0,
                        GetLineNumber(bm.BytePositionInStream) * VerticalScrollBar.Track.TickHeight(MaxLine) - ctrl.ActualHeight,
                        0,
                        0
                    );
                }
                catch
                {
                    ctrl.Margin = new Thickness(0);
                }
            });

        /// <summary>
        /// Clear ScrollMarker
        /// </summary>
        public void ClearAllScrollMarker() => MarkerGrid.Children.Clear();

        /// <summary>
        /// Clear ScrollMarker
        /// </summary>
        /// <param name="marker">Type of marker to clear</param>
        public void ClearScrollMarker(ScrollMarker marker) =>
            TraverseScrollMarker(sm =>
            {
                if (sm.Tag is BookMark mark && mark.Marker == marker)
                    MarkerGrid.Children.Remove(sm);
            });

        /// <summary>
        /// Clear ScrollMarker
        /// </summary>
        /// <param name="marker">Type of marker to clear</param>
        public void ClearScrollMarker(ScrollMarker marker, long position) =>
            TraverseScrollMarker(sm =>
            {
                if (sm.Tag is BookMark mark && mark.Marker == marker && mark.BytePositionInStream == position)
                    MarkerGrid.Children.Remove(sm);
            });

        /// <summary>
        /// Clear ScrollMarker at position
        /// </summary>
        public void ClearScrollMarker(long position) =>
            TraverseScrollMarker(sm =>
            {
                if (sm.Tag is BookMark mark && mark.BytePositionInStream == position)
                    MarkerGrid.Children.Remove(sm);
            });

        #endregion Bookmark and other scrollmarker

        #region Context menu

        /// <summary>
        /// Allow or not the context menu to appear on right-click
        /// </summary>
        public bool AllowContextMenu { get; set; } = true;

        private void Control_RightClick(object sender, EventArgs e)
        {
            if (AllowContextMenu)
            {
                //position                
                if (sender is IByteControl ctrl)
                    _rightClickBytePosition = ctrl.BytePositionInStream;

                if (SelectionLength <= 1)
                {
                    SelectionStart = _rightClickBytePosition;
                    SelectionStop = _rightClickBytePosition;
                }

                #region Disable ctrl

                CopyAsCMenu.IsEnabled = false;
                CopyAsciicMenu.IsEnabled = false;
                FindAllCMenu.IsEnabled = false;
                CopyHexaCMenu.IsEnabled = false;
                UndoCMenu.IsEnabled = false;
                DeleteCMenu.IsEnabled = false;
                FillByteCMenu.IsEnabled = false;
                CopyTblcMenu.IsEnabled = false;

                #endregion

                if (SelectionLength > 0)
                {
                    CopyAsciicMenu.IsEnabled = true;
                    CopyAsCMenu.IsEnabled = true;
                    FindAllCMenu.IsEnabled = true;
                    CopyHexaCMenu.IsEnabled = true;
                    DeleteCMenu.IsEnabled = true;
                    FillByteCMenu.IsEnabled = true;

                    if (_tblCharacterTable != null)
                        CopyTblcMenu.IsEnabled = true;
                }

                if (UndoCount > 0)
                    UndoCMenu.IsEnabled = true;

                //Show context menu
                Focus();
                CMenu.Visibility = Visibility.Visible;
            }
        }

        private void FindAllCMenu_Click(object sender, RoutedEventArgs e) => FindAll(SelectionByteArray, true);

        private void CopyToClipBoardCMenu_Click(object sender, RoutedEventArgs e)
        {
            //Copy to clipboard
            switch ((sender as MenuItem).Name)
            {
                case nameof(CopyHexaCMenu):
                    CopyToClipboard(CopyPasteMode.HexaString);
                    break;
                case nameof(CopyAsciicMenu):
                    CopyToClipboard(CopyPasteMode.AsciiString);
                    break;
                case nameof(CopyCSharpCMenu):
                    CopyToClipboard(CopyPasteMode.CSharpCode);
                    break;
                case nameof(CopyFSharpCMenu):
                    CopyToClipboard(CopyPasteMode.FSharpCode);
                    break;
                case nameof(CopyCcMenu):
                    CopyToClipboard(CopyPasteMode.CCode);
                    break;
                case nameof(CopyJavaCMenu):
                    CopyToClipboard(CopyPasteMode.JavaCode);
                    break;
                case nameof(CopyVbNetCMenu):
                    CopyToClipboard(CopyPasteMode.VbNetCode);
                    break;
                case nameof(CopyPascalCMenu):
                    CopyToClipboard(CopyPasteMode.PascalCode);
                    break;
                case nameof(CopyTblcMenu):
                    CopyToClipboard(CopyPasteMode.TblString);
                    break;
            }
        }

        private void DeleteCMenu_Click(object sender, RoutedEventArgs e) => DeleteSelection();

        private void UndoCMenu_Click(object sender, RoutedEventArgs e) => Undo();

        private void BookMarkCMenu_Click(object sender, RoutedEventArgs e) => SetBookMark(_rightClickBytePosition);

        private void ClearBookMarkCMenu_Click(object sender, RoutedEventArgs e) =>
            ClearScrollMarker(ScrollMarker.Bookmark);

        private void PasteMenu_Click(object sender, RoutedEventArgs e) => Paste(false); //Paste Without Insert

        private void SelectAllCMenu_Click(object sender, RoutedEventArgs e) => SelectAll();

        private void FillByteCMenu_Click(object sender, RoutedEventArgs e)
        {
            var window = new GiveByteWindow();

            //For present crash When used in Winform
            try
            {
                window.Owner = Application.Current.MainWindow;
            }
            catch
            {
                // TODO : add Winform code
            }

            if (window.ShowDialog() == true && window.HexTextBox.LongValue <= 255)
                FillWithByte((byte)window.HexTextBox.LongValue);
        }

        private void ReplaceByteCMenu_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReplaceByteWindow();

            //For present crash When used in Winform
            try
            {
                window.Owner = Application.Current.MainWindow;
            }
            catch
            {
                // TODO : add Winform code
            }

            if (window.ShowDialog() == true && window.HexTextBox.LongValue <= 255 &&
                window.ReplaceHexTextBox.LongValue <= 255)
                ReplaceByte((byte)window.HexTextBox.LongValue, (byte)window.ReplaceHexTextBox.LongValue);
        }

        #endregion Context menu

        #region Bottom and top rectangle

        /// <summary>
        /// Vertical Move Method By Time,
        /// </summary>
        /// <param name="readToMove">whether the veticalbar value should be changed</param>
        /// <param name="distance">the value that vertical value move down(negative for up)</param>
        private void VerticalMoveByTime(Func<bool> readToMove, Func<double> distance)
        {
            ThreadPool.QueueUserWorkItem(cb =>
            {
                while (readToMove())
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (Mouse.LeftButton != MouseButtonState.Pressed) return;

                        VerticalScrollBar.Value += distance();

                        //Selection stop
                        if (_mouseOnBottom)
                            SelectionStop = LastVisibleBytePosition;
                        else if (_mouseOnTop)
                            SelectionStop = FirstVisibleBytePosition;

                        //Give the control to dispatcher for do events
                        Application.Current.DoEvents();

                    });
                }
            });
        }

        private void BottomRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            _mouseOnBottom = true;
            var curTime = ++_bottomEnterTimes;

            VerticalMoveByTime
            (
                () => _mouseOnBottom && curTime == _bottomEnterTimes,
                () => (int)MouseWheelSpeed
            );
        }

        private void TopRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            var curTime = ++_topEnterTimes;
            _mouseOnTop = true;

            VerticalMoveByTime
            (
                () => _mouseOnTop && curTime == _topEnterTimes,
                () => -(int)MouseWheelSpeed
            );
        }

        private void BottomRectangle_MouseLeave(object sender, MouseEventArgs e) => _mouseOnBottom = false;

        private void TopRectangle_MouseLeave(object sender, MouseEventArgs e) => _mouseOnTop = false;

        private void BottomRectangle_MouseDown(object sender, MouseButtonEventArgs e) => _mouseOnBottom = false;

        private void TopRectangle_MouseDown(object sender, MouseButtonEventArgs e) => _mouseOnTop = false;

        #endregion Bottom and Top rectangle

        #region MouseWheel support
        /// <summary>
        /// Control the mouse wheel speed
        /// </summary>
        public MouseWheelSpeed MouseWheelSpeed { get; set; } = MouseWheelSpeed.System;

        private void Control_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            #region Mousewheel Zoom support
            if (_scaler == null) InitialiseZoom();

            if (Keyboard.Modifiers == ModifierKeys.Control && AllowZoom)
            {
                if (e.Delta > 0) //Zoom
                    ZoomScale += 0.1;

                if (e.Delta < 0) //UnZoom
                    ZoomScale -= 0.1;

                return;
            }
            #endregion

            #region Scroll up and down in the hex editor
            if (_provider == null || !_provider.IsOnLongProcess)
            {
                if (e.Delta > 0) //UP
                    VerticalScrollBar.Value -= e.Delta / 120 *
                        (MouseWheelSpeed == MouseWheelSpeed.System
                            ? MouseWheelScrollLines
                            : (int)MouseWheelSpeed);

                if (e.Delta < 0) //Down
                    VerticalScrollBar.Value += e.Delta / 120 *
                       -(MouseWheelSpeed == MouseWheelSpeed.System
                            ? MouseWheelScrollLines
                            : (int)MouseWheelSpeed);
            }
            #endregion
        }
        #endregion

        #region Highlight support
        /// <summary>
        /// Byte at selection start
        /// </summary>
        internal byte? SelectionByte { get; set; }

        /// <summary>
        /// Set to true for highlight the same byte are selected in view.
        /// </summary>
        public bool AllowAutoHighLightSelectionByte
        {
            get => (bool)GetValue(AllowAutoHighLightSelectionByteProperty);
            set => SetValue(AllowAutoHighLightSelectionByteProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowAutoHighLightSelectionByte.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowAutoHighLightSelectionByteProperty =
            DependencyProperty.Register(nameof(AllowAutoHighLightSelectionByte), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, AutoHighLiteSelectionByte_Changed));

        /// <summary>
        /// Brush used to color the selectionbyte
        /// </summary>
        public Brush AutoHighLiteSelectionByteBrush
        {
            get => (Brush)GetValue(AutoHighLiteSelectionByteBrushProperty);
            set => SetValue(AutoHighLiteSelectionByteBrushProperty, value);
        }

        // Using a DependencyProperty as the backing store for AutoHighLiteSelectionByteBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoHighLiteSelectionByteBrushProperty =
            DependencyProperty.Register(nameof(AutoHighLiteSelectionByteBrush), typeof(Brush), typeof(HexEditor),
                new FrameworkPropertyMetadata(Brushes.LightBlue, AutoHighLiteSelectionByte_Changed));

        private static void AutoHighLiteSelectionByte_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.UpdateVisual();
        }

        /// <summary>
        /// Un highlight all byte as highlighted with find all methods
        /// </summary>
        public void UnHighLightAll()
        {
            _markedPositionList.Clear();
            UpdateHighLight();
            ClearScrollMarker(ScrollMarker.SearchHighLight);
        }

        /// <summary>
        /// Add highlight at position start
        /// </summary>
        /// <param name="startPosition">Position to start the highlight</param>
        /// <param name="length">The length to highlight</param>
        /// <param name="updateVisual">Set to true for update the visual after adding</param>
        public void AddHighLight(long startPosition, long length, bool updateVisual = true)
        {
            for (var i = startPosition; i < startPosition + length; i++)
                if (!_markedPositionList.ContainsValue(i))
                    _markedPositionList.Add(i, i);

            if (updateVisual) UpdateHighLight();
        }

        /// <summary>
        /// Remove highlight from position start
        /// </summary>
        /// <param name="startPosition">Position to start the remove of highlight</param>
        /// <param name="length">The length of highlight to removing</param>
        /// <param name="updateVisual">Set to true for update the visual after removing</param>
        public void RemoveHighLight(long startPosition, long length, bool updateVisual = true)
        {
            for (var i = startPosition; i < startPosition + length; i++)
                if (_markedPositionList.ContainsValue(i))
                    _markedPositionList.Remove(i);

            if (updateVisual) UpdateHighLight();
        }
        #endregion Highlight support

        #region ByteCount property/methods

        public bool AllowByteCount
        {
            get => (bool)GetValue(AllowByteCountProperty);
            set => SetValue(AllowByteCountProperty, value);
        }

        // Using a DependencyProperty as the backing store for AllowByteCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowByteCountProperty =
            DependencyProperty.Register(nameof(AllowByteCount), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(false, AllowByteCount_PropertyChanged));

        private static void AllowByteCount_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl)) return;

            if (e.NewValue != e.OldValue)
                ctrl.UpdateByteCount();

            ctrl.UpdateStatusBar();
        }

        /// <summary>
        /// Update the bytecount var.
        /// </summary>
        private void UpdateByteCount()
        {
            _bytecount = null;

            if (CheckIsOpen(_provider) && AllowByteCount)
                _bytecount = _provider.GetByteCount();
        }

        #endregion ByteCount Property

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            //Dispose managed object
            if (disposing)
            {
                _provider?.Dispose();
                _tblCharacterTable?.Dispose();
                _viewBuffer = null;
                _markedPositionList = null;
            }

            _disposedValue = true;
        }

        public void Dispose() => Dispose(true);

        #endregion

        #region IByteControl grouping support
        public ByteSpacerPosition ByteSpacerPositioning
        {
            get => (ByteSpacerPosition)GetValue(ByteSpacerPositioningProperty);
            set => SetValue(ByteSpacerPositioningProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteSpacerPositioning.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteSpacerPositioningProperty =
            DependencyProperty.Register(nameof(ByteSpacerPositioning), typeof(ByteSpacerPosition), typeof(HexEditor),
                new FrameworkPropertyMetadata(ByteSpacerPosition.Both, ByteSpacer_Changed));

        public ByteSpacerWidth ByteSpacerWidthTickness
        {
            get => (ByteSpacerWidth)GetValue(ByteSpacerWidthTicknessProperty);
            set => SetValue(ByteSpacerWidthTicknessProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteSpacer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteSpacerWidthTicknessProperty =
            DependencyProperty.Register(nameof(ByteSpacerWidthTickness), typeof(ByteSpacerWidth), typeof(HexEditor),
                new FrameworkPropertyMetadata(ByteSpacerWidth.Normal, ByteSpacer_Changed));

        private static void ByteSpacer_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.RefreshView(true);
        }

        public ByteSpacerGroup ByteGrouping
        {
            get => (ByteSpacerGroup)GetValue(ByteGroupingProperty);
            set => SetValue(ByteGroupingProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteGrouping.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteGroupingProperty =
            DependencyProperty.Register(nameof(ByteGrouping), typeof(ByteSpacerGroup), typeof(HexEditor),
                new FrameworkPropertyMetadata(ByteSpacerGroup.EightByte, ByteSpacer_Changed));

        public ByteSpacerVisual ByteSpacerVisualStyle
        {
            get => (ByteSpacerVisual)GetValue(ByteSpacerVisualStyleProperty);
            set => SetValue(ByteSpacerVisualStyleProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteSpacerVisualStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteSpacerVisualStyleProperty =
            DependencyProperty.Register(nameof(ByteSpacerVisualStyle), typeof(ByteSpacerVisual), typeof(HexEditor),
                new FrameworkPropertyMetadata(ByteSpacerVisual.Empty, ByteSpacer_Changed));

        /// <summary>
        /// Add byte spacer
        /// </summary>
        private void AddByteSpacer(StackPanel stack, int colomn, bool forceEmpty = false)
        {
            if (colomn % (int)ByteGrouping != 0 || colomn <= 0) return;

            if (!forceEmpty)
                switch (ByteSpacerVisualStyle)
                {
                    case ByteSpacerVisual.Empty:
                        stack.Children.Add(new TextBlock { Width = (int)ByteSpacerWidthTickness });
                        break;
                    case ByteSpacerVisual.Line:

                        #region Line

                        stack.Children.Add(new Line
                        {
                            Y2 = LineHeight,
                            X1 = (int)ByteSpacerWidthTickness / 2D,
                            X2 = (int)ByteSpacerWidthTickness / 2D,
                            Stroke = BorderBrush,
                            StrokeThickness = 1,
                            Width = (int)ByteSpacerWidthTickness
                        });

                        #endregion

                        break;
                    case ByteSpacerVisual.Dash:

                        #region LineDash

                        stack.Children.Add(new Line
                        {
                            Y2 = LineHeight - 1,
                            X1 = (int)ByteSpacerWidthTickness / 2D,
                            X2 = (int)ByteSpacerWidthTickness / 2D,
                            Stroke = BorderBrush,
                            StrokeDashArray = new DoubleCollection(new double[] { 2 }),
                            StrokeThickness = 1,
                            Width = (int)ByteSpacerWidthTickness
                        });

                        #endregion

                        break;
                }
            else
                stack.Children.Add(new TextBlock { Width = (int)ByteSpacerWidthTickness });
        }

        #endregion IByteControl grouping

        #region Caret support

        /// <summary>
        /// Initialize the caret
        /// </summary>
        private void InitializeCaret()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                BaseGrid.Children.Add(_caret);
                _caret.CaretHeight = LineHeight;
                _caret.BlinkPeriod = 600;
                _caret.Hide();
            }
        }

        /// <summary>
        /// Move the caret in a screen point
        /// </summary>
        internal void MoveCaret(Point point) => _caret.MoveCaret(point);

        /// <summary>
        /// Hide the caret
        /// </summary>
        internal void HideCaret() => _caret.Hide();

        /// <summary>
        /// Return true if caret is visible
        /// </summary>
        public bool IsCaretVisible => _caret.IsVisibleCaret;

        /// <summary>
        /// Set the site of caret
        /// </summary>
        internal void SetCaretSize(double width, double height)
        {
            _caret.CaretWidth = width;
            _caret.CaretHeight = height;
        }

        /// <summary>
        /// Set the mode of caret (Insert or Overtwrite)
        /// </summary>
        internal void SetCaretMode(CaretMode mode) => _caret.CaretMode = mode;

        #endregion

        #region Append/expend bytes to end of file
        //////////
        // TODO: Will be updated soon with the possibility to insert byte anywhere :)
        //////////

        /// <summary>
        /// Allow control to append/expend byte at end of file
        /// </summary>
        public bool AllowExtend { get; set; }

        /// <summary>
        /// Show a message box is true before append byte at end of file
        /// </summary>
        public bool AppendNeedConfirmation { get; set; } = true;

        /// <summary>
        /// Append one byte at end of file
        /// </summary>
        internal void AppendByte(byte[] bytesToAppend)
        {
            if (!AllowExtend) return;
            if (!CheckIsOpen(_provider)) return;

            if (AppendNeedConfirmation)
                if (MessageBox.Show(FModel.Properties.Resources.AppendByteConfirmationString, ApplicationName,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question, MessageBoxResult.Yes) != MessageBoxResult.Yes) return;

            _provider?.AppendByte(bytesToAppend);
            RefreshView();
        }

        #endregion

        #region Drag and drop support

        /// <summary>
        /// Allow the control to catch the file dropping 
        /// Note : AllowDrop need to be true
        /// </summary>
        public bool AllowFileDrop { get; set; } = true;

        /// <summary>
        /// Allow the control to catch the text dropping 
        /// Note : AllowDrop need to be true
        /// </summary>
        public bool AllowTextDrop { get; set; } = true;

        /// <summary>
        /// Show a messagebox for confirm open when a file are already open
        /// </summary>
        public bool FileDroppingConfirmation { get; set; } = true;

        private void Control_Drop(object sender, DragEventArgs e)
        {
            #region Text Dropping (Will be supported soon)

            var textDrop = e.Data.GetData(DataFormats.Text);
            if (textDrop != null && AllowTextDrop)
            {
                var textDropped = textDrop as string;

                if (!string.IsNullOrEmpty(textDropped) && CheckIsOpen(_provider))
                {
                    #region Insert at mouve over position
                    var position = SelectionStart;
                    bool rtn = false;
                    TraverseHexAndStringBytes(ctrl =>
                    {
                        Application.Current.DoEvents();

                        if (ctrl.IsMouseOverMe)
                        {
                            position = ctrl.BytePositionInStream;
                            rtn = true;
                        }
                    }, ref rtn);
                    #endregion

                    _provider.Paste(position, textDropped, AllowExtend);

                    RefreshView();
                }

                return;
            }

            #endregion

            #region File dropping (Only open first selected file catched in GetData)

            var fileDrop = e.Data.GetData(DataFormats.FileDrop);
            if (fileDrop != null && AllowFileDrop)
            {
                var filename = fileDrop as string[];

                if (!CheckIsOpen(_provider))
                    FileName = filename[0];
                else
                {
                    if (FileDroppingConfirmation && MessageBox.Show(
                            $"{FModel.Properties.Resources.FileDroppingConfirmationString} {Path.GetFileName(filename[0])} ?",
                            ApplicationName,
                            MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        FileName = filename[0];
                    else
                        FileName = filename[0];
                }
            }

            #endregion
        }

        #endregion

        #region Save/Load control state

        /// <summary>
        /// Save the current state of ByteProvider in a xml text file.
        /// </summary>
        public void SaveCurrentState(string filename)
        {
            if (!CheckIsOpen(_provider)) return;
            _provider.SaveState(filename);
        }

        /// <summary>
        /// Load state of control from a xml text file.
        /// </summary>
        public void LoadCurrentState(string filename)
        {
            if (!CheckIsOpen(_provider)) return;
            _provider.LoadState(filename);
            RefreshView();
        }

        #endregion

        #region Shift the first visible byte in the views to the left ...

        /// <summary>
        /// Shift the first visible byte in the view to the left. 
        /// Very useful for editing fixed-width tables. Use with BytePerLine to create visual tables ...
        /// The value is the number of byte to shift.
        /// </summary>
        public int ByteShiftLeft
        {
            get => (int)GetValue(ByteShiftLeftProperty);
            set => SetValue(ByteShiftLeftProperty, value);
        }

        // Using a DependencyProperty as the backing store for ByteShiftLeft.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ByteShiftLeftProperty =
            DependencyProperty.Register(nameof(ByteShiftLeft), typeof(int), typeof(HexEditor),
                new FrameworkPropertyMetadata(0, ByteShiftLeft_Changed, ByteShiftLeft_CoerceValue));

        private static void ByteShiftLeft_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl)
                ctrl.RefreshView(true);
        }

        private static object ByteShiftLeft_CoerceValue(DependencyObject d, object basevalue) =>
            (int)basevalue < 0 ? 0 : basevalue;

        #endregion

        #region Reverse bytes selection

        /// <summary>
        /// Reverse selection of bytes array like this {AA, FF, EE, DC} => {DC, EE, FF, AA}
        /// </summary>
        public void ReverseSelection()
        {
            if (!CheckIsOpen(_provider)) return;

            _provider.Reverse(SelectionStart, SelectionStop);

            RefreshView();
        }

        #endregion

        #region Line offset coloring...

        /// <summary>
        /// High light header and offset on SelectionStart
        /// </summary>
        public bool HighLightSelectionStart
        {
            get => _highLightSelectionStart;
            set
            {
                _highLightSelectionStart = value;
                RefreshView();
            }
        }

        #endregion

        #region Configure the start/stop bytes that are loaded visually into the hexadecimal editor
        public bool AllowVisualByteAddress
        {
            get { return (bool)GetValue(AllowVisualByteAdressProperty); }
            set { SetValue(AllowVisualByteAdressProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllowVisualByteAdress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowVisualByteAdressProperty =
            DependencyProperty.Register(nameof(AllowVisualByteAddress), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(false, AllowVisualByteAddress_Changed));

        private static void AllowVisualByteAddress_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
            {
                ctrl.UpdateScrollBar();
                ctrl.RefreshView(true);
            }
        }

        /// <summary>
        /// Set the last byte are virtually loaded in the control. 
        /// Note that all other bytes address after will not be shown in control.
        /// This property are read only
        /// </summary>
        public long VisualByteAdressStop => VisualByteAdressStart + VisualByteAdressLength;

        /// <summary>
        /// Set the length from first byte set by VisualStartByteAdress property are virtually loaded in the control. 
        /// </summary>
        public long VisualByteAdressLength
        {
            get { return (long)GetValue(VisualByteAdressLengthProperty); }
            set { SetValue(VisualByteAdressLengthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VisualByteAdressLength.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VisualByteAdressLengthProperty =
            DependencyProperty.Register(nameof(VisualByteAdressLength), typeof(long), typeof(HexEditor),
                new FrameworkPropertyMetadata(1L, VisualByteAdressLength_Changed, VisualByteAdressLength_CoerceValue));

        private static object VisualByteAdressLength_CoerceValue(DependencyObject d, object baseValue)
        {
            if (!(d is HexEditor ctrl)) return baseValue;

            var value = (long)baseValue;

            if (value < 1 || !CheckIsOpen(ctrl._provider)) return 1L;

            return value >= ctrl._provider.Length ? ctrl._provider.Length : baseValue;
        }

        private static void VisualByteAdressLength_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
            {
                ctrl.UpdateScrollBar();
                ctrl.RefreshView();
            }
        }

        /// <summary>
        /// Set the first byte are virtually loaded in the control. 
        /// Note that all other bytes address before will not be shown in control.
        /// </summary>
        public long VisualByteAdressStart
        {
            get => (long)GetValue(VisualByteAdressStartProperty);
            set => SetValue(VisualByteAdressStartProperty, value);
        }

        // Using a DependencyProperty as the backing store for VisualStartByteAdress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VisualByteAdressStartProperty =
            DependencyProperty.Register(nameof(VisualByteAdressStart), typeof(long), typeof(HexEditor),
                new FrameworkPropertyMetadata(0L, VisualByteAdressStart_Changed, VisualByteAdressStart_CoerceValue));

        private static object VisualByteAdressStart_CoerceValue(DependencyObject d, object baseValue)
        {
            if (!(d is HexEditor ctrl)) return baseValue;

            var value = (long)baseValue;

            if (!CheckIsOpen(ctrl._provider)) return 0L;

            return value >= ctrl._provider.Length ? ctrl._provider.Length : baseValue;

            //return (long)baseValue;
        }

        private static void VisualByteAdressStart_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl) || e.NewValue == e.OldValue) return;

            ctrl.UpdateScrollBar();
            ctrl.RefreshView();
        }
        #endregion

        #region Preload iByteControl at control creation

        /// <summary>
        /// Set to true for preload iByteControls at control creation for maximise the file/stream opening
        /// </summary>
        public bool AllowPreloadByteInEditor { get; set; } = false;

        private void Control_Loaded(object sender, RoutedEventArgs e)
        {
            if (AllowPreloadByteInEditor) BuildDataLines(MaxVisibleLine, true);
        }

        #endregion

        #region Zoom in/out support
        /// <summary>
        /// Get or set the scale transform to work with zoom
        /// </summary>
        private ScaleTransform _scaler = null;

        /// <summary>
        /// Allow or not the zoom in control
        /// </summary>
        private bool _allowZoom = true;

        /// <summary>
        /// Get or set the zoom scale 
        /// Posible Scale : 0.5 to 2.0 (50% to 200%)
        /// </summary>
        public double ZoomScale
        {
            get => (double)GetValue(ZoomScaleProperty);
            set => SetValue(ZoomScaleProperty, value);
        }

        // Using a DependencyProperty as the backing store for ZoomScale.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ZoomScaleProperty =
            DependencyProperty.Register(nameof(ZoomScale), typeof(double), typeof(HexEditor),
                new FrameworkPropertyMetadata(1D, ZoomScale_ChangedCallBack,
                    ZoomScale_CoerceValueCallBack));

        private static object ZoomScale_CoerceValueCallBack(DependencyObject d, object baseValue) =>
            (double)baseValue >= 0.5 && (double)baseValue <= 2.0001
                ? (double)baseValue
                : d.GetValue(ZoomScaleProperty);

        private static void ZoomScale_ChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl)) return;
            if (e.NewValue == e.OldValue) return;

            ctrl.UpdateZoom();
        }

        /// <summary>
        /// Allow or not the capability to zoom control content (use with ScaleX and ScaleY property)
        /// </summary>
        public bool AllowZoom
        {
            get => _allowZoom;

            set
            {
                _allowZoom = value;

                InitialiseZoom();
            }
        }
        /// <summary>
        /// Initialize the support of zoom
        /// </summary>
        private void InitialiseZoom()
        {
            if (_scaler == null)
            {
                _scaler = new ScaleTransform(ZoomScale, ZoomScale);

                HexHeaderStackPanel.LayoutTransform = _scaler;
                HexDataStackPanel.LayoutTransform = _scaler;
                StringDataStackPanel.LayoutTransform = _scaler;
                LinesInfoStackPanel.LayoutTransform = _scaler;
            }
        }

        /// <summary>
        /// Update the zoom to ScaleX, ScaleY value if AllowZoom is true
        /// </summary>
        private void UpdateZoom()
        {
            if (AllowZoom)
            {
                if (_scaler == null) InitialiseZoom();
                _scaler.ScaleY = ZoomScale;
                _scaler.ScaleX = ZoomScale;

                //TODO: Update caret size...

                ClearLineInfo();
                ClearAllBytes(true);
                RefreshView(true);
            }
        }

        /// <summary>
        /// Reset the zoom to 100%
        /// </summary>
        public void ResetZoom() => ZoomScale = 1.0;

        private void ZoomResetButton_Click(object sender, RoutedEventArgs e) => ResetZoom();
        #endregion

        #region Delete byte support
        /// <summary>
        /// Allow to delete byte on control
        /// </summary>
        public bool AllowDeleteByte
        {
            get { return (bool)GetValue(AllowDeleteByteProperty); }
            set { SetValue(AllowDeleteByteProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllowDeleteByte.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowDeleteByteProperty =
            DependencyProperty.Register(nameof(AllowDeleteByte), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, Control_DeletePropertyChanged));

        /// <summary>
        /// Hide bytes that are deleted
        /// TODO: NOT COMPLETED... NEED TO FIX THE FIRST VISIBLE BYTE...
        /// </summary>
        public bool HideByteDeleted
        {
            get { return (bool)GetValue(HideByteDeletedProperty); }
            set { SetValue(HideByteDeletedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HideByteDeleted.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HideByteDeletedProperty =
            DependencyProperty.Register(nameof(HideByteDeleted), typeof(bool), typeof(HexEditor),
                 new FrameworkPropertyMetadata(false, Control_DeletePropertyChanged));

        private static void Control_DeletePropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is HexEditor ctrl && e.NewValue != e.OldValue)
                ctrl.RefreshView(true);
        }

        /// <summary>
        /// Delete selection, add scroll marker and update control
        /// </summary>
        public void DeleteSelection()
        {
            if (!CanDelete) return;
            if (!CheckIsOpen(_provider)) return;

            var position = SelectionStart > SelectionStop ? SelectionStop : SelectionStart;
            var firstbyte = FirstVisibleBytePosition;

            _provider.AddByteDeleted(position, SelectionLength);

            SetScrollMarker(position, ScrollMarker.ByteDeleted);

            UpdateScrollBar();
            RefreshView(true);

            //Update selection
            SetPosition(firstbyte);
            UnSelectAll(true);

            //Launch deleted event
            BytesDeleted?.Invoke(this, new EventArgs());
        }

        #endregion

        #region Byte click and double click support

        /// <summary>
        /// Select all same byte of SelectionStart in rage of SelectionStart at double click
        /// </summary>
        public bool AllowAutoSelectSameByteAtDoubleClick
        {
            get { return (bool)GetValue(AllowAutoSelectSameByteAtDoubleClickProperty); }
            set { SetValue(AllowAutoSelectSameByteAtDoubleClickProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllowAutoSelectSameByteAtDoubleClick.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllowAutoSelectSameByteAtDoubleClickProperty =
            DependencyProperty.Register(nameof(AllowAutoSelectSameByteAtDoubleClick), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(true, Control_AllowAutoSelectSameByteAtDoubleClick));

        private static void Control_AllowAutoSelectSameByteAtDoubleClick(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl) || e.NewValue == e.OldValue) return;

            ctrl.RefreshView();
        }

        private void Control_Click(object sender, EventArgs e)
        {
            if (!(sender is IByteControl ctrl)) return;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
                SelectionStop = ctrl.BytePositionInStream;
            else
                SelectionStart = SelectionStop = ctrl.BytePositionInStream;

            UpdateSelectionColor(ctrl is StringByte ? FirstColor.StringByteData : FirstColor.HexByteData);
            UpdateVisual();

            //launch click event 
            ByteClick?.Invoke(sender, e);
        }

        private void Control_DoubleClick(object sender, EventArgs e)
        {
            if (!(sender is IByteControl ctrl)) return;
            if (!CheckIsOpen(_provider)) return;

            #region Select all same byte of SelectionStart in rage of selectionStart
            if (AllowAutoSelectSameByteAtDoubleClick)
            {
                var (singleByte, succes) = _provider.GetByte(SelectionStart);
                if (succes)
                {
                    var startPosition = SelectionStart;
                    var stopPosition = SelectionStop;

                    //Selection start
                    while (_provider.GetByte(GetValidPositionFrom(startPosition--, -1)).singleByte == singleByte && startPosition > 0)
                        SelectionStart = startPosition;

                    //Selection stop
                    while (_provider.GetByte(GetValidPositionFrom(stopPosition++, 1)).singleByte == singleByte && stopPosition < _provider.Length)
                        SelectionStop = stopPosition;
                }
            }
            #endregion

            UpdateSelectionColor(ctrl is StringByte ? FirstColor.StringByteData : FirstColor.HexByteData);
            UpdateVisual();

            //launch click event 
            ByteDoubleClick?.Invoke(sender, e);
        }
        #endregion

        #region WORK IN PROGRESS // CustomBackgroundBlock implementation

        /// <summary>
        /// Use CustomBackgroundBlock in the control
        /// ONLY DETECT EXE FILE FOR NOW... NOT POSSIBLE TO CREATE OWN CBB (WILL BE POSSIBLE SOON)
        /// </summary>
        public bool UseCustomBackGroudBlock
        {
            get => (bool)GetValue(UseCustomBackGroudBlockProperty);
            set => SetValue(UseCustomBackGroudBlockProperty, value);
        }

        // Using a DependencyProperty as the backing store for UseCustomBackGroudBlock.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UseCustomBackGroudBlockProperty =
            DependencyProperty.Register(nameof(UseCustomBackGroudBlock), typeof(bool), typeof(HexEditor),
                new FrameworkPropertyMetadata(false, Control_UseCustomBackGroudBlockPropertyChanged));

        private static void Control_UseCustomBackGroudBlockPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is HexEditor ctrl) || e.NewValue == e.OldValue) return;

            ctrl.RefreshView();
        }

        private List<CustomBackgroundBlock> _cbbList = new List<CustomBackgroundBlock>();

        internal CustomBackgroundBlock GetCustomBackgroundBlock(long BytePositionInStream) =>
            _cbbList?.FirstOrDefault(cbb => BytePositionInStream >= cbb.StartOffset &&
                                            BytePositionInStream <= cbb.StopOffset);

        #endregion

    }
}