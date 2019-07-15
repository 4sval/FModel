using FModel.Properties;
using Newtonsoft.Json.Linq;
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

            JObject myObject = JObject.Parse(GetKeysFromKeychain());
            if (myObject != null)
            {
                DynamicKeysManager.AESEntries = new List<AESEntry>();
                foreach (PaksEntry item in ThePak.dynamicPaksList)
                {
                    if (myObject.ToString().Contains(item.thePak))
                    {
                        JToken token = myObject.FindTokens(item.thePak).FirstOrDefault();

                        bool pakAlreadyExist = DynamicKeysManager.AESEntries.Where(i => i.thePak == item.thePak).Any();

                        if (!pakAlreadyExist)
                        {
                            DynamicKeysManager.serialize(token.ToString().ToUpper().Substring(2), item.thePak);

                            displayNewPaks(item.thePak);
                        }
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
        private static string GetKeysFromKeychain()
        {
            if (DLLImport.IsInternetAvailable())
            {
                JToken dynamicPaks = JObject.Parse(Keychain.GetEndpoint("http://benbotfn.tk:8080/api/aes")).FindTokens("additionalKeys").FirstOrDefault();
                return JToken.Parse(dynamicPaks.ToString()).ToString().TrimStart('[').TrimEnd(']');
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
