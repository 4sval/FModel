using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Generic;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Utilities
{
    static class EndpointsUtility
    {
        public static string GetEndpoint(string url)
        {
            RestClient EndpointClient = new RestClient(url);
            RestRequest EndpointRequest = new RestRequest(Method.GET);

            string response = EndpointClient.Execute(EndpointRequest).Content;

            return response;
        }

        public static List<BackupInfosEntry> GetBackupsFromDropbox()
        {
            if (DLLImport.IsInternetAvailable())
            {
                string EndpointContent = GetEndpoint("https://dl.dropbox.com/s/mi7oyjywdtatxb0/FModel3_Backups.json?dl=0");
                if (!string.IsNullOrEmpty(EndpointContent))
                {
                    List<BackupInfosEntry> ListToReturn = new List<BackupInfosEntry>();
                    JArray array = JArray.Parse(EndpointContent);
                    foreach (JProperty prop in array.Children<JObject>().Properties())
                    {
                        ListToReturn.Add(new BackupInfosEntry(prop.Name, prop.Value.Value<string>()));
                    }
                    return ListToReturn;
                }
                else
                {
                    new UpdateMyConsole("Error while checking for backup files", CColors.Red, true).Append();
                    return null;
                }
            }
            else
            {
                new UpdateMyConsole("Your internet connection is currently unavailable, can't check for backup files at the moment.", CColors.Blue, true).Append();
                return null;
            }
        }

        public static string GetKeysFromBen()
        {
            if (DLLImport.IsInternetAvailable())
            {
                string EndpointContent = GetEndpoint("http://benbotfn.tk:8080/api/aes");
                if (!string.IsNullOrEmpty(EndpointContent))
                {
                    if (string.IsNullOrEmpty(FProp.Default.FPak_MainAES))
                    {
                        JToken mainKeyToken = JObject.Parse(EndpointContent).SelectToken("mainKey");
                        FProp.Default.FPak_MainAES = mainKeyToken != null ? $"{mainKeyToken.Value<string>().Substring(2).ToUpperInvariant()}" : "";
                        FProp.Default.Save();
                    }

                    JToken dynamicPaks = JObject.Parse(EndpointContent).SelectToken("additionalKeys");
                    return JToken.Parse(dynamicPaks.ToString()).ToString().TrimStart('[').TrimEnd(']');
                }
                else
                {
                    new UpdateMyConsole("API Down or Rate Limit Exceeded", CColors.Blue, true).Append();
                    return null;
                }
            }
            else
            {
                new UpdateMyConsole("Your internet connection is currently unavailable, can't check for dynamic keys at the moment.", CColors.Blue, true).Append();
                return null;
            }
        }
    }
}
