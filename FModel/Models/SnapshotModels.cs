using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FModel.Models;

public class Snapshot
{
    public string Name { get; set; }

    [JsonIgnore]
    public string FolderName { get; set; }

    public DateTime CreatedAt { get; set; }
    public List<SnapshotFile> Files { get; set; }
}

public class SnapshotFile
{
    public string FilePath { get; set; }
    public string FileHash { get; set; }
    public string SnapshotName { get; set; }
    [JsonIgnore]
    public string FolderName { get; set; }
}

public class SnapshotFilePair
{
    public SnapshotFile OldFile { get; set; }
    public SnapshotFile NewFile { get; set; }
}

public class SnapshotFileRename
{
    public SnapshotFile OldFile { get; set; }
    public SnapshotFile NewFile { get; set; }
}

public class ComparisonResult
{
    public List<SnapshotFile> AddedFiles { get; set; } = new();
    public List<SnapshotFile> DeletedFiles { get; set; } = new();
    public List<SnapshotFilePair> ModifiedFiles { get; set; } = new();
    public List<SnapshotFileRename> RenamedFiles { get; set; } = new();
}