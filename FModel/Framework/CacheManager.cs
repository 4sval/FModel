using System;
using System.Collections.Generic;
using System.IO;
using FModel.Settings;
using Serilog;

namespace FModel.Framework;

public static class CacheManager
{
    public static string DataDirectory =>
        Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, ".data")).FullName;

    public static string ChunksDirectory =>
        Directory.CreateDirectory(Path.Combine(DataDirectory, "chunks")).FullName;

    public static string ManifestsDirectory =>
        Directory.CreateDirectory(Path.Combine(DataDirectory, "manifests")).FullName;

    public static string MappingsDirectory =>
        Directory.CreateDirectory(Path.Combine(DataDirectory, "mappings")).FullName;

    public static void EnsureDirectories()
    {
        _ = ChunksDirectory;
        _ = ManifestsDirectory;
        _ = MappingsDirectory;
    }

    public static void MigrateLegacyFiles()
    {
        EnsureDirectories();

        var movedFiles = 0;
        var skippedFiles = 0;
        var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.GetFullPath(UserSettings.Default.OutputDirectory),
            Path.GetFullPath(DataDirectory)
        };

        foreach (var sourceDirectory in sources)
        {
            foreach (var file in new DirectoryInfo(sourceDirectory).EnumerateFiles("*", SearchOption.TopDirectoryOnly))
            {
                var destinationDirectory = GetDestinationDirectory(file.Name);
                if (destinationDirectory is null) continue;

                var destinationPath = Path.Combine(destinationDirectory, file.Name);
                if (File.Exists(destinationPath))
                {
                    skippedFiles++;
                    continue;
                }

                try
                {
                    File.Move(file.FullName, destinationPath);
                    UpdateMappingEndpointPath(file.FullName, destinationPath);
                    movedFiles++;
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    Log.Warning(e, "Could not migrate cache file '{CacheFile}'", file.FullName);
                }
            }
        }

        if (movedFiles > 0)
            Log.Information("Migrated {CacheFileCount} cached file(s) into dedicated cache directories", movedFiles);
        if (skippedFiles > 0)
            Log.Warning("Skipped {CacheFileCount} cache migration(s) because the destination file already exists", skippedFiles);
    }

    private static string GetDestinationDirectory(string fileName)
    {
        if (fileName.EndsWith(".jmap.gz", StringComparison.OrdinalIgnoreCase))
            return MappingsDirectory;

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".chunk" or ".iochunk" or ".iopart" or ".utoc" => ChunksDirectory,
            ".manifest" => ManifestsDirectory,
            ".usmap" or ".jmap" => MappingsDirectory,
            _ => null
        };
    }

    private static void UpdateMappingEndpointPath(string oldPath, string newPath)
    {
        if (!newPath.StartsWith(MappingsDirectory, StringComparison.OrdinalIgnoreCase)) return;

        if (UserSettings.Default.PerDirectory is null) return;

        foreach (var directorySettings in UserSettings.Default.PerDirectory.Values)
        {
            if (directorySettings.Endpoints is null) continue;

            foreach (var endpoint in directorySettings.Endpoints)
            {
                if (endpoint is not null && string.Equals(endpoint.FilePath, oldPath, StringComparison.OrdinalIgnoreCase))
                    endpoint.FilePath = newPath;
            }
        }
    }
}
