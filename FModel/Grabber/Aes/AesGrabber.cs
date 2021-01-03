using FModel.Logger;
using FModel.ViewModels.MenuItem;
using FModel.Windows.CustomNotifier;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FModel.Grabber.Aes
{
    static class AesGrabber
    {
        public static async Task<bool> Load(bool forceReload = false)
        {
            if (Globals.Game.ActualGame == EGame.Fortnite && MenuItems.pakFiles.AtLeastOnePak())
            {
                if (forceReload)
                {
                    Dictionary<string, string> staticKeys = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.StaticAesKeys))
                        staticKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(Properties.Settings.Default.StaticAesKeys);

                    Dictionary<string, Dictionary<string, string>> oldDynamicKeys = new Dictionary<string, Dictionary<string, string>>();
                    try
                    {
                        if (!string.IsNullOrEmpty(Properties.Settings.Default.DynamicAesKeys))
                            oldDynamicKeys = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(Properties.Settings.Default.DynamicAesKeys);
                    }
                    catch (JsonSerializationException) { /* Needed for the transition bewteen global dynamic keys and "per game" dynamic keys */ }

                    BenResponse benResponse = await AesData.GetData().ConfigureAwait(false);
                    if (benResponse != null)
                    {
                        if (!string.IsNullOrEmpty(benResponse.MainKey))
                        {
                            string mainKey = $"0x{benResponse.MainKey[2..].ToUpper()}";
                            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AES]", $"BenBot Main key is {mainKey}");
                            staticKeys[Globals.Game.ActualGame.ToString()] = mainKey;
                            Properties.Settings.Default.StaticAesKeys = JsonConvert.SerializeObject(staticKeys, Formatting.None);
                        }

                        if (oldDynamicKeys.TryGetValue(Globals.Game.ActualGame.ToString(), out var gameDict))
                        {
                            Dictionary<string, string> difference = benResponse.DynamicKeys
                                .Where(x => !x.Key.Contains("optional") && (!gameDict.ContainsKey(x.Key) || !gameDict[x.Key].Equals(x.Value)))
                                .ToDictionary(x => x.Key, x => x.Value);
                            foreach (KeyValuePair<string, string> KvP in difference)
                            {
                                Globals.gNotifier.ShowCustomMessage(
                                    Properties.Resources.PakFiles,
                                    string.Format(
                                        Properties.Resources.PakCanBeOpened,
                                        KvP.Key[(KvP.Key.IndexOf("Paks/") + "Paks/".Length)..]),
                                    "/FModel;component/Resources/lock-open-variant.ico");
                                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AES]", $"{KvP.Key} with key {KvP.Value} can be opened");
                            }
                        }

                        foreach (var (key, value) in benResponse.DynamicKeys.ToList())
                        {
                            if (key.Contains("optional"))
                            {
                                if (!benResponse.DynamicKeys.TryGetValue(key.Replace("optional", ""), out string _))
                                    benResponse.DynamicKeys[key.Replace("optional", "")] = value;
                            }
                            else
                            {
                                if (!benResponse.DynamicKeys.TryGetValue(key.Replace("-WindowsClient", "optional-WindowsClient"), out string _))
                                    benResponse.DynamicKeys[key.Replace("-WindowsClient", "optional-WindowsClient")] = value;
                            }
                        }

                        oldDynamicKeys[Globals.Game.ActualGame.ToString()] = benResponse.DynamicKeys;
                        Properties.Settings.Default.DynamicAesKeys = JsonConvert.SerializeObject(oldDynamicKeys, Formatting.None);
                        Properties.Settings.Default.Save();

                        DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AES]", $"BenBot Dynamic keys are {Properties.Settings.Default.DynamicAesKeys}");
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
