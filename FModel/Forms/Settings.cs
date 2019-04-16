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

namespace FModel.Forms
{
    public partial class Settings : Form
    {
        private static string PAKsPathBefore;
        private static string OutputPathBefore;

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
        public static Image SetImageOpacity(Image image, float opacity)
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

        public Settings()
        {
            InitializeComponent();

            textBox2.Text = Properties.Settings.Default.PAKsPath;
            textBox1.Text = Properties.Settings.Default.ExtractOutput;

            //ICON CREATION
            checkBox2.Checked = Properties.Settings.Default.createIconForCosmetics;
            checkBox5.Checked = Properties.Settings.Default.createIconForVariants;
            checkBox3.Checked = Properties.Settings.Default.createIconForConsumablesWeapons;
            checkBox4.Checked = Properties.Settings.Default.createIconForTraps;
            checkBox6.Checked = Properties.Settings.Default.createIconForChallenges;

            //MERGER
            textBox3.Text = Properties.Settings.Default.mergerFileName;
            imgsPerRow.Value = Properties.Settings.Default.mergerImagesRow;

            //WATERMARK
            button1.Enabled = Properties.Settings.Default.isWatermark;
            checkBox7.Checked = Properties.Settings.Default.isWatermark;
            trackBar1.Enabled = Properties.Settings.Default.isWatermark;
            trackBar2.Enabled = Properties.Settings.Default.isWatermark;
            trackBar1.Value = Properties.Settings.Default.wOpacity;
            trackBar2.Value = Properties.Settings.Default.wSize;

            //FEATURED
            checkBox8.Checked = Properties.Settings.Default.loadFeaturedImage;
            if (Properties.Settings.Default.loadFeaturedImage == false)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                {
                    filenameLabel.Text = "File Name: " + Path.GetFileName(Properties.Settings.Default.wFilename);

                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplate);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)Properties.Settings.Default.wOpacity / 100);
                    g.DrawImage(ResizeImage(opacityImage, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize), (522 - Properties.Settings.Default.wSize) / 2, (522 - Properties.Settings.Default.wSize) / 2, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize);

                    wPictureBox.Image = bmp;
                }
            }
            if (Properties.Settings.Default.loadFeaturedImage == true)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                {
                    filenameLabel.Text = "File Name: " + Path.GetFileName(Properties.Settings.Default.wFilename);

                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplateF);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)Properties.Settings.Default.wOpacity / 100);
                    g.DrawImage(ResizeImage(opacityImage, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize), (522 - Properties.Settings.Default.wSize) / 2, (522 - Properties.Settings.Default.wSize) / 2, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize);

                    wPictureBox.Image = bmp;
                }
            }

            PAKsPathBefore = Properties.Settings.Default.PAKsPath;
            OutputPathBefore = Properties.Settings.Default.ExtractOutput;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            //INPUT
            Properties.Settings.Default.PAKsPath = textBox2.Text; //SET
            string PAKsPathAfter = Properties.Settings.Default.PAKsPath;
            if (PAKsPathBefore != PAKsPathAfter)
            {
                MessageBox.Show("Please, restart FModel to apply your new input path", "Fortnite .PAK Path Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            //OUTPUT
            Properties.Settings.Default.ExtractOutput = textBox1.Text; //SET
            if (!Directory.Exists(Properties.Settings.Default.ExtractOutput))
                Directory.CreateDirectory(Properties.Settings.Default.ExtractOutput);
            string OutputPathAfter = Properties.Settings.Default.ExtractOutput;
            if (OutputPathBefore != OutputPathAfter)
            {
                MessageBox.Show("Please, restart FModel to apply your new output path", "FModel Output Path Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            //ICON CREATION
            if (checkBox2.Checked == true)
                Properties.Settings.Default.createIconForCosmetics = true;
            if (checkBox2.Checked == false)
                Properties.Settings.Default.createIconForCosmetics = false;
            if (checkBox5.Checked == true)
                Properties.Settings.Default.createIconForVariants = true;
            if (checkBox5.Checked == false)
                Properties.Settings.Default.createIconForVariants = false;
            if (checkBox3.Checked == true)
                Properties.Settings.Default.createIconForConsumablesWeapons = true;
            if (checkBox3.Checked == false)
                Properties.Settings.Default.createIconForConsumablesWeapons = false;
            if (checkBox4.Checked == true)
                Properties.Settings.Default.createIconForTraps = true;
            if (checkBox4.Checked == false)
                Properties.Settings.Default.createIconForTraps = false;
            if (checkBox6.Checked == true)
                Properties.Settings.Default.createIconForChallenges = true;
            if (checkBox6.Checked == false)
                Properties.Settings.Default.createIconForChallenges = false;

            //MERGER
            Properties.Settings.Default.mergerFileName = textBox3.Text;
            Properties.Settings.Default.mergerImagesRow = Decimal.ToInt32(imgsPerRow.Value);

            //WATERMARK
            if (checkBox7.Checked == true)
                Properties.Settings.Default.isWatermark = true; 
            if (checkBox7.Checked == false)
                Properties.Settings.Default.isWatermark = false;
            Properties.Settings.Default.wSize = trackBar2.Value;
            Properties.Settings.Default.wOpacity = trackBar1.Value;

            //FEATURED
            if (checkBox8.Checked == true)
                Properties.Settings.Default.loadFeaturedImage = true;
            if (checkBox8.Checked == false)
                Properties.Settings.Default.loadFeaturedImage = false;

            Properties.Settings.Default.Save(); //SAVE
            Close();
        }

        #region SELECT WATERMARK
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = "Choose your watermark";
            theDialog.Multiselect = false;
            theDialog.Filter = "PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|DDS Files (*.dds)|*.dds|All Files (*.*)|*.*";

            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.wFilename = theDialog.FileName;
                Properties.Settings.Default.Save();
                filenameLabel.Text = "File Name: " + Path.GetFileName(Properties.Settings.Default.wFilename);

                if (checkBox8.Checked == false)
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                    {
                        Bitmap bmp = new Bitmap(Properties.Resources.wTemplate);
                        Graphics g = Graphics.FromImage(bmp);

                        Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                        var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                        g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                        wPictureBox.Image = bmp;
                    }
                }
                if (checkBox8.Checked == true)
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                    {
                        Bitmap bmp = new Bitmap(Properties.Resources.wTemplateF);
                        Graphics g = Graphics.FromImage(bmp);

                        Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                        var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                        g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                        wPictureBox.Image = bmp;
                    }
                }
            }
        }
        #endregion

        #region RESIZE WATERMARK
        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked == false)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                {
                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplate);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                    wPictureBox.Image = bmp;
                    wPictureBox.Refresh();
                }
            }
            if (checkBox8.Checked == true)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                {
                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplateF);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
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
                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                {
                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplate);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                    wPictureBox.Image = bmp;
                    wPictureBox.Refresh();
                }
            }
            if (checkBox8.Checked == true)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                {
                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplateF);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                    wPictureBox.Image = bmp;
                    wPictureBox.Refresh();
                }
            }
        }
        #endregion

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
                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                {
                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
                }
                wPictureBox.Image = bmp;
            }
            if (checkBox8.Checked == true)
            {
                Bitmap bmp = new Bitmap(Properties.Resources.wTemplateF);
                Graphics g = Graphics.FromImage(bmp);
                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                {
                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
                }
                wPictureBox.Image = bmp;
            }
        }
    }
}
