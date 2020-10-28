using FModel.PakReader.Parsers.Class;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader
{
    public abstract class Package
    {
        public abstract string JsonData { get; }
        public abstract FName[] ExportTypes { get; }
        public abstract IUExport[] Exports { get; }
        
        public T GetExport<T>() where T : IUExport
        {
            var exports = Exports;
            for (int i = 0; i < exports.Length; i++)
            {
                if (exports[i] is T)
                    return (T)exports[i];
            }
            return default;
        }
        public T GetIndexedExport<T>(int index) where T : IUExport
        {
            var exports = Exports;
            var foundCount = 0;
            for (var i = 0; i < exports.Length; i++)
            {
                if (exports[i] is T cast)
                {
                    if (foundCount == index)
                        return cast;
                    foundCount++;
                }
            }
            return default;
        }
        public T GetTypedExport<T>(string exportType) where T : IUExport
        {
            int index = 0;
            var exportTypes = ExportTypes;
            for (int i = 0; i < exportTypes.Length; i++)
            {
                if (exportTypes[i].String == exportType)
                    index = i;
            }
            return (T)Exports[index];
        }

        public bool HasExport() => Exports != default;
    }
    
    public sealed class ExportList
    {
        public string JsonData;
        public FName[] ExportTypes;
        public IUExport[] Exports;
    }

    public sealed class JsonExport
    {
        public string ExportType;
        public object ExportValue;
    }
}