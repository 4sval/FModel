using FModel.Logger;
using FModel.Utils;
using FModel.ViewModels.MenuItem;
using FModel.Windows.Launcher;
using PakReader.Pak;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using EpicManifestParser.Objects;

namespace FModel.Grabber.Paks
{
    static class PaksGrabber
    {
        private static readonly Regex _pakFileRegex = new Regex(@"^FortniteGame/Content/Paks/.+\.pak$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public static async Task PopulateMenu()
        {
            PopulateBase();

            await Task.Run(async () =>
            {
                if (string.IsNullOrEmpty(Properties.Settings.Default.PakPath))
                {
                    await Application.Current.Dispatcher.InvokeAsync(delegate
                    {
                        FLauncher launcher = new FLauncher();
                        bool? result = launcher.ShowDialog();
                        if (result.HasValue && result.Value)
                        {
                            Properties.Settings.Default.PakPath = launcher.Path;
                            Properties.Settings.Default.Save();
                        }
                    });
                }

                // Add Pak Files
                if (Properties.Settings.Default.PakPath.EndsWith(".manifest"))
                {
                    byte[] manifestBytes = await File.ReadAllBytesAsync(Properties.Settings.Default.PakPath);
                    Manifest manifest = new Manifest(manifestBytes, new ManifestOptions
                    {
                        ChunkBaseUri = new Uri("http://download.epicgames.com/Builds/Fortnite/CloudDir/ChunksV3/", UriKind.Absolute),
                        ChunkCacheDirectory = Directory.CreateDirectory(Path.Combine(Properties.Settings.Default.OutputPath, "PakChunks"))
                    });
                    int pakFiles = 0;

                    foreach (FileManifest fileManifest in manifest.FileManifests)
                    {
                        if (!_pakFileRegex.IsMatch(fileManifest.Name))
                        {
                            continue;
                        }

                        var pakFileName = fileManifest.Name.Replace('/', '\\');
                        PakFileReader pakFile = new PakFileReader(pakFileName, fileManifest.GetStream());

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
                }
                else if (Directory.Exists(Properties.Settings.Default.PakPath))
                {
                    // define the current game thank to the pak path
                    Folders.SetGameName(Properties.Settings.Default.PakPath);

                    string[] paks = Directory.GetFiles(Properties.Settings.Default.PakPath, "*.pak");
                    for (int i = 0; i < paks.Length; i++)
                    {
                        if (!Utils.Paks.IsFileReadLocked(new FileInfo(paks[i])))
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
