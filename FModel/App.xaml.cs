using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Win32;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using CUE4Parse;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using Newtonsoft.Json;

namespace FModel;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    [DllImport("kernel32.dll")]
    [SupportedOSPlatform("windows")]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("winbrand.dll", CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [SupportedOSPlatform("windows")]
    private static extern string BrandingFormatString(string format);

    public App()
    {
        InitializeComponent();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += AppExit;
        }

#if DEBUG
        if (OperatingSystem.IsWindows())
            AttachConsole(-1);
#endif

        try
        {
            UserSettings.Default = JsonConvert.DeserializeObject<UserSettings>(
                File.ReadAllText(UserSettings.FilePath), JsonNetSerializer.SerializerSettings);
        }
        catch
        {
            UserSettings.Default = new UserSettings();
        }

        var createMe = false;
        if (!Directory.Exists(UserSettings.Default.OutputDirectory))
        {
            var currentDir = AppContext.BaseDirectory;
            try
            {
                var outputDir = Directory.CreateDirectory(Path.Combine(currentDir, "Output"));
                using (File.Create(Path.Combine(outputDir.FullName, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose))
                {

                }

                UserSettings.Default.OutputDirectory = outputDir.FullName;
            }
            catch (UnauthorizedAccessException exception)
            {
                throw new Exception("FModel cannot create the output directory where it is currently located. Please move FModel to a different location.", exception);
            }
        }

        if (!Directory.Exists(UserSettings.Default.RawDataDirectory))
        {
            createMe = true;
            UserSettings.Default.RawDataDirectory = Path.Combine(UserSettings.Default.OutputDirectory, "Exports");
        }

        if (!Directory.Exists(UserSettings.Default.PropertiesDirectory))
        {
            createMe = true;
            UserSettings.Default.PropertiesDirectory = Path.Combine(UserSettings.Default.OutputDirectory, "Exports");
        }

        if (!Directory.Exists(UserSettings.Default.TextureDirectory))
        {
            createMe = true;
            UserSettings.Default.TextureDirectory = Path.Combine(UserSettings.Default.OutputDirectory, "Exports");
        }

        if (!Directory.Exists(UserSettings.Default.AudioDirectory))
        {
            createMe = true;
            UserSettings.Default.AudioDirectory = Path.Combine(UserSettings.Default.OutputDirectory, "Exports");
        }

        if (!Directory.Exists(UserSettings.Default.ModelDirectory))
        {
            createMe = true;
            UserSettings.Default.ModelDirectory = Path.Combine(UserSettings.Default.OutputDirectory, "Exports");
        }

        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FModel"));
        Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, "Backups"));
        if (createMe)
            Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, "Exports"));
        Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, "Logs"));
        Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, ".data"));

        const string template = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Enriched}: {Message:lj}{NewLine}{Exception}";
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .Enrich.With<SourceEnricher>()
            .MinimumLevel.Verbose()
            .WriteTo.Console(outputTemplate: template, theme: AnsiConsoleTheme.Literate)
            .WriteTo.File(outputTemplate: template,
                path: Path.Combine(UserSettings.Default.OutputDirectory, "Logs", $"FModel-Debug-Log-{DateTime.Now:yyyy-MM-dd}.log"))
#else
            .Enrich.With<CallerEnricher>()
            .WriteTo.File(outputTemplate: template,
                path: Path.Combine(UserSettings.Default.OutputDirectory, "Logs", $"FModel-Log-{DateTime.Now:yyyy-MM-dd}.log"))
#endif
            .CreateLogger();

        Log.Information("Version {Version} ({CommitId})", Constants.APP_VERSION, Constants.APP_COMMIT_ID);
        Log.Information("{OS}", GetOperatingSystemProductName());
        Log.Information("{RuntimeVer}", RuntimeInformation.FrameworkDescription);
        Log.Information("Culture {SysLang}", CultureInfo.CurrentCulture);

        // Subscribed after logger is initialised so Log.Error calls inside
        // the handler are always directed to the configured sinks.
        Dispatcher.UIThread.UnhandledExceptionFilter += (_, e) =>
        {
            Log.Error("{Exception}", e.Exception);
            e.RequestCatch = true;
            ShowErrorDialog(e.Exception);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            // SetObserved prevents the process from terminating on GC finalisation of faulted tasks.
            // Only surface the error dialog for unexpected faults; silently absorb cancellations.
            e.SetObserved();
            // Check all InnerExceptions, not just InnerException[0], so that an AggregateException
            // containing e.g. both an OperationCanceledException and an IOException is not silently
            // absorbed when the cancellation happens to be first in the list.
            if (e.Exception.InnerExceptions.All(ex => ex is OperationCanceledException))
                return;
            Log.Error("{Exception}", e.Exception);
            ShowErrorDialog(e.Exception ?? new Exception("Unobserved task faulted"));
        };

        base.OnFrameworkInitializationCompleted();
    }

    private void AppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        Log.Information("––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––");
        Log.CloseAndFlush();
        UserSettings.Save();
    }

    internal static void ShowErrorDialog(Exception ex)
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                var result = EErrorKind.Ignore;
                var tcs = new TaskCompletionSource();

                var resetBtn = new Button { Content = "Reset Settings" };
                var restartBtn = new Button { Content = "Restart" };
                var okBtn = new Button { Content = "OK" };

                var dialog = new Window
                {
                    Title = "Fatal Error",
                    SizeToContent = SizeToContent.WidthAndHeight,
                    MinWidth = 420,
                    MaxWidth = 640,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new StackPanel
                    {
                        Margin = new Thickness(16),
                        Spacing = 12,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"An unhandled {ex.GetBaseException().GetType().Name} occurred:\n{ex.Message}",
                                TextWrapping = TextWrapping.Wrap,
                            },
                            new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                Spacing = 8,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Children = { resetBtn, restartBtn, okBtn },
                            },
                        },
                    },
                };

                resetBtn.Click += (_, _) => { result = EErrorKind.ResetSettings; dialog.Close(); };
                restartBtn.Click += (_, _) => { result = EErrorKind.Restart; dialog.Close(); };
                okBtn.Click += (_, _) => { result = EErrorKind.Ignore; dialog.Close(); };
                dialog.Closed += (_, _) => tcs.TrySetResult();

                var owner = (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (owner != null)
                {
                    await dialog.ShowDialog(owner);
                }
                else
                {
                    try
                    { dialog.Show(); }
                    catch { return; }
                    await tcs.Task;
                }

                if (result == EErrorKind.ResetSettings)
                    UserSettings.Delete();
                if (result != EErrorKind.Ignore)
                    ApplicationService.ApplicationView.Restart();
            }
            catch (Exception dialogEx)
            {
                Log.Error("{Exception}", dialogEx);
            }
        }).ContinueWith(
            t => Log.Error("{Exception}", t.Exception!.InnerException),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private string GetOperatingSystemProductName()
    {
        var productName = string.Empty;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                productName = BrandingFormatString("%WINDOWS_LONG%");
            }
            catch
            {
                // ignored
            }
        }

        if (string.IsNullOrEmpty(productName))
            productName = Environment.OSVersion.VersionString;

        return $"{productName} ({(Environment.Is64BitOperatingSystem ? "64" : "32")}-bit)";
    }

    public static string GetRegistryValue(string path, string? name = null, RegistryHive root = RegistryHive.CurrentUser)
    {
        if (!OperatingSystem.IsWindows())
            return string.Empty;
        using var rk = RegistryKey.OpenBaseKey(root, RegistryView.Default).OpenSubKey(path);
        if (rk != null)
            return rk.GetValue(name, null) as string ?? string.Empty;
        return string.Empty;
    }
}

