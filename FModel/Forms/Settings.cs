using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FModel.Properties;

namespace FModel.Forms
{
    public partial class Settings : Form
    {
        private static string _paKsPathBefore;
        private static string _outputPathBefore;
        private static string _oldLanguage;

        public Settings()
        {
            InitializeComponent();

            textBox2.Text = Properties.Settings.Default.PAKsPath;
            textBox1.Text = Properties.Settings.Default.ExtractOutput;

            textBox4.Text = Properties.Settings.Default.eEmail;
            textBox5.Text = Properties.Settings.Default.ePassword;

            textBox6.Text = Properties.Settings.Default.challengesWatermark;
            checkBox2.Checked = Properties.Settings.Default.challengesDebug;
            if (string.IsNullOrWhiteSpace(textBox6.Text))
            {
                textBox6.Text = "{Bundle_Name} Generated using FModel & JohnWickParse - {Date}";
            }
            else { textBox6.Text = Properties.Settings.Default.challengesWatermark; }

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
            if (!Properties.Settings.Default.loadFeaturedImage)
            {
                if (File.Exists(Properties.Settings.Default.wFilename))
                {
                    filenameLabel.Text = @"File Name: " + Path.GetFileName(Properties.Settings.Default.wFilename);

                    Bitmap bmp = new Bitmap(Resources.wTemplate);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)Properties.Settings.Default.wOpacity / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize), (522 - Properties.Settings.Default.wSize) / 2, (522 - Properties.Settings.Default.wSize) / 2, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize);

                    wPictureBox.Image = bmp;
                }
            }
            if (Properties.Settings.Default.loadFeaturedImage)
            {
                if (File.Exists(Properties.Settings.Default.wFilename))
                {
                    filenameLabel.Text = @"File Name: " + Path.GetFileName(Properties.Settings.Default.wFilename);

                    Bitmap bmp = new Bitmap(Resources.wTemplateF);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)Properties.Settings.Default.wOpacity / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize), (522 - Properties.Settings.Default.wSize) / 2, (522 - Properties.Settings.Default.wSize) / 2, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize);

                    wPictureBox.Image = bmp;
                }
            }

            _oldLanguage = Properties.Settings.Default.IconLanguage;
            comboBox1.SelectedIndex = comboBox1.FindStringExact(Properties.Settings.Default.IconLanguage);

            _paKsPathBefore = Properties.Settings.Default.PAKsPath;
            _outputPathBefore = Properties.Settings.Default.ExtractOutput;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            //INPUT
            Properties.Settings.Default.PAKsPath = textBox2.Text; //SET
            string paKsPathAfter = Properties.Settings.Default.PAKsPath;
            if (_paKsPathBefore != paKsPathAfter)
            {
                MessageBox.Show(@"Please, restart FModel to apply your new input path", @"Fortnite .PAK Path Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            //OUTPUT
            Properties.Settings.Default.ExtractOutput = textBox1.Text; //SET
            if (!Directory.Exists(Properties.Settings.Default.ExtractOutput))
                Directory.CreateDirectory(Properties.Settings.Default.ExtractOutput);
            string outputPathAfter = Properties.Settings.Default.ExtractOutput;
            if (_outputPathBefore != outputPathAfter)
            {
                MessageBox.Show(@"Please, restart FModel to apply your new output path", @"FModel Output Path Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Properties.Settings.Default.eEmail = textBox4.Text;
            Properties.Settings.Default.ePassword = textBox5.Text;

            Properties.Settings.Default.challengesDebug = checkBox2.Checked;
            Properties.Settings.Default.challengesWatermark = textBox6.Text;

            //MERGER
            Properties.Settings.Default.mergerFileName = textBox3.Text;
            Properties.Settings.Default.mergerImagesRow = Decimal.ToInt32(imgsPerRow.Value);

            //WATERMARK
            if (checkBox7.Checked)
                Properties.Settings.Default.isWatermark = true; 
            if (checkBox7.Checked == false)
                Properties.Settings.Default.isWatermark = false;
            Properties.Settings.Default.wSize = trackBar2.Value;
            Properties.Settings.Default.wOpacity = trackBar1.Value;

            //FEATURED
            if (checkBox8.Checked)
                Properties.Settings.Default.loadFeaturedImage = true;
            if (checkBox8.Checked == false)
                Properties.Settings.Default.loadFeaturedImage = false;

            //LOCRES
            Properties.Settings.Default.IconLanguage = comboBox1.SelectedItem.ToString();
            if (comboBox1.SelectedItem.ToString() != _oldLanguage)
            {
                LoadLocRes.LoadMySelectedLocRes(Properties.Settings.Default.IconLanguage);
            }

            Properties.Settings.Default.Save(); //SAVE
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
                Properties.Settings.Default.wFilename = theDialog.FileName;
                Properties.Settings.Default.Save();
                filenameLabel.Text = @"File Name: " + Path.GetFileName(Properties.Settings.Default.wFilename);

                if (checkBox8.Checked == false)
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                    {
                        Bitmap bmp = new Bitmap(Resources.wTemplate);
                        Graphics g = Graphics.FromImage(bmp);

                        Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                        var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                        g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                        wPictureBox.Image = bmp;
                    }
                }

                if (checkBox8.Checked)
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                    {
                        Bitmap bmp = new Bitmap(Resources.wTemplateF);
                        Graphics g = Graphics.FromImage(bmp);

                        Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                        var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                        g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

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
                    Bitmap bmp = new Bitmap(Resources.wTemplate);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                    wPictureBox.Image = bmp;
                    wPictureBox.Refresh();
                }
            }
            if (checkBox8.Checked)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                {
                    Bitmap bmp = new Bitmap(Resources.wTemplateF);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

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
                    Bitmap bmp = new Bitmap(Resources.wTemplate);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                    wPictureBox.Image = bmp;
                    wPictureBox.Refresh();
                }
            }
            if (checkBox8.Checked)
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
                {
                    Bitmap bmp = new Bitmap(Resources.wTemplateF);
                    Graphics g = Graphics.FromImage(bmp);

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

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
            if (checkBox7.Checked)
            {
                button1.Enabled = true;
                trackBar1.Enabled = true;
                trackBar2.Enabled = true;
            }
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox8.Checked)
            {
                Bitmap bmp = new Bitmap(Resources.wTemplate);
                Graphics g = Graphics.FromImage(bmp);
                if (File.Exists(Properties.Settings.Default.wFilename))
                {
                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
                }
                wPictureBox.Image = bmp;
            }
            if (checkBox8.Checked)
            {
                Bitmap bmp = new Bitmap(Resources.wTemplateF);
                Graphics g = Graphics.FromImage(bmp);
                if (File.Exists(Properties.Settings.Default.wFilename))
                {
                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                    var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
                }
                wPictureBox.Image = bmp;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var assetsForm = new IconGeneratorAssets();
            if (Application.OpenForms[assetsForm.Name] == null)
            {
                assetsForm.Show();
            }
            else
            {
                Application.OpenForms[assetsForm.Name].Focus();
            }
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textBox4.UseSystemPasswordChar = false;
                textBox5.UseSystemPasswordChar = false;
            }
            else
            {
                textBox4.UseSystemPasswordChar = true;
                textBox5.UseSystemPasswordChar = true;
            }
        }
    }
}
