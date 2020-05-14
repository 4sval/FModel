using PakReader;
using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
	public readonly struct FPathHashIndexEntry
	{
		public string Filename { get; }

		public int Location { get; }

		internal FPathHashIndexEntry(BinaryReader reader)
		{
			Filename = reader.ReadFString();
			Location = reader.ReadInt32();
		}
	}
}
