using FindReplace;
using ICSharpCode.AvalonEdit;
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Utilities
{
    class CColors
    {
        public static string Blue = "#6495ED";
        public static string Red = "#ED6464";
        public static string White = "#EFEFEF";
        public static string ChallengeDescription = "RoyalBlue";
        public static string ChallengeCount = "Goldenrod";
        public static string ChallengeReward = "Crimson";
    }

    class UpdateMyProcessEvents
    {
        private string _textToDisplay;
        private string _stateText;
        public UpdateMyProcessEvents(string textToDisplay, string stateText)
        {
            _textToDisplay = textToDisplay;
            _stateText = stateText;
        }

        public void Update()
        {
            FWindow.FMain.Dispatcher.InvokeAsync(() =>
            {
                FWindow.FMain.PEventTextBlock.Text = _textToDisplay;
                FWindow.FMain.StateTextBlock.Text = _stateText;
                switch (_stateText)
                {
                    case "Error":
                    case "Yikes":
                        FWindow.FMain.StateTextBlock.Background = new SolidColorBrush(Color.FromRgb(244, 66, 66));
                        break;
                    case "Waiting":
                    case "Loading":
                    case "Processing":
                        FWindow.FMain.StateTextBlock.Background = new SolidColorBrush(Color.FromRgb(0, 141, 255));
                        break;
                    case "Success":
                        FWindow.FMain.StateTextBlock.Background = new SolidColorBrush(Color.FromRgb(0, 158, 63));
                        break;
                }
            });
        }
    }

    class UpdateMyConsole
    {
        private string _textToDisplay;
        private bool _newLine;
        private string _displayedColor;
        public UpdateMyConsole(string textToDisplay, string displayedColor, bool newLine = false)
        {
            _textToDisplay = textToDisplay;
            _displayedColor = displayedColor;
            _newLine = newLine;
        }

        public void Append()
        {
            FWindow.FMain.Dispatcher.InvokeAsync(() =>
            {
                BrushConverter bc = new BrushConverter();
                TextRange tr = new TextRange(FWindow.FMain.ConsoleBox_Main.Document.ContentEnd, FWindow.FMain.ConsoleBox_Main.Document.ContentEnd);
                tr.Text = _newLine ? $"{_textToDisplay}{Environment.NewLine}" : _textToDisplay;
                try
                {
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                        bc.ConvertFromString(_displayedColor));
                }
                catch (FormatException) { }
                FWindow.FMain.ConsoleBox_Main.ScrollToEnd();
            });
        }
    }

    class AvalonEdit
    {
        /// <summary>
        /// Adapter for Avalonedit TextEditor
        /// </summary>
        public class TextEditorAdapter : IEditor
        {
            public TextEditorAdapter(TextEditor editor) { te = editor; }

            TextEditor te;
            public string Text { get { return te.Text; } }
            public int SelectionStart { get { return te.SelectionStart; } }
            public int SelectionLength { get { return te.SelectionLength; } }
            public void BeginChange() { te.BeginChange(); }
            public void EndChange() { te.EndChange(); }
            public void Select(int start, int length)
            {
                te.Select(start, length);
                var loc = te.Document.GetLocation(start);
                te.ScrollTo(loc.Line, loc.Column);
            }
            public void Replace(int start, int length, string ReplaceWith) { te.Document.Replace(start, length, ReplaceWith); }

        }

        public static FindReplaceMgr SetFindReplaceDiag()
        {
            FindReplaceMgr FRM = new FindReplaceMgr();
            FRM.CurrentEditor = new TextEditorAdapter(FWindow.FMain.AssetPropertiesBox_Main);
            FRM.ShowSearchIn = false;
            FRM.OwnerWindow = FWindow.FMain;

            return FRM;
        }

        public static void SetAEConfig()
        {
            FindReplaceMgr FRM = SetFindReplaceDiag();
            FWindow.FMain.CommandBindings.Add(FRM.FindBinding);
            FWindow.FMain.CommandBindings.Add(FRM.ReplaceBinding);
            FWindow.FMain.CommandBindings.Add(FRM.FindNextBinding);
        }
    }

    class UIHelper
    {
        public static void DisplayError(string pak = null, string key = null)
        {
            if (string.IsNullOrEmpty(pak) && string.IsNullOrEmpty(key))
            {
                new UpdateMyConsole($"0x{FProp.Default.FPak_MainAES}", CColors.Red).Append();
                new UpdateMyConsole(" doesn't work with the main pak files", CColors.White, true).Append();
            }
            else
            {
                new UpdateMyConsole($"0x{key}", CColors.Red).Append();
                new UpdateMyConsole(" doesn't work with ", CColors.White).Append();
                new UpdateMyConsole(pak, CColors.Red, true).Append();
            }
        }
        public static void DisplayEmergencyError(Exception ex)
        {
            new UpdateMyConsole("Message: ", CColors.Red).Append();
            new UpdateMyConsole(ex.Message, CColors.White, true).Append();

            new UpdateMyConsole("Source: ", CColors.Red).Append();
            new UpdateMyConsole(ex.Source, CColors.White, true).Append();

            new UpdateMyConsole("Target: ", CColors.Red).Append();
            new UpdateMyConsole(ex.TargetSite.ToString(), CColors.White, true).Append();

            new UpdateMyConsole("\nContact me: @AsvalFN on Twitter or open an issue on GitHub", CColors.Red, true).Append();
        }
        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    FrameworkElement frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }
    }
}
