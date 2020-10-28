using System;
using FModel.PakReader.Parsers.Objects;

namespace FModel.PakReader.Parsers.PropertyTagData
{
    public sealed class LazyObjectProperty : BaseProperty<object>
    {
        internal LazyObjectProperty(PackageReader reader, FPropertyTag tag)
        {
            throw new NotImplementedException(string.Format(FModel.Properties.Resources.ParsingNotSupported, "LazyObjectProperty"));
        }
    }
}
