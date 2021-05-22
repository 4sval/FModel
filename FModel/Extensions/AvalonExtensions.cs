using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace FModel.Extensions
{
    public static class AvalonExtensions
    {
        private static readonly IHighlightingDefinition _jsonHighlighter = LoadHighlighter("Json.xshd");
        private static readonly IHighlightingDefinition _iniHighlighter = LoadHighlighter("Ini.xshd");
        private static readonly IHighlightingDefinition _xmlHighlighter = LoadHighlighter("Xml.xshd");
        private static readonly IHighlightingDefinition _cppHighlighter = LoadHighlighter("Cpp.xshd");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IHighlightingDefinition LoadHighlighter(string resourceName)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            using var stream = executingAssembly.GetManifestResourceStream($"{executingAssembly.GetName().Name}.Resources.{resourceName}");
            using var reader = new XmlTextReader(stream);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IHighlightingDefinition HighlighterSelector(string ext)
        {
            switch (ext)
            {
                case "ini":
                case "csv":
                    return _iniHighlighter;
                case "xml":
                    return _xmlHighlighter;
                case "h":
                case "cpp":
                    return _cppHighlighter;
                case "bat":
                case "txt":
                case "po":
                    return null;
                default:
                    return _jsonHighlighter;
            }
        }
    }
}