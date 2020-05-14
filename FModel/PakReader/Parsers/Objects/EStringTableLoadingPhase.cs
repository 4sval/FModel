namespace PakReader.Parsers.Objects
{
    public enum EStringTableLoadingPhase : byte
    {
		/** This string table is pending load, and load should be attempted when possible */
		PendingLoad,
		/** This string table is currently being loaded, potentially asynchronously */
		Loading,
		/** This string was loaded, though that load may have failed */
		Loaded,
	}
}
