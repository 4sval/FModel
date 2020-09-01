using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace FModel.Properties
{
    public sealed partial class Settings
    {
        private static string _userSettings = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FModel\\DoNotDelete.json";

        /// <summary>
        /// IMPORTANT: i believe Upgrade doesn't like int32 so use int64 (maybe because it's for x64?) for all int values
        /// </summary>
        public override void Upgrade()
        {
#if DEBUG
            _userSettings = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FModel\\DoNotDelete_Debug.json";
#endif

            if (File.Exists(_userSettings))
            {
                var dico = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(_userSettings));
                foreach (SettingsProperty setting in Default.Properties)
                {
                    if (dico.TryGetValue(setting.Name, out var value))
                    {
                        Default[setting.Name] = value;
                    }
                }
            }

            Default.SkipVersion = false; // just in case
            Default.UpdateSettings = false; // just in case
            Default.Save();
        }

        /// <summary>
        /// ty .net core <3
        /// </summary>
        /// <returns></returns>
        public static async Task SaveToFile()
        {
#if DEBUG
            _userSettings = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FModel\\DoNotDelete_Debug.json";
#endif

            Default.Save();
            await File.WriteAllTextAsync(
                    _userSettings,
                    JsonConvert.SerializeObject(Default, Formatting.Indented)).ConfigureAwait(false);
        }
    }
}
