using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FModel.Properties;

namespace FModel.Forms
{
    public partial class UpdateModeSettings : Form
    {
        public UpdateModeSettings()
        {
            InitializeComponent();

            //ICON CREATION
            checkBox2.Checked = Properties.Settings.Default.UMCosmetics;
            checkBox5.Checked = Properties.Settings.Default.UMVariants;
            checkBox3.Checked = Properties.Settings.Default.UMConsumablesWeapons;
            checkBox4.Checked = Properties.Settings.Default.UMTraps;
            checkBox6.Checked = Properties.Settings.Default.UMChallenges;

            //FEATURED
            checkBox8.Checked = Properties.Settings.Default.UMFeatured;
            if (File.Exists(Properties.Settings.Default.UMFilename))
            {
                filenameLabel.Text = @"File Name: " + Path.GetFileName(Properties.Settings.Default.UMFilename);

                Bitmap bmp = new Bitmap(checkBox8.Checked ? Resources.wTemplateF : Resources.wTemplate);
                Graphics g = Graphics.FromImage(bmp);

                Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)Properties.Settings.Default.UMOpacity / 100);
                g.DrawImage(ImageUtilities.ResizeImage(opacityImage, Properties.Settings.Default.UMSize, Properties.Settings.Default.UMSize), (522 - Properties.Settings.Default.UMSize) / 2, (522 - Properties.Settings.Default.UMSize) / 2, Properties.Settings.Default.UMSize, Properties.Settings.Default.UMSize);

                wPictureBox.Image = bmp;
            }

            //WATERMARK
            button1.Enabled = Properties.Settings.Default.UMWatermark;
            trackBar1.Enabled = Properties.Settings.Default.UMWatermark;
            trackBar2.Enabled = Properties.Settings.Default.UMWatermark;
            trackBar2.Value = Properties.Settings.Default.UMSize;
            trackBar1.Value = Properties.Settings.Default.UMOpacity;
            checkBox7.Checked = Properties.Settings.Default.UMWatermark;

            //TEXTURES
            checkBox9.Checked = Properties.Settings.Default.UMTCosmeticsVariants;
            checkBox14.Checked = Properties.Settings.Default.UMTLoading;
            checkBox1.Checked = Properties.Settings.Default.UMTWeapons;
            checkBox10.Checked = Properties.Settings.Default.UMTBanners;
            checkBox11.Checked = Properties.Settings.Default.UMTFeaturedIMGs;
            checkBox12.Checked = Properties.Settings.Default.UMTAthena;
            checkBox13.Checked = Properties.Settings.Default.UMTDevices;
            checkBox15.Checked = Properties.Settings.Default.UMTVehicles;
            checkBoxUMCTGalleries.Checked = Properties.Settings.Default.UMCTGalleries;
        }

        private void optionsOKButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.UMCosmetics             = checkBox2.Checked;
            Properties.Settings.Default.UMVariants              = checkBox5.Checked;
            Properties.Settings.Default.UMConsumablesWeapons    = checkBox3.Checked;
            Properties.Settings.Default.UMTraps                 = checkBox4.Checked;
            Properties.Settings.Default.UMChallenges            = checkBox6.Checked;
            Properties.Settings.Default.UMFeatured              = checkBox8.Checked;
            Properties.Settings.Default.UMWatermark             = checkBox7.Checked;
            Properties.Settings.Default.UMTCosmeticsVariants    = checkBox9.Checked;
            Properties.Settings.Default.UMTLoading              = checkBox14.Checked;
            Properties.Settings.Default.UMTWeapons              = checkBox1.Checked;
            Properties.Settings.Default.UMTBanners              = checkBox10.Checked;
            Properties.Settings.Default.UMTFeaturedIMGs         = checkBox11.Checked;
            Properties.Settings.Default.UMTAthena               = checkBox12.Checked;
            Properties.Settings.Default.UMTDevices              = checkBox13.Checked;
            Properties.Settings.Default.UMTVehicles             = checkBox15.Checked;
            Properties.Settings.Default.UMCTGalleries           = checkBoxUMCTGalleries.Checked;

            Properties.Settings.Default.UMSize                  = trackBar2.Value;
            Properties.Settings.Default.UMOpacity               = trackBar1.Value;

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
            Bitmap bmp = new Bitmap(checkBox8.Checked ? Resources.wTemplateF : Resources.wTemplate);
            Graphics g = Graphics.FromImage(bmp);
            if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
            {
                Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
            }

            wPictureBox.Image = bmp;
        }

        #region MOVE PANEL
        //CONSUMABLES & WEAPONS
        private void checkBox3_MouseEnter(object sender, EventArgs e)
        {
            panel1.Visible = true;
        }
        private void checkBox3_MouseLeave(object sender, EventArgs e)
        {
            panel1.Visible = false;
        }
        private void checkBox3_MouseMove(object sender, MouseEventArgs e)
        {
            panel1.Left = e.X + 40;
            panel1.Top = e.Y + 5;
        }

        //2D ASSETS
        private void checkBox10_MouseEnter(object sender, EventArgs e)
        {
            panel2.Visible = true;
        }
        private void checkBox10_MouseLeave(object sender, EventArgs e)
        {
            panel2.Visible = false;
        }
        private void checkBox10_MouseMove(object sender, MouseEventArgs e)
        {
            panel2.Left = e.X + 225;
            panel2.Top = e.Y + 225;
        }
        #endregion
    }
}
