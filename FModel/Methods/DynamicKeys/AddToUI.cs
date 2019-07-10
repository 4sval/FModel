using FModel.Methods.BackupPAKs.Parser.AESKeyParser;
using FModel.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace FModel
{
    static class AddToUI
    {
        private static List<AESEntry> _oldKeysList = null;
        private static string[] _KeysFromTheApi = null;

        /// <summary>
        /// ask the keychain api for all dynamic keys and their guids
        /// if an API guid match a local guid, the key is saved and the pak can be opened with this key
        /// </summary>
        public static void checkAndAddDynamicKeys()
        {
            if (!File.Exists(DynamicKeysManager.path))
            {
                DynamicKeysManager.AESEntries = new List<AESEntry>();
                DynamicKeysManager.serialize("", "");
            }
            else
            {
                DynamicKeysManager.deserialize();
                _oldKeysList = DynamicKeysManager.AESEntries;
            }

            _KeysFromTheApi = GetKeysFromKeychain();
            if (_KeysFromTheApi != null)
            {
                DynamicKeysManager.AESEntries = new List<AESEntry>();
                foreach (string myString in _KeysFromTheApi)
                {
                    string[] parts = myString.Split(':');
                    string apiGuid = Keychain.getPakGuidFromKeychain(parts);

                    string actualPakGuid = ThePak.dynamicPaksList.Where(i => i.thePakGuid == apiGuid).Select(i => i.thePakGuid).FirstOrDefault();
                    string actualPakName = ThePak.dynamicPaksList.Where(i => i.thePakGuid == apiGuid).Select(i => i.thePak).FirstOrDefault();

                    bool pakAlreadyExist = DynamicKeysManager.AESEntries.Where(i => i.thePak == actualPakName).Any();

                    if (!string.IsNullOrEmpty(actualPakGuid) && !pakAlreadyExist)
                    {
                        byte[] bytes = Convert.FromBase64String(parts[1]);
                        string aeskey = BitConverter.ToString(bytes).Replace("-", "");

                        DynamicKeysManager.serialize(aeskey.ToUpper(), actualPakName);

                        displayNewPaks(actualPakName);
                    }
                }
                new UpdateMyConsole("", Color.Green, true).AppendToConsole();
            }

            DynamicKeysManager.deserialize();
        }

        /// <summary>
        /// just set the array to be the keys from the api
        /// </summary>
        /// <returns></returns>
        private static string[] GetKeysFromKeychain()
        {
            if (DLLImport.IsInternetAvailable() && (!string.IsNullOrWhiteSpace(Settings.Default.eEmail) && !string.IsNullOrWhiteSpace(Settings.Default.ePassword)))
            {
                string myContent = Keychain.GetEndpoint("https://fortnite-public-service-prod11.ol.epicgames.com/fortnite/api/storefront/v2/keychain", true);

                if (myContent.Contains("\"errorCode\": \"errors.com.epicgames.common.authentication.authentication_failed\""))
                {
                    new UpdateMyConsole("[EPIC] Authentication Failed.", Color.Red, true).AppendToConsole();
                    return null;
                }
                else
                {
                    new UpdateMyConsole("[EPIC] Authentication Success.", Color.CornflowerBlue, true).AppendToConsole();
                    return AesKeyParser.FromJson(myContent);
                }
            }
            else { return null; }
        }

        /// <summary>
        /// check if an old list of keys exist, if so, search for the pakname
        /// if pakname not found that means the key is brand new and has to be added but in this case we just "print" it as a FYI to the user
        /// </summary>
        /// <param name="pakName"> the pak name </param>
        private static void displayNewPaks(string pakName)
        {
            if (_oldKeysList != null)
            {
                //display new paks that can be opened
                bool wasThereBeforeStartup = _oldKeysList.Where(i => i.thePak == pakName).Any();
                if (!wasThereBeforeStartup)
                {
                    new UpdateMyConsole(pakName, Color.Firebrick).AppendToConsole();
                    new UpdateMyConsole(" can now be opened.", Color.Black, true).AppendToConsole();
                }
            }
            else
            {
                //display all paks that can be opened
                new UpdateMyConsole(pakName, Color.Firebrick).AppendToConsole();
                new UpdateMyConsole(" can be opened.", Color.Black, true).AppendToConsole();
            }
        }
    }
}
