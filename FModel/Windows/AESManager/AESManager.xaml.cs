using FModel.Grabber.Aes;
using FModel.Logger;
using FModel.Utils;
using FModel.ViewModels.MenuItem;
using FModel.Windows.CustomNotifier;
using Newtonsoft.Json;
using PakReader.Pak;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FModel.Windows.AESManager
{
    /// <summary>
    /// Logique d'interaction pour AESManager.xaml
    /// </summary>
    public partial class AESManager : Window
    {
        public AESManager()
        {
            InitializeComponent();
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) => SaveAndExit();

        private void OnClick(object sender, RoutedEventArgs e) => Close();

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (MenuItems.pakFiles.AtLeastOnePak())
            {
                Dictionary<string, string> staticKeys = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(Properties.Settings.Default.StaticAesKeys))
                    staticKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(Properties.Settings.Default.StaticAesKeys);

                Dictionary<string, Dictionary<string, string>> dynamicAesKeys = new Dictionary<string, Dictionary<string, string>>();
                try
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.DynamicAesKeys))
                        dynamicAesKeys = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(Properties.Settings.Default.DynamicAesKeys);
                }
                catch (JsonSerializationException) { /* Needed for the transition bewteen global dynamic keys and "per game" dynamic keys */ }

                if (staticKeys.TryGetValue(Globals.Game.ActualGame.ToString(), out var sKey))
                {
                    StaticKey_TxtBox.Text = sKey;
                    DebugHelper.WriteLine("{0} {1} {2} {3} {4}", "[FModel]", "[Window]", "[AES Manager]", "[GET]", $"Main PAKs with key: {sKey}");
                }

                DynamicKeys_Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });
                DynamicKeys_Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                DynamicKeys_Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                DynamicKeys_Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                DynamicKeys_Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });

                int gridRow = 0;
                DynamicKeys_Grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Pixel) });
                foreach (PakFileReader pak in MenuItems.pakFiles.GetDynamicPakFileReaders())
                {
                    gridRow++;
                    string trigger;
                    {
                        if (Properties.Settings.Default.PakPath.EndsWith(".manifest"))
                            trigger = $"{pak.Directory.Replace('\\', '/')}/{pak.FileName}";
                        else
                            trigger = $"{Properties.Settings.Default.PakPath[Properties.Settings.Default.PakPath.LastIndexOf(Folders.GetGameName(), StringComparison.Ordinal)..].Replace("\\", "/")}/{pak.FileName}";
                    }
                    string key;
                    {
                        if (dynamicAesKeys.TryGetValue(Globals.Game.ActualGame.ToString(), out var gameDict) && gameDict.TryGetValue(trigger, out var dKey))
                            key = dKey;
                        else
                            key = "";
                    }
                    DebugHelper.WriteLine("{0} {1} {2} {3} {4}", "[FModel]", "[Window]", "[AES Manager]", "[GET]", $"{pak.FileName} with key: {key}");

                    DynamicKeys_Grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    Label label = new Label
                    {
                        Content = pak.FileName[0..^4].Replace("_", "__"), //name less ".pak"
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    TextBox textBox = new TextBox
                    {
                        Name = $"pakchunk{Regex.Match(pak.FileName[0..^4], @"\d+").Value}",
                        Text = key,
                        TextWrapping = TextWrapping.NoWrap,
                        VerticalAlignment = VerticalAlignment.Top,
                        Foreground = new SolidColorBrush(Color.FromRgb(239, 239, 239)),
                        Margin = new Thickness(5, 3, 5, 0)
                    };

                    label.SetValue(Grid.RowProperty, gridRow);
                    label.SetValue(Grid.ColumnProperty, 1);
                    textBox.SetValue(Grid.RowProperty, gridRow);
                    textBox.SetValue(Grid.ColumnProperty, 2);
                    DynamicKeys_Grid.Children.Add(label);
                    DynamicKeys_Grid.Children.Add(textBox);
                }
                DynamicKeys_Grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Pixel) });
            }
        }

        private void SaveAndExit()
        {
            Dictionary<string, string> staticKeys = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(Properties.Settings.Default.StaticAesKeys))
                staticKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(Properties.Settings.Default.StaticAesKeys);

            staticKeys[Globals.Game.ActualGame.ToString()] = StaticKey_TxtBox.Text;
            DebugHelper.WriteLine("{0} {1} {2} {3} {4}", "[FModel]", "[Window]", "[AES Manager]", "[SET]", $"Main PAKs with key: {StaticKey_TxtBox.Text}");

            Dictionary<string, Dictionary<string, string>> dynamicAesKeys = new Dictionary<string, Dictionary<string, string>>();
            try
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.DynamicAesKeys))
                    dynamicAesKeys = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(Properties.Settings.Default.DynamicAesKeys);
            }
            catch (JsonSerializationException) { /* Needed for the transition bewteen global dynamic keys and "per game" dynamic keys */ }

            dynamicAesKeys[Globals.Game.ActualGame.ToString()] = new Dictionary<string, string>();
            foreach (PakFileReader pak in MenuItems.pakFiles.GetDynamicPakFileReaders())
            {
                string trigger;
                {
                    if (Properties.Settings.Default.PakPath.EndsWith(".manifest"))
                        trigger = $"{pak.Directory.Replace('\\', '/')}/{pak.FileName}";
                    else
                        trigger = $"{Properties.Settings.Default.PakPath[Properties.Settings.Default.PakPath.LastIndexOf(Folders.GetGameName(), StringComparison.Ordinal)..].Replace("\\", "/")}/{pak.FileName}";
                }
                TextBox textBox = DependencyObjects.FindChild<TextBox>(this, $"pakchunk{Regex.Match(pak.FileName[0..^4], @"\d+").Value}");
                if (!string.IsNullOrEmpty(textBox.Text))
                {
                    dynamicAesKeys[Globals.Game.ActualGame.ToString()][trigger] = textBox.Text;
                    DebugHelper.WriteLine("{0} {1} {2} {3} {4}", "[FModel]", "[Window]", "[AES Manager]", "[SET]", $"{pak.FileName} with key: {textBox.Text}");
                }
            }

            Properties.Settings.Default.StaticAesKeys = JsonConvert.SerializeObject(staticKeys, Formatting.None);
            Properties.Settings.Default.DynamicAesKeys = JsonConvert.SerializeObject(dynamicAesKeys, Formatting.None);
            Properties.Settings.Default.Save();
            DebugHelper.WriteLine("{0} {1} {2}", "[FModel]", "[Window]", "Closing AES Manager");

            Keys.NoKeyGoodBye();
        }

        private async void RefreshOnClick(object sender, RoutedEventArgs e)
        {
            if (Globals.Game.ActualGame == EGame.Fortnite)
            {
                bool success = await AesGrabber.Load(true).ConfigureAwait(false);
                if (success)
                {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        Dictionary<string, string> staticKeys = new Dictionary<string, string>();
                        if (!string.IsNullOrEmpty(Properties.Settings.Default.StaticAesKeys))
                            staticKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(Properties.Settings.Default.StaticAesKeys);

                        Dictionary<string, Dictionary<string, string>> dynamicAesKeys = new Dictionary<string, Dictionary<string, string>>();
                        try
                        {
                            if (!string.IsNullOrEmpty(Properties.Settings.Default.DynamicAesKeys))
                                dynamicAesKeys = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(Properties.Settings.Default.DynamicAesKeys);
                        }
                        catch (JsonSerializationException) { /* Needed for the transition bewteen global dynamic keys and "per game" dynamic keys */ }

                        if (staticKeys.TryGetValue(Globals.Game.ActualGame.ToString(), out var sKey))
                        {
                            StaticKey_TxtBox.Text = sKey;
                            DebugHelper.WriteLine("{0} {1} {2} {3} {4}", "[FModel]", "[Window]", "[AES Manager]", "[UPDATE]", $"Main PAKs with key: {sKey}");
                        }

                        foreach (PakFileReader pak in MenuItems.pakFiles.GetDynamicPakFileReaders())
                        {
                            string trigger;
                            {
                                if (Properties.Settings.Default.PakPath.EndsWith(".manifest"))
                                    trigger = $"{pak.Directory.Replace('\\', '/')}/{pak.FileName}";
                                else
                                    trigger = $"{Properties.Settings.Default.PakPath[Properties.Settings.Default.PakPath.LastIndexOf(Folders.GetGameName(), StringComparison.Ordinal)..].Replace("\\", "/")}/{pak.FileName}";
                            }
                            string key;
                            {
                                if (dynamicAesKeys.TryGetValue(Globals.Game.ActualGame.ToString(), out var gameDict) && gameDict.TryGetValue(trigger, out var dKey))
                                    key = dKey;
                                else
                                    key = "";
                            }
                            DebugHelper.WriteLine("{0} {1} {2} {3} {4}", "[FModel]", "[Window]", "[AES Manager]", "[UPDATE]", $"{pak.FileName} with key: {key}");

                            TextBox textBox = DependencyObjects.FindChild<TextBox>(this, $"pakchunk{Regex.Match(pak.FileName[0..^4], @"\d+").Value}");
                            if (textBox != null)
                                textBox.Text = key;
                        }
                    });

                    Globals.gNotifier.ShowCustomMessage(Properties.Resources.AES, Properties.Resources.AesKeysUpdated, "/FModel;component/Resources/api.ico");
                }
            }
        }
    }
}