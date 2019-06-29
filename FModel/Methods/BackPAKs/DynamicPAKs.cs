using FModel.Methods.BackupPAKs.Parser.AccessCodeParser;
using FModel.Methods.BackupPAKs.Parser.TokenParser;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace FModel
{
    static class DynamicPAKs
    {
        private static string AccessToken { get; set; }
        private static string AccessCode { get; set; }

        /// <summary>
        /// get url content as string with authentication
        /// </summary>
        /// <param name="url"></param>
        /// <param name="auth"></param>
        /// <returns> url content </returns>
        public static string GetEndpoint(string url, bool auth)
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.ExchangeToken) || isTokenExpired())
            {
                refreshToken();
            }

            RestClient EndpointClient = new RestClient(url);
            RestRequest EndpointRequest = new RestRequest(Method.GET);
            if (auth)
            {
                EndpointRequest.AddHeader("Authorization", "bearer " + Properties.Settings.Default.ExchangeToken);
            }

            var response = EndpointClient.Execute(EndpointRequest);
            string content = JToken.Parse(response.Content).ToString(Newtonsoft.Json.Formatting.Indented);

            return content;
        }


        private static void getAccessToken(string email, string password)
        {
            RestClient getAccessTokenClient = new RestClient("https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token");
            RestRequest getAccessTokenRequest = new RestRequest(Method.POST);

            getAccessTokenRequest.AddParameter("grant_type", "password");
            getAccessTokenRequest.AddParameter("username", email);
            getAccessTokenRequest.AddParameter("password", password);
            getAccessTokenRequest.AddParameter("includePerms", "true");

            getAccessTokenRequest.AddHeader("Authorization", "basic MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE=");
            getAccessTokenRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            AccessToken = TokenParser.FromJson(getAccessTokenClient.Execute(getAccessTokenRequest).Content).AccessToken;
        }
        private static void getAccessCode(string accessToken)
        {
            RestClient getAccessCodeClient = new RestClient("https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/exchange");
            RestRequest getAccessCodeRequest = new RestRequest(Method.GET);

            getAccessCodeRequest.AddHeader("Authorization", "bearer " + accessToken);

            AccessCode = AccessCodeParser.FromJson(getAccessCodeClient.Execute(getAccessCodeRequest).Content).Code;
        }
        private static void getExchangeToken(string accessCode)
        {
            RestClient getExchangeTokenClient = new RestClient("https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token");
            RestRequest getExchangeTokenRequest = new RestRequest(Method.POST);

            getExchangeTokenRequest.AddHeader("Authorization", "basic ZWM2ODRiOGM2ODdmNDc5ZmFkZWEzY2IyYWQ4M2Y1YzY6ZTFmMzFjMjExZjI4NDEzMTg2MjYyZDM3YTEzZmM4NGQ=");
            getExchangeTokenRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            getExchangeTokenRequest.AddParameter("grant_type", "exchange_code");
            getExchangeTokenRequest.AddParameter("exchange_code", accessCode);
            getExchangeTokenRequest.AddParameter("includePerms", "true");
            getExchangeTokenRequest.AddParameter("token_type", "eg1");

            string content = getExchangeTokenClient.Execute(getExchangeTokenRequest).Content;

            Properties.Settings.Default.ExchangeToken = TokenParser.FromJson(content).AccessToken;
            Properties.Settings.Default.TokenExpiration = DateTimeOffset.Parse(TokenParser.FromJson(content).ExpiresAt).ToUnixTimeMilliseconds();
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// check the current time and the expiration date of our token
        /// 60 seconds before it expires, it's considered expired and should be refreshed
        /// </summary>
        /// <returns></returns>
        private static bool isTokenExpired()
        {
            long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if ((currentTime - 60000) >= Properties.Settings.Default.TokenExpiration)
            {
                return true;
            }
            else { return false; }
        }

        /// <summary>
        /// steps to get our token or refresh
        /// </summary>
        public static void refreshToken()
        {
            Console.WriteLine("refresh");
            getAccessToken(Properties.Settings.Default.eEmail, Properties.Settings.Default.ePassword);
            getAccessCode(AccessToken);
            getExchangeToken(AccessCode);
        }

        private static IEnumerable<string> SplitGuid(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        /// <summary>
        /// split KeychainPart each 8 letter
        /// for each of these letters, convert to hexadecimal as string
        /// </summary>
        /// <param name="KeychainPart"></param>
        /// <returns> the guid (ie 17722063-2246354315-4143272431-3887619937) </returns>
        public static string getPakGuidFromKeychain(string[] KeychainPart)
        {
            StringBuilder sB = new StringBuilder();
            IEnumerable<string> guid = SplitGuid(KeychainPart[0], 8);
            int count = 0;

            foreach (string p in guid)
            {
                count += 1;

                if (count != guid.Count()) { sB.Append((uint)int.Parse(p, NumberStyles.HexNumber) + "-"); }
                else { sB.Append((uint)int.Parse(p, NumberStyles.HexNumber)); }
            }

            return sB.ToString();
        }
    }
}
