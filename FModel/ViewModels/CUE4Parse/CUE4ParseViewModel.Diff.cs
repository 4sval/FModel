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
    const string DiffTabId = "Diff";

    public async Task ShowAssetDiff(string assetPath)
    {
        var leftFile = TryGetFileByPathOrName(DiffProvider?.Files, assetPath);
        var rightFile = TryGetFileByPathOrName(Provider?.Files, assetPath);

        var leftImage = LoadTabImageForDiff(DiffProvider, leftFile);
        var rightImage = LoadTabImageForDiff(Provider, rightFile);

        var titleExtra = Path.GetFileName(assetPath);
        string extension = Path.GetExtension(assetPath).TrimStart('.');

        var existingTab = TabControl.TabsItems.FirstOrDefault(tab => tab.ParentExportType == DiffTabId);

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
                var tab = new TabItem(new FakeGameFile("Diff Viewer"), DiffTabId)
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

        bool isBlueprint = UserSettings.Default.ShowDecompileOption && (IsBlueprintPackage(DiffProvider, rightFile) && IsBlueprintPackage(Provider, leftFile));

        var (l, r) = GetExtractedTextsForDiff(leftFile, rightFile, isBlueprint);
        if (AreTextsEqual(l, r))
        {
            return new SameDataMessage();
        }

        if (isBlueprint)
            extension = "cpp";

        var dataDiffViewer = new DataDiffViewer(l, r, extension);
        await dataDiffViewer.Initialize();

        return dataDiffViewer;
    }

    private (List<string> left, List<string> right) GetExtractedTextsForDiff(GameFile leftFile, GameFile rightFile, bool isBlueprint)
    {
        List<string> left = leftFile != null ? SplitIntoChunks(ExtractTextForDiff(DiffProvider, leftFile, isBlueprint)) : [];
        List<string> right = rightFile != null ? SplitIntoChunks(ExtractTextForDiff(Provider, rightFile, isBlueprint)) : [];

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

    private string ExtractTextForDiff(AbstractVfsFileProvider provider, GameFile entry, bool isBlueprint)
    {
        if (isBlueprint)
            return Decompile(entry, false);

        if (TryExtractStructuredText(entry, provider, out var result))
            return result;

        return $"[No diffable text for type: {entry.Extension}]";
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

    private static bool IsBlueprintPackage(AbstractVfsFileProvider provider, GameFile entry)
    {
        try
        {
            var pkg = provider.LoadPackage(entry);
            foreach (var export in pkg.GetExports())
            {
                var className = export.Class?.Name;
                if (className != null)
                {
                    if (className.Contains("Blueprint", StringComparison.OrdinalIgnoreCase) ||
                        className.Contains("GeneratedClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch { } // Do nothing

        return false;
    }

    private static bool AreTextsEqual(List<string> leftChunks, List<string> rightChunks)
    {
        return ComputeHash(leftChunks).SequenceEqual(ComputeHash(rightChunks));
    }

    private static byte[] ComputeHash(List<string> chunks)
    {
        if (chunks == null || chunks.Count == 0)
            return [];
        var combined = string.Concat(chunks);
        return SHA256.HashData(Encoding.UTF8.GetBytes(combined));
    }

    private static List<string> SplitIntoChunks(string text, int maxLines = 100_000)
    {
        return [.. text.Split('\n')
                   .Select((line, index) => new { line, index })
                   .GroupBy(x => x.index / maxLines)
                   .Select(g => string.Join("\n", g.Select(x => x.line)))];
    }
}
