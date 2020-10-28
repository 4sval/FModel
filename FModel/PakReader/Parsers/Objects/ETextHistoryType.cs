namespace FModel.PakReader.Parsers.Objects
{
    public enum ETextHistoryType : sbyte
    {
		None = -1,
		Base = 0,
		NamedFormat,
		OrderedFormat,
		ArgumentFormat,
		AsNumber,
		AsPercent,
		AsCurrency,
		AsDate,
		AsTime,
		AsDateTime,
		Transform,
		StringTableEntry,
		TextGenerator,

		// Add new enum types at the end only! They are serialized by index.
	}
}
