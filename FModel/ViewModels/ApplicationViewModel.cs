using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.VirtualFileSystem;
using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels.Commands;
using FModel.Views;
using FModel.Views.Resources.Controls;
using Serilog;

namespace FModel.ViewModels;

public class ApplicationViewModel : ViewModel
{
    private EBuildKind _build;
    public EBuildKind Build
    {
        get => _build;
        private init
        {
            SetProperty(ref _build, value);
            RaisePropertyChanged(nameof(TitleExtra));
        }
    }

    private FStatus _status;
    public FStatus Status
    {
        get => _status;
        private init => SetProperty(ref _status, value);
    }

    public IEnumerable<EAssetCategory> Categories { get; } = AssetCategoryExtensions.GetBaseCategories();

    private bool _isAssetsExplorerVisible;
    public bool IsAssetsExplorerVisible
    {
        get => _isAssetsExplorerVisible;
        set
        {
            if (value && !UserSettings.Default.FeaturePreviewNewAssetExplorer)
                return;

            SetProperty(ref _isAssetsExplorerVisible, value);
        }
    }

    private int _selectedLeftTabIndex;
    public int SelectedLeftTabIndex
    {
        get => _selectedLeftTabIndex;
        set
        {
            if (value is < 0 or > 2)
                return;
            SetProperty(ref _selectedLeftTabIndex, value);
        }
    }

    public RightClickMenuCommand RightClickMenuCommand => _rightClickMenuCommand ??= new RightClickMenuCommand(this);
    private RightClickMenuCommand _rightClickMenuCommand;
    public MenuCommand MenuCommand => _menuCommand ??= new MenuCommand(this);
    private MenuCommand _menuCommand;
    public CopyCommand CopyCommand => _copyCommand ??= new CopyCommand(this);
    private CopyCommand _copyCommand;

    public string InitialWindowTitle => $"FModel ({Constants.APP_SHORT_COMMIT_ID} - {Constants.APP_BUILD_DATE:MMM d, yyyy})";
    public string GameDisplayName => CUE4Parse?.Provider.GameDisplayName ?? "Unknown";
    public string TitleExtra => UserSettings.Default.CurrentDir is { } dir
        ? $"({dir.UeVersion}){(Build != EBuildKind.Release ? $" ({Build})" : "")}"
        : Build != EBuildKind.Release ? $"({Build})" : string.Empty;

    public LoadingModesViewModel LoadingModes { get; }
    public CustomDirectoriesViewModel? CustomDirectories { get; private set; }
    public CUE4ParseViewModel? CUE4Parse { get; private set; }
    public SettingsViewModel? SettingsView { get; private set; }
    public AesManagerViewModel? AesManager { get; private set; }
    public AudioPlayerViewModel? AudioPlayer { get; private set; }

    public ApplicationViewModel()
    {
        Status = new FStatus();
#if DEBUG
        Build = EBuildKind.Debug;
#elif RELEASE
        Build = EBuildKind.Release;
#else
        Build = EBuildKind.Unknown;
#endif
        LoadingModes = new LoadingModesViewModel();

        // For existing installations, use the cached directory settings immediately.
        // For first-run (no settings), initialization is deferred to EnsureInitializedAsync()
        // which is called from MainWindow.OnLoaded after the window is visible.
        var gameDirectory = UserSettings.Default.GameDirectory;
        if (!string.IsNullOrEmpty(gameDirectory) &&
            UserSettings.Default.PerDirectory.TryGetValue(gameDirectory, out var currentDir))
        {
            UserSettings.Default.CurrentDir = currentDir;
            InitializeInternals();
        }
    }

    /// <summary>
    /// Handles the first-run case: shows the directory-selector dialog and then
    /// completes internal initialization. Should be called from MainWindow.OnLoaded
    /// when <see cref="CUE4Parse"/> is still null (i.e., no prior configuration exists).
    /// </summary>
    public async Task EnsureInitializedAsync(Window owner)
    {
        if (CUE4Parse != null)
            return; // Already initialized synchronously in constructor.

        var dir = await AvoidEmptyGameDirectoryAsync(false, owner);
        if (dir is null)
        {
            Environment.Exit(0);
            return;
        }

        UserSettings.Default.CurrentDir = dir;
        InitializeInternals();
    }

    private void InitializeInternals()
    {
        CUE4Parse = new CUE4ParseViewModel();
        CUE4Parse.Provider.VfsRegistered += (sender, count) =>
        {
            if (sender is not IAesVfsReader reader)
                return;
            Status.UpdateStatusLabel($"{count} Archives ({reader.Name})", "Registered");
            CUE4Parse.GameDirectory.Add(reader);
        };
        CUE4Parse.Provider.VfsMounted += (sender, count) =>
        {
            if (sender is not IAesVfsReader reader)
                return;
            Status.UpdateStatusLabel($"{count:N0} Packages ({reader.Name})", "Mounted");
            CUE4Parse.GameDirectory.Verify(reader);
        };
        CUE4Parse.Provider.VfsUnmounted += (sender, _) =>
        {
            if (sender is not IAesVfsReader reader)
                return;
            CUE4Parse.GameDirectory.Disable(reader);
        };

        CustomDirectories = new CustomDirectoriesViewModel();
        SettingsView = new SettingsViewModel();
        AesManager = new AesManagerViewModel(CUE4Parse);
        AudioPlayer = new AudioPlayerViewModel();

        Status.SetStatus(EStatusKind.Ready);
    }

    public async Task<DirectorySettings?> AvoidEmptyGameDirectoryAsync(bool bAlreadyLaunched, Window? owner)
    {
        var gameDirectory = UserSettings.Default.GameDirectory;
        if (!bAlreadyLaunched && UserSettings.Default.PerDirectory.TryGetValue(gameDirectory, out var currentDir))
            return currentDir;

        var gameLauncherViewModel = new GameSelectorViewModel(gameDirectory);
        var selector = new DirectorySelector(gameLauncherViewModel);

        // Fall back to MainWindow when no explicit owner is provided.
        owner ??= (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null)
            return null;

        var ok = await selector.ShowDialog<bool?>(owner);
        if (ok != true)
            return null;

        UserSettings.Default.GameDirectory = gameLauncherViewModel.SelectedDirectory.GameDirectory;
        if (!bAlreadyLaunched || UserSettings.Default.CurrentDir?.Equals(gameLauncherViewModel.SelectedDirectory) == true)
            return gameLauncherViewModel.SelectedDirectory;

        // UserSettings.Save(); // ??? change key then change game, key saved correctly what?
        UserSettings.Default.CurrentDir = gameLauncherViewModel.SelectedDirectory;
        await RestartWithWarningAsync();
        return null;
    }

    public Task RestartWithWarningAsync() => RestartWithWarningAsync(null);

    public async Task RestartWithWarningAsync(Window? owner)
    {
        Log.Information("FModel will restart to apply your changes.");

        var okButton = new Avalonia.Controls.Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        var dialog = new Window
        {
            Title = "Uh oh, a restart is needed",
            Width = 420,
            Height = 160,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Avalonia.Controls.StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new Avalonia.Controls.TextBlock
                    {
                        Text = "It looks like you just changed something.\nFModel will restart to apply your changes.",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    okButton
                }
            }
        };

        okButton.Click += (_, _) => dialog.Close();

        owner ??= (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            // No owner available — show non-modal and wait for it to close.
            var tcs = new TaskCompletionSource();
            dialog.Closed += (_, _) => tcs.TrySetResult();
            dialog.Show();
            await tcs.Task;
        }

        Restart();
    }

    public void Restart()
    {
        var path = Path.GetFullPath(Environment.GetCommandLineArgs()[0]);
        if (path.EndsWith(".dll"))
        {
            new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true
                }
            }.Start();
        }
        else if (path.EndsWith(".exe"))
        {
            new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    CreateNoWindow = true
                }
            }.Start();
        }

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
        else
            Environment.Exit(0);
    }

    public async Task UpdateProvider(bool isLaunch)
    {
        if (AesManager is null || CUE4Parse is null)
            return;

        if (!isLaunch && !AesManager.HasChange)
            return;

        CUE4Parse.ClearProvider();
        await ApplicationService.ThreadWorkerView.Begin(cancellationToken =>
        {
            // TODO: refactor after release, select updated keys only
            var aes = AesManager.AesKeys.Select(x =>
            {
                cancellationToken.ThrowIfCancellationRequested(); // cancel if needed

                var k = x.Key.Trim();
                if (k.Length != 66)
                    k = Constants.ZERO_64_CHAR;
                return new KeyValuePair<FGuid, FAesKey>(x.Guid, new FAesKey(k));
            });

            CUE4Parse.LoadVfs(aes);
            AesManager.SetAesKeys();
        });
        RaisePropertyChanged(nameof(GameDisplayName));
    }

    public static async Task InitVgmStream()
    {
        var vgmZipFilePath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", "vgmstream-win.zip");
        var vgmFileInfo = new FileInfo(vgmZipFilePath);

        if (!vgmFileInfo.Exists || vgmFileInfo.LastWriteTimeUtc < DateTime.UtcNow.AddMonths(-4))
        {
            await ApplicationService.ApiEndpointView.DownloadFileAsync("https://github.com/vgmstream/vgmstream/releases/latest/download/vgmstream-win.zip", vgmZipFilePath);
            vgmFileInfo.Refresh();

            if (vgmFileInfo.Length > 0)
            {
                var zipDir = Path.GetDirectoryName(vgmZipFilePath)!;
                await using var zipFs = File.OpenRead(vgmZipFilePath);
                using var zip = new ZipArchive(zipFs, ZipArchiveMode.Read);

                foreach (var entry in zip.Entries)
                {
                    var entryPath = Path.Combine(zipDir, entry.FullName);
                    await using var entryFs = File.Create(entryPath);
                    await using var entryStream = entry.Open();
                    await entryStream.CopyToAsync(entryFs);
                }
            }
            else
            {
                FLogger.Append(ELog.Error, () => FLogger.Text("Could not download VgmStream", Constants.WHITE, true));
            }
        }
    }

    public static async Task InitImGuiSettings(bool forceDownload)
    {
        const string imgui = "imgui.ini";
        var imguiPath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", imgui);

        if (File.Exists(imgui))
            File.Move(imgui, imguiPath, true);
        if (File.Exists(imguiPath) && !forceDownload)
            return;

        await ApplicationService.ApiEndpointView.DownloadFileAsync($"https://cdn.fmodel.app/d/configurations/{imgui}", imguiPath);
        if (new FileInfo(imguiPath).Length == 0)
        {
            FLogger.Append(ELog.Error, () => FLogger.Text("Could not download ImGui settings", Constants.WHITE, true));
        }
    }

    public static async Task InitOodle()
    {
        var oodlePath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", OodleHelper.OODLE_NAME_OLD);
        if (!File.Exists(oodlePath))
        {
            oodlePath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", OodleHelper.OODLE_NAME_CURRENT);
        }

        OodleHelper.Initialize(oodlePath);
        if (OodleHelper.Instance is null)
            FLogger.Append(ELog.Error, () => FLogger.Text("Failed to download Oodle", Constants.WHITE, true));
    }

    public static async Task InitZlib()
    {
        var zlibPath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", ZlibHelper.DLL_NAME);
        var zlibFileInfo = new FileInfo(zlibPath);

        if (!zlibFileInfo.Exists || zlibFileInfo.LastWriteTimeUtc < DateTime.UtcNow.AddMonths(-4))
        {
            if (!await ZlibHelper.DownloadDllAsync(zlibPath))
            {
                FLogger.Append(ELog.Error, () => FLogger.Text("Failed to download Zlib-ng", Constants.WHITE, true));
                if (!zlibFileInfo.Exists)
                    return;
            }
        }

        ZlibHelper.Initialize(zlibPath);
    }

    public static async Task InitDetex()
    {
        var detexPath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", DetexHelper.DLL_NAME);
        if (File.Exists(DetexHelper.DLL_NAME))
        {
            File.Move(DetexHelper.DLL_NAME, detexPath, true);
        }
        else if (!File.Exists(detexPath))
        {
            await DetexHelper.LoadDllAsync(detexPath);
        }

        DetexHelper.Initialize(detexPath);
    }
}
