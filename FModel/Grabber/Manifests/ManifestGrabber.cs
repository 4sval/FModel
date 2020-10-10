﻿using EpicManifestParser.Objects;
using FModel.Utils;
using System;
using System.Threading.Tasks;

namespace FModel.Grabber.Manifests
{
    static class ManifestGrabber
    {
        public static async Task<ManifestInfo> TryGetLatestManifestInfo()
        {
            if (IsExpired())
            {
                OAuth auth = await Endpoints.GetOAuthInfo();
                if (auth != null)
                {
                    Properties.Settings.Default.AccessToken = auth.AccessToken;
                    Properties.Settings.Default.LauncherExpiration = DateTimeOffset.Now.AddSeconds(Convert.ToDouble(auth.ExpiresIn)).ToUnixTimeMilliseconds();
                    Properties.Settings.Default.Save();
                }
            }

            string ret = await Endpoints.GetStringEndpoint("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live", Properties.Settings.Default.AccessToken);
            if (!string.IsNullOrEmpty(ret)) return new ManifestInfo(ret);
            else return null;
        }

        private static bool IsExpired()
        {
            long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if ((currentTime - 60000) >= Properties.Settings.Default.LauncherExpiration)
            {
                return true;
            }
            return false;
        }
    }
}