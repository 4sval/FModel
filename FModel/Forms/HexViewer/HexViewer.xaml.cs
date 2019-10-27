using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfHexaEditor.Core;
using WpfHexaEditor.Core.CharacterTable;
using WpfHexaEditor.Dialog;
using FModel.Properties;
using FProp = FModel.Properties.Settings;

namespace FModel.Forms.HexViewer
{
    /// <summary>
    /// Logique d'interaction pour HexViewer.xaml
    /// </summary>
    public partial class HexViewer : Window
    {
        private enum SettingEnum
        {
            HeaderVisibility,
            ReadOnly,
            ScrollVisibility,
            StatusBarVisibility
        }

        public HexViewer()
        {
            InitializeComponent();

            UpdateAllSettings();
        }

        private void OpenMenu_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            //fileDialog.InitialDirectory = FProp.Default.FOutput_Path + "\\Exports\\";

            if (fileDialog.ShowDialog() != null && File.Exists(fileDialog.FileName))
            {
                Application.Current.MainWindow.Cursor = Cursors.Wait;

                HexEdit.FileName = fileDialog.FileName;
                TabHexEditor.Header = Path.GetFileName(fileDialog.FileName);

                Application.Current.MainWindow.Cursor = null;
            }
        }

        private void SaveMenu_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Cursor = Cursors.Wait;
            //HexEdit.SaveTBLFile();
            HexEdit.SubmitChanges();
            Application.Current.MainWindow.Cursor = null;
        }

        private void CloseFileMenu_Click(object sender, RoutedEventArgs e) => HexEdit.CloseProvider();

        private void SetReadOnlyMenu_Click(object sender, RoutedEventArgs e) => UpdateSetting(SettingEnum.ReadOnly);

        private void ShowHeaderMenu_Click(object sender, RoutedEventArgs e) => UpdateSetting(SettingEnum.HeaderVisibility);

        private void StatusBarVisibility_Click(object sender, RoutedEventArgs e) => UpdateSetting(SettingEnum.StatusBarVisibility);

        private void UpdateSetting(SettingEnum setting)
        {
            switch (setting)
            {
                case SettingEnum.HeaderVisibility:
                    HexEdit.HeaderVisibility = !Settings.Default.HeaderVisibility
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                    Settings.Default.HeaderVisibility = HexEdit.HeaderVisibility == Visibility.Visible;
                    break;
                case SettingEnum.ReadOnly:
                    HexEdit.ReadOnlyMode = Settings.Default.ReadOnly;

                    HexEdit.ClearAllChange();
                    HexEdit.RefreshView();
                    break;
                case SettingEnum.StatusBarVisibility:
                    HexEdit.StatusBarVisibility = !Settings.Default.StatusBarVisibility
                        ? Visibility.Collapsed
                        : Visibility.Visible;

                    Settings.Default.StatusBarVisibility = HexEdit.StatusBarVisibility == Visibility.Visible;
                    break;
            }
        }

        private void UpdateAllSettings()
        {
            UpdateSetting(SettingEnum.HeaderVisibility);
            UpdateSetting(SettingEnum.ReadOnly);
            UpdateSetting(SettingEnum.ScrollVisibility);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            HexEdit.CloseProvider();
            Settings.Default.Save();
        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e) => Close();

        private void CopyHexaMenu_Click(object sender, RoutedEventArgs e) => HexEdit.CopyToClipboard(CopyPasteMode.HexaString);

        private void CopyStringMenu_Click(object sender, RoutedEventArgs e) => HexEdit.CopyToClipboard();

        private void DeleteSelectionMenu_Click(object sender, RoutedEventArgs e) => HexEdit.DeleteSelection();

        private void GOPosition_Click(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(PositionText.Text, out var position))
                HexEdit.SetPosition(position, 1);
            else
                MessageBox.Show("Enter long value.");

            ViewMenu.IsSubmenuOpen = false;
        }

        private void PositionText_TextChanged(object sender, TextChangedEventArgs e) =>
            GoPositionButton.IsEnabled = long.TryParse(PositionText.Text, out var _);

        private void UndoMenu_Click(object sender, RoutedEventArgs e) => HexEdit.Undo();

        private void RedoMenu_Click(object sender, RoutedEventArgs e) => HexEdit.Redo();

        private void SetBookMarkButton_Click(object sender, RoutedEventArgs e) => HexEdit.SetBookMark();

        private void DeleteBookmark_Click(object sender, RoutedEventArgs e) => HexEdit.ClearScrollMarker(ScrollMarker.Bookmark);

        private void FindAllSelection_Click(object sender, RoutedEventArgs e) => HexEdit.FindAllSelection(true);

        private void SelectAllButton_Click(object sender, RoutedEventArgs e) => HexEdit.SelectAll();

        private void CTableASCIIButton_Click(object sender, RoutedEventArgs e)
        {
            HexEdit.TypeOfCharacterTable = CharacterTableType.Ascii;
            CTableAsciiButton.IsChecked = true;
            CTableTblButton.IsChecked = false;
            CTableTblDefaultAsciiButton.IsChecked = false;
        }

        private void CTableTBLButton_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();

            if (fileDialog.ShowDialog() != null)
            {
                if (File.Exists(fileDialog.FileName))
                {
                    Application.Current.MainWindow.Cursor = Cursors.Wait;

                    HexEdit.LoadTblFile(fileDialog.FileName);
                    HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile;
                    CTableAsciiButton.IsChecked = false;
                    CTableTblButton.IsChecked = true;
                    CTableTblDefaultAsciiButton.IsChecked = false;

                    Application.Current.MainWindow.Cursor = null;
                }
            }
        }

        private void CTableTBLDefaultASCIIButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Cursor = Cursors.Wait;

            HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile;
            HexEdit.LoadDefaultTbl();

            Application.Current.MainWindow.Cursor = null;
        }

        private void SaveAsMenu_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new SaveFileDialog();

            if (fileDialog.ShowDialog() != null)
                HexEdit.SubmitChanges(fileDialog.FileName, true);
        }

        private void CTableTblDefaultEBCDICButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Cursor = Cursors.Wait;

            HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile;
            HexEdit.LoadDefaultTbl(DefaultCharacterTableType.EbcdicWithSpecialChar);

            Application.Current.MainWindow.Cursor = null;
        }

        private void CTableTblDefaultEBCDICNoSPButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Cursor = Cursors.Wait;

            HexEdit.TypeOfCharacterTable = CharacterTableType.TblFile;
            HexEdit.LoadDefaultTbl(DefaultCharacterTableType.EbcdicNoSpecialChar);

            Application.Current.MainWindow.Cursor = null;
        }

        private void FindMenu_Click(object sender, RoutedEventArgs e) =>
            new FindWindow(HexEdit, HexEdit.SelectionByteArray)
            {
                Owner = this
            }.Show();

        private void ReplaceMenu_Click(object sender, RoutedEventArgs e) =>
            new FindReplaceWindow(HexEdit, HexEdit.SelectionByteArray)
            {
                Owner = this
            }.Show();
    }
}
