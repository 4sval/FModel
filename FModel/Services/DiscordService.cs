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
        private const string _APP_ID = "684489366189768767";

        private RichPresence _currentPresence;
        private readonly DiscordRpcClient _client = new(_APP_ID);
        private readonly Timestamps _timestamps = new() {Start = DateTime.UtcNow};

        private readonly Assets _staticAssets = new()
        {
            LargeImageKey = "official_logo", SmallImageKey = "verified", SmallImageText = $"v{Constants.APP_VERSION}"
        };

        private readonly Button[] _buttons =
        {
            new() {Label = "Join FModel", Url = Constants.DISCORD_LINK},
            new() {Label = "Support us", Url = Constants.DONATE_LINK}
        };

        public void Initialize(FGame game)
        {
            _currentPresence = new RichPresence
            {
                Assets = _staticAssets,
                Timestamps = _timestamps,
                Buttons = _buttons,
                Details = $"{game.GetDescription()} - Idling"
            };

            _client.OnReady += (_, args) => Log.Information("{Username}#{Discriminator} ({UserId}) is now ready", args.User.Username, args.User.Discriminator, args.User.ID);
            _client.SetPresence(_currentPresence);
            _client.Initialize();
        }

        public void UpdatePresence(CUE4ParseViewModel viewModel) =>
            UpdatePresence(
                $"{viewModel.Game.GetDescription()} - {viewModel.Provider.MountedVfs.Count}/{viewModel.Provider.MountedVfs.Count + viewModel.Provider.UnloadedVfs.Count} Packages",
                $"Mode: {UserSettings.Default.LoadingMode.GetDescription()} - {viewModel.SearchVm.ResultsCount:### ### ###} Loaded Assets".Trim());

        public void UpdatePresence(string details, string state)
        {
            if (!_client.IsInitialized) return;
            _currentPresence.Details = details;
            _currentPresence.State = state;
            _client.SetPresence(_currentPresence);
            _client.Invoke();
        }

        public void UpdateButDontSavePresence(string details = null, string state = null)
        {
            if (!_client.IsInitialized) return;
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
            if (!_client.IsInitialized) return;
            _client.SetPresence(_currentPresence);
            _client.Invoke();
        }

        public void Shutdown()
        {
            if (_client.IsInitialized)
                _client.Deinitialize();
        }

        public void Dispose()
        {
            if (!_client.IsDisposed)
                _client.Dispose();
        }
    }
}