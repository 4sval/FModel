using FModel.Framework;
using FModel.Models;
using FModel.Services;
using Newtonsoft.Json;
using System.Linq;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using System;
using System.Collections.Generic;
using System.IO;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using CUE4Parse.FileProvider.Objects;
using FModel.Extensions;
using FModel.Settings;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Objects.UObject;
using SkiaSharp;

namespace FModel.ViewModels;
public class FileComparisonViewModel : ViewModel
{
    public SnapshotFile OldFile { get; }
    public SnapshotFile NewFile { get; }

    private readonly ApplicationViewModel _applicationView;

    public List<DiffLine> DiffLines { get; private set; }
    public bool IsImageComparison { get; private set; }
    public ImageSource OldImageSource { get; private set; }
    public ImageSource NewImageSource { get; private set; }

    public FileComparisonViewModel(SnapshotFile oldFile, SnapshotFile newFile)
    {
        OldFile = oldFile;
        NewFile = newFile;
        _applicationView = ApplicationService.ApplicationView;
        LoadAndCompareContent();
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

    private CUE4ParseExtensions.LoadPackageResult LoadPackageFromSnapshot(SnapshotFile snapshotFile)
    {
        if (snapshotFile == null)
        {
            Log("LoadPackageFromSnapshot called with null snapshotFile.");
            return null;
        }

        Log($"Processing file: {snapshotFile.FilePath} from snapshot: {snapshotFile.SnapshotName} (Folder: {snapshotFile.FolderName})");

        string tempDir = string.Empty;
        try
        {
            var absoluteFilePath = Path.Combine(UserSettings.Default.OutputDirectory, "Snapshots", snapshotFile.FolderName, snapshotFile.FilePath.Replace("/", "\\"));
            Log($"Constructed absolute path: {absoluteFilePath}");

            bool fileExists = File.Exists(absoluteFilePath);
            Log($"File.Exists check returned: {fileExists}");

            if (fileExists)
            {
                tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                var tempFilePath = Path.Combine(tempDir, snapshotFile.FilePath.Replace("/", "\\"));
                Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));
                File.Copy(absoluteFilePath, tempFilePath);
                Log($"Copied file to temporary directory: {tempFilePath}");

                var mainProvider = _applicationView.CUE4Parse.Provider;
                var versions = mainProvider?.Versions;
                var keys = mainProvider?.Keys;

                var tempProvider = new DefaultFileProvider(new DirectoryInfo(tempDir), SearchOption.AllDirectories, versions);
                tempProvider.Initialize();
                if (keys != null)
                {
                    tempProvider.SubmitKeys(keys);
                }

                if (mainProvider?.MappingsContainer != null)
                {
                    tempProvider.MappingsContainer = mainProvider.MappingsContainer;
                }

                if (tempProvider.TryGetGameFile(snapshotFile.FilePath, out var fileEntry))
                {
                    Log("Successfully found game file in temporary provider.");
                    var result = tempProvider.GetLoadPackageResult(fileEntry);
                    Log(result != null ? "Successfully loaded package." : "Failed to load package.");
                    return result;
                }
                else
                {
                    Log("ERROR: Could not find game file in temporary provider despite file being copied.");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION in LoadPackageFromSnapshot: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
        Log("LoadPackageFromSnapshot finished with null result.");
        return null;
    }

    private void LoadAndCompareContent()
    {
        var oldPackageResult = LoadPackageFromSnapshot(OldFile);
        var newPackageResult = LoadPackageFromSnapshot(NewFile);

        var oldTexture = oldPackageResult?.Package.GetExports().FirstOrDefault(e => e is UTexture2D) as UTexture2D;
        var newTexture = newPackageResult?.Package.GetExports().FirstOrDefault(e => e is UTexture2D) as UTexture2D;

        if (newTexture != null && oldTexture != null)
        {
            IsImageComparison = true;

            try
            {
                var oldCTexture = oldTexture.Decode();
                if (oldCTexture != null)
                {
                    var oldSkBitmap = oldCTexture.ToSkBitmap();
                    using (var data = oldSkBitmap.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        var stream = new MemoryStream(data.ToArray());
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = stream;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                        OldImageSource = bitmapImage;
                    }
                }
            }
            catch (Exception) { /* Failed to decode old image */ }

            try
            {
                var newCTexture = newTexture.Decode();
                if (newCTexture != null)
                {
                    var newSkBitmap = newCTexture.ToSkBitmap();
                    using (var data = newSkBitmap.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        var stream = new MemoryStream(data.ToArray());
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = stream;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                        NewImageSource = bitmapImage;
                    }
                }
            }
            catch (Exception) { /* Failed to decode new image */ }

            RaisePropertyChanged(nameof(IsImageComparison));
            RaisePropertyChanged(nameof(OldImageSource));
            RaisePropertyChanged(nameof(NewImageSource));
        }
        else
        {
            IsImageComparison = false;
            string oldText = oldPackageResult != null ? JsonConvert.SerializeObject(oldPackageResult.GetDisplayData(), Formatting.Indented) : "File not found in snapshot: " + OldFile.FilePath;
            string newText = newPackageResult != null ? JsonConvert.SerializeObject(newPackageResult.GetDisplayData(), Formatting.Indented) : "File not found in snapshot: " + NewFile.FilePath;

            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(oldText ?? string.Empty, newText ?? string.Empty);

            DiffLines = new List<DiffLine>();
            foreach (var line in diff.Lines)
            {
                DiffLineType type = line.Type switch
                {
                    ChangeType.Inserted => DiffLineType.Added,
                    ChangeType.Deleted => DiffLineType.Deleted,
                    _ => DiffLineType.Unchanged
                };
                DiffLines.Add(new DiffLine { Text = line.Text, Type = type });
            }

            RaisePropertyChanged(nameof(IsImageComparison));
            RaisePropertyChanged(nameof(DiffLines));
        }
    }
}
