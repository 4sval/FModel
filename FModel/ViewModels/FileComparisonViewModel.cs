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

    private readonly string _oldFolderName;
    private readonly string _newFolderName;

    public FileComparisonViewModel(SnapshotFile oldFile, SnapshotFile newFile, string oldFolderName, string newFolderName)
    {
        OldFile = oldFile;
        NewFile = newFile;
        _oldFolderName = oldFolderName;
        _newFolderName = newFolderName;

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

    private CUE4ParseExtensions.LoadPackageResult LoadPackageFromSnapshot(SnapshotFile snapshotFile, string folderName)
    {
        if (snapshotFile == null)
        {
            Log("LoadPackageFromSnapshot called with null snapshotFile.");
            return null;
        }

        Log($"Processing file: {snapshotFile.FilePath} from snapshot: {snapshotFile.SnapshotName} (Folder: {folderName})");

        // Handle Live Session loading
        if (folderName == "Live")
        {
            try
            {
                var mainProvider = _applicationView.CUE4Parse.Provider;
                if (mainProvider.TryGetGameFile(snapshotFile.FilePath, out var fileEntry))
                {
                    Log("Successfully found game file in main provider for Live session.");
                    var result = mainProvider.GetLoadPackageResult(fileEntry);
                    Log(result != null ? "Successfully loaded package from Live session." : "Failed to load package from Live session.");
                    return result;
                }
                else
                {
                    Log($"ERROR: Could not find game file {snapshotFile.FilePath} in main provider for Live session.");
                }
            }
            catch (Exception ex)
            {
                Log($"EXCEPTION in Live Session LoadPackageFromSnapshot: {ex.Message}\n{ex.StackTrace}");
            }
            return null;
        }

        // Handle Snapshot loading
        try
        {
            var snapshotDirectory = Path.Combine(UserSettings.Default.OutputDirectory, "Snapshots", folderName);
            if (!Directory.Exists(snapshotDirectory))
            {
                Log($"ERROR: Snapshot directory not found at {snapshotDirectory}");
                return null;
            }

            var mainProvider = _applicationView.CUE4Parse.Provider;
            var versions = mainProvider?.Versions;
            var keys = mainProvider?.Keys;

            // Create a provider that points directly to the snapshot folder, so it can see all parts of an asset (.uasset, .ubulk, etc.)
            var snapshotProvider = new DefaultFileProvider(new DirectoryInfo(snapshotDirectory), SearchOption.AllDirectories, versions);
            snapshotProvider.Initialize();

            if (keys != null)
            {
                snapshotProvider.SubmitKeys(keys);
            }

            if (mainProvider?.MappingsContainer != null)
            {
                snapshotProvider.MappingsContainer = mainProvider.MappingsContainer;
            }

            if (snapshotProvider.TryGetGameFile(snapshotFile.FilePath, out var fileEntry))
            {
                Log("Successfully found game file in snapshot provider.");
                var result = snapshotProvider.GetLoadPackageResult(fileEntry);
                Log(result != null ? "Successfully loaded package from snapshot." : "Failed to load package from snapshot.");
                return result;
            }
            else
            {
                Log($"ERROR: Could not find game file {snapshotFile.FilePath} in snapshot provider at {snapshotDirectory}.");
            }
        }
        catch (Exception ex)
        {
            Log($"EXCEPTION in Snapshot LoadPackageFromSnapshot: {ex.Message}\n{ex.StackTrace}");
        }

        Log("LoadPackageFromSnapshot finished with null result.");
        return null;
    }

    private void LoadAndCompareContent()
    {
        var oldPackageResult = LoadPackageFromSnapshot(OldFile, _oldFolderName);
        var newPackageResult = LoadPackageFromSnapshot(NewFile, _newFolderName);

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
