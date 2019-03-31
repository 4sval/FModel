using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FModel
{
    public partial class UpdateModeOption : Form
    {
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        public Image SetImageOpacity(Image image, float opacity)
        {
            try
            {
                //create a Bitmap the size of the image provided  
                Bitmap bmp = new Bitmap(image.Width, image.Height);

                //create a graphics object from the image  
                using (Graphics gfx = Graphics.FromImage(bmp))
                {

                    //create a color matrix object  
                    ColorMatrix matrix = new ColorMatrix();

                    //set the opacity  
                    matrix.Matrix33 = opacity;

                    //create image attributes  
                    ImageAttributes attributes = new ImageAttributes();

                    //set the color(opacity) of the image  
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    //now draw the image  
                    gfx.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                }
                return bmp;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public UpdateModeOption()
        {
            InitializeComponent();

            checkBox2.Checked = Properties.Settings.Default.UMCosmetics;
            checkBox5.Checked = Properties.Settings.Default.UMVariants;
            checkBox3.Checked = Properties.Settings.Default.UMConsumablesWeapons;
            checkBox4.Checked = Properties.Settings.Default.UMTraps;
            checkBox6.Checked = Properties.Settings.Default.UMChallenges;

            checkBox8.Checked = Properties.Settings.Default.UMFeatured;
            checkBox7.Checked = Properties.Settings.Default.UMWatermark;

            trackBar2.Value = Properties.Settings.Default.UMSize;
            trackBar1.Value = Properties.Settings.Default.UMOpacity;

            checkBox9.Checked = Properties.Settings.Default.UMTCosmeticsVariants;
            checkBox14.Checked = Properties.Settings.Default.UMTLoading;
            checkBox1.Checked = Properties.Settings.Default.UMTWeapons;
            checkBox10.Checked = Properties.Settings.Default.UMTBanners;
            checkBox11.Checked = Properties.Settings.Default.UMTFeaturedIMGs;
            checkBox12.Checked = Properties.Settings.Default.UMTAthena;
            checkBox13.Checked = Properties.Settings.Default.UMTDevices;
            checkBox15.Checked = Properties.Settings.Default.UMTVehicles;

            button1.Enabled = Properties.Settings.Default.UMWatermark;
            trackBar1.Enabled = Properties.Settings.Default.UMWatermark;
            trackBar2.Enabled = Properties.Settings.Default.UMWatermark;

            if (Properties.Settings.Default.UMFeatured == false)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
                {
                    filenameLabel.Text = "File Name: " + Path.GetFileName(Properties.Settings.Default.UMFilename);

                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplate);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)Properties.Settings.Default.UMOpacity / 100);
                    g.DrawImage(ResizeImage(opacityImage, Properties.Settings.Default.UMSize, Properties.Settings.Default.UMSize), (522 - Properties.Settings.Default.UMSize) / 2, (522 - Properties.Settings.Default.UMSize) / 2, Properties.Settings.Default.UMSize, Properties.Settings.Default.UMSize);

                    wPictureBox.Image = bmp;
                }
            }
            if (Properties.Settings.Default.UMFeatured == true)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
                {
                    filenameLabel.Text = "File Name: " + Path.GetFileName(Properties.Settings.Default.UMFilename);

                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplateF);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)Properties.Settings.Default.UMOpacity / 100);
                    g.DrawImage(ResizeImage(opacityImage, Properties.Settings.Default.UMSize, Properties.Settings.Default.UMSize), (522 - Properties.Settings.Default.UMSize) / 2, (522 - Properties.Settings.Default.UMSize) / 2, Properties.Settings.Default.UMSize, Properties.Settings.Default.UMSize);

                    wPictureBox.Image = bmp;
                }
            }
        }

        private void optionsOKButton_Click(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true)
                Properties.Settings.Default.UMCosmetics = true;
            if (checkBox2.Checked == false)
                Properties.Settings.Default.UMCosmetics = false;
            if (checkBox5.Checked == true)
                Properties.Settings.Default.UMVariants = true;
            if (checkBox5.Checked == false)
                Properties.Settings.Default.UMVariants = false;
            if (checkBox3.Checked == true)
                Properties.Settings.Default.UMConsumablesWeapons = true;
            if (checkBox3.Checked == false)
                Properties.Settings.Default.UMConsumablesWeapons = false;
            if (checkBox4.Checked == true)
                Properties.Settings.Default.UMTraps = true;
            if (checkBox4.Checked == false)
                Properties.Settings.Default.UMTraps = false;
            if (checkBox6.Checked == true)
                Properties.Settings.Default.UMChallenges = true;
            if (checkBox6.Checked == false)
                Properties.Settings.Default.UMChallenges = false;
            if (checkBox8.Checked == true)
                Properties.Settings.Default.UMFeatured = true;
            if (checkBox8.Checked == false)
                Properties.Settings.Default.UMFeatured = false;
            if (checkBox7.Checked == true)
                Properties.Settings.Default.UMWatermark = true;
            if (checkBox7.Checked == false)
                Properties.Settings.Default.UMWatermark = false;
            if (checkBox9.Checked == true)
                Properties.Settings.Default.UMTCosmeticsVariants = true;
            if (checkBox9.Checked == false)
                Properties.Settings.Default.UMTCosmeticsVariants = false;
            if (checkBox14.Checked == true)
                Properties.Settings.Default.UMTLoading = true;
            if (checkBox14.Checked == false)
                Properties.Settings.Default.UMTLoading = false;
            if (checkBox1.Checked == true)
                Properties.Settings.Default.UMTWeapons = true;
            if (checkBox1.Checked == false)
                Properties.Settings.Default.UMTWeapons = false;
            if (checkBox10.Checked == true)
                Properties.Settings.Default.UMTBanners = true;
            if (checkBox10.Checked == false)
                Properties.Settings.Default.UMTBanners = false;
            if (checkBox11.Checked == true)
                Properties.Settings.Default.UMTFeaturedIMGs = true;
            if (checkBox11.Checked == false)
                Properties.Settings.Default.UMTFeaturedIMGs = false;
            if (checkBox12.Checked == true)
                Properties.Settings.Default.UMTAthena = true;
            if (checkBox12.Checked == false)
                Properties.Settings.Default.UMTAthena = false;
            if (checkBox13.Checked == true)
                Properties.Settings.Default.UMTDevices = true;
            if (checkBox13.Checked == false)
                Properties.Settings.Default.UMTDevices = false;
            if (checkBox15.Checked == true)
                Properties.Settings.Default.UMTVehicles = true;
            if (checkBox15.Checked == false)
                Properties.Settings.Default.UMTVehicles = false;

            Properties.Settings.Default.UMSize = trackBar2.Value;
            Properties.Settings.Default.UMOpacity = trackBar1.Value;

            Properties.Settings.Default.Save();
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = "Choose your watermark";
            theDialog.Multiselect = false;
            theDialog.Filter = "PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|DDS Files (*.dds)|*.dds|All Files (*.*)|*.*";

            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.UMFilename = theDialog.FileName;
                Properties.Settings.Default.Save();
                filenameLabel.Text = "File Name: " + Path.GetFileName(Properties.Settings.Default.UMFilename);

                if (checkBox8.Checked == false)
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
                    {
                        Bitmap bmp = new Bitmap(Properties.Resources.wTemplate);
                        Graphics g = Graphics.FromImage(bmp);

                        Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                        var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                        g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                        wPictureBox.Image = bmp;
                    }
                }
                if (checkBox8.Checked == true)
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
                    {
                        Bitmap bmp = new Bitmap(Properties.Resources.wTemplateF);
                        Graphics g = Graphics.FromImage(bmp);

                        Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                        var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                        g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                        wPictureBox.Image = bmp;
                    }
                }
            }
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked == false)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
                {
                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplate);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                    wPictureBox.Image = bmp;
                    wPictureBox.Refresh();
                }
            }
            if (checkBox8.Checked == true)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
                {
                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplateF);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                    wPictureBox.Image = bmp;
                    wPictureBox.Refresh();
                }
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked == false)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
                {
                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplate);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                    wPictureBox.Image = bmp;
                    wPictureBox.Refresh();
                }
            }
            if (checkBox8.Checked == true)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
                {
                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplateF);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                    wPictureBox.Image = bmp;
                    wPictureBox.Refresh();
                }
            }
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox7.Checked == false)
            {
                button1.Enabled = false;
                trackBar1.Enabled = false;
                trackBar2.Enabled = false;
            }
            if (checkBox7.Checked == true)
            {
                button1.Enabled = true;
                trackBar1.Enabled = true;
                trackBar2.Enabled = true;
            }
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked == false)
            {
                Bitmap bmp = new Bitmap(Properties.Resources.wTemplate);
                Graphics g = Graphics.FromImage(bmp);
                if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
                {
                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
                }
                wPictureBox.Image = bmp;
            }
            if (checkBox8.Checked == true)
            {
                Bitmap bmp = new Bitmap(Properties.Resources.wTemplateF);
                Graphics g = Graphics.FromImage(bmp);
                if (!string.IsNullOrEmpty(Properties.Settings.Default.UMFilename))
                {
                    Image watermark = Image.FromFile(Properties.Settings.Default.UMFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
                }
                wPictureBox.Image = bmp;
            }
        }

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
            panel1.Left = e.X;
            panel1.Top = e.Y;
        }

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
    }
}
