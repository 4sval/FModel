using System;
using System.IO;

namespace FModel.PakReader.Parsers.Objects
{
    public readonly struct FPackageFileSummary
    {
        const int PACKAGE_FILE_TAG = unchecked((int)0x9E2A83C1);
        const int PACKAGE_FILE_TAG_SWAPPED = unchecked((int)0xC1832A9E);

        private readonly EUnrealEngineObjectUE4Version FileVersionUE4;
        private readonly EUnrealEngineObjectLicenseeUE4Version FileVersionLicenseeUE4;
        private readonly FCustomVersionContainer CustomVersionContainer;

        public readonly int TotalHeaderSize;
        public readonly EPackageFlags PackageFlags;
        public readonly string FolderName;
        public readonly int NameCount;
        public readonly int NameOffset;
        //public readonly string LocalizationId; only serialized in editor
        public readonly int GatherableTextDataCount;
        public readonly int GatherableTextDataOffset;
        public readonly int ExportCount;
        public readonly int ExportOffset;
        public readonly int ImportCount;
        public readonly int ImportOffset;
        public readonly int DependsOffset;
        public readonly int SoftPackageReferencesCount;
        public readonly int SoftPackageReferencesOffset;
        public readonly int SearchableNamesOffset;
        public readonly int ThumbnailTableOffset;
        public readonly FGuid Guid;
        public readonly FGenerationInfo[] Generations;
        public readonly FEngineVersion SavedByEngineVersion;
        public readonly FEngineVersion CompatibleWithEngineVersion;
        public readonly ECompressionFlags CompressionFlags;
        public readonly uint PackageSource;
        public readonly bool bUnversioned;
        public readonly int AssetRegistryDataOffset;
        public readonly long BulkDataStartOffset;
        public readonly int WorldTileInfoDataOffset;
        public readonly int[] ChunkIDs;
        public readonly int PreloadDependencyCount;
        public readonly int PreloadDependencyOffset;

        internal FPackageFileSummary(BinaryReader reader)
        {
            bUnversioned = false;
            CustomVersionContainer = default;

            var Tag = reader.ReadInt32();
            if (Tag != PACKAGE_FILE_TAG && Tag != PACKAGE_FILE_TAG_SWAPPED)
            {
                throw new FileLoadException("Not a UE package");
            }

            // The package has been stored in a separate endianness than the linker expected so we need to force
            // endian conversion. Latent handling allows the PC version to retrieve information about cooked packages.
            if (Tag == PACKAGE_FILE_TAG_SWAPPED)
            {
                // Set proper tag.
                Tag = PACKAGE_FILE_TAG;
                // Toggle forced byte swapping.
                throw new NotImplementedException("Byte swapping for packages not implemented");
            }

            /**
		    * The package file version number when this package was saved.
		    *
		    * Lower 16 bits stores the UE3 engine version
		    * Upper 16 bits stores the UE4/licensee version
		    * For newer packages this is -7
		    *		-2 indicates presence of enum-based custom versions
		    *		-3 indicates guid-based custom versions
		    *		-4 indicates removal of the UE3 version. Packages saved with this ID cannot be loaded in older engine versions
		    *		-5 indicates the replacement of writing out the "UE3 version" so older versions of engine can gracefully fail to open newer packages
		    *		-6 indicates optimizations to how custom versions are being serialized
		    *		-7 indicates the texture allocation info has been removed from the summary
		    */
            var LegacyFileVersion = reader.ReadInt32();
            if (LegacyFileVersion < 0) // means we have modern version numbers
            {
                if (LegacyFileVersion < -7) // CurrentLegacyFileVersion
                {
                    // we can't safely load more than this because the legacy version code differs in ways we can not predict.
                    // Make sure that the linker will fail to load with it.
                    throw new FileLoadException("Can't load legacy UE3 file");
                }

                if (LegacyFileVersion != -4)
                {
                    reader.BaseStream.Position += 4; // LegacyUE3Version (int32)
                }
                FileVersionUE4 = (EUnrealEngineObjectUE4Version)reader.ReadInt32();
                FileVersionLicenseeUE4 = (EUnrealEngineObjectLicenseeUE4Version)reader.ReadInt32();

                if (LegacyFileVersion <= -2)
                {
                    CustomVersionContainer = new FCustomVersionContainer(reader);
                }

                if (FileVersionUE4 != 0 && FileVersionLicenseeUE4 != 0)
                {
                    // this file is unversioned, remember that, then use current versions
                    bUnversioned = true;

                    // set the versions to latest here, etc.
                }
            }
            else
            {
                // This is probably an old UE3 file, make sure that the linker will fail to load with it.
                throw new FileLoadException("Can't load legacy UE3 file");
            }

            TotalHeaderSize = reader.ReadInt32();
            FolderName = reader.ReadFString();
            PackageFlags = (EPackageFlags)reader.ReadUInt32();
            NameCount = reader.ReadInt32();
            NameOffset = reader.ReadInt32();

            // only serialize when file version is past VER_UE4_SERIALIZE_TEXT_IN_PACKAGES
            GatherableTextDataCount = reader.ReadInt32();
            GatherableTextDataOffset = reader.ReadInt32();

            ExportCount = reader.ReadInt32();
            ExportOffset = reader.ReadInt32();
            ImportCount = reader.ReadInt32();
            ImportOffset = reader.ReadInt32();
            DependsOffset = reader.ReadInt32();

            // only serialize when file version is past VER_UE4_ADD_STRING_ASSET_REFERENCES_MAP
            SoftPackageReferencesCount = reader.ReadInt32();
            SoftPackageReferencesOffset = reader.ReadInt32();

            // only serialize when file version is past VER_UE4_ADDED_SEARCHABLE_NAMES
            SearchableNamesOffset = reader.ReadInt32();

            ThumbnailTableOffset = reader.ReadInt32();
            Guid = new FGuid(reader);

            {
                var GenerationCount = reader.ReadInt32();
                if (GenerationCount > 0)
                {
                    Generations = new FGenerationInfo[GenerationCount];
                    for (int i = 0; i < Generations.Length; i++)
                    {
                        Generations[i] = new FGenerationInfo(reader);
                    }
                }
                else
                    Generations = null;
            }

            // only serialize when file version is past VER_UE4_ENGINE_VERSION_OBJECT
            SavedByEngineVersion = new FEngineVersion(reader);

            // only serialize when file version is past VER_UE4_PACKAGE_SUMMARY_HAS_COMPATIBLE_ENGINE_VERSION
            CompatibleWithEngineVersion = new FEngineVersion(reader);

            CompressionFlags = (ECompressionFlags)reader.ReadUInt32();
            if (CompressionFlags != ECompressionFlags.COMPRESS_None) // No support for deprecated compression
                throw new FileLoadException($"Incompatible compression flags ({(uint)CompressionFlags})");

            if (reader.ReadTArray(() => new FCompressedChunk(reader)).Length != 0) // "CompressedChunks"
            {
                throw new FileLoadException("Package level compression is enabled");
            }

            PackageSource = reader.ReadUInt32();
            reader.ReadTArray(() => reader.ReadFString()); // "AdditionalPackagesToCook"

            if (LegacyFileVersion > -7)
            {
                // We haven't used texture allocation info for ages and it's no longer supported anyway
                if (reader.ReadInt32() != 0) // "NumTextureAllocations"
                {
                    throw new FileLoadException("Can't load legacy UE3 file");
                }
            }

            AssetRegistryDataOffset = reader.ReadInt32();
            BulkDataStartOffset = reader.ReadInt64();

            // only serialize when file version is past VER_UE4_WORLD_LEVEL_INFO
            WorldTileInfoDataOffset = reader.ReadInt32();

            // only serialize when file version is past VER_UE4_CHANGED_CHUNKID_TO_BE_AN_ARRAY_OF_CHUNKIDS
            ChunkIDs = reader.ReadTArray(() => reader.ReadInt32());

            // only serialize when file version is past VER_UE4_PRELOAD_DEPENDENCIES_IN_COOKED_EXPORTS
            PreloadDependencyCount = reader.ReadInt32();
            PreloadDependencyOffset = reader.ReadInt32();
        }
    }
}
