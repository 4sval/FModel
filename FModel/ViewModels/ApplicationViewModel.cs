using FModel.Extensions;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using FModel.ViewModels.Commands;
using FModel.Views;
using FModel.Views.Resources.Controls;
using Ionic.Zip;
using Oodle.NET;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using OodleCUE4 = CUE4Parse.Compression.Oodle;

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

    public RightClickMenuCommand RightClickMenuCommand => _rightClickMenuCommand ??= new RightClickMenuCommand(this);
    private RightClickMenuCommand _rightClickMenuCommand;
    public MenuCommand MenuCommand => _menuCommand ??= new MenuCommand(this);
    private MenuCommand _menuCommand;
    public CopyCommand CopyCommand => _copyCommand ??= new CopyCommand(this);
    private CopyCommand _copyCommand;

    public string InitialWindowTitle => $"FModel {UserSettings.Default.UpdateMode}";
    public string GameDisplayName => CUE4Parse.Provider.GameDisplayName ?? "Unknown";
    public string TitleExtra => $"({UserSettings.Default.CurrentDir.UeVersion}){(Build != EBuildKind.Release ? $" ({Build})" : "")}";

    public LoadingModesViewModel LoadingModes { get; }
    public CustomDirectoriesViewModel CustomDirectories { get; }
    public CUE4ParseViewModel CUE4Parse { get; }
    public SettingsViewModel SettingsView { get; }
    public AesManagerViewModel AesManager { get; }
    public AudioPlayerViewModel AudioPlayer { get; }
    public MapViewerViewModel MapViewer { get; }
    private OodleCompressor _oodle;

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

        UserSettings.Default.CurrentDir = AvoidEmptyGameDirectory(false);
        if (UserSettings.Default.CurrentDir is null)
        {
            //If no game is selected, many things will break before a shutdown request is processed in the normal way.
            //A hard exit is preferable to an unhandled expection in this case
            Environment.Exit(0);
        }

        CUE4Parse = new CUE4ParseViewModel();
        CustomDirectories = new CustomDirectoriesViewModel();
        SettingsView = new SettingsViewModel(CUE4Parse.Game);
        AesManager = new AesManagerViewModel(CUE4Parse);
        MapViewer = new MapViewerViewModel(CUE4Parse);
        AudioPlayer = new AudioPlayerViewModel();

        Status.SetStatus(EStatusKind.Ready);
    }

    public DirectorySettings AvoidEmptyGameDirectory(bool bAlreadyLaunched)
    {
        var gameDirectory = UserSettings.Default.GameDirectory;
        if (!bAlreadyLaunched && UserSettings.Default.PerDirectory.TryGetValue(gameDirectory, out var currentDir))
            return currentDir;

        var gameLauncherViewModel = new GameSelectorViewModel(gameDirectory);
        var result = new DirectorySelector(gameLauncherViewModel).ShowDialog();
        if (!result.HasValue || !result.Value) return null;

        UserSettings.Default.GameDirectory = gameLauncherViewModel.SelectedDirectory.GameDirectory;
        if (!bAlreadyLaunched || UserSettings.Default.CurrentDir.Equals(gameLauncherViewModel.SelectedDirectory))
            return gameLauncherViewModel.SelectedDirectory;

        UserSettings.Default.CurrentDir = gameLauncherViewModel.SelectedDirectory;
        RestartWithWarning();
        return null;
    }

    public void RestartWithWarning()
    {
        MessageBox.Show("It looks like you just changed something.\nFModel will restart to apply your changes.", "Uh oh, a restart is needed", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    Arguments = $"\"{Path.GetFullPath(Environment.GetCommandLineArgs()[0])}\"",
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

        Application.Current.Shutdown();
    }

    public async Task UpdateProvider(bool isLaunch)
    {
        if (!isLaunch && !AesManager.HasChange) return;

        CUE4Parse.ClearProvider();
        await ApplicationService.ThreadWorkerView.Begin(cancellationToken =>
        {
            CUE4Parse.LoadVfs(cancellationToken, AesManager.AesKeys);
            CUE4Parse.Provider.LoadIniConfigs();
            AesManager.SetAesKeys();
        });
        RaisePropertyChanged(nameof(GameDisplayName));
    }

    public async Task InitVgmStream()
    {
        var vgmZipFilePath = Path.Combine(UserSettings.Default.OutputDirectory, ".data", "vgmstream-win.zip");
        if (File.Exists(vgmZipFilePath)) return;

        await ApplicationService.ApiEndpointView.DownloadFileAsync("https://github.com/vgmstream/vgmstream/releases/latest/download/vgmstream-win.zip", vgmZipFilePath);
        if (new FileInfo(vgmZipFilePath).Length > 0)
        {
            var zip = ZipFile.Read(vgmZipFilePath);
            var zipDir = vgmZipFilePath.SubstringBeforeLast("\\");
            foreach (var e in zip) e.Extract(zipDir, ExtractExistingFileAction.OverwriteSilently);
        }
        else
        {
            FLogger.Append(ELog.Error, () => FLogger.Text("Could not download VgmStream", Constants.WHITE, true));
        }
    }

    public async Task InitOodle()
    {
        var dataDir = Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, ".data"));
        var oodlePath = Path.Combine(dataDir.FullName, OodleCUE4.OODLE_DLL_NAME);

        if (File.Exists(OodleCUE4.OODLE_DLL_NAME))
        {
            File.Move(OodleCUE4.OODLE_DLL_NAME, oodlePath, true);
        }
        else if (!File.Exists(oodlePath))
        {
            var result = await OodleCUE4.DownloadOodleDll(oodlePath);
            if (!result) return;
        }

        if (File.Exists("oo2core_8_win64.dll"))
            File.Delete("oo2core_8_win64.dll");

        _oodle = new OodleCompressor(oodlePath);

        unsafe
        {
            OodleCUE4.DecompressFunc = (bufferPtr, bufferSize, outputPtr, outputSize, a, b, c, d, e, f, g, h, i, threadModule) =>
                _oodle.Decompress(new IntPtr(bufferPtr), bufferSize, new IntPtr(outputPtr), outputSize,
                    (OodleLZ_FuzzSafe) a, (OodleLZ_CheckCRC) b, (OodleLZ_Verbosity) c, d, e, f, g, h, i, (OodleLZ_Decode_ThreadPhase) threadModule);
        }
    }

    public async Task InitImGuiSettings(bool forceDownload)
    {
        var imgui = Path.Combine(/*UserSettings.Default.OutputDirectory, ".data", */"imgui.ini");
        if (File.Exists(imgui) && !forceDownload) return;

        await ApplicationService.ApiEndpointView.DownloadFileAsync("https://cdn.fmodel.app/d/configurations/imgui.ini", imgui);
        if (new FileInfo(imgui).Length == 0)
        {
            FLogger.Append(ELog.Error, () => FLogger.Text("Could not download ImGui settings", Constants.WHITE, true));
        }
    }
}
