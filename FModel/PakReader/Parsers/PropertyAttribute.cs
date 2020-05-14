using System;

namespace PakReader.Parsers
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class UPropAttribute : Attribute
    {
        public string Name { get; }

        public UPropAttribute(string name) =>
            Name = name;
    }
}
