using Newtonsoft.Json;
using RestSharp;
using System;

namespace FModel.Methods.Auth
{
    static class AuthFlow
    {
        private const string _EPIC_OAUTH_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";

        public static void SetOAuthLauncherToken()
        {
            var oauthClient = new RestClient(_EPIC_OAUTH_URL);
            var oauthRes = oauthClient.Execute(
                new RestRequest(Method.POST)
                    .AddHeader("Content-Type", "application/x-www-form-urlencoded")
                    .AddHeader("Authorization", "basic MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE=")
                    .AddParameter("grant_type", "client_credentials")
                    .AddParameter("token_type", "eg1"));

            var response = JsonConvert.DeserializeObject<dynamic>(oauthRes.Content);
            var launcher_access_token = response["access_token"];
            var launcher_expires_in = response["expires_in"];

            Properties.Settings.Default.ELauncherToken = launcher_access_token;
            Properties.Settings.Default.ELauncherExpiration = DateTimeOffset.Now.AddSeconds(Convert.ToDouble(launcher_expires_in)).ToUnixTimeMilliseconds();
            Properties.Settings.Default.Save();
        }

        public static bool IsLauncherTokenExpired()
        {
            long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if ((currentTime - 60000) >= Properties.Settings.Default.ELauncherExpiration)
            {
                return true;
            }
            return false;
        }
    }
}
