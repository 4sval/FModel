using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Generic;
using System.Text;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.Utilities
{
    static class EndpointsUtility
    {
        public static string GetEndpoint(string url)
        {
            DebugHelper.WriteLine("Sending GET request to " + url);

            RestClient EndpointClient = new RestClient(url);
            RestRequest EndpointRequest = new RestRequest(Method.GET);

            string response = EndpointClient.Execute(EndpointRequest).Content;

            return response;
        }

        public static List<BackupInfosEntry> GetBackupsFromDropbox()
        {
            if (DLLImport.IsInternetAvailable())
            {
                string EndpointContent = GetEndpoint("https://dl.dropbox.com/s/0ykcue03qweb98r/FModel.json?dl=0");
                if (!string.IsNullOrEmpty(EndpointContent))
                {
                    List<BackupInfosEntry> ListToReturn = new List<BackupInfosEntry>();
                    JToken FData = JToken.Parse(EndpointContent);

                    //GLOBAL MESSAGES
                    JArray FGMessages = FData["Global_Messages"].Value<JArray>();
                    if (!string.IsNullOrEmpty(FGMessages[0]["Message"].Value<string>()))
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (JToken t in FGMessages)
                        {
                            string text = t["Message"].Value<string>();
                            bool nl = t["bNewLine"].Value<bool>();
                            new UpdateMyConsole(text, t["Color"].Value<string>(), nl).Append();

                            if (nl)
                                sb.AppendLine(text);
                            else
                                sb.Append(text);
                        }
                        DebugHelper.WriteLine("Dropbox: MOTD: " + sb.ToString().TrimEnd());
                    }

                    //BACKUPS
                    foreach (JProperty prop in FData["Backups"].Value<JObject>().Properties())
                    {
                        DebugHelper.WriteLine("Dropbox: " + prop.Name + " available to download");
                        ListToReturn.Add(new BackupInfosEntry(prop.Name, prop.Value.Value<string>()));
                    }
                    return ListToReturn;
                }
                else
                {
                    DebugHelper.WriteLine("Dropbox: Error while checking for backup files");
                    new UpdateMyConsole("Error while checking for backup files", CColors.Red, true).Append();
                    return null;
                }
            }
            else
            {
                DebugHelper.WriteLine("Dropbox: Your internet connection is currently unavailable, can't check for backup files at the moment.");
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
                        if (mainKeyToken != null)
                        {
                            FProp.Default.FPak_MainAES = $"{mainKeyToken.Value<string>().Substring(2).ToUpperInvariant()}";
                            FProp.Default.Save();

                            DebugHelper.WriteLine("BenBotAPI: Main AES key set to " + mainKeyToken.Value<string>());
                        }
                        else
                            DebugHelper.WriteLine("BenBotAPI: Main AES key not found in endpoint response");
                    }

                    JToken dynamicPaks = JObject.Parse(EndpointContent).SelectToken("additionalKeys");
                    return JToken.Parse(dynamicPaks.ToString()).ToString().TrimStart('[').TrimEnd(']');
                }
                else
                {
                    DebugHelper.WriteLine("BenBotAPI: Down or Rate Limit Exceeded");
                    new UpdateMyConsole("API Down or Rate Limit Exceeded", CColors.Blue, true).Append();
                    return string.Empty;
                }
            }
            else
            {
                DebugHelper.WriteLine("BenBotAPI: Your internet connection is currently unavailable, can't check for dynamic keys at the moment.");
                new UpdateMyConsole("Your internet connection is currently unavailable, can't check for dynamic keys at the moment.", CColors.Blue, true).Append();
                return string.Empty;
            }
        }
    }
}
