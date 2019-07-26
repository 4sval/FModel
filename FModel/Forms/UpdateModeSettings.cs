using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FModel.Properties;

namespace FModel.Forms
{
    public partial class UpdateModeSettings : Form
    {
        private static string _oldLanguage;

        public UpdateModeSettings()
        {
            // Check if watermark exists
            Utilities.CheckWatermark();

            InitializeComponent();

            checkBox1.Checked = Properties.Settings.Default.rarityNew;

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
                    bmp = new Bitmap(Properties.Settings.Default.rarityNew ? new Bitmap(Resources.wTemplateF) : new Bitmap(Resources.wTemplateFv1));
                }
                else
                {
                    bmp = new Bitmap(Properties.Settings.Default.rarityNew ? new Bitmap(Resources.wTemplate) : new Bitmap(Resources.wTemplatev1));
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
            RegisterSettings.updateModeListParameters = new List<string>();

            Properties.Settings.Default.UMFeatured              = checkBox8.Checked;
            Properties.Settings.Default.UMWatermark             = checkBox7.Checked;
            Properties.Settings.Default.rarityNew               = checkBox1.Checked;

            Properties.Settings.Default.UMSize                  = trackBar2.Value;
            Properties.Settings.Default.UMOpacity               = trackBar1.Value;

            //PARAMETERS
            if (checkedListBox1.GetItemCheckState(0) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Athena/Items/Cosmetics/"); }
            if (checkedListBox1.GetItemCheckState(1) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Athena/Items/CosmeticVariantTokens/"); }
            if (checkedListBox1.GetItemCheckState(2) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Athena/Items/BannerToken/"); }
            if (checkedListBox1.GetItemCheckState(3) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Athena/Items/ChallengeBundles/"); }
            if (checkedListBox1.GetItemCheckState(4) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Athena/Items/Consumables/"); }
            if (checkedListBox1.GetItemCheckState(5) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Athena/Items/Gameplay/"); }
            if (checkedListBox1.GetItemCheckState(6) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Athena/Items/Traps/"); }
            if (checkedListBox1.GetItemCheckState(7) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Athena/Items/Weapons/"); }
            if (checkedListBox1.GetItemCheckState(8) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Heroes/"); }
            if (checkedListBox1.GetItemCheckState(9) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Items/Defenders/"); }
            if (checkedListBox1.GetItemCheckState(10) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Items/Workers/"); }
            if (checkedListBox1.GetItemCheckState(11) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Items/Traps/"); }
            if (checkedListBox1.GetItemCheckState(12) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Items/Weapons/"); }
            if (checkedListBox1.GetItemCheckState(13) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Items/Ingredients/"); }
            if (checkedListBox1.GetItemCheckState(14) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Items/PersistentResources/"); }
            if (checkedListBox1.GetItemCheckState(15) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Items/CardPacks/"); }
            if (checkedListBox1.GetItemCheckState(16) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/Items/Tokens/"); }

            if (checkedListBox2.GetItemCheckState(0) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/2dAssets/"); }
            if (checkedListBox2.GetItemCheckState(1) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/UI/Foundation/Textures/BattleRoyale/FeaturedItems/"); }
            if (checkedListBox2.GetItemCheckState(2) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/UI/Foundation/Textures/Icons/"); }
            if (checkedListBox2.GetItemCheckState(3) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/UI/Foundation/Textures/Banner/"); }
            if (checkedListBox2.GetItemCheckState(4) == CheckState.Checked) { RegisterSettings.updateModeListParameters.Add("../FortniteGame/Content/UI/Foundation/Textures/LoadingScreens/"); }

            //LOCRES
            Properties.Settings.Default.IconLanguage = comboBox1.SelectedItem.ToString();
            if (comboBox1.SelectedItem.ToString() != _oldLanguage)
            {
                LoadLocRes.LoadMySelectedLocRes(Properties.Settings.Default.IconLanguage);
            }

            Properties.Settings.Default.Save();
            Close();
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
                    Bitmap bmp = new Bitmap(checkBox8.Checked ? Resources.wTemplateF : Resources.wTemplate);
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
                Bitmap bmp = new Bitmap(checkBox8.Checked ? Resources.wTemplateF : Resources.wTemplate);
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
                Bitmap bmp = new Bitmap(checkBox8.Checked ? Resources.wTemplateF : Resources.wTemplate);
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
                Bitmap bmp = checkBox1.Checked ? new Bitmap(Resources.wTemplate) : new Bitmap(Resources.wTemplatev1);
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
                Bitmap bmp = checkBox1.Checked ? new Bitmap(Resources.wTemplateF) : new Bitmap(Resources.wTemplateFv1);
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

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                Bitmap bmp = checkBox8.Checked ? new Bitmap(Resources.wTemplateFv1) : new Bitmap(Resources.wTemplatev1);
                Graphics g = Graphics.FromImage(bmp);
                if (File.Exists(Properties.Settings.Default.UMFilename))
                {
                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
                }
                wPictureBox.Image = bmp;
            }
            if (checkBox1.Checked)
            {
                Bitmap bmp = checkBox8.Checked ? new Bitmap(Resources.wTemplateF) : new Bitmap(Resources.wTemplate);
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
}
