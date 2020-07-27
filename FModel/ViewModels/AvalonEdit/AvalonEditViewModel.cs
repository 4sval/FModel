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
        public static void Set(this AvalonEditViewModel vm, string assetProperties, string ownerPath, IHighlightingDefinition format = null)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.Document = new TextDocument(assetProperties);
                vm.OwnerName = Path.GetFileName(ownerPath);
                vm.OwnerPath = Path.GetDirectoryName(ownerPath);
                vm.Highlighter = format ?? JsonHighlighter;
            });

            if (Properties.Settings.Default.AutoSave) vm.Save(true);
        }
        public static void Reset(this AvalonEditViewModel vm)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                vm.Document = new TextDocument();
                vm.OwnerName = string.Empty;
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
                        string path = Properties.Settings.Default.OutputPath + "\\JSONs" + vm.OwnerPath + "\\";
                        string file = Folders.GetUniqueFilePath(path + Path.ChangeExtension(vm.OwnerName, ".json"));

                        Directory.CreateDirectory(path);
                        File.WriteAllText(file, vm.Document.Text);
                        if (File.Exists(file))
                        {
                            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AvalonEditViewModel]", $"{vm.OwnerName} successfully saved");
                            FConsole.AppendText(string.Format(Properties.Resources.SaveSuccess, Path.GetFileName(file)), FColors.Green, true);
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(Properties.Settings.Default.OutputPath + "\\JSONs" + vm.OwnerPath);
                        var saveFileDialog = new SaveFileDialog
                        {
                            Title = Properties.Resources.Save,
                            FileName = Path.ChangeExtension(vm.OwnerName, ".json"),
                            InitialDirectory = Properties.Settings.Default.OutputPath + "\\JSONs" + vm.OwnerPath,
                            Filter = Properties.Resources.JsonFilter
                        };
                        if ((bool)saveFileDialog.ShowDialog())
                        {
                            File.WriteAllText(saveFileDialog.FileName, vm.Document.Text);
                            if (File.Exists(saveFileDialog.FileName))
                            {
                                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AvalonEditViewModel]", $"{vm.OwnerName} successfully saved");
                                Globals.gNotifier.ShowCustomMessage(Properties.Resources.Success, Properties.Resources.DataSaved, string.Empty, saveFileDialog.FileName);
                            }
                        }
                    }
                }
                else Globals.gNotifier.ShowCustomMessage(Properties.Resources.Error, Properties.Resources.NoDataToSave);
            });
        }

        public static readonly IHighlightingDefinition JsonHighlighter = LoadHighlighter("Json.xshd");
        public static readonly IHighlightingDefinition IniHighlighter = LoadHighlighter("Ini.xshd");
        public static readonly IHighlightingDefinition XmlHighlighter = LoadHighlighter("Xml.xshd");
        public static readonly IHighlightingDefinition CppHighlighter = LoadHighlighter("Cpp.xshd");
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
        public string OwnerName
        {
            get { return _ownerName; }

            set { this.SetProperty(ref this._ownerName, value); }
        }
        private string _ownerPath;
        public string OwnerPath
        {
            get { return _ownerPath; }

            set { this.SetProperty(ref this._ownerPath, value); }
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
