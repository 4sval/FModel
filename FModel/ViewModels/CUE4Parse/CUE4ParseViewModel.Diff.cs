using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Oodle.Objects;
using CUE4Parse.UE4.Shaders;
using CUE4Parse.UE4.Wwise;
using CUE4Parse_Conversion.Textures;
using FModel.Extensions;
using FModel.Framework;
using FModel.Settings;
using FModel.Views.Resources.Controls.Diff;
using Newtonsoft.Json;
using Serilog;
using SkiaSharp;

namespace FModel.ViewModels.CUE4Parse;

public partial class CUE4ParseViewModel
{
    public async Task ShowAssetDiff(string assetPath)
    {
        GameFile leftFile = TryGetFileByPathOrName(DiffProvider?.Files, assetPath);
        GameFile rightFile = TryGetFileByPathOrName(Provider?.Files, assetPath);

        var leftImage = LoadTabImageForDiff(DiffProvider, leftFile);
        var rightImage = LoadTabImageForDiff(Provider, rightFile);

        var titleExtra = Path.GetFileName(assetPath);
        string extension = Path.GetExtension(assetPath).TrimStart('.');

        var existingTab = TabControl.TabsItems.FirstOrDefault(tab => tab.ParentExportType == "Diff");

        await Application.Current.Dispatcher.Invoke(async () =>
        {
            var diffContent = await CreateDiffViewer(leftFile, rightFile, leftImage, rightImage, extension);

            if (existingTab != null)
            {
                existingTab.TitleExtra = titleExtra;
                existingTab.DiffContent = diffContent;
                if (TabControl.SelectedTab != existingTab)
                    TabControl.SelectedTab = existingTab;
            }
            else
            {
                var tab = new TabItem(new FakeGameFile("Diff Viewer"), "Diff")
                {
                    TitleExtra = titleExtra,
                    DiffContent = diffContent
                };
                TabControl.AddTab(tab);
                TabControl.SelectedTab = tab;
            }
        });
    }

    private async Task<object> CreateDiffViewer(GameFile leftFile, GameFile rightFile, TabImage leftImage, TabImage rightImage, string extension)
    {
        if (leftImage != null || rightImage != null)
        {
            if (leftImage != null && leftImage.VisuallyEquals(rightImage))
            {
                return new SameDataMessage();
            }

            var viewer = new ImageDiffViewer();
            viewer.SetImages(leftImage, rightImage);
            return viewer;
        }

        var (l, r) = GetExtractedTextsForDiff(leftFile, rightFile);
        if (AreTextsEqual(l, r))
        {
            return new SameDataMessage();
        }

        var dataDiffViewer = new DataDiffViewer(l, r, extension);
        await dataDiffViewer.Initialize();

        return dataDiffViewer;
    }

    private static List<string> SplitIntoChunks(string text)
    {
        const int maxLinesPerChunk = 150_000;
        var lines = text.Split('\n');
        var chunks = new List<string>();

        for (int i = 0; i < lines.Length; i += maxLinesPerChunk)
        {
            var chunkLines = lines.Skip(i).Take(maxLinesPerChunk);
            chunks.Add(string.Join("\n", chunkLines));
        }
        return chunks;
    }

    private (List<string> left, List<string> right) GetExtractedTextsForDiff(GameFile leftFile, GameFile rightFile)
    {
        List<string> left = leftFile != null ? SplitIntoChunks(ExtractTextForDiff(DiffProvider, leftFile)) : [];
        List<string> right = rightFile != null ? SplitIntoChunks(ExtractTextForDiff(Provider, rightFile)) : [];

        return (left, right);
    }

    private static GameFile TryGetFileByPathOrName(FileProviderDictionary files, string assetPath)
    {
        if (files.TryGetValue(assetPath, out var file))
            return file;

        var fileName = Path.GetFileName(assetPath);
        var matches = files.Where(kvp => Path.GetFileName(kvp.Key)!.Equals(fileName, StringComparison.OrdinalIgnoreCase)).ToList();

        switch (matches.Count)
        {
            case 1:
                return matches[0].Value;
            case > 1:
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new DiffFileSelectionDialog(matches.Select(m => m.Value).ToList());
                    if (dialog.ShowDialog() == true)
                    {
                        file = dialog.SelectedFile;
                    }
                });
                return file;
            default:
                return null;
        }
    }

    private static string ExtractTextForDiff(AbstractVfsFileProvider provider, GameFile entry)
    {
        var ext = entry.Extension.ToLowerInvariant();
        try
        {
            switch (ext)
            {
                case "uasset":
                case "umap":
                    {
                        var package = provider.GetLoadPackageResult(entry);
                        return JsonConvert.SerializeObject(package.GetDisplayData(), Formatting.Indented);
                    }
                // Common text-based formats
                case "json":
                case "manifest":
                case "uproject":
                case "uplugin":
                case "upluginmanifest":
                case "xml":
                case "ini":
                case "txt":
                case "log":
                case "bat":
                case "cfg":
                case "csv":
                case "pem":
                case "tps":
                case "tgc":
                case "lua":
                case "js":
                case "po":
                case "h":
                case "cpp":
                case "c":
                case "hpp":
                case "cs":
                case "vb":
                case "py":
                case "md":
                case "markdown":
                case "yml":
                case "yaml":
                case "sh":
                case "cmd":
                case "sql":
                case "css":
                case "scss":
                case "less":
                case "ts":
                case "tsx":
                case "jsx":
                case "html":
                case "htm":
                case "lsd":
                case "dat":
                case "ddr":
                case "ide":
                case "ipl":
                case "zon":
                case "verse":
                    {
                        var data = provider.SaveAsset(entry);
                        using var ms = new MemoryStream(data);
                        using var reader = new StreamReader(ms, true);
                        return reader.ReadToEnd();
                    }
                // Localization
                case "locmeta":
                    {
                        var archive = entry.CreateReader();
                        var metadata = new FTextLocalizationMetaDataResource(archive);
                        return JsonConvert.SerializeObject(metadata, Formatting.Indented);
                    }
                case "locres":
                    {
                        var archive = entry.CreateReader();
                        var locres = new FTextLocalizationResource(archive);
                        return JsonConvert.SerializeObject(locres, Formatting.Indented);
                    }
                // Asset registry
                case "bin" when entry.Name.Contains("AssetRegistry", StringComparison.OrdinalIgnoreCase):
                    {
                        var archive = entry.CreateReader();
                        var registry = new FAssetRegistryState(archive);
                        return JsonConvert.SerializeObject(registry, Formatting.Indented);
                    }
                // Shader cache
                case "bin" when entry.Name.Contains("GlobalShaderCache", StringComparison.OrdinalIgnoreCase):
                    {
                        var archive = entry.CreateReader();
                        var registry = new FGlobalShaderCache(archive);
                        return JsonConvert.SerializeObject(registry, Formatting.Indented);
                    }
                // Wwise
                case "bnk":
                case "pck":
                    {
                        var archive = entry.CreateReader();
                        var wwise = new WwiseReader(archive);
                        return JsonConvert.SerializeObject(wwise, Formatting.Indented);
                    }
                // Oodle dictionary
                case "udic":
                    {
                        var archive = entry.CreateReader();
                        var header = new FOodleDictionaryArchive(archive).Header;
                        return JsonConvert.SerializeObject(header, Formatting.Indented);
                    }
                // Shader bytecode
                case "ushaderbytecode":
                case "ushadercode":
                    {
                        var archive = entry.CreateReader();
                        var ar = new FShaderCodeArchive(archive);
                        return JsonConvert.SerializeObject(ar, Formatting.Indented);
                    }
                // Pipeline cache
                case "upipelinecache":
                    {
                        var archive = entry.CreateReader();
                        var ar = new FPipelineCacheFile(archive);
                        return JsonConvert.SerializeObject(ar, Formatting.Indented);
                    }
                default:
                    {
                        // Fallback: if it's a small file, try to display as text
                        var data = provider.SaveAsset(entry);
                        if (data.Length < 1024 * 1024) // 1MB
                        {
                            try
                            {
                                using var ms = new MemoryStream(data);
                                using var reader = new StreamReader(ms, true);
                                return reader.ReadToEnd();
                            }
                            catch { /* Ignore binary files */ }
                        }
                        return $"[No diffable text for type: {ext}]";
                    }
            }
        }
        catch (Exception ex)
        {
            return $"[Failed to extract: {ex.Message}]";
        }
    }

    private static TabImage LoadTabImageForDiff(AbstractVfsFileProvider provider, GameFile entry)
    {
        if (entry == null)
            return null;

        var ext = entry.Extension.ToLowerInvariant();
        var name = entry.NameWithoutExtension;
        const bool rnn = false;

        switch (ext)
        {
            case "png":
            case "jpg":
            case "jpeg":
            case "bmp":
                {
                    var data = provider.SaveAsset(entry);
                    using var ms = new MemoryStream(data);
                    var bmp = SKBitmap.Decode(ms);
                    return bmp != null
                        ? new TabImage(name, rnn, bmp)
                        : null;
                }

            case "svg":
                {
                    var data = provider.SaveAsset(entry);
                    using var ms = new MemoryStream(data);
                    var bmp = RenderSvg(ms);
                    return bmp != null
                        ? new TabImage(name, rnn, bmp)
                        : null;
                }
            case "uasset":
                {
                    try
                    {
                        var pkg = provider.LoadPackage(entry);

                        var pointer = new FPackageIndex(pkg, 1).ResolvedObject;

                        if (pointer?.Object?.Value is not UTexture texture)
                            return null;

                        CTexture[] textures;
                        if (texture is UTexture2DArray arr)
                            textures = arr.DecodeTextureArray(UserSettings.Default.CurrentDir.TexturePlatform);
                        else
                        {
                            var single = texture.Decode(UserSettings.Default.CurrentDir.TexturePlatform);
                            if (texture is UTextureCube)
                            {
                                single = single?.ToPanorama();
                            }

                            textures = [single];
                        }

                        if (textures != null)
                        {
                            var ct = textures.FirstOrDefault();
                            return ct != null
                                ? new TabImage(name, texture.RenderNearestNeighbor, ct)
                                : null;
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warning("Failed to decode UTexture for diff: {EntryPath} – {Message}", entry.Path, e.Message);
                        return null;
                    }

                    return null;
                }

            default:
                return null;
        }
    }

    private static bool AreTextsEqual(List<string> leftChunks, List<string> rightChunks)
    {
        if ((leftChunks == null || leftChunks.Count == 0) && (rightChunks == null || rightChunks.Count == 0))
            return true;

        if (leftChunks == null || rightChunks == null)
            return false;

        if (leftChunks.Count != rightChunks.Count)
            return false;

        var leftHash = ComputeHashForChunks(leftChunks);
        var rightHash = ComputeHashForChunks(rightChunks);

        return leftHash.SequenceEqual(rightHash);
    }

    private static byte[] ComputeHashForChunks(List<string> chunks)
    {
        var combined = string.Concat(chunks);
        return SHA256.HashData(Encoding.UTF8.GetBytes(combined));
    }
}
