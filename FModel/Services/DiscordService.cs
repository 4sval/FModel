using System;
using DiscordRPC;
using FModel.Extensions;
using FModel.Settings;
using FModel.ViewModels;
using Serilog;

namespace FModel.Services
{
    public sealed class DiscordService
    {
        public static DiscordHandler DiscordHandler { get; } = new();
    }

    public class DiscordHandler
    {
        private const string _APP_ID = ""; // Disabled: no Discord application registered for this fork yet

        private RichPresence _currentPresence;
        private readonly DiscordRpcClient? _client = string.IsNullOrEmpty(_APP_ID) ? null : new(_APP_ID);
        private readonly Timestamps _timestamps = new() {Start = DateTime.UtcNow};

        private readonly Assets _staticAssets = new()
        {
            LargeImageKey = "official_logo", SmallImageKey = "verified", SmallImageText = $"v{Constants.APP_VERSION} ({Constants.APP_SHORT_COMMIT_ID})"
        };

        private readonly Button[] _buttons =
        {
            new() {Label = "Join FModel", Url = Constants.DISCORD_LINK},
            new() {Label = "Support us", Url = Constants.DONATE_LINK}
        };

        public void Initialize(string gameName)
        {
            if (_client == null) return; // _APP_ID is empty, Discord RPC is disabled for this fork

            _currentPresence = new RichPresence
            {
                Assets = _staticAssets,
                Timestamps = _timestamps,
                Buttons = _buttons,
                Details = $"{gameName} - Idling"
            };

            _client.OnReady += (_, args) => Log.Information("@{Username} ({UserId}) is now ready", args.User.Username, args.User.ID);
            _client.SetPresence(_currentPresence);
            _client.Initialize();
        }

        public void UpdatePresence(CUE4ParseViewModel viewModel) =>
            UpdatePresence(
                $"{viewModel.Provider.GameDisplayName ?? viewModel.Provider.ProjectName} - {viewModel.Provider.MountedVfs.Count}/{viewModel.Provider.MountedVfs.Count + viewModel.Provider.UnloadedVfs.Count} Packages",
                $"Mode: {UserSettings.Default.LoadingMode.GetDescription()} - {viewModel.SearchVm.ResultsCount:### ### ###} Loaded Assets".Trim());

        public void UpdatePresence(string details, string state)
        {
            if (_client is not { IsInitialized: true }) return;
            _currentPresence.Details = details;
            _currentPresence.State = state;
            _client.SetPresence(_currentPresence);
            _client.Invoke();
        }

        public void UpdateButDontSavePresence(string details = null, string state = null)
        {
            if (_client is not { IsInitialized: true }) return;
            _client.SetPresence(new RichPresence
            {
                Assets = _staticAssets,
                Timestamps = _timestamps,
                Buttons = _buttons,
                Details = details ?? _currentPresence.Details,
                State = state ?? _currentPresence.State
            });
            _client.Invoke();
        }

        public void UpdateToSavedPresence()
        {
            if (_client is not { IsInitialized: true }) return;
            _client.SetPresence(_currentPresence);
            _client.Invoke();
        }

        public void Shutdown()
        {
            if (_client is { IsInitialized: true })
                _client.Deinitialize();
        }

        public void Dispose()
        {
            if (_client is { IsDisposed: false })
                _client.Dispose();
        }
    }
}
