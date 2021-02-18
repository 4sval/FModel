using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using EpicManifestParser.Objects;
using FModel.Grabber.Manifests;
using FModel.Logger;
using FModel.PakReader.IO;
using FModel.PakReader.Pak;
using FModel.Utils;
using FModel.ViewModels.MenuItem;
using FModel.Windows.Launcher;

namespace FModel.Grabber.Paks
{
    static class PaksGrabber
    {
        private static readonly Regex _pakFileRegex = new Regex(@"FortniteGame(/|\\)Content(/|\\)Paks(/|\\)(pakchunk(?:0|10.*|\w+)-WindowsClient|global)\.(pak|ucas)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static async Task PopulateMenu()
        {
            await Application.Current.Dispatcher.InvokeAsync(delegate
            {
                PopulateBase();
            });

            await Task.Run(async () =>
            {
                if (string.IsNullOrEmpty(Properties.Settings.Default.PakPath))
                {
                    await Application.Current.Dispatcher.InvokeAsync(delegate
                    {
                        var launcher = new FLauncher();
                        bool? result = launcher.ShowDialog();
                        if (result.HasValue && result.Value)
                        {
                            Properties.Settings.Default.PakPath = launcher.Path;
                            Properties.Settings.Default.Save();
                        }
                    });
                }

                // Add Pak Files
                if (Properties.Settings.Default.PakPath.EndsWith("-fn.manifest"))
                {
                    ManifestInfo manifestInfo = await ManifestGrabber.TryGetLatestManifestInfo().ConfigureAwait(false);

                    if (manifestInfo == null)
                    {
                        throw new Exception("Failed to load latest manifest.");
                    }

                    DirectoryInfo chunksDir = Directory.CreateDirectory(Path.Combine(Properties.Settings.Default.OutputPath, "PakChunks"));
                    string manifestPath = Path.Combine(chunksDir.FullName, manifestInfo.Filename);
                    byte[] manifestData;

                    if (File.Exists(manifestPath))
                    {
                        manifestData = await File.ReadAllBytesAsync(manifestPath);
                    }
                    else
                    {
                        manifestData = await manifestInfo.DownloadManifestDataAsync().ConfigureAwait(false);
                        await File.WriteAllBytesAsync(manifestPath, manifestData).ConfigureAwait(false);
                    }

                    Manifest manifest = new Manifest(manifestData, new ManifestOptions
                    {
                        ChunkBaseUri = new Uri("http://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/ChunksV3/", UriKind.Absolute),
                        ChunkCacheDirectory = Directory.CreateDirectory(Path.Combine(Properties.Settings.Default.OutputPath, "PakChunks"))
                    });
                    int pakFiles = 0;

                    foreach (FileManifest fileManifest in manifest.FileManifests)
                    {
                        if (!_pakFileRegex.IsMatch(fileManifest.Name))
                        {
                            continue;
                        }

                        var pakStream = fileManifest.GetStream();
                        if (pakStream.Length == 365) continue;

                        var pakFileName = fileManifest.Name.Replace('/', '\\');
                        if (pakFileName.EndsWith(".pak"))
                        {
                            PakFileReader pakFile = new PakFileReader(pakFileName, pakStream);
                            if (pakFiles++ == 0)
                            {
                                // define the current game thank to the pak path
                                Folders.SetGameName(pakFileName);

                                Globals.Game.Version = pakFile.Info.Version;
                                Globals.Game.SubVersion = pakFile.Info.SubVersion;
                            }

                            await Application.Current.Dispatcher.InvokeAsync(delegate
                            {
                                MenuItems.pakFiles.Add(new PakMenuItemViewModel
                                {
                                    PakFile = pakFile,
                                    IsEnabled = false
                                });
                            });
                        }
                        else if (pakFileName.EndsWith(".ucas"))
                        {
                            var utocStream = manifest.FileManifests.FirstOrDefault(x => x.Name.Equals(fileManifest.Name.Replace(".ucas", ".utoc")));
                            var ioStore = new FFileIoStoreReader(pakFileName.SubstringAfterLast('\\'), pakFileName.SubstringBeforeLast('\\'), utocStream.GetStream(), pakStream);
                            await Application.Current.Dispatcher.InvokeAsync(delegate
                            {
                                MenuItems.pakFiles.Add(new PakMenuItemViewModel
                                {
                                    IoStore = ioStore,
                                    IsEnabled = false
                                });
                            });
                        }
                    }
                }
                else if (Properties.Settings.Default.PakPath.EndsWith("-val.manifest"))
                {
                    //var manifest = await ValorantAPIManifestV1.DownloadAndParse(Directory.CreateDirectory(Path.Combine(Properties.Settings.Default.OutputPath, "PakChunks"))).ConfigureAwait(false);
                    var manifest = await ValorantAPIManifestV2.DownloadAndParse(Directory.CreateDirectory(Path.Combine(Properties.Settings.Default.OutputPath, "PakChunks"))).ConfigureAwait(false);

                    if (manifest == null)
                    {
                        throw new Exception("Failed to load latest manifest.");
                    }

                    for (var i = 0; i < manifest.Paks.Length; i++)
                    {
                        var pak = manifest.Paks[i];
                        var pakFileName = @$"ShooterGame\Content\Paks\{pak.Name}";
                        var pakFile = new PakFileReader(pakFileName, manifest.GetPakStream(i));

                        if (i == 0)
                        {
                            Folders.SetGame(EGame.Valorant);
                            Globals.Game.Version = pakFile.Info.Version;
                            Globals.Game.SubVersion = pakFile.Info.SubVersion;
                        }

                        await Application.Current.Dispatcher.InvokeAsync(delegate
                        {
                            MenuItems.pakFiles.Add(new PakMenuItemViewModel
                            {
                                PakFile = pakFile,
                                IsEnabled = false
                            });
                        });
                    }
                }
                else if (Directory.Exists(Properties.Settings.Default.PakPath))
                {
                    // define the current game thank to the pak path
                    Folders.SetGameName(Properties.Settings.Default.PakPath);

                    // paks
                    string[] paks = Directory.GetFiles(Properties.Settings.Default.PakPath);
                    for (int i = 0; i < paks.Length; i++)
                    {
                        var pakInfo = new FileInfo(paks[i]);
                        if (pakInfo.Length == 365)
                        {
                            continue;
                        }

                        if (!Utils.Paks.IsFileReadLocked(pakInfo))
                        {
                            if (paks[i].EndsWith(".pak"))
                            {
                                PakFileReader pakFile = new PakFileReader(paks[i]);
                                DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[PAK]", "[Registering]", $"{pakFile.FileName} with GUID {pakFile.Info.EncryptionKeyGuid.Hex}");
                                if (i == 0)
                                {
                                    Globals.Game.Version = pakFile.Info.Version;
                                    Globals.Game.SubVersion = pakFile.Info.SubVersion;
                                }

                                await Application.Current.Dispatcher.InvokeAsync(delegate
                                {
                                    MenuItems.pakFiles.Add(new PakMenuItemViewModel
                                    {
                                        PakFile = pakFile,
                                        IsEnabled = false
                                    });
                                });
                            }
                            else if (paks[i].EndsWith(".ucas"))
                            {
                                var utoc = paks[i].Replace(".ucas", ".utoc");
                                if (!Utils.Paks.IsFileReadLocked(new FileInfo(utoc)))
                                {
                                    var utocStream = new MemoryStream(await File.ReadAllBytesAsync(utoc));
                                    var ucasStream = File.OpenRead(paks[i]);
                                    var ioStore = new FFileIoStoreReader(paks[i].SubstringAfterLast('\\'), paks[i].SubstringBeforeLast('\\'), utocStream, ucasStream);
                                    DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[IO Store]", "[Registering]", $"{ioStore.FileName} with GUID {ioStore.TocResource.Header.EncryptionKeyGuid.Hex}");

                                    await Application.Current.Dispatcher.InvokeAsync(delegate
                                    {
                                        MenuItems.pakFiles.Add(new PakMenuItemViewModel
                                        {
                                            IoStore = ioStore,
                                            IsEnabled = false
                                        });
                                    });
                                }
                                else
                                {
                                    FConsole.AppendText(string.Format(Properties.Resources.PakFileLocked, Path.GetFileNameWithoutExtension(utoc)), FColors.Red, true);
                                    DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[IO Store]", "[Locked]", utoc);
                                }
                            }
                        }
                        else
                        {
                            FConsole.AppendText(string.Format(Properties.Resources.PakFileLocked, Path.GetFileNameWithoutExtension(paks[i])), FColors.Red, true);
                            DebugHelper.WriteLine("{0} {1} {2} {3}", "[FModel]", "[PAK]", "[Locked]", paks[i]);
                        }
                    }
                }
            });
        }

        private static void PopulateBase()
        {
            // Loading Mode
            PakMenuItemViewModel parent = new PakMenuItemViewModel
            {
                Header = $"{Properties.Resources.LoadingMode}   🠞   {Properties.Resources.Default}",
                Icon = new Image { Source = new BitmapImage(new Uri("Resources/progress-download.png", UriKind.Relative)) }
            };
            parent.Childrens = new ObservableCollection<PakMenuItemViewModel>
            {
                new PakMenuItemViewModel // Default Mode
                {
                    Header = Properties.Resources.Default,
                    Parent = parent,
                    IsCheckable = true,
                    IsChecked = true,
                    StaysOpenOnClick = true
                },
                new PakMenuItemViewModel
                {
                    Header = Properties.Resources.NewFiles,
                    Parent = parent,
                    IsCheckable = true,
                    StaysOpenOnClick = true
                },
                new PakMenuItemViewModel
                {
                    Header = Properties.Resources.ModifiedFiles,
                    Parent = parent,
                    IsCheckable = true,
                    StaysOpenOnClick = true
                },
                new PakMenuItemViewModel
                {
                    Header = Properties.Resources.NewModifiedFiles,
                    Parent = parent,
                    IsCheckable = true,
                    StaysOpenOnClick = true
                }
            };
            MenuItems.pakFiles.Add(parent);

            // Load All
            MenuItems.pakFiles.Add(new PakMenuItemViewModel
            {
                Header = Properties.Resources.LoadAll,
                Icon = new Image { Source = new BitmapImage(new Uri("Resources/folder-download.png", UriKind.Relative)) },
                IsEnabled = false
            });

            // Separator
            MenuItems.pakFiles.Add(new Separator { });
        }
    }
}