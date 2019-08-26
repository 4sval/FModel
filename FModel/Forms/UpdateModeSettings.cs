using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using FModel.Properties;

namespace FModel.Forms
{
    public partial class UpdateModeSettings : Form
    {
        private static string _oldLanguage;
        private static string[] parameters { get; set; }

        public UpdateModeSettings()
        {
            InitializeComponent();

            // Check if watermark exists
            Utilities.CheckWatermark();

            if (!string.IsNullOrEmpty(Properties.Settings.Default.UMParameters))
            {
                parameters = Properties.Settings.Default.UMParameters.Split(',');
                setCheckListBoxItemChecked(parameters);
            }

            comboBox2.SelectedIndex = comboBox2.FindStringExact(Properties.Settings.Default.rarityDesign);

            //WATERMARK
            button1.Enabled = Properties.Settings.Default.UMWatermark;
            checkBox7.Checked = Properties.Settings.Default.UMWatermark;
            trackBar1.Enabled = Properties.Settings.Default.UMWatermark;
            trackBar2.Enabled = Properties.Settings.Default.UMWatermark;
            trackBar1.Value = Properties.Settings.Default.UMOpacity;
            trackBar2.Value = Properties.Settings.Default.UMSize;

            //FEATURED
            checkBox8.Checked = Properties.Settings.Default.UMFeatured;
            if (File.Exists(Properties.Settings.Default.UMFilename))
            {
                filenameLabel.Text = @"File Name: " + Path.GetFileName(Properties.Settings.Default.UMFilename);

                Bitmap bmp = null;
                if (Properties.Settings.Default.UMFeatured)
                {
                    bmp = new Bitmap(string.Equals(Properties.Settings.Default.rarityDesign, "Flat") ? new Bitmap(Resources.Template_F_F) : string.Equals(Properties.Settings.Default.rarityDesign, "Minimalist") ? new Bitmap(Resources.Template_M_F) : new Bitmap(Resources.Template_D_F));
                }
                else
                {
                    bmp = new Bitmap(string.Equals(Properties.Settings.Default.rarityDesign, "Flat") ? new Bitmap(Resources.Template_F_N) : string.Equals(Properties.Settings.Default.rarityDesign, "Minimalist") ? new Bitmap(Resources.Template_M_N) : new Bitmap(Resources.Template_D_N));
                }
                Graphics g = Graphics.FromImage(bmp);

                Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)Properties.Settings.Default.UMOpacity / 100);
                g.DrawImage(ImageUtilities.ResizeImage(opacityImage, Properties.Settings.Default.UMSize, Properties.Settings.Default.UMSize), (522 - Properties.Settings.Default.UMSize) / 2, (522 - Properties.Settings.Default.UMSize) / 2, Properties.Settings.Default.UMSize, Properties.Settings.Default.UMSize);

                wPictureBox.Image = bmp;
            }

            _oldLanguage = Properties.Settings.Default.IconLanguage;
            comboBox1.SelectedIndex = comboBox1.FindStringExact(Properties.Settings.Default.IconLanguage);
        }

        private void optionsOKButton_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            RegisterSettings.updateModeListParameters = new List<string>();

            Properties.Settings.Default.UMFeatured              = checkBox8.Checked;
            Properties.Settings.Default.UMWatermark             = checkBox7.Checked;
            Properties.Settings.Default.rarityDesign            = comboBox2.SelectedItem.ToString();

            Properties.Settings.Default.UMSize                  = trackBar2.Value;
            Properties.Settings.Default.UMOpacity               = trackBar1.Value;

            //PARAMETERS
            if (checkedListBox1.GetItemCheckState(0) == CheckState.Checked) { sb.Append("BRCosmetics,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Athena/Items/Cosmetics/"); }
            if (checkedListBox1.GetItemCheckState(1) == CheckState.Checked) { sb.Append("BRVariants,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Athena/Items/CosmeticVariantTokens/"); }
            if (checkedListBox1.GetItemCheckState(2) == CheckState.Checked) { sb.Append("BRBanners,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Athena/Items/BannerToken/"); }
            if (checkedListBox1.GetItemCheckState(3) == CheckState.Checked) { sb.Append("BRChallenges,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Athena/Items/ChallengeBundles/"); }
            if (checkedListBox1.GetItemCheckState(4) == CheckState.Checked) { sb.Append("BRConsumables,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Athena/Items/Consumables/"); }
            if (checkedListBox1.GetItemCheckState(5) == CheckState.Checked) { sb.Append("BRGadgets,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Athena/Items/Gameplay/"); }
            if (checkedListBox1.GetItemCheckState(6) == CheckState.Checked) { sb.Append("BRTraps,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Athena/Items/Traps/"); }
            if (checkedListBox1.GetItemCheckState(7) == CheckState.Checked) { sb.Append("BRWeapons,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Athena/Items/Weapons/"); }
            if (checkedListBox1.GetItemCheckState(8) == CheckState.Checked) { sb.Append("STWHeros,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Heroes/"); }
            if (checkedListBox1.GetItemCheckState(9) == CheckState.Checked) { sb.Append("STWDefenders,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Items/Defenders/"); }
            if (checkedListBox1.GetItemCheckState(10) == CheckState.Checked) { sb.Append("STWWorkers,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Items/Workers/"); }
            if (checkedListBox1.GetItemCheckState(11) == CheckState.Checked) { sb.Append("STWSchematics,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Items/Schematics/"); }
            if (checkedListBox1.GetItemCheckState(12) == CheckState.Checked) { sb.Append("STWTraps,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Items/Traps/"); }
            if (checkedListBox1.GetItemCheckState(13) == CheckState.Checked) { sb.Append("STWWeapons,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Items/Weapons/"); }
            if (checkedListBox1.GetItemCheckState(14) == CheckState.Checked) { sb.Append("STWIngredients,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Items/Ingredients/"); }
            if (checkedListBox1.GetItemCheckState(15) == CheckState.Checked) { sb.Append("STWResources,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Items/PersistentResources/"); }
            if (checkedListBox1.GetItemCheckState(16) == CheckState.Checked) { sb.Append("STWCardpacks,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Items/CardPacks/"); }
            if (checkedListBox1.GetItemCheckState(17) == CheckState.Checked) { sb.Append("Tokens,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/Items/Tokens/"); }

            if (checkedListBox2.GetItemCheckState(0) == CheckState.Checked) { sb.Append("T2DAssets,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/2dAssets/"); }
            if (checkedListBox2.GetItemCheckState(1) == CheckState.Checked) { sb.Append("TFeatured,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/UI/Foundation/Textures/BattleRoyale/FeaturedItems/"); }
            if (checkedListBox2.GetItemCheckState(2) == CheckState.Checked) { sb.Append("TIcons,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/UI/Foundation/Textures/Icons/"); }
            if (checkedListBox2.GetItemCheckState(3) == CheckState.Checked) { sb.Append("TBanners,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/UI/Foundation/Textures/Banner/"); }
            if (checkedListBox2.GetItemCheckState(4) == CheckState.Checked) { sb.Append("TLoadingScreens,"); RegisterSettings.updateModeListParameters.Add("FortniteGame/Content/UI/Foundation/Textures/LoadingScreens/"); }

            Properties.Settings.Default.UMParameters = sb.ToString();

            //LOCRES
            Properties.Settings.Default.IconLanguage = comboBox1.SelectedItem.ToString();
            if (comboBox1.SelectedItem.ToString() != _oldLanguage)
            {
                LoadLocRes.LoadMySelectedLocRes(Properties.Settings.Default.IconLanguage);
            }

            Properties.Settings.Default.Save();
            Close();
        }

        private void setCheckListBoxItemChecked(string[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                switch (parameters[i])
                {
                    case "BRCosmetics":
                        checkedListBox1.SetItemChecked(0, true);
                        break;
                    case "BRVariants":
                        checkedListBox1.SetItemChecked(1, true);
                        break;
                    case "BRBanners":
                        checkedListBox1.SetItemChecked(2, true);
                        break;
                    case "BRChallenges":
                        checkedListBox1.SetItemChecked(3, true);
                        break;
                    case "BRConsumables":
                        checkedListBox1.SetItemChecked(4, true);
                        break;
                    case "BRGadgets":
                        checkedListBox1.SetItemChecked(5, true);
                        break;
                    case "BRTraps":
                        checkedListBox1.SetItemChecked(6, true);
                        break;
                    case "BRWeapons":
                        checkedListBox1.SetItemChecked(7, true);
                        break;
                    case "STWHeros":
                        checkedListBox1.SetItemChecked(8, true);
                        break;
                    case "STWDefenders":
                        checkedListBox1.SetItemChecked(9, true);
                        break;
                    case "STWWorkers":
                        checkedListBox1.SetItemChecked(10, true);
                        break;
                    case "STWSchematics":
                        checkedListBox1.SetItemChecked(11, true);
                        break;
                    case "STWTraps":
                        checkedListBox1.SetItemChecked(12, true);
                        break;
                    case "STWWeapons":
                        checkedListBox1.SetItemChecked(13, true);
                        break;
                    case "STWIngredients":
                        checkedListBox1.SetItemChecked(14, true);
                        break;
                    case "STWResources":
                        checkedListBox1.SetItemChecked(15, true);
                        break;
                    case "STWCardpacks":
                        checkedListBox1.SetItemChecked(16, true);
                        break;
                    case "Tokens":
                        checkedListBox1.SetItemChecked(17, true);
                        break;
                    case "T2DAssets":
                        checkedListBox2.SetItemChecked(0, true);
                        break;
                    case "TFeatured":
                        checkedListBox2.SetItemChecked(1, true);
                        break;
                    case "TIcons":
                        checkedListBox2.SetItemChecked(2, true);
                        break;
                    case "TBanners":
                        checkedListBox2.SetItemChecked(3, true);
                        break;
                    case "TLoadingScreens":
                        checkedListBox2.SetItemChecked(4, true);
                        break;
                    default:
                        break;
                }
            }
        }

        #region SELECT WATERMARK
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = @"Choose your watermark";
            theDialog.Multiselect = false;
            theDialog.Filter = @"PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|DDS Files (*.dds)|*.dds|All Files (*.*)|*.*";

            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.UMFilename = theDialog.FileName;
                Properties.Settings.Default.Save();
                filenameLabel.Text = @"File Name: " + Path.GetFileName(Properties.Settings.Default.UMFilename);

                if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
                {
                    Bitmap bmp = null;
                    if (Properties.Settings.Default.loadFeaturedImage)
                    {
                        bmp = new Bitmap(string.Equals(comboBox2.SelectedItem.ToString(), "Flat") ? new Bitmap(Resources.Template_F_F) : string.Equals(comboBox2.SelectedItem.ToString(), "Minimalist") ? new Bitmap(Resources.Template_M_F) : new Bitmap(Resources.Template_D_F));
                    }
                    else
                    {
                        bmp = new Bitmap(string.Equals(comboBox2.SelectedItem.ToString(), "Flat") ? new Bitmap(Resources.Template_F_N) : string.Equals(comboBox2.SelectedItem.ToString(), "Minimalist") ? new Bitmap(Resources.Template_M_N) : new Bitmap(Resources.Template_D_N));
                    }
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                    wPictureBox.Image = bmp;
                }
            }
        }
        #endregion

        #region RESIZE WATERMARK
        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
            {
                Bitmap bmp = null;
                if (checkBox8.Checked)
                {
                    bmp = new Bitmap(string.Equals(comboBox2.SelectedItem.ToString(), "Flat") ? new Bitmap(Resources.Template_F_F) : string.Equals(comboBox2.SelectedItem.ToString(), "Minimalist") ? new Bitmap(Resources.Template_M_F) : new Bitmap(Resources.Template_D_F));
                }
                else
                {
                    bmp = new Bitmap(string.Equals(comboBox2.SelectedItem.ToString(), "Flat") ? new Bitmap(Resources.Template_F_N) : string.Equals(comboBox2.SelectedItem.ToString(), "Minimalist") ? new Bitmap(Resources.Template_M_N) : new Bitmap(Resources.Template_D_N));
                }
                Graphics g = Graphics.FromImage(bmp);

                Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                wPictureBox.Image = bmp;
                wPictureBox.Refresh();
            }
        }
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
            {
                Bitmap bmp = null;
                if (checkBox8.Checked)
                {
                    bmp = new Bitmap(string.Equals(comboBox2.SelectedItem.ToString(), "Flat") ? new Bitmap(Resources.Template_F_F) : string.Equals(comboBox2.SelectedItem.ToString(), "Minimalist") ? new Bitmap(Resources.Template_M_F) : new Bitmap(Resources.Template_D_F));
                }
                else
                {
                    bmp = new Bitmap(string.Equals(comboBox2.SelectedItem.ToString(), "Flat") ? new Bitmap(Resources.Template_F_N) : string.Equals(comboBox2.SelectedItem.ToString(), "Minimalist") ? new Bitmap(Resources.Template_M_N) : new Bitmap(Resources.Template_D_N));
                }
                Graphics g = Graphics.FromImage(bmp);

                Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                wPictureBox.Image = bmp;
                wPictureBox.Refresh();
            }
        }
        #endregion

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            button1.Enabled     = checkBox7.Checked;
            trackBar1.Enabled   = checkBox7.Checked;
            trackBar2.Enabled   = checkBox7.Checked;
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox8.Checked)
            {
                Bitmap bmp = string.Equals(comboBox2.SelectedItem.ToString(), "Flat") ? new Bitmap(Resources.Template_F_N) : string.Equals(comboBox2.SelectedItem.ToString(), "Minimalist") ? new Bitmap(Resources.Template_M_N) : new Bitmap(Resources.Template_D_N);
                Graphics g = Graphics.FromImage(bmp);
                if (File.Exists(Properties.Settings.Default.UMFilename))
                {
                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
                }
                wPictureBox.Image = bmp;
            }
            if (checkBox8.Checked)
            {
                Bitmap bmp = string.Equals(comboBox2.SelectedItem.ToString(), "Flat") ? new Bitmap(Resources.Template_F_F) : string.Equals(comboBox2.SelectedItem.ToString(), "Minimalist") ? new Bitmap(Resources.Template_M_F) : new Bitmap(Resources.Template_D_F);
                Graphics g = Graphics.FromImage(bmp);
                if (File.Exists(Properties.Settings.Default.UMFilename))
                {
                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
                }
                wPictureBox.Image = bmp;
            }
        }

        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            Bitmap bmp = null;
            switch (comboBox2.SelectedItem.ToString())
            {
                case "Default":
                    bmp = checkBox8.Checked ? new Bitmap(Resources.Template_D_F) : new Bitmap(Resources.Template_D_N);
                    break;
                case "Flat":
                    bmp = checkBox8.Checked ? new Bitmap(Resources.Template_F_F) : new Bitmap(Resources.Template_F_N);
                    break;
                case "Minimalist":
                    bmp = checkBox8.Checked ? new Bitmap(Resources.Template_M_F) : new Bitmap(Resources.Template_M_N);
                    break;
            }

            Graphics g = Graphics.FromImage(bmp);
            if (File.Exists(Properties.Settings.Default.UMFilename))
            {
                Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
            }
            wPictureBox.Image = bmp;
        }
    }
}
