using System;
using AdonisUI.Controls;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoUpdaterDotNET;
using FModel.ViewModels.ApiEndpoints.Models;
using Newtonsoft.Json;
using RestSharp;
using Serilog;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;
using FModel.Settings;

namespace FModel.ViewModels.ApiEndpoints
{
    public class FModelApi : AbstractApiProvider
    {
        private News _news;
        private Info _infos;
        private Backup[] _backups;
        private readonly IDictionary<string, CommunityDesign> _communityDesigns = new Dictionary<string, CommunityDesign>();
        
        public FModelApi(IRestClient client) : base(client)
        {
        }

        public async Task<News> GetNewsAsync(CancellationToken token)
        {
            var request = new RestRequest($"https://api.fmodel.app/v1/news/{Constants.APP_VERSION}", Method.GET);
            var response = await _client.ExecuteAsync<News>(request, token).ConfigureAwait(false);
            Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
            return response.Data;
        }

        public News GetNews(CancellationToken token)
        {
            return _news ??= GetNewsAsync(token).GetAwaiter().GetResult();
        }

        public async Task<Info> GetInfosAsync(CancellationToken token, EUpdateMode updateMode)
        {
            var request = new RestRequest($"https://api.fmodel.app/v1/infos/{updateMode}", Method.GET);
            var response = await _client.ExecuteAsync<Info>(request, token).ConfigureAwait(false);
            Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
            return response.Data;
        }

        public Info GetInfos(CancellationToken token, EUpdateMode updateMode)
        {
            return _infos ?? GetInfosAsync(token, updateMode).GetAwaiter().GetResult();
        }

        public async Task<Backup[]> GetBackupsAsync(CancellationToken token, string gameName)
        {
            var request = new RestRequest($"https://api.fmodel.app/v1/backups/{gameName}", Method.GET);
            var response = await _client.ExecuteAsync<Backup[]>(request, token).ConfigureAwait(false);
            Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
            return response.Data;
        }

        public Backup[] GetBackups(CancellationToken token, string gameName)
        {
            return _backups ??= GetBackupsAsync(token, gameName).GetAwaiter().GetResult();
        }

        public async Task<CommunityDesign> GetDesignAsync(string designName)
        {
            var request = new RestRequest($"https://api.fmodel.app/v1/designs/{designName}", Method.GET);
            var response = await _client.ExecuteAsync<Community>(request).ConfigureAwait(false);
            Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
            return response.Data != null ? new CommunityDesign(response.Data) : null;
        }

        public CommunityDesign GetDesign(string designName)
        {
            if (_communityDesigns.TryGetValue(designName, out var communityDesign) && communityDesign != null)
                return communityDesign;

            communityDesign = GetDesignAsync(designName).GetAwaiter().GetResult();
            _communityDesigns[designName] = communityDesign;
            return communityDesign;
        }
        
        public void CheckForUpdates(EUpdateMode updateMode)
        {
            AutoUpdater.ParseUpdateInfoEvent += ParseUpdateInfoEvent;
            AutoUpdater.CheckForUpdateEvent += CheckForUpdateEvent;
            AutoUpdater.Start($"https://api.fmodel.app/v1/infos/{updateMode}");
        }
        
        private void ParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
        {
            _infos = JsonConvert.DeserializeObject<Info>(args.RemoteData);
            if (_infos != null)
            {
                args.UpdateInfo = new UpdateInfoEventArgs
                {
                    CurrentVersion = _infos.Version,
                    ChangelogURL = _infos.ChangelogUrl,
                    DownloadURL = _infos.DownloadUrl
                };
            }
        }
        
        private void CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null)
            {
                Version currentVersion = new Version(args.CurrentVersion);
                if (currentVersion == args.InstalledVersion) return;

                bool downgrade = currentVersion < args.InstalledVersion;
                var messageBox = new MessageBoxModel
                {
                    Text = $"The latest version of FModel {UserSettings.Default.UpdateMode} is {args.CurrentVersion}. You are using version {args.InstalledVersion}. Do you want to {(downgrade ? "downgrade" : "update")} the application now?",
                    Caption = $"{(downgrade ? "Downgrade" : "Update")} Available",
                    Icon = MessageBoxImage.Question,
                    Buttons = MessageBoxButtons.YesNo(),
                    IsSoundEnabled = false
                };
                
                MessageBox.Show(messageBox);
                if (messageBox.Result != MessageBoxResult.Yes) return;
                
                try
                {
                    if (AutoUpdater.DownloadUpdate(args))
                    {
                        Application.Current.Shutdown();
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(
                    "There is a problem reaching the update server, please check your internet connection or try again later.",
                    "Update Check Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}