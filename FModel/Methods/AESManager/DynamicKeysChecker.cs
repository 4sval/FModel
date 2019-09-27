using FModel.Methods.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FProp = FModel.Properties.Settings;

namespace FModel.Methods.AESManager
{
    static class DynamicKeysChecker
    {
        private static readonly string AESManager_PATH = FProp.Default.FOutput_Path + "\\FAESManager.xml";
        private static List<AESInfosEntry> _oldAESEntriesList;

        public static void SetDynamicKeys()
        {
            if (!File.Exists(AESManager_PATH))
            {
                AESEntries.AESEntriesList = new List<AESInfosEntry>();
                KeysManager.Serialize(string.Empty, string.Empty);
            }
            else
            {
                KeysManager.Deserialize();
                _oldAESEntriesList = AESEntries.AESEntriesList;
            }

            if (PAKEntries.PAKEntriesList != null && PAKEntries.PAKEntriesList.Any())
            {
                string[] KeysFromKeychain = EndpointsUtility.GetKeysFromKeychain();
                if (KeysFromKeychain != null)
                {
                    AESEntries.AESEntriesList = new List<AESInfosEntry>();
                    KeysManager.Serialize(string.Empty, string.Empty); //to delete the old keys in case there's no new keys in the api and old ones got moved to the main files
                    foreach (string GuidKeyItem in KeysFromKeychain)
                    {
                        string[] Parts = GuidKeyItem.Split(':');
                        AddDynamicKeysToAESManager(Parts[0], Parts[1]);
                    }
                }
            }

            FWindow.FMain.Dispatcher.InvokeAsync(() =>
            {
                PAKsUtility.DisableNonKeyedPAKs();
            });
        }

        private static void AddDynamicKeysToAESManager(string GuidPart, string AESPart)
        {
            foreach (PAKInfosEntry Pak in PAKEntries.PAKEntriesList.Where(x => x.bTheDynamicPAK == true //DYNAMIC PAK ONLY
            && string.Equals(PAKsUtility.GetEpicGuid(x.ThePAKGuid), GuidPart) //LOCAL GUID MATCH API GUID
            && !AESEntries.AESEntriesList.Where(w => string.Equals(w.ThePAKName, Path.GetFileNameWithoutExtension(x.ThePAKPath))).Any() //IS NOT ALREADY ADDED
            ))
            {
                string AESKey = BitConverter.ToString(Convert.FromBase64String(AESPart)).Replace("-", "");

                KeysManager.Serialize(Path.GetFileNameWithoutExtension(Pak.ThePAKPath), AESKey.ToUpperInvariant());

                if (_oldAESEntriesList != null)
                {
                    if (!_oldAESEntriesList.Where(x => string.Equals(x.ThePAKKey, AESKey.ToUpperInvariant())).Any())
                    {
                        new UpdateMyConsole(Path.GetFileName(Pak.ThePAKPath), CColors.Blue).Append();
                        new UpdateMyConsole(" can now be opened.", CColors.White, true).Append();
                    }
                    //else mean there was a FAESManager.xml but the key was already there and didn't change
                }
                else
                {
                    //mostly for new users
                    new UpdateMyConsole(Path.GetFileName(Pak.ThePAKPath), CColors.Blue).Append();
                    new UpdateMyConsole(" can be opened.", CColors.White, true).Append();
                }
            }
        }
    }
}
