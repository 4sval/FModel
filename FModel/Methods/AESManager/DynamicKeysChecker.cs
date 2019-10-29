using FModel.Methods.Utilities;
using Newtonsoft.Json;
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

        public static void SetDynamicKeys(bool reaload = false)
        {
            if (FProp.Default.ReloadAES || reaload)
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
                    string KeysFromBen = EndpointsUtility.GetKeysFromBen();
                    if (!string.IsNullOrEmpty(KeysFromBen))
                    {
                        Dictionary<string, string> KeysDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(KeysFromBen);
                        if (KeysDict != null)
                        {
                            AESEntries.AESEntriesList = new List<AESInfosEntry>();
                            foreach (PAKInfosEntry Pak in PAKEntries.PAKEntriesList.Where(x => x.bTheDynamicPAK == true //DYNAMIC PAK ONLY 
                            && !AESEntries.AESEntriesList.Where(w => string.Equals(w.ThePAKName, Path.GetFileNameWithoutExtension(x.ThePAKPath))).Any() //IS NOT ALREADY ADDED
                            ))
                            {
                                if (KeysDict.ContainsKey(Path.GetFileName(Pak.ThePAKPath)))
                                {
                                    KeysManager.Serialize(Path.GetFileNameWithoutExtension(Pak.ThePAKPath), KeysDict[Path.GetFileName(Pak.ThePAKPath)].ToUpperInvariant().Substring(2));

                                    if (_oldAESEntriesList != null)
                                    {
                                        if (!_oldAESEntriesList.Where(x => string.Equals(x.ThePAKKey, KeysDict[Path.GetFileName(Pak.ThePAKPath)].ToUpperInvariant().Substring(2))).Any())
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
                                else
                                {
                                    KeysManager.Serialize(Path.GetFileName(Pak.ThePAKPath), string.Empty);
                                }
                            }
                        }
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(AESManager_PATH));
                using (var fileStream = new FileStream(AESManager_PATH, FileMode.Create))
                {
                    KeysManager.serializer.Serialize(fileStream, AESEntries.AESEntriesList);
                }
            }
            else
                KeysManager.Deserialize();

            FWindow.FMain.Dispatcher.InvokeAsync(() =>
            {
                PAKsUtility.DisableNonKeyedPAKs();
            });
        }
    }
}
