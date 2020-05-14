using PakReader;
using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
	public readonly struct FPakDirectoryEntry
	{
		public string Directory { get; }

		public FPathHashIndexEntry[] Entries { get; }

		internal FPakDirectoryEntry(BinaryReader reader)
		{
			Directory = reader.ReadFString();
			Entries = reader.ReadTArray(() => new FPathHashIndexEntry(reader));
		}
	}
}
