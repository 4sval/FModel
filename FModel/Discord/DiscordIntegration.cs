using DiscordRPC;
using DiscordRPC.Logging;
using FModel.Logger;
using System;
using System.Reflection;

namespace FModel.Discord
{
    static class DiscordIntegration
    {
        private const string _DISCORD_APP_ID = "684489366189768767";

        private static readonly Timestamps _baseTimestamp = new Timestamps { Start = DateTime.UtcNow };
        private static readonly Assets _assets = new Assets
        {
            LargeImageKey = "official_logo",
            SmallImageKey = "verified",
            SmallImageText = $"v{Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5)}"
        };
        private static readonly DiscordRpcClient _client = new DiscordRpcClient(_DISCORD_APP_ID);
        private static RichPresence _presence;

        public static void Dispose() => _client.Dispose();
        public static void Deinitialize() => _client.Deinitialize();
        private static void Initialize()
        {
            _client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };
            _client.OnReady += (sender, e) =>
            {
                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Discord RPC]", $"Ready for {e.User.Username}#{e.User.Discriminator} ({e.User.ID})");
            };
            _client.Initialize();
        }

        public static void StartClient()
        {
            _client.SetPresence(new RichPresence
            {
                Assets = _assets,
                Timestamps = _baseTimestamp,
                State = Properties.Resources.Idling
            });
            Initialize();
            SaveCurrentPresence();
        }

        public static void Update(string detail = null, string state = null)
        {
            _client.SetPresence(new RichPresence
            {
                Assets = _assets,
                Timestamps = _baseTimestamp,
                Details = string.IsNullOrEmpty(detail) ? _presence?.Details : detail,
                State = string.IsNullOrEmpty(state) ? _presence?.State : state
            });
            _client.Invoke();
        }

        public static void Restore()
        {
            _client.SetPresence(_presence);
            _client.Invoke();
        }

        public static void SaveCurrentPresence() => _presence = _client.CurrentPresence;
    }
}
