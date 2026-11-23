using System;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Versions;
using Serilog;

namespace FModel.Framework;

public static class StaticMappingGenerator
{
    public static string Generate(string executable, string mappingsDirectory, EGame game, EUsmapCompressionMethod compression, string gameDisplayName = null)
    {
        if (game < EGame.GAME_UE4_18)
            throw new NotSupportedException("Static mapping generation requires Unreal Engine 4.18 or later.");

        var directory = Directory.CreateDirectory(mappingsDirectory).FullName;
        var pendingOutput = Path.Combine(directory, $"{Guid.NewGuid():N}.usmap");

        try
        {
            if (!UsmapGenerator.Generate(executable, pendingOutput, compression))
                throw new InvalidOperationException("UsmapGen could not create the mapping.");

            using var stream = File.OpenRead(pendingOutput);
            var engineVersion = new UsmapParser(stream, comparer: StringComparer.Ordinal).EngineVersion;

            var major = engineVersion?.Major ?? ((int) game >> 24);
            var minor = engineVersion?.Minor ?? (((int) game >> 16) & 0xFF);
            var suffix = compression == EUsmapCompressionMethod.None ? "" : $"_{compression}";
            var name = gameDisplayName == null ? null : string.Join("_", gameDisplayName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrEmpty(name) || string.Equals(name, "Unknown", StringComparison.OrdinalIgnoreCase))
                name = Path.GetFileNameWithoutExtension(executable);

            // Example: 501_GameName_22-11-2026_ZStandard.usmap (Engine version without using "." is intentional)
            var fileName = FormattableString.Invariant($"{major}{minor:D2}_{name}_{DateTime.Today:dd-MM-yyyy}{suffix}.usmap");
            var output = Path.Combine(directory, fileName);
            File.Move(pendingOutput, output, overwrite: true);
            return output;
        }
        finally
        {
            try
            {
                File.Delete(pendingOutput);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warning(ex, "Could not remove temporary mapping {Path}", pendingOutput);
            }
        }
    }

    public static string FindShippingExe(string archiveDirectory)
    {
        if (!Directory.Exists(archiveDirectory))
            throw new DirectoryNotFoundException("Static mapping generation requires a local archive directory.");

        var archive = new DirectoryInfo(archiveDirectory);
        for (var directory = archive; directory.Parent != null; directory = directory.Parent)
        {
            if (Search(directory) is { } executable)
                return executable;
        }

        var options = new EnumerationOptions { AttributesToSkip = FileAttributes.ReparsePoint };
        string match = null;
        foreach (var project in FindProjects(archive))
        {
            if (Search(project) is not { } executable)
                continue;

            if (match != null)
                throw new FileNotFoundException($"Found multiple Win64 shipping executables under '{archiveDirectory}'.");

            match = executable;
        }

        return match ?? throw new FileNotFoundException($"No Win64 shipping executable was found near '{archiveDirectory}'.");

        IEnumerable<DirectoryInfo> FindProjects(DirectoryInfo directory)
        {
            foreach (var child in directory.EnumerateDirectories("*", options))
            {
                if (child.Name.Equals("Engine", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (Directory.Exists(Path.Combine(child.FullName, "Content", "Paks")))
                {
                    yield return child;
                    continue;
                }

                foreach (var project in FindProjects(child))
                    yield return project;
            }
        }

        static string Search(DirectoryInfo project)
        {
            var binaries = Path.Combine(project.FullName, "Binaries", "Win64");
            if (!Directory.Exists(binaries))
                return null;

            var preferred = Path.Combine(binaries, $"{project.Name}-Win64-Shipping.exe");
            if (File.Exists(preferred))
                return preferred;

            var projectExecutable = Path.Combine(binaries, $"{project.Name}.exe");
            if (File.Exists(projectExecutable))
                return projectExecutable;

            using var candidates = Directory.EnumerateFiles(binaries, "*-Shipping.exe").GetEnumerator();
            if (!candidates.MoveNext())
                return null;

            var executable = candidates.Current;
            if (candidates.MoveNext())
                throw new FileNotFoundException($"Found multiple shipping executables in '{binaries}'.");

            return executable;
        }
    }
}
