using System;

namespace FModel
{
    public struct BackupFilesEntry : IEquatable<BackupFilesEntry>
    {
        internal BackupFilesEntry(string fileName, string fileDownload)
        {
            bFileName = fileName;
            bFileDownload = fileDownload;
        }
        public string bFileName { get; set; }
        public string bFileDownload { get; set; }

        bool IEquatable<BackupFilesEntry>.Equals(BackupFilesEntry other)
        {
            throw new NotImplementedException();
        }
    }
}
