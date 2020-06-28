using System.Collections.Generic;

namespace PakReader.Parsers.Class
{
    /// <summary>
    /// IReadOnlyDictionary<string, object> is only used to be able to iterate over properties
    /// The derived class must have a "readonly Dictionary<string, object>" of properties
    /// </summary>
    public interface IUExport : IReadOnlyDictionary<string, object>
    {
        
    }

    public static class IUExportExtension
    {
        public static T GetExport<T>(this IUExport export, params string[] names)
        {
            foreach (string name in names)
            {
                if (export != null && export.TryGetValue(name, out var obj) && obj is T)
                    return (T)obj;
            }
            return default;
        }
    }
}
