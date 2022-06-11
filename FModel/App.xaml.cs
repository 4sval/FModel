using AdonisUI.Controls;
using Microsoft.Win32;
using Serilog;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using FModel.Framework;
using FModel.Services;
using FModel.Settings;
using Newtonsoft.Json;
using Serilog.Sinks.SystemConsole.Themes;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

namespace FModel;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("winbrand.dll", CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    static extern string BrandingFormatString(string format);

    protected override void OnStartup(StartupEventArgs e)
    {
#if DEBUG
        AttachConsole(-1);
#endif
        base.OnStartup(e);

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
            UserSettings.Default.OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Output");
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
        if (createMe) Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, "Exports"));
        Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, "Logs"));
        Directory.CreateDirectory(Path.Combine(UserSettings.Default.OutputDirectory, ".data"));

        Log.Logger = new LoggerConfiguration().WriteTo.Console(theme: AnsiConsoleTheme.Literate).WriteTo.File(
            Path.Combine(UserSettings.Default.OutputDirectory, "Logs", $"FModel-Log-{DateTime.Now:yyyy-MM-dd}.txt"),
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [FModel] [{Level:u3}] {Message:lj}{NewLine}{Exception}").CreateLogger();

        Log.Information("Version {Version}", Constants.APP_VERSION);
        Log.Information("{OS}", GetOperatingSystemProductName());
        Log.Information("{RuntimeVer}", RuntimeInformation.FrameworkDescription);
        Log.Information("Culture {SysLang}", Thread.CurrentThread.CurrentUICulture);
    }

    private void AppExit(object sender, ExitEventArgs e)
    {
        Log.Information("––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––");
        Log.CloseAndFlush();
        UserSettings.Save();
        Environment.Exit(0);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error("{Exception}", e.Exception);

        var messageBox = new MessageBoxModel
        {
            Text = $"An unhandled exception occurred: {e.Exception.Message}",
            Caption = "Fatal Error",
            Icon = MessageBoxImage.Error,
            Buttons = new[]
            {
                MessageBoxButtons.Custom("Reset Settings", EErrorKind.ResetSettings),
                MessageBoxButtons.Custom("Restart", EErrorKind.Restart),
                MessageBoxButtons.Custom("OK", EErrorKind.Ignore)
            },
            IsSoundEnabled = false
        };

        MessageBox.Show(messageBox);
        if (messageBox.Result == MessageBoxResult.Custom && (EErrorKind) messageBox.ButtonPressed.Id != EErrorKind.Ignore)
        {
            if ((EErrorKind) messageBox.ButtonPressed.Id == EErrorKind.ResetSettings)
                UserSettings.Default = new UserSettings();

            ApplicationService.ApplicationView.Restart();
        }

        e.Handled = true;
    }

    private string GetOperatingSystemProductName()
    {
        var productName = string.Empty;
        try
        {
            productName = BrandingFormatString("%WINDOWS_LONG%");
        }
        catch
        {
            // ignored
        }

        if (string.IsNullOrEmpty(productName))
            productName = Environment.OSVersion.VersionString;

        return $"{productName} ({(Environment.Is64BitOperatingSystem ? "64" : "32")}-bit)";
    }

    public static string GetRegistryValue(string path, string name = null, RegistryHive root = RegistryHive.CurrentUser)
    {
        using var rk = RegistryKey.OpenBaseKey(root, RegistryView.Default).OpenSubKey(path);
        if (rk != null)
            return rk.GetValue(name, null) as string;
        return string.Empty;
    }
}