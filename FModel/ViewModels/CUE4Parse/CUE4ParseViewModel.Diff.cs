using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Localization;
using CUE4Parse.UE4.Oodle.Objects;
using CUE4Parse.UE4.Shaders;
using CUE4Parse.UE4.Wwise;
using FModel.Extensions;
using FModel.Framework;
using FModel.Views.Resources.Controls.Diff;
using Newtonsoft.Json;

namespace FModel.ViewModels.CUE4Parse;

public partial class CUE4ParseViewModel
{
    public async Task ShowAssetDiff(string assetPath)
    {
        GameFile entry1 = null, entry2 = null;
        _ = DiffProvider?.Files?.TryGetValue(assetPath, out entry1);
        _ = Provider?.Files?.TryGetValue(assetPath, out entry2);

        var leftImage = LoadTabImageForDiff(DiffProvider, entry1);
        var rightImage = LoadTabImageForDiff(Provider, entry2);

        var titleExtra = Path.GetFileNameWithoutExtension(assetPath);
        string extension = Path.GetExtension(assetPath).TrimStart('.');

        var existingTab = TabControl.TabsItems.FirstOrDefault(tab => tab.ParentExportType == "Diff");

        await Application.Current.Dispatcher.Invoke(async () =>
        {
            var diffContent = await CreateDiffViewer(leftImage, rightImage, assetPath, extension);

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

    private async Task<object> CreateDiffViewer(TabImage leftImage, TabImage rightImage, string assetPath, string extension)
    {
        if (leftImage != null || rightImage != null)
        {
            var viewer = new ImageDiffViewer();
            viewer.SetImages(leftImage, rightImage);
            return viewer;
        }

        var (l, r) = GetExtractedTextsForDiff(assetPath);
        var dataDiffViewer = new DataDiffViewer(l, r, extension);
        await dataDiffViewer.Initialize();

        return dataDiffViewer;
    }

    private static List<string> SplitIntoChunks(string text, int maxLinesPerChunk = 20000)
    {
        var lines = text.Split('\n');
        var chunks = new List<string>();

        for (int i = 0; i < lines.Length; i += maxLinesPerChunk)
        {
            var chunkLines = lines.Skip(i).Take(maxLinesPerChunk);
            chunks.Add(string.Join("\n", chunkLines));
        }
        return chunks;
    }

    private (List<string> left, List<string> right) GetExtractedTextsForDiff(string assetPath)
    {
        GameFile entry1 = null, entry2 = null;
        DiffProvider?.Files?.TryGetValue(assetPath, out entry1);
        Provider.Files?.TryGetValue(assetPath, out entry2);

        List<string> left = entry1 != null ? SplitIntoChunks(ExtractTextForDiff(DiffProvider, entry1)) : [];
        List<string> right = entry2 != null ? SplitIntoChunks(ExtractTextForDiff(Provider, entry2)) : [];

        return (left, right);
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
}
