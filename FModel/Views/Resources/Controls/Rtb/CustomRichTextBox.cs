using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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

public enum ELog
{
    Information,
    Warning,
    Error,
    Debug,
    None
}

public class FLogger : ITextFormatter
{
    public static CustomRichTextBox Logger;
    private static readonly BrushConverter _brushConverter = new();
    private static int _previous;

    public static void Append(ELog type, Action job)
    {
        Application.Current.Dispatcher.Invoke(delegate
        {
            switch (type)
            {
                case ELog.Information:
                    Text("[INF] ", Constants.BLUE);
                    break;
                case ELog.Warning:
                    Text("[WRN] ", Constants.YELLOW);
                    break;
                case ELog.Error:
                    Text("[ERR] ", Constants.RED);
                    break;
                case ELog.Debug:
                    Text("[DBG] ", Constants.GREEN);
                    break;
            }

            job();
        });
    }

    public static void Text(string message, string color, bool newLine = false)
    {
        try
        {
            Logger.Document.ContentEnd.InsertTextInRun(message);
            if (newLine) Logger.Document.ContentEnd.InsertLineBreak();

            Logger.Selection.Select(Logger.Document.ContentStart.GetPositionAtOffset(_previous), Logger.Document.ContentEnd);
            Logger.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, _brushConverter.ConvertFromString(color));
        }
        finally
        {
            Finally();
        }
    }

    public static void Link(string message, string url, bool newLine = false)
    {
        try
        {
            new Hyperlink(new Run(newLine ? $"{message}{Environment.NewLine}" : message), Logger.Document.ContentEnd)
            {
                NavigateUri = new Uri(url),
                OverridesDefaultStyle = true,
                Style = new Style(typeof(Hyperlink)) { Setters =
                {
                    new Setter(FrameworkContentElement.CursorProperty, Cursors.Hand),
                    new Setter(TextBlock.TextDecorationsProperty, TextDecorations.Underline),
                    new Setter(TextElement.ForegroundProperty, Brushes.Cornsilk)
                }}
            }.Click += (sender, _) => Process.Start("explorer.exe", $"/select, \"{((Hyperlink)sender).NavigateUri.AbsoluteUri}\"");
        }
        finally
        {
            Finally();
        }
    }

    private static void Finally()
    {
        Logger.ScrollToEnd();
        _previous = Math.Abs(Logger.Document.ContentEnd.GetOffsetToPosition(Logger.Document.ContentStart)) - 2;
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
