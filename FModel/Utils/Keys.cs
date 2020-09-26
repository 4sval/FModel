using FModel.Logger;
using FModel.ViewModels.MenuItem;
using FModel.ViewModels.StatusBar;
using Newtonsoft.Json;
using PakReader;
using PakReader.Parsers.Objects;
using System;
using System.Collections.Generic;

namespace FModel.Utils
{
    static class Keys
    {
        /// <summary>
        /// PakFileReader.ReadIndexInternal will not set AesKey because we're already checking if it works here
        /// it's good for FModel so we don't test the key, read the index and test the key again to set it
        /// </summary>
        /// <param name="disableAll"></param>
        public static void NoKeyGoodBye(bool disableAll = false)
        {
            if (MenuItems.pakFiles.AtLeastOnePak())
            {
                if (disableAll)
                    foreach (PakMenuItemViewModel menuItem in MenuItems.pakFiles.GetMenuItemsWithPakFiles())
                        menuItem.IsEnabled = false;
                else
                {
                    Dictionary<string, string> staticKeys = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.StaticAesKeys))
                        staticKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(Properties.Settings.Default.StaticAesKeys);

                    Dictionary<string, Dictionary<string, string>> dynamicKeys = new Dictionary<string, Dictionary<string, string>>();
                    try
                    {
                        if (!string.IsNullOrEmpty(Properties.Settings.Default.DynamicAesKeys))
                            dynamicKeys = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(Properties.Settings.Default.DynamicAesKeys);
                    }
                    catch (JsonSerializationException) { /* Needed for the transition bewteen global dynamic keys and "per game" dynamic keys */ }

                    bool isMainKey = staticKeys.TryGetValue(Globals.Game.ActualGame.ToString(), out var _);
                    bool mainError = false; // used to avoid notifications about all static paks not working with the key

                    StatusBarVm.statusBarViewModel.Reset();
                    foreach (PakMenuItemViewModel menuItem in MenuItems.pakFiles.GetMenuItemsWithPakFiles())
                    {
                        // reset everyone
                        menuItem.PakFile.AesKey = null;

                        if (!mainError && isMainKey)
                        {
                            if (menuItem.PakFile.Info.EncryptionKeyGuid.Equals(new FGuid(0u, 0u, 0u, 0u)) &&
                                staticKeys.TryGetValue(Globals.Game.ActualGame.ToString(), out var sKey))
                            {
                                sKey = sKey.StartsWith("0x") ? sKey[2..].ToUpperInvariant() : sKey.ToUpperInvariant();
                                try
                                {
                                    // i can use TestAesKey here but that means it's gonna test here then right after to set the key
                                    // so a try catch when setting the key seems better
                                    menuItem.PakFile.AesKey = sKey.Trim().ToBytesKey();
                                }
                                catch (System.Exception e)
                                {
                                    mainError = true;
                                    StatusBarVm.statusBarViewModel.Set(e.Message, Properties.Resources.Error);
                                    DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AES]", $"0x{sKey} is NOT!!!! working with user's pak files");
                                    if (string.IsNullOrEmpty(sKey))
                                        FConsole.AppendText(Properties.Resources.NoKeyWarning, FColors.Red, true);
                                    else
                                        FConsole.AppendText(string.Format(Properties.Resources.StaticKeyNotWorking, $"0x{sKey}"), FColors.Red, true);
                                }
                            }
                        }

                        string trigger;
                        {
                            if (Properties.Settings.Default.PakPath.EndsWith(".manifest"))
                                trigger = $"{menuItem.PakFile.Directory.Replace('\\', '/')}/{menuItem.PakFile.FileName}";
                            else
                                trigger = $"{Properties.Settings.Default.PakPath[Properties.Settings.Default.PakPath.LastIndexOf(Folders.GetGameName(), StringComparison.Ordinal)..].Replace("\\", "/")}/{menuItem.PakFile.FileName}";
                        }
                        if (dynamicKeys.TryGetValue(Globals.Game.ActualGame.ToString(), out var gameDict) && gameDict.TryGetValue(trigger, out var key))
                        {
                            string dKey = key.StartsWith("0x") ? key[2..].ToUpperInvariant() : key.ToUpperInvariant();
                            try
                            {
                                // i can use TestAesKey here but that means it's gonna test here then right after to set the key
                                // so a try catch when setting the key seems better
                                menuItem.PakFile.AesKey = dKey.Trim().ToBytesKey();
                            }
                            catch (System.Exception e)
                            {
                                StatusBarVm.statusBarViewModel.Set(e.Message, Properties.Resources.Error);
                                FConsole.AppendText(string.Format(Properties.Resources.DynamicKeyNotWorking, $"0x{dKey}", menuItem.PakFile.FileName), FColors.Red, true);
                                DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[AES]", $"0x{dKey} is NOT!!!! working with {menuItem.PakFile.FileName}");
                            }
                        }

                        menuItem.IsEnabled = menuItem.PakFile.AesKey != null || !menuItem.PakFile.Info.bEncryptedIndex;
                    }

                    MenuItems.pakFiles[1].IsEnabled = MenuItems.pakFiles.AtLeastOnePakWithKey();
                }
            }
        }
    }
}