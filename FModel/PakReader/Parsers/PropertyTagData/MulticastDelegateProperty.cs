using System;
using PakReader.Parsers.Objects;

namespace PakReader.Parsers.PropertyTagData
{
    public sealed class MulticastDelegateProperty : BaseProperty<object>
    {
        internal MulticastDelegateProperty(PackageReader reader, FPropertyTag tag)
        {
            throw new NotImplementedException(string.Format(FModel.Properties.Resources.ParsingNotSupported, "MulticastDelegateProperty"));
        }
    }
}
