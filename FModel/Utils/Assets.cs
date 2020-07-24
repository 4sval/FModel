using PakReader.Pak;
using System;
using System.Linq;
using System.Collections.Generic;
using PakReader.Parsers.Class;
using FModel.ViewModels.ImageBox;
using Newtonsoft.Json;
using FModel.Logger;
using PakReader.Parsers.Objects;
using System.IO;
using FModel.ViewModels.AvalonEdit;
using PakReader;
using System.Threading.Tasks;
using FModel.ViewModels.StatusBar;
using System.Collections;
using FModel.ViewModels.ListBox;
using System.Diagnostics;
using FModel.Windows.SoundPlayer;
using System.Windows;
using FModel.Windows.CustomNotifier;
using FModel.ViewModels.Buttons;
using System.Threading;
using SkiaSharp;
using System.Text;
using FModel.ViewModels.DataGrid;
using FModel.PakReader;
using ICSharpCode.AvalonEdit.Highlighting;
using static FModel.Creator.Creator;

namespace FModel.Utils
{
    static class Assets
    {
        /// <summary>
        /// used to cache assets
        /// PakPackage to get the properties of the asset
        /// ArraySegment<byte>[] to export the raw data
        /// </summary>
        private static readonly Dictionary<FPakEntry, Dictionary<PakPackage, ArraySegment<byte>[]>> _CachedFiles = new Dictionary<FPakEntry, Dictionary<PakPackage, ArraySegment<byte>[]>>();
        private static Stopwatch _timer;

        public static void ClearCachedFiles() => _CachedFiles.Clear();

        /// <summary>
        /// USER SELECTION ARRAY WILL BREAK AT THE FIRST ERROR
        /// This won't happen for other type of extraction like in diff mode where we have to skip errors
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        public static async Task GetUserSelection(IList selection)
        {
            _timer = Stopwatch.StartNew();
            ImageBoxVm.imageBoxViewModel.Reset();
            AvalonEditVm.avalonEditViewModel.Reset();
            ExtractStopVm.stopViewModel.IsEnabled = true;
            ExtractStopVm.extractViewModel.IsEnabled = false;
            StatusBarVm.statusBarViewModel.Set(string.Empty, Properties.Resources.Loading);
            Tasks.TokenSource = new CancellationTokenSource();

            await Task.Run(() =>
            {
                foreach (var item in selection)
                {
                    if (Tasks.TokenSource.IsCancellationRequested)
                        throw new TaskCanceledException(Properties.Resources.Canceled);

                    Thread.Sleep(10); // this is actually useful because it smh unfreeze the ui so the user can cancel even tho it's a Task so...
                    if (item is ListBoxViewModel selected)
                    {
                        if (Globals.CachedPakFiles.TryGetValue(selected.PakEntry.PakFileName, out var r))
                        {
                            string mount = r.MountPoint;
                            string ext = selected.PakEntry.GetExtension();
                            switch (ext)
                            {
                                case ".ini":
                                case ".txt":
                                case ".bat":
                                case ".xml":
                                case ".h":
                                case ".uproject":
                                case ".uplugin":
                                case ".upluginmanifest":
                                case ".json":
                                    {
                                        IHighlightingDefinition syntax = ext switch
                                        {
                                            ".ini" => AvalonEditVm.IniHighlighter,
                                            ".txt" => AvalonEditVm.BaseHighlighter,
                                            ".bat" => AvalonEditVm.BaseHighlighter,
                                            ".xml" => AvalonEditVm.XmlHighlighter,
                                            ".h" => AvalonEditVm.CppHighlighter,
                                            _ => AvalonEditVm.JsonHighlighter
                                        };
                                        using var asset = GetMemoryStream(selected.PakEntry.PakFileName, mount + selected.PakEntry.GetPathWithoutExtension());
                                        asset.Position = 0;
                                        using var reader = new StreamReader(asset);
                                        AvalonEditVm.avalonEditViewModel.Set(reader.ReadToEnd(), mount + selected.PakEntry.Name, syntax);
                                        break;
                                    }
                                case ".locmeta":
                                    {
                                        using var asset = GetMemoryStream(selected.PakEntry.PakFileName, mount + selected.PakEntry.GetPathWithoutExtension());
                                        asset.Position = 0;
                                        AvalonEditVm.avalonEditViewModel.Set(JsonConvert.SerializeObject(new LocMetaReader(asset), Formatting.Indented), mount + selected.PakEntry.Name);
                                        break;
                                    }
                                case ".locres":
                                    {
                                        using var asset = GetMemoryStream(selected.PakEntry.PakFileName, mount + selected.PakEntry.GetPathWithoutExtension());
                                        asset.Position = 0;
                                        AvalonEditVm.avalonEditViewModel.Set(JsonConvert.SerializeObject(new LocResReader(asset).Entries, Formatting.Indented), mount + selected.PakEntry.Name);
                                        break;
                                    }
                                case ".udic":
                                    {
                                        using var asset = GetMemoryStream(selected.PakEntry.PakFileName, mount + selected.PakEntry.GetPathWithoutExtension());
                                        asset.Position = 0;
                                        AvalonEditVm.avalonEditViewModel.Set(JsonConvert.SerializeObject(new FOodleDictionaryArchive(asset).Header, Formatting.Indented), mount + selected.PakEntry.Name);
                                        break;
                                    }
                                case ".bin":
                                    {
                                        if (
                                        !selected.PakEntry.Name.Equals("FortniteGame/AssetRegistry.bin") && // this file is 85mb...
                                        selected.PakEntry.Name.Contains("AssetRegistry")) // only parse AssetRegistry (basically the ones in dynamic paks)
                                        {
                                            using var asset = GetMemoryStream(selected.PakEntry.PakFileName, mount + selected.PakEntry.GetPathWithoutExtension());
                                            asset.Position = 0;
                                            AvalonEditVm.avalonEditViewModel.Set(JsonConvert.SerializeObject(new FAssetRegistryState(asset), Formatting.Indented), mount + selected.PakEntry.Name);
                                        }
                                        break;
                                    }
                                case ".bnk":
                                case ".pck":
                                    {
                                        using var asset = GetMemoryStream(selected.PakEntry.PakFileName, mount + selected.PakEntry.GetPathWithoutExtension());
                                        asset.Position = 0;
                                        WwiseReader bnk = new WwiseReader(new BinaryReader(asset));
                                        Application.Current.Dispatcher.Invoke(delegate
                                        {
                                            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", $"Opening Audio Player for {selected.PakEntry.GetNameWithExtension()}");
                                            if (!FWindows.IsWindowOpen<Window>(Properties.Resources.AudioPlayer))
                                                new AudioPlayer().LoadFiles(bnk.AudioFiles, mount + selected.PakEntry.GetPathWithoutFile());
                                            else
                                                ((AudioPlayer)FWindows.GetOpenedWindow<Window>(Properties.Resources.AudioPlayer)).LoadFiles(bnk.AudioFiles, mount + selected.PakEntry.GetPathWithoutFile());
                                        });
                                        break;
                                    }
                                case ".png":
                                    {
                                        using var asset = GetMemoryStream(selected.PakEntry.PakFileName, mount + selected.PakEntry.GetPathWithoutExtension());
                                        asset.Position = 0;
                                        ImageBoxVm.imageBoxViewModel.Set(SKBitmap.Decode(asset), mount + selected.PakEntry.Name);
                                        break;
                                    }
                                case ".ushaderbytecode":
                                    break;
                                default:
                                    AvalonEditVm.avalonEditViewModel.Set(GetJsonProperties(selected.PakEntry, mount, true), mount + selected.PakEntry.Name);
                                    break;
                            }

                            if (Properties.Settings.Default.AutoExport)
                                Export(selected.PakEntry, true);
                        }
                    }
                }
            }).ContinueWith(t =>
            {
                _timer.Stop();
                ExtractStopVm.stopViewModel.IsEnabled = false;
                ExtractStopVm.extractViewModel.IsEnabled = true;

                if (t.Exception != null) Tasks.TaskCompleted(t.Exception);
                else StatusBarVm.statusBarViewModel.Set(string.Format(Properties.Resources.TimeElapsed, _timer.ElapsedMilliseconds), Properties.Resources.Success);
            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static MemoryStream GetMemoryStream(string pakName, string pathWithoutExtension)
        {
            if (Globals.CachedPakFiles.TryGetValue(pakName, out PakFileReader pak))
            {
                if (pak.Initialized && !pak.TryGetFile(pathWithoutExtension, out ArraySegment<byte> uasset, out _, out _))
                {
                    if (uasset != null)
                    {
                        return new MemoryStream(uasset.Array, uasset.Offset, uasset.Count);
                    }
                }
            }
            return null;
        }

        public static string GetJsonProperties(FPakEntry entry, string mount) => GetJsonProperties(entry, mount, false);
        public static string GetJsonProperties(FPakEntry entry, string mount, bool loadContent)
        {
            PakPackage p = GetPakPackage(entry, mount, loadContent);
            if (!p.Equals(default))
            {
                return p.JsonData;
            }
            return string.Empty;
        }

        public static PakPackage GetPakPackage(FPakEntry entry, string mount) => GetPakPackage(entry, mount, false);
        public static PakPackage GetPakPackage(FPakEntry entry, string mount, bool loadContent)
        {
            TryGetPakPackage(entry, mount, out var p);

            if (loadContent)
            {
                // Texture
                var i = p.GetExport<UTexture2D>();
                if (i != null)
                {
                    ImageBoxVm.imageBoxViewModel.Set(i.Image, entry.GetNameWithExtension());
                    return p;
                }

                // Sound
                var s = p.GetExport<USoundWave>();
                if (s != null && (s.AudioFormat.String.Equals("OGG") || s.AudioFormat.String.Equals("OGG10000-1-1-1-1-1")))
                {
                    string path = Properties.Settings.Default.OutputPath + "\\Sounds\\" + mount + entry.GetPathWithoutExtension() + ".ogg";
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    if (File.Exists(path))
                    {
                        if (!Paks.IsFileWriteLocked(new FileInfo(path))) // aka isn't already being played, rewrite it
                        {
                            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
                            using var writer = new BinaryWriter(stream);
                            writer.Write(s.Sound);
                            writer.Flush();
                        }
                    }
                    else
                    {
                        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
                        using var writer = new BinaryWriter(stream);
                        writer.Write(s.Sound);
                        writer.Flush();
                    }

                    if (Properties.Settings.Default.AutoOpenSounds)
                    {
                        Application.Current.Dispatcher.Invoke(delegate
                        {
                            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", $"Opening Audio Player for {entry.GetNameWithExtension()}");
                            if (!FWindows.IsWindowOpen<Window>(Properties.Resources.AudioPlayer))
                                new AudioPlayer().LoadFile(path);
                            else
                                ((AudioPlayer)FWindows.GetOpenedWindow<Window>(Properties.Resources.AudioPlayer)).LoadFile(path);
                        });
                    }
                    return p;
                }
                else if (s != null && (s.AudioFormat.String.Equals("OPUS") || s.AudioFormat.String.Equals("ADPCM")))
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", $"Opening Audio Player for {entry.GetNameWithExtension()}");
                        if (!FWindows.IsWindowOpen<Window>(Properties.Resources.AudioPlayer))
                            new AudioPlayer().LoadFiles(new Dictionary<string, byte[]>(1) {{ entry.GetNameWithoutExtension() + "." + s.AudioFormat.String.ToLowerInvariant(), s.Sound }}, entry.GetPathWithoutFile());
                        else
                            ((AudioPlayer)FWindows.GetOpenedWindow<Window>(Properties.Resources.AudioPlayer)).LoadFiles(new Dictionary<string, byte[]>(1) { { entry.GetNameWithoutExtension() + "." + s.AudioFormat.String.ToLowerInvariant(), s.Sound } }, entry.GetPathWithoutFile());
                    });
                }
                else if (s != null)
                {
                    string path = Properties.Settings.Default.OutputPath + "\\Sounds\\" + mount + entry.GetPathWithoutExtension() + "." + s.AudioFormat.String.ToLowerInvariant();
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
                    using var writer = new BinaryWriter(stream);
                    writer.Write(s.Sound);
                    writer.Flush();
                    return p;
                }

                // Image Creator
                if (TryDrawIcon(entry.Name, p.ExportTypes, p.Exports))
                    return p;
            }

            return p;
        }
        private static bool TryGetPakPackage(FPakEntry entry, string mount, out PakPackage package)
        {
            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[Assets]", "[Package]", $"Searching for '{mount + entry.Name}'s package");
            if (_CachedFiles.TryGetValue(entry, out var dict))
            {
                package = dict.ElementAt(0).Key;
                return true;
            }

            if (Globals.CachedPakFiles.TryGetValue(entry.PakFileName, out PakFileReader pak))
            {
                if (pak.Initialized && pak.TryGetFile(mount + entry.GetPathWithoutExtension(), out ArraySegment<byte> uasset, out ArraySegment<byte> uexp, out ArraySegment<byte> ubulk))
                {
                    package = new PakPackage(uasset, uexp, ubulk);
                    _CachedFiles[entry] = new Dictionary<PakPackage, ArraySegment<byte>[]>
                    {
                        [package] = new ArraySegment<byte>[] { uasset, uexp, ubulk }
                    };
                    return true;
                }
            }

            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[Assets]", "[Package]", $"No package found for '{mount + entry.Name}'");
            package = default;
            return false;
        }

        public static ArraySegment<byte>[] GetArraySegmentByte(FPakEntry entry, string mount)
        {
            TryGetArraySegmentByte(entry, mount, out var b);
            return b;
        }
        private static bool TryGetArraySegmentByte(FPakEntry entry, string mount, out ArraySegment<byte>[] arraySegment)
        {
            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[Assets]", "[ArraySegment]", $"Searching for '{mount + entry.Name}'s ArraySegment<byte>");
            if (_CachedFiles.TryGetValue(entry, out var dict))
            {
                arraySegment = dict.ElementAt(0).Value;
                return true;
            }

            if (Globals.CachedPakFiles.TryGetValue(entry.PakFileName, out PakFileReader pak))
            {
                if (pak.Initialized && pak.TryGetFile(mount + entry.GetPathWithoutExtension(), out ArraySegment<byte> uasset, out ArraySegment<byte> uexp, out ArraySegment<byte> ubulk))
                {
                    arraySegment = new ArraySegment<byte>[] { uasset, uexp, ubulk };
                    return true;
                }
            }

            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[Assets]", "[ArraySegment]", $"No ArraySegment<byte> found for '{mount + entry.Name}'");
            arraySegment = default;
            return false;
        }

        public static void Filter(string filter, string item, out bool bSearch)
        {
            if (filter.StartsWith("!="))
                bSearch = item.IndexOf(filter.Substring(2), StringComparison.CurrentCultureIgnoreCase) < 0;
            else if (filter.StartsWith("=="))
                bSearch = item.IndexOf(filter.Substring(2), StringComparison.CurrentCulture) >= 0;
            else
                bSearch = item.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        public static void Export(FPakEntry entry, bool autoSave)
        {
            switch (entry.GetExtension())
            {
                case ".uasset": // embedded data export
                case ".umap": // embedded data export
                    {
                        if (Globals.CachedPakFiles.TryGetValue(entry.PakFileName, out var r))
                        {
                            string mount = r.MountPoint;
                            if (TryGetArraySegmentByte(entry, mount, out var data))
                            {
                                string[] ext = string.Join(":", entry.GetExtension(), entry.Uexp?.GetExtension(), entry.Ubulk?.GetExtension()).Split(':');
                                for (int i = 0; i < data.Length; i++)
                                {
                                    if (data[i] == null)
                                        continue;

                                    string basePath = Properties.Settings.Default.OutputPath + "\\Exports\\" + mount.Substring(1);
                                    string fullPath = basePath + Path.ChangeExtension(entry.Name, ext[i]);
                                    string name = Path.GetFileName(fullPath);
                                    Directory.CreateDirectory(basePath + entry.GetPathWithoutFile());

                                    using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
                                    using var writer = new BinaryWriter(stream);
                                    writer.Write(data[i]);
                                    writer.Flush();

                                    if (File.Exists(fullPath))
                                    {
                                        DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Assets]", $"{name} successfully exported");
                                        if (autoSave)
                                            FConsole.AppendText(string.Format(Properties.Resources.DataExported, name), FColors.Green, true);
                                        else
                                            Globals.gNotifier.ShowCustomMessage(Properties.Resources.Success, string.Format(Properties.Resources.DataExported, name), string.Empty, fullPath);
                                    }
                                }
                            }
                        }
                        break;
                    }
                default: // single data export
                    {
                        if (Globals.CachedPakFiles.TryGetValue(entry.PakFileName, out var r))
                        {
                            string basePath = Properties.Settings.Default.OutputPath + "\\Exports\\" + r.MountPoint.Substring(1);
                            string fullPath = basePath + entry.Name;
                            string name = Path.GetFileName(fullPath);
                            Directory.CreateDirectory(basePath + entry.GetPathWithoutFile());

                            using var data = GetMemoryStream(entry.PakFileName, r.MountPoint + entry.GetPathWithoutExtension());
                            using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
                            data.WriteTo(stream);

                            if (File.Exists(fullPath))
                            {
                                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Assets]", $"{name} successfully exported");
                                if (autoSave)
                                    FConsole.AppendText(string.Format(Properties.Resources.DataExported, name), FColors.Green, true);
                                else
                                    Globals.gNotifier.ShowCustomMessage(Properties.Resources.Success, string.Format(Properties.Resources.DataExported, name, string.Empty, fullPath));
                            }
                        }
                        break;
                    }
            }
        }

        public static void Copy(IList entries, ECopy mode)
        {
            StringBuilder sb = new StringBuilder();
            if (entries[0] is ListBoxViewModel)
            {
                foreach (ListBoxViewModel selectedItem in entries)
                {
                    sb.AppendLine(Copy(selectedItem.PakEntry, mode));
                }
            }
            else if (entries[0] is DataGridViewModel)
            {
                foreach (DataGridViewModel selectedItem in entries)
                {
                    sb.AppendLine(Copy(selectedItem.Name, mode));
                }
            }
            Copy(sb.ToString().Trim());
        }
        public static string Copy(FPakEntry entry, ECopy mode)
        {
            if (Globals.CachedPakFiles.TryGetValue(entry.PakFileName, out var r))
            {
                string toCopy = r.MountPoint.Substring(1);
                if (mode == ECopy.Path)
                    toCopy += entry.Name;
                else if (mode == ECopy.PathNoExt)
                    toCopy += entry.GetPathWithoutExtension();
                else if (mode == ECopy.PathNoFile)
                    toCopy += entry.GetPathWithoutFile();
                else if (mode == ECopy.File)
                    toCopy = entry.GetNameWithExtension();
                else if (mode == ECopy.FileNoExt)
                    toCopy = entry.GetNameWithoutExtension();
                return toCopy;
            }
            return string.Empty;
        }
        public static string Copy(string fullPath, ECopy mode)
        {
            string toCopy = string.Empty;
            if (mode == ECopy.Path)
                toCopy = fullPath;
            else if (mode == ECopy.PathNoExt)
                toCopy = fullPath.Substring(0, fullPath.LastIndexOf("."));
            else if (mode == ECopy.PathNoFile)
                toCopy = fullPath.Substring(0, fullPath.LastIndexOf("/") + 1);
            else if (mode == ECopy.File)
                toCopy = fullPath.Substring(fullPath.LastIndexOf("/") + 1);
            else if (mode == ECopy.FileNoExt)
                toCopy = fullPath.Substring(fullPath.LastIndexOf("/") + 1, fullPath.LastIndexOf(".") - (fullPath.LastIndexOf("/") + 1));
            return toCopy;
        }
        public static void Copy(string toCopy)
        {
            Clipboard.SetText(toCopy);
            if (Clipboard.GetText().Equals(toCopy))
                Globals.gNotifier.ShowCustomMessage(Properties.Resources.Success, Properties.Resources.CopySuccess, "/FModel;component/Resources/check-circle.ico");
        }
    }
}
