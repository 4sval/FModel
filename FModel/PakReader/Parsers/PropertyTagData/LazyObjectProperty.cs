using System;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class LazyObjectProperty : BaseProperty<object>
    {
        internal LazyObjectProperty(PackageReader reader, FPropertyTag tag)
        {
            throw new NotImplementedException(string.Format(FModel.Properties.Resources.ParsingNotSupported, "LazyObjectProperty"));
        }
    }
}
