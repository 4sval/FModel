using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace FModel.Views.Resources.Controls;

/// <summary>
/// https://github.com/xceedsoftware/wpftoolkit/tree/master/ExtendedWPFToolkitSolution/Src/Xceed.Wpf.Toolkit/RichTextBox
/// </summary>
public interface ITextFormatter
{
    string GetText(FlowDocument document);
    void SetText(FlowDocument document, string text);
}

public class FLogger : ITextFormatter
{
    public static CustomRichTextBox Logger;
    private static readonly BrushConverter _brushConverter = new();

    public static void AppendInformation() => AppendText("[INF] ", Constants.BLUE);
    public static void AppendWarning() => AppendText("[WRN] ", Constants.YELLOW);
    public static void AppendError() => AppendText("[ERR] ", Constants.RED);
    public static void AppendDebug() => AppendText("[DBG] ", Constants.GREEN);

    public static void AppendText(string message, string color, bool newLine = false)
    {
        Application.Current.Dispatcher.Invoke(delegate
        {
            var textRange = new TextRange(Logger.Document.ContentEnd, Logger.Document.ContentEnd)
            {
                Text = newLine ? $"{message}{Environment.NewLine}" : message
            };

            try
            {
                textRange.ApplyPropertyValue(TextElement.ForegroundProperty, _brushConverter.ConvertFromString(color));
            }
            finally
            {
                Logger.ScrollToEnd();
            }
        });
    }

    public string GetText(FlowDocument document)
    {
        return new TextRange(document.ContentStart, document.ContentEnd).Text;
    }

    public void SetText(FlowDocument document, string text)
    {
        new TextRange(document.ContentStart, document.ContentEnd).Text = text;
    }
}

public class CustomRichTextBox : RichTextBox
{
    private bool _preventDocumentUpdate;
    private bool _preventTextUpdate;

    public CustomRichTextBox()
    {
    }

    public CustomRichTextBox(FlowDocument document) : base(document)
    {
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        "Text", typeof(string), typeof(CustomRichTextBox),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnTextPropertyChanged, CoerceTextProperty, true, UpdateSourceTrigger.LostFocus));

    public string Text
    {
        get => (string) GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((CustomRichTextBox) d).UpdateDocumentFromText();
    }

    private static object CoerceTextProperty(DependencyObject d, object value)
    {
        return value ?? "";
    }

    public static readonly DependencyProperty TextFormatterProperty =
        DependencyProperty.Register(
            "TextFormatter", typeof(ITextFormatter), typeof(CustomRichTextBox),
            new FrameworkPropertyMetadata(new FLogger(), OnTextFormatterPropertyChanged));

    public ITextFormatter TextFormatter
    {
        get => (ITextFormatter) GetValue(TextFormatterProperty);
        set => SetValue(TextFormatterProperty, value);
    }

    private static void OnTextFormatterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CustomRichTextBox richTextBox)
        {
            richTextBox.OnTextFormatterPropertyChanged((ITextFormatter) e.OldValue, (ITextFormatter) e.NewValue);
        }
    }

    protected virtual void OnTextFormatterPropertyChanged(ITextFormatter oldValue, ITextFormatter newValue)
    {
        UpdateTextFromDocument();
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        UpdateTextFromDocument();
        base.OnTextChanged(e);
    }

    private void UpdateTextFromDocument()
    {
        if (_preventTextUpdate)
            return;

        _preventDocumentUpdate = true;
        SetCurrentValue(TextProperty, TextFormatter.GetText(Document));
        _preventDocumentUpdate = false;
    }

    private void UpdateDocumentFromText()
    {
        if (_preventDocumentUpdate)
            return;

        _preventTextUpdate = true;
        TextFormatter.SetText(Document, Text);
        _preventTextUpdate = false;
    }

    public void Clear()
    {
        Document.Blocks.Clear();
    }

    public override void BeginInit()
    {
        base.BeginInit();
        _preventTextUpdate = true;
        _preventDocumentUpdate = true;
    }

    public override void EndInit()
    {
        base.EndInit();
        _preventTextUpdate = false;
        _preventDocumentUpdate = false;

        if (!string.IsNullOrEmpty(Text))
        {
            UpdateDocumentFromText();
        }
        else
        {
            UpdateTextFromDocument();
        }
    }
}