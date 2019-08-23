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
        /// ask the keychain api for all dynamic keys
        /// if an API pak name match a local pak name, the key is saved and the pak can be opened with this key
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

            string data = GetKeysFromKeychain();
            if (!string.IsNullOrEmpty(data))
            {
                JObject myObject = JObject.Parse(data);
                if (myObject != null)
                {
                    DynamicKeysManager.AESEntries = new List<AESEntry>();
                    foreach (PaksEntry item in ThePak.dynamicPaksList)
                    {
                        if (myObject.ToString().Contains(item.thePak))
                        {
                            JToken token = myObject.FindTokens(item.thePak).FirstOrDefault();

                            DynamicKeysManager.serialize(token.ToString().ToUpper().Substring(2), item.thePak);

                            displayNewPaks(token.ToString().ToUpper().Substring(2), item.thePak);
                        }
                        else
                        {
                            DynamicKeysManager.serialize("", item.thePak);
                        }
                    }
                }
            }

            DynamicKeysManager.deserialize();
        }

        /// <summary>
        /// return the dynamic keys part from Ben API
        /// </summary>
        /// <returns></returns>
        private static string GetKeysFromKeychain()
        {
            try
            {
                if (DLLImport.IsInternetAvailable())
                {
                    string data = Keychain.GetEndpoint("http://benbotfn.tk:8080/api/aes");
                    if (string.IsNullOrEmpty(data)) { new UpdateMyConsole("API Down or Rate Limit Exceeded", Color.CornflowerBlue, true).AppendToConsole(); return null; }

                    JToken dynamicPaks = JObject.Parse(data).FindTokens("additionalKeys").FirstOrDefault();
                    return JToken.Parse(dynamicPaks.ToString()).ToString().TrimStart('[').TrimEnd(']');
                }
                else
                {
                    new UpdateMyConsole("Your internet connection is currently unavailable, can't check for dynamic keys at the moment.", Color.CornflowerBlue, true).AppendToConsole();
                    return null;
                }
            }
            catch (Exception)
            {
                new UpdateMyConsole("Error while checking for dynamic keys", Color.Crimson, true).AppendToConsole();
                return null;
            }
        }

        /// <summary>
        /// check if an old list of keys exist, if so, search for the pakname
        /// if pakname not found that means the key is brand new and has to be added but in this case we just "print" it as a FYI to the user
        /// </summary>
        /// <param name="pakName"> the pak name </param>
        private static void displayNewPaks(string pakKey, string pakName)
        {
            if (_oldKeysList != null)
            {
                //display new paks that can be opened
                bool wasThereBeforeStartup = _oldKeysList.Where(i => i.theKey == pakKey).Any();
                if (!wasThereBeforeStartup)
                {
                    new UpdateMyConsole(pakName, Color.CornflowerBlue).AppendToConsole();
                    new UpdateMyConsole(" can now be opened.", Color.Black, true).AppendToConsole();
                }
            }
            else
            {
                //display all paks that can be opened
                new UpdateMyConsole(pakName, Color.CornflowerBlue).AppendToConsole();
                new UpdateMyConsole(" can be opened.", Color.Black, true).AppendToConsole();
            }
        }
    }
}
