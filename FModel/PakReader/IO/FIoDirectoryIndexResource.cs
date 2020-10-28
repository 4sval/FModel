using System.IO;

namespace FModel.PakReader.IO
{
    public class FIoDirectoryIndexResource
    {
        public readonly string MountPoint;
        public readonly FIoDirectoryIndexEntry[] DirectoryEntries;
        public readonly FIoFileIndexEntry[] FileEntries;
        public readonly string[] StringTable;
        
        public FIoDirectoryIndexResource(Stream directoryIndexStream, bool caseSensitive)
        {
            using var reader = new BinaryReader(directoryIndexStream);
            MountPoint = reader.ReadFString();
            if (MountPoint.StartsWith("../../.."))
            {
                MountPoint = MountPoint[8..];
            }
            else
            {
                // Weird mount point location...
                MountPoint = "/";
            }
            if (!caseSensitive)
            {
                MountPoint = MountPoint.ToLowerInvariant();
            }
            DirectoryEntries = reader.ReadTArray(() => new FIoDirectoryIndexEntry(reader));
            FileEntries = reader.ReadTArray(() => new FIoFileIndexEntry(reader));
            StringTable = reader.ReadTArray(reader.ReadFString);
        }
    }
}