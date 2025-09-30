using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.VirtualFileSystem;
using FModel.Models;
using FModel.Settings;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace FModel.Services
{
    public class SnapshotService
    {
        private readonly string _snapshotDirectory = Path.Combine(UserSettings.Default.OutputDirectory, "Snapshots");

        public Task InitializeSnapshotDirectoryAsync()
        {
            Directory.CreateDirectory(_snapshotDirectory);
            return Task.CompletedTask;
        }

        public async Task CreateSnapshotAsync(string name, IEnumerable<GameFile> files, IProgress<(int processed, int total)> progress = null)
        {
            await InitializeSnapshotDirectoryAsync();

            var snapshotFolderName = name.Replace(" ", "_").Replace(":", "-");
            var snapshotFolderPath = Path.Combine(_snapshotDirectory, snapshotFolderName);
            Directory.CreateDirectory(snapshotFolderPath);

            var manifest = new Snapshot
            {
                Name = name,
                CreatedAt = DateTime.UtcNow,
                Files = new List<SnapshotFile>()
            };

            var fileList = files.ToList();
            var totalFiles = fileList.Count;
            var processedFiles = 0;

            foreach (var file in fileList)
            {
                progress?.Report((processedFiles, totalFiles));
                processedFiles++;
                if (file.IsUePackagePayload) continue;

                var provider = ApplicationService.ApplicationView.CUE4Parse.Provider;
                provider.Files.FindPayloads(file, out var uexp, out var ubulks, out var uptnls, true);
                var allParts = new List<GameFile> { file };
                if (uexp != null) allParts.Add(uexp);
                allParts.AddRange(ubulks);
                allParts.AddRange(uptnls);

                foreach (var part in allParts)
                {
                    var fileData = part.Read();
                    if (fileData == null || fileData.Length == 0) continue;

                    var relativePath = part.Path.Replace("/", "\\");
                    var fullPath = Path.Combine(snapshotFolderPath, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                    await File.WriteAllBytesAsync(fullPath, fileData);

                    using var sha256 = SHA256.Create();
                    var fileHash = BitConverter.ToString(sha256.ComputeHash(fileData)).Replace("-", "").ToLowerInvariant();

                    manifest.Files.Add(new SnapshotFile
                    {
                        FilePath = part.Path,
                        FileHash = fileHash,
                        SnapshotName = name,
                        FolderName = snapshotFolderName
                    });
                }
            }

            var manifestPath = Path.Combine(snapshotFolderPath, "manifest.json");
            var jsonContent = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            await File.WriteAllTextAsync(manifestPath, jsonContent);
        }

        public async Task<List<Snapshot>> GetSnapshotsAsync()
        {
            await InitializeSnapshotDirectoryAsync();
            var snapshots = new List<Snapshot>();
            var snapshotFolders = Directory.GetDirectories(_snapshotDirectory);

            foreach (var folder in snapshotFolders)
            {
                var manifestPath = Path.Combine(folder, "manifest.json");
                if (File.Exists(manifestPath))
                {
                    var jsonContent = await File.ReadAllTextAsync(manifestPath);
                    var snapshot = JsonConvert.DeserializeObject<Snapshot>(jsonContent);
                    if (snapshot != null)
                    {
                        snapshot.FolderName = new DirectoryInfo(folder).Name;
                        foreach (var file in snapshot.Files)
                        {
                            file.FolderName = snapshot.FolderName;
                        }
                        snapshots.Add(snapshot);
                    }
                }
            }

            return snapshots.OrderByDescending(s => s.CreatedAt).ToList();
        }

        public async Task<ComparisonResult> CompareSnapshotsAsync(string snapshotFolderNameA, string snapshotFolderNameB)
        {
            var snapshotA = await LoadSnapshotByFolderName(snapshotFolderNameA);
            var snapshotB = await LoadSnapshotByFolderName(snapshotFolderNameB);

            var filesA = snapshotA.Files.ToDictionary(f => f.FilePath, f => f, StringComparer.OrdinalIgnoreCase);
            var filesB = snapshotB.Files.ToDictionary(f => f.FilePath, f => f, StringComparer.OrdinalIgnoreCase);

            var result = new ComparisonResult();

            foreach (var fileB in filesB.Values)
            {
                if (filesA.TryGetValue(fileB.FilePath, out var fileA))
                {
                    if (fileA.FileHash != fileB.FileHash)
                    {
                        result.ModifiedFiles.Add(new SnapshotFilePair { OldFile = fileA, NewFile = fileB });
                    }
                    filesA.Remove(fileB.FilePath);
                }
                else
                {
                    result.AddedFiles.Add(fileB);
                }
            }

            result.DeletedFiles.AddRange(filesA.Values);

            // Detect renames
            var addedFilesLookup = result.AddedFiles.ToLookup(f => f.FileHash);
            var deletedFilesLookup = result.DeletedFiles.ToLookup(f => f.FileHash);

            var renamedHashes = addedFilesLookup.Select(g => g.Key).Intersect(deletedFilesLookup.Select(g => g.Key));

            var addedFilesToRemove = new List<SnapshotFile>();
            var deletedFilesToRemove = new List<SnapshotFile>();

            foreach (var hash in renamedHashes)
            {
                var addedWithHash = addedFilesLookup[hash].ToList();
                var deletedWithHash = deletedFilesLookup[hash].ToList();

                var pairs = Math.Min(addedWithHash.Count, deletedWithHash.Count);
                for (int i = 0; i < pairs; i++)
                {
                    var addedFile = addedWithHash[i];
                    var deletedFile = deletedWithHash[i];

                    result.RenamedFiles.Add(new SnapshotFileRename { OldFile = deletedFile, NewFile = addedFile });
                    addedFilesToRemove.Add(addedFile);
                    deletedFilesToRemove.Add(deletedFile);
                }
            }

            result.AddedFiles = result.AddedFiles.Except(addedFilesToRemove).ToList();
            result.DeletedFiles = result.DeletedFiles.Except(deletedFilesToRemove).ToList();
            return result;
        }

        public async Task<ComparisonResult> CompareProviderToSnapshotAsync(Snapshot snapshotA, IFileProvider providerB)
        {
            var filesA = snapshotA.Files.ToDictionary(f => f.FilePath, f => f, StringComparer.OrdinalIgnoreCase);

            // Create a virtual manifest for the current provider
            var filesB = new Dictionary<string, SnapshotFile>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in providerB.Files.Values)
            {
                if (file.IsUePackagePayload) continue;

                var fileData = file.Read();
                if (fileData == null || fileData.Length == 0) continue;

                using var sha256 = SHA256.Create();
                var fileHash = BitConverter.ToString(sha256.ComputeHash(fileData)).Replace("-", "").ToLowerInvariant();

                filesB[file.Path] = new SnapshotFile
                {
                    FilePath = file.Path,
                    FileHash = fileHash,
                    SnapshotName = "Live Session"
                };
            }

            var result = new ComparisonResult();

            foreach (var fileB in filesB.Values)
            {
                if (filesA.TryGetValue(fileB.FilePath, out var fileA))
                {
                    if (fileA.FileHash != fileB.FileHash)
                    {
                        result.ModifiedFiles.Add(new SnapshotFilePair { OldFile = fileA, NewFile = fileB });
                    }
                    filesA.Remove(fileB.FilePath);
                }
                else
                {
                    result.AddedFiles.Add(fileB);
                }
            }

            result.DeletedFiles.AddRange(filesA.Values);

            // Rename detection logic can be added here if necessary, similar to CompareSnapshotsAsync

            return result;
        }

        private static void Log(string message)
        {
            try
            {
                var logPath = Path.Combine(UserSettings.Default.OutputDirectory, "debug_comparison.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch (Exception) { /* Ignore logging errors */ }
        }

        private async Task<Snapshot> LoadSnapshotByFolderName(string folderName)
        {
            Log($"--- Loading snapshot from folder: {folderName} ---");
            var manifestPath = Path.Combine(_snapshotDirectory, folderName, "manifest.json");

            if (!File.Exists(manifestPath))
            {
                Log($"Manifest file not found at: {manifestPath}");
                return null;
            }

            var jsonContent = await File.ReadAllTextAsync(manifestPath);
            var snapshot = JsonConvert.DeserializeObject<Snapshot>(jsonContent);

            if (snapshot == null)
            {
                Log("Failed to deserialize manifest file.");
                return null;
            }

            snapshot.FolderName = folderName;
            Log($"Snapshot '{snapshot.Name}' loaded. Assigning FolderName '{folderName}' to root.");
            foreach (var file in snapshot.Files)
            {
                file.FolderName = folderName;
            }
            Log($"Assigned FolderName to {snapshot.Files.Count} child files.");

            return snapshot;
        }
    }
}