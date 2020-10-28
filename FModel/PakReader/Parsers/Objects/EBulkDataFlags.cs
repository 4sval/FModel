namespace FModel.PakReader.Parsers.Objects
{
    /**
     * Flags serialized with the bulk data.
     */
    public enum EBulkDataFlags : uint
    {
		/** Empty flag set. */
		BULKDATA_None = 0,
		/** If set, payload is stored at the end of the file and not inline. */
		BULKDATA_PayloadAtEndOfFile = 1 << 0,
		/** If set, payload should be [un]compressed using ZLIB during serialization. */
		BULKDATA_SerializeCompressedZLIB = 1 << 1,
		/** Force usage of SerializeElement over bulk serialization. */
		BULKDATA_ForceSingleElementSerialization = 1 << 2,
		/** Bulk data is only used once at runtime in the game. */
		BULKDATA_SingleUse = 1 << 3,
		/** Bulk data won't be used and doesn't need to be loaded. */
		BULKDATA_Unused = 1 << 5,
		/** Forces the payload to be saved inline, regardless of its size. */
		BULKDATA_ForceInlinePayload = 1 << 6,
		/** Flag to check if either compression mode is specified. */
		BULKDATA_SerializeCompressed = (BULKDATA_SerializeCompressedZLIB),
		/** Forces the payload to be always streamed, regardless of its size. */
		BULKDATA_ForceStreamPayload = 1 << 7,
		/** If set, payload is stored in a .upack file alongside the uasset. */
		BULKDATA_PayloadInSeperateFile = 1 << 8,
		/** DEPRECATED: If set, payload is compressed using platform specific bit window. */
		BULKDATA_SerializeCompressedBitWindow = 1 << 9,
		/** There is a new default to inline unless you opt out. */
		BULKDATA_Force_NOT_InlinePayload = 1 << 10,
		/** This payload is optional and may not be on device. */
		BULKDATA_OptionalPayload = 1 << 11,
		/** This payload will be memory mapped, this requires alignment, no compression etc. */
		BULKDATA_MemoryMappedPayload = 1 << 12,
		/** Bulk data size is 64 bits long. */
		BULKDATA_Size64Bit = 1 << 13,
		/** Duplicate non-optional payload in optional bulk data. */
		BULKDATA_DuplicateNonOptionalPayload = 1 << 14,
		/** Indicates that an old ID is present in the data, at some point when the DDCs are flushed we can remove this. */
		BULKDATA_BadDataVersion = 1 << 15,
		/** BulkData did not have it's offset changed during the cook and does not need the fix up at load time */
		BULKDATA_NoOffsetFixUp = 1 << 16,

		/* Runtime only flags below this point! Note that they take the high bits in reverse order! */

		/** Assigned at runtime to indicate that the BulkData allocation is a memory mapped region of a file and not raw data. */
		BULKDATA_DataIsMemoryMapped = 1 << 30,
		/** Assigned at runtime to indicate that the BulkData object has an async loading request in flight and will need to wait on it. */
		BULKDATA_HasAsyncReadPending = 1 << 29,
		/** Assigned at runtime to indicate that the BulkData object should be considered for discard even if it cannot load from disk. */
		BULKDATA_AlwaysAllowDiscard = 1 << 28,
	}
}
