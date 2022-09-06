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
        private set
        {
            SetProperty(ref _build, value);
            RaisePropertyChanged(nameof(TitleExtra));
        }
    }

    private bool _isReady;

    public bool IsReady
    {
        get => _isReady;
        private set => SetProperty(ref _isReady, value);
    }

    private EStatusKind _status;

    public EStatusKind Status
    {
        get => _status;
        set
        {
            SetProperty(ref _status, value);
            IsReady = Status != EStatusKind.Loading && Status != EStatusKind.Stopping;
        }
    }

    public RightClickMenuCommand RightClickMenuCommand => _rightClickMenuCommand ??= new RightClickMenuCommand(this);
    private RightClickMenuCommand _rightClickMenuCommand;
    public MenuCommand MenuCommand => _menuCommand ??= new MenuCommand(this);
    private MenuCommand _menuCommand;
    public CopyCommand CopyCommand => _copyCommand ??= new CopyCommand(this);
    private CopyCommand _copyCommand;

    public string TitleExtra =>
        $"{UserSettings.Default.UpdateMode} - {CUE4Parse.Game.GetDescription()} (" + // FModel {UpdateMode} - {FGame} ({UE}) ({Build})
        $"{(CUE4Parse.Game == FGame.Unknown && UserSettings.Default.ManualGames.TryGetValue(UserSettings.Default.GameDirectory, out var settings) ? settings.OverridedGame : UserSettings.Default.OverridedGame[CUE4Parse.Game])})" +
        $"{(Build != EBuildKind.Release ? $" ({Build})" : "")}";

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
        Status = EStatusKind.Loading;
#if DEBUG
        Build = EBuildKind.Debug;
#elif RELEASE
        Build = EBuildKind.Release;
#else
        Build = EBuildKind.Unknown;
#endif
        LoadingModes = new LoadingModesViewModel();

        AvoidEmptyGameDirectoryAndSetEGame(false);
        CUE4Parse = new CUE4ParseViewModel(UserSettings.Default.GameDirectory);
        CustomDirectories = new CustomDirectoriesViewModel(CUE4Parse.Game, UserSettings.Default.GameDirectory);
        SettingsView = new SettingsViewModel(CUE4Parse.Game);
        AesManager = new AesManagerViewModel(CUE4Parse);
        MapViewer = new MapViewerViewModel(CUE4Parse);
        AudioPlayer = new AudioPlayerViewModel();
        Status = EStatusKind.Ready;
    }

    public void AvoidEmptyGameDirectoryAndSetEGame(bool bAlreadyLaunched)
    {
        var gameDirectory = UserSettings.Default.GameDirectory;
        if (!string.IsNullOrEmpty(gameDirectory) && !bAlreadyLaunched) return;

        var gameLauncherViewModel = new GameSelectorViewModel(gameDirectory);
        var result = new DirectorySelector(gameLauncherViewModel).ShowDialog();
        if (!result.HasValue || !result.Value) return;

        UserSettings.Default.GameDirectory = gameLauncherViewModel.SelectedDetectedGame.GameDirectory;
        if (!bAlreadyLaunched || gameDirectory == gameLauncherViewModel.SelectedDetectedGame.GameDirectory) return;

        RestartWithWarning();
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
            FLogger.AppendError();
            FLogger.AppendText("Could not download VgmStream", Constants.WHITE, true);
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
}