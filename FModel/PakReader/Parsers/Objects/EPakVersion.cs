namespace PakReader.Parsers.Objects
{
    // NOTE: THIS IS NOT AN ACTUAL ENUM IN UE4.
    // LINK: https://github.com/EpicGames/UnrealEngine/blob/8b6414ae4bca5f93b878afadcc41ab518b09984f/Engine/Source/Runtime/PakFile/Public/IPlatformFilePak.h#L85
    public enum EPakVersion
    {
        INITIAL = 1,
        NO_TIMESTAMPS = 2,
        COMPRESSION_ENCRYPTION = 3,         // UE4.13+
        INDEX_ENCRYPTION = 4,               // UE4.17+ - encrypts only pak file index data leaving file content as is
        RELATIVE_CHUNK_OFFSETS = 5,         // UE4.20+
        DELETE_RECORDS = 6,                 // UE4.21+ - this constant is not used in UE4 code
        ENCRYPTION_KEY_GUID = 7,            // ... allows to use multiple encryption keys over the single project
        FNAME_BASED_COMPRESSION_METHOD = 8, // UE4.22+ - use string instead of enum for compression method
        FROZEN_INDEX = 9,
        PATH_HASH_INDEX = 10,


        LAST,
        INVALID,
        LATEST = LAST - 1
    }
}
