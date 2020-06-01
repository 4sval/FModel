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

                    Dictionary<string, string> oldDynamicKeys = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.DynamicAesKeys))
                        oldDynamicKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(Properties.Settings.Default.DynamicAesKeys);

                    BenResponse benResponse = await AesData.GetData().ConfigureAwait(false);
                    if (benResponse != null)
                    {
                        if (!string.IsNullOrEmpty(benResponse.MainKey))
                        {
                            string mainKey = $"0x{benResponse.MainKey.Substring(2).ToUpper()}";
                            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AES]", $"BenBot Main key is {mainKey}");
                            staticKeys[Globals.Game.ActualGame.ToString()] = mainKey;
                            Properties.Settings.Default.StaticAesKeys = JsonConvert.SerializeObject(staticKeys, Formatting.None);
                        }

                        string dynamicKeys = JsonConvert.SerializeObject(benResponse.DynamicKeys, Formatting.None);
                        DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AES]", $"BenBot Dynamic keys are {dynamicKeys}");
                        Properties.Settings.Default.DynamicAesKeys = dynamicKeys;
                        Properties.Settings.Default.Save();

                        Dictionary<string, string> difference = benResponse.DynamicKeys
                            .Where(x => !oldDynamicKeys.ContainsKey(x.Key) || !oldDynamicKeys[x.Key].Equals(x.Value))
                            .ToDictionary(x => x.Key, x => x.Value);
                        foreach (KeyValuePair<string, string> KvP in difference)
                        {
                            Globals.gNotifier.ShowCustomMessage(
                                Properties.Resources.PakFiles,
                                string.Format(
                                    Properties.Resources.PakCanBeOpened,
                                    KvP.Key.Substring(KvP.Key.IndexOf("Paks/") + "Paks/".Length)),
                                "/FModel;component/Resources/lock-open-variant.ico");
                            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AES]", $"{KvP.Key} with key {KvP.Value} can be opened");
                        }

                        return true;
                    }
                }
            }
            return false;
        }
    }
}
