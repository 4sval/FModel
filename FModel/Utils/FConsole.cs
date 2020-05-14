using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;

namespace FModel.Utils
{
    public class FConsole : ITextFormatter
    {
        public static RichTextBox fConsoleControl;

        public static void AppendText(string message, string color, bool newLine = false)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                BrushConverter bc = new BrushConverter();
                TextRange textRange = new TextRange(
                    fConsoleControl.Document.ContentEnd,
                    fConsoleControl.Document.ContentEnd)
                {
                    Text = newLine ? $"{message}{Environment.NewLine}" : message
                };
                try
                {
                    textRange.ApplyPropertyValue(TextElement.ForegroundProperty,
                        bc.ConvertFromString(color));
                }
                finally
                {
                    fConsoleControl.ScrollToEnd();
                }
            });
        }

        public string GetText(FlowDocument document) => new TextRange(document.ContentStart, document.ContentEnd).Text;
        public void SetText(FlowDocument document, string text)
        {
            new TextRange(document.ContentStart, document.ContentEnd).Text = text;
        }
    }
}
