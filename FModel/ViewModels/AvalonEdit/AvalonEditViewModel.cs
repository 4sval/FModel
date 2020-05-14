using FModel.Logger;
using FModel.Utils;
using FModel.Windows.AvalonEditFindReplace;
using FModel.Windows.CustomNotifier;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml;

namespace FModel.ViewModels.AvalonEdit
{
    static class AvalonEditVm
    {
        public static readonly AvalonEditViewModel avalonEditViewModel = new AvalonEditViewModel();
        public static void Set(this AvalonEditViewModel vm, string assetProperties, string ownerName, IHighlightingDefinition format = null)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.Document = new TextDocument(assetProperties);
                vm.OwerName = ownerName;
                vm.Highlighter = format ?? JsonHighlighter;
            });

            if (Properties.Settings.Default.AutoSave) vm.Save(true);
        }
        public static void Reset(this AvalonEditViewModel vm)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.Document = new TextDocument();
                vm.OwerName = string.Empty;
                vm.Highlighter = JsonHighlighter;
            });
        }

        // always call this on the UI thread
        public static bool HasData(this AvalonEditViewModel vm) => !string.IsNullOrEmpty(vm.Document?.Text);
        public static void Save(this AvalonEditViewModel vm, bool autoSave)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (vm.HasData())
                {
                    if (autoSave)
                    {
                        string path = Properties.Settings.Default.OutputPath + "\\JSONs\\" + Path.ChangeExtension(vm.OwerName, ".json");
                        File.WriteAllText(path, vm.Document.Text);
                        if (File.Exists(path))
                        {
                            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AvalonEditViewModel]", $"{vm.OwerName} successfully saved");
                            FConsole.AppendText(string.Format(Properties.Resources.SaveSuccess, Path.ChangeExtension(vm.OwerName, ".json")), FColors.Green, true);
                        }
                    }
                    else
                    {
                        var saveFileDialog = new SaveFileDialog
                        {
                            Title = Properties.Resources.Save,
                            FileName = Path.ChangeExtension(vm.OwerName, ".json"),
                            InitialDirectory = Properties.Settings.Default.OutputPath + "\\JSONs\\",
                            Filter = Properties.Resources.JsonFilter
                        };
                        if ((bool)saveFileDialog.ShowDialog())
                        {
                            File.WriteAllText(saveFileDialog.FileName, vm.Document.Text);
                            if (File.Exists(saveFileDialog.FileName))
                            {
                                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AvalonEditViewModel]", $"{vm.OwerName} successfully saved");
                                Globals.gNotifier.ShowCustomMessage(Properties.Resources.Success, Properties.Resources.DataSaved);
                            }
                        }
                    }
                }
                else Globals.gNotifier.ShowCustomMessage(Properties.Resources.Error, Properties.Resources.NoDataToSave);
            });
        }

        public static readonly IHighlightingDefinition JsonHighlighter = LoadHighlighter("Json.xshd");
        public static readonly IHighlightingDefinition IniHighlighter = LoadHighlighter("Ini.xshd");
        public static IHighlightingDefinition LoadHighlighter(string resourceName)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            using var stream = executingAssembly.GetManifestResourceStream($"{executingAssembly.GetName().Name}.Resources.{resourceName}");
            using var reader = new XmlTextReader(stream);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        /// <summary>
        /// Adapter for Avalonedit TextEditor
        /// </summary>
        public class TextEditorAdapter : IEditor
        {
            public TextEditorAdapter(TextEditor editor) { te = editor; }

            readonly TextEditor te;
            public string Text { get { return te.Text; } }
            public int SelectionStart { get { return te.SelectionStart; } }
            public int SelectionLength { get { return te.SelectionLength; } }
            public void BeginChange() { te.BeginChange(); }
            public void EndChange() { te.EndChange(); }
            public void Select(int start, int length)
            {
                te.Select(start, length);
                TextLocation loc = te.Document.GetLocation(start);
                te.ScrollTo(loc.Line, loc.Column);
            }
            public void Replace(int start, int length, string ReplaceWith) { te.Document.Replace(start, length, ReplaceWith); }
        }
    }

    public class AvalonEditViewModel : PropertyChangedBase
    {
        private string _ownerName; // used to get the name of the file when saving or exporting
        public string OwerName
        {
            get { return _ownerName; }

            set { this.SetProperty(ref this._ownerName, value); }
        }
        private TextDocument _document;
        public TextDocument Document
        {
            get { return _document; }

            set { this.SetProperty(ref this._document, value); }
        }

        private IHighlightingDefinition _highlighter;
        public IHighlightingDefinition Highlighter
        {
            get { return _highlighter; }

            set { this.SetProperty(ref this._highlighter, value); }
        }
    }
}
