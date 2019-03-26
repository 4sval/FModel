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
    public partial class OptionsWindow : Form
    {
        private static string PAKBefore;
        private static string OutputBefore;
        private static string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToString() + "\\FModel";

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

        public OptionsWindow()
        {
            InitializeComponent();

            checkBox1.Checked = Properties.Settings.Default.ExtractAndSerialize;
            textBox1.Text = Properties.Settings.Default.ExtractOutput;
            textBox2.Text = Properties.Settings.Default.FortnitePAKs;
            textBox3.Text = Properties.Settings.Default.mergerFileName;
            imgsPerRow.Value = Properties.Settings.Default.mergerImagesRow;
            checkBox2.Checked = Properties.Settings.Default.createIconForCosmetics;
            checkBox5.Checked = Properties.Settings.Default.createIconForVariants;
            checkBox3.Checked = Properties.Settings.Default.createIconForConsumablesWeapons;
            checkBox4.Checked = Properties.Settings.Default.createIconForTraps;
            checkBox6.Checked = Properties.Settings.Default.createIconForChallenges;
            checkBox7.Checked = Properties.Settings.Default.isWatermark;
            comboBox1.SelectedItem = Properties.Settings.Default.IconName;
            trackBar2.Value = Properties.Settings.Default.wSize;
            trackBar1.Value = Properties.Settings.Default.wOpacity;

            button1.Enabled = Properties.Settings.Default.isWatermark;
            trackBar1.Enabled = Properties.Settings.Default.isWatermark;
            trackBar2.Enabled = Properties.Settings.Default.isWatermark;

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

            PAKBefore = Properties.Settings.Default.FortnitePAKs;
            OutputBefore = Properties.Settings.Default.ExtractOutput;
        }

        private void optionsOKButton_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                Properties.Settings.Default.ExtractAndSerialize = true;
            }
            if (checkBox1.Checked == false)
            {
                Properties.Settings.Default.ExtractAndSerialize = false;
            }
            if (checkBox2.Checked == true)
            {
                Properties.Settings.Default.createIconForCosmetics = true;
            }
            if (checkBox2.Checked == false)
            {
                Properties.Settings.Default.createIconForCosmetics = false;
            }
            if (checkBox5.Checked == true)
            {
                Properties.Settings.Default.createIconForVariants = true;
            }
            if (checkBox5.Checked == false)
            {
                Properties.Settings.Default.createIconForVariants = false;
            }
            if (checkBox3.Checked == true)
            {
                Properties.Settings.Default.createIconForConsumablesWeapons = true;
            }
            if (checkBox3.Checked == false)
            {
                Properties.Settings.Default.createIconForConsumablesWeapons = false;
            }
            if (checkBox4.Checked == true)
            {
                Properties.Settings.Default.createIconForTraps = true;
            }
            if (checkBox4.Checked == false)
            {
                Properties.Settings.Default.createIconForTraps = false;
            }
            if (checkBox6.Checked == true)
            {
                Properties.Settings.Default.createIconForChallenges = true;
            }
            if (checkBox6.Checked == false)
            {
                Properties.Settings.Default.createIconForChallenges = false;
            }
            if (checkBox7.Checked == true)
            {
                Properties.Settings.Default.isWatermark = true;
            }
            if (checkBox7.Checked == false)
            {
                Properties.Settings.Default.isWatermark = false;
            }
            if (comboBox1.SelectedItem == null)
            {
                Properties.Settings.Default.IconName = "Selected Item Name (i.e. CID_001_Athena_Commando_F_Default)";
            }
            else
            {
                Properties.Settings.Default.IconName = comboBox1.SelectedItem.ToString();
            }

            Properties.Settings.Default.ExtractOutput = textBox1.Text;
            Properties.Settings.Default.FortnitePAKs = textBox2.Text;
            Properties.Settings.Default.mergerFileName = textBox3.Text;
            Properties.Settings.Default.mergerImagesRow = Decimal.ToInt32(imgsPerRow.Value);
            Properties.Settings.Default.wSize = trackBar2.Value;
            Properties.Settings.Default.wOpacity = trackBar1.Value;

            if (!Directory.Exists(Properties.Settings.Default.ExtractOutput))
                Directory.CreateDirectory(Properties.Settings.Default.ExtractOutput);

            string PAKAfter = Properties.Settings.Default.FortnitePAKs;
            if (PAKBefore != PAKAfter)
            {
                MessageBox.Show("Please, restart FModel to apply your new input path", "FModel Path Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            string OutputAfter = Properties.Settings.Default.ExtractOutput;
            if (OutputBefore != OutputAfter)
            {
                MessageBox.Show("Please, restart FModel to apply your new output path", "FModel Path Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

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
                Properties.Settings.Default.wFilename = theDialog.FileName;
                Properties.Settings.Default.Save();
                filenameLabel.Text = "File Name: " + Path.GetFileName(Properties.Settings.Default.wFilename);

                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                {
                    Bitmap bmp = new Bitmap(Properties.Resources.wTemplate);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    g.DrawImage(ResizeImage(watermark, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                    wPictureBox.Image = bmp;
                }
            }
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
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

        private void trackBar1_ValueChanged(object sender, EventArgs e)
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
    }
}
