using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using Cyotek.Windows.Forms;
using FModel.Properties;

namespace FModel.Forms
{
    //refactor asap
    public partial class Settings : Form
    {
        private static string _paKsPathBefore;
        private static string _outputPathBefore;
        private static string _oldLanguage;
        private static Color headerColor;
        private static Bitmap bmp;
        private static Graphics g;

        public Settings()
        {
            InitializeComponent();

            // Check if watermark exists
            Utilities.CheckWatermark();

            textBox2.Text = Properties.Settings.Default.PAKsPath;
            textBox1.Text = Properties.Settings.Default.ExtractOutput;

            textBox6.Text = Properties.Settings.Default.challengesWatermark;
            if (string.IsNullOrWhiteSpace(textBox6.Text))
            {
                textBox6.Text = "@UseTheWatermarkBecauseWhyNot - {Date}";
            }

            // Check if watermark exists
            Utilities.CheckWatermark();

            comboBox2.SelectedIndex = comboBox2.FindStringExact(Properties.Settings.Default.rarityDesign);

            //WATERMARK
            button1.Enabled     = Properties.Settings.Default.isWatermark;
            checkBox7.Checked   = Properties.Settings.Default.isWatermark;
            trackBar1.Enabled   = Properties.Settings.Default.isWatermark;
            trackBar2.Enabled   = Properties.Settings.Default.isWatermark;
            trackBar1.Value     = Properties.Settings.Default.wOpacity;
            trackBar2.Value     = Properties.Settings.Default.wSize;

            //CHALLENGES
            button3.Enabled = Properties.Settings.Default.isChallengesTheme;
            button4.Enabled = Properties.Settings.Default.isChallengesTheme;
            checkBox2.Checked = Properties.Settings.Default.isChallengesTheme;
            trackBar3.Enabled = File.Exists(Properties.Settings.Default.challengesBannerFileName);
            button5.Enabled = File.Exists(Properties.Settings.Default.challengesBannerFileName);
            trackBar3.Value = Properties.Settings.Default.challengesOpacity;
            string[] colorParts = Properties.Settings.Default.challengesColors.Split(',');
            headerColor = Color.FromArgb(255, Int32.Parse(colorParts[0]), Int32.Parse(colorParts[1]), Int32.Parse(colorParts[2]));
            if (!checkBox2.Checked)
            {
                pictureBox1.Image = Resources.cTemplate;
            }
            else { drawChallengeTemplate(headerColor, File.Exists(Properties.Settings.Default.challengesBannerFileName)); }

            //FEATURED
            checkBox8.Checked = Properties.Settings.Default.loadFeaturedImage;
            if (File.Exists(Properties.Settings.Default.wFilename))
            {
                filenameLabel.Text = @"File Name: " + Path.GetFileName(Properties.Settings.Default.wFilename);

                Bitmap bmp = null;
                if (Properties.Settings.Default.loadFeaturedImage)
                {
                    bmp = new Bitmap(string.Equals(Properties.Settings.Default.rarityDesign, "Flat") ? new Bitmap(Resources.Template_F_F) : string.Equals(Properties.Settings.Default.rarityDesign, "Minimalist") ? new Bitmap(Resources.Template_M_F) : new Bitmap(Resources.Template_D_F));
                }
                else
                {
                    bmp = new Bitmap(string.Equals(Properties.Settings.Default.rarityDesign, "Flat") ? new Bitmap(Resources.Template_F_N) : string.Equals(Properties.Settings.Default.rarityDesign, "Minimalist") ? new Bitmap(Resources.Template_M_N) : new Bitmap(Resources.Template_D_N));
                }
                Graphics g = Graphics.FromImage(bmp);

                Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)Properties.Settings.Default.wOpacity / 100);
                g.DrawImage(ImageUtilities.ResizeImage(opacityImage, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize), (522 - Properties.Settings.Default.wSize) / 2, (522 - Properties.Settings.Default.wSize) / 2, Properties.Settings.Default.wSize, Properties.Settings.Default.wSize);

                wPictureBox.Image = bmp;
            }

            checkBox1.Checked = Properties.Settings.Default.openSound;

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

            //CHALLENGES
            Properties.Settings.Default.challengesWatermark = textBox6.Text;
            Properties.Settings.Default.isChallengesTheme = checkBox2.Checked;
            Properties.Settings.Default.challengesOpacity = trackBar3.Value;
            Properties.Settings.Default.challengesColors = headerColor.R + "," + headerColor.G + "," + headerColor.B;

            Properties.Settings.Default.rarityDesign = comboBox2.SelectedItem.ToString();

            //WATERMARK
            Properties.Settings.Default.isWatermark = checkBox7.Checked; 
            Properties.Settings.Default.wSize       = trackBar2.Value;
            Properties.Settings.Default.wOpacity    = trackBar1.Value;

            //FEATURED
            Properties.Settings.Default.loadFeaturedImage = checkBox8.Checked;

            //LOCRES
            Properties.Settings.Default.IconLanguage = comboBox1.SelectedItem.ToString();
            if (comboBox1.SelectedItem.ToString() != _oldLanguage)
            {
                LoadLocRes.LoadMySelectedLocRes(Properties.Settings.Default.IconLanguage);
            }

            Properties.Settings.Default.openSound = checkBox1.Checked;

            Properties.Settings.Default.Save(); //SAVE
            bmp.Dispose();
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

                if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
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

                    Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
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
            if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
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

                Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);

                wPictureBox.Image = bmp;
                wPictureBox.Refresh();
            }
        }
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.wFilename))
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

                Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
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
                Bitmap bmp = string.Equals(comboBox2.SelectedItem.ToString(), "Flat") ? new Bitmap(Resources.Template_F_F) : string.Equals(comboBox2.SelectedItem.ToString(), "Minimalist") ? new Bitmap(Resources.Template_M_F) : new Bitmap(Resources.Template_D_F);
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
            if (File.Exists(Properties.Settings.Default.wFilename))
            {
                Image watermark = Image.FromFile(Properties.Settings.Default.wFilename);
                var opacityImage = ImageUtilities.SetImageOpacity(watermark, (float)trackBar1.Value / 100);
                g.DrawImage(ImageUtilities.ResizeImage(opacityImage, trackBar2.Value, trackBar2.Value), (522 - trackBar2.Value) / 2, (522 - trackBar2.Value) / 2, trackBar2.Value, trackBar2.Value);
            }
            wPictureBox.Image = bmp;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                var newForm = new Form();

                PictureBox pb = new PictureBox();
                pb.Dock = DockStyle.Fill;
                pb.Image = pictureBox1.Image;
                pb.SizeMode = PictureBoxSizeMode.Zoom;

                newForm.Size = pictureBox1.Image.Size;
                newForm.Icon = Resources.FModel;
                newForm.Text = "Challenges Design Template";
                newForm.StartPosition = FormStartPosition.CenterScreen;
                newForm.Controls.Add(pb);
                newForm.Show();
            }
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            button3.Enabled = checkBox2.Checked;
            button4.Enabled = checkBox2.Checked;
            trackBar3.Enabled = File.Exists(Properties.Settings.Default.challengesBannerFileName);

            if (!checkBox2.Checked)
            {
                pictureBox1.Image = Resources.cTemplate;
            }
            else { drawChallengeTemplate(headerColor, File.Exists(Properties.Settings.Default.challengesBannerFileName)); }
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            ColorPickerDialog MyDialog = new ColorPickerDialog();

            if (MyDialog.ShowDialog() == DialogResult.OK)
            {
                headerColor = MyDialog.Color;
                drawChallengeTemplate(headerColor, File.Exists(Properties.Settings.Default.challengesBannerFileName));
            }
            MyDialog.Dispose();
        }

        private void drawChallengeTemplate(Color headerColor, bool isBanner = false)
        {
            bmp = new Bitmap(1024, 410);
            g = Graphics.FromImage(bmp);
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.SmoothingMode = SmoothingMode.HighQuality;

            #region header
            if (isBanner)
            {
                if (File.Exists(Properties.Settings.Default.challengesBannerFileName))
                {
                    g.FillRectangle(new SolidBrush(headerColor), new Rectangle(0, 0, bmp.Width, 256));

                    Image banner = Image.FromFile(Properties.Settings.Default.challengesBannerFileName);
                    var opacityImage = ImageUtilities.SetImageOpacity(banner, (float)trackBar3.Value / 1000);
                    g.DrawImage(ImageUtilities.ResizeImage(opacityImage, 1024, 256), 0, 0);
                }
            }
            else
            {
                g.FillRectangle(new SolidBrush(headerColor), new Rectangle(0, 0, bmp.Width, 256));
            }

            GraphicsPath gp = new GraphicsPath();
            gp.StartFigure();
            gp.AddLine(0, 256, bmp.Width, 256);
            gp.AddLine(bmp.Width, 256, bmp.Width, 241);
            gp.AddLine(bmp.Width, 241, bmp.Width / 2 + 25, 236);
            gp.AddLine(bmp.Width / 2 + 25, 236, bmp.Width / 2 + 35, 249);
            gp.AddLine(bmp.Width / 2 + 35, 249, 0, 241);
            gp.CloseFigure();
            g.FillPath(new SolidBrush(ControlPaint.Light(headerColor)), gp);

            GraphicsPath p = new GraphicsPath();
            Pen myPen = new Pen(ControlPaint.Light(headerColor, (float)0.2), 3);
            myPen.LineJoin = LineJoin.Round; //needed to avoid spikes
            p.AddString(
                "{LAST FOLDER HERE}",
                Properties.Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[1],
                (int)FontStyle.Regular, 30,
                new Point(30, 70),
                FontUtilities.leftString
                );
            g.DrawPath(myPen, p);
            g.FillPath(new SolidBrush(ControlPaint.Dark(headerColor, (float)0.05)), p);

            g.DrawString("{BUNDLE DISPLAY NAME HERE}", new Font(Properties.Settings.Default.IconLanguage == "Japanese" ? FontUtilities.pfc.Families[2] : FontUtilities.pfc.Families[1], 40), new SolidBrush(Color.White), new Point(25, 105));

            g.FillRectangle(new SolidBrush(ControlPaint.Dark(headerColor, (float)0.1)), new Rectangle(0, 256, bmp.Width, bmp.Height));
            #endregion

            #region quest background
            int theY = 290;
            g.FillRectangle(new SolidBrush(Color.FromArgb(50, headerColor.R, headerColor.G, headerColor.B)), new Rectangle(25, theY, bmp.Width - 50, 70));

            gp = new GraphicsPath();
            gp.StartFigure();
            gp.AddLine(32, theY + 5, 29, theY + 67);
            gp.AddLine(29, theY + 67, bmp.Width - 160, theY + 62);
            gp.AddLine(bmp.Width - 160, theY + 62, bmp.Width - 150, theY + 4);
            gp.CloseFigure();
            g.FillPath(new SolidBrush(Color.FromArgb(50, headerColor.R, headerColor.G, headerColor.B)), gp);

            g.FillRectangle(new SolidBrush(headerColor), new Rectangle(60, theY + 47, 500, 7));

            gp = new GraphicsPath();
            gp.StartFigure();
            gp.AddLine(39, theY + 35, 45, theY + 32);
            gp.AddLine(45, theY + 32, 48, theY + 37);
            gp.AddLine(48, theY + 37, 42, theY + 40);
            gp.CloseFigure();
            g.FillPath(new SolidBrush(headerColor), gp);
            #endregion

            #region watermark
            string text = textBox6.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                text = "{Bundle_Name} Generated using FModel & JohnWickParse - {Date}";
            }

            if (text.Contains("{Bundle_Name}"))
            {
                text = text.Replace("{Bundle_Name}", "{BUNDLE DISPLAY NAME HERE}");
            }
            if (text.Contains("{Date}"))
            {
                text = text.Replace("{Date}", DateTime.Now.ToString("dd/MM/yyyy"));
            }

            g.DrawString(text, new Font(FontUtilities.pfc.Families[0], 15), new SolidBrush(Color.FromArgb(150, 255, 255, 255)), new Point(bmp.Width - 10, 210), FontUtilities.rightString);
            #endregion

            pictureBox1.Image = bmp;
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Title = @"Choose your banner";
            theDialog.Multiselect = false;
            theDialog.Filter = @"PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|DDS Files (*.dds)|*.dds|All Files (*.*)|*.*";

            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.challengesBannerFileName = theDialog.FileName;
                Properties.Settings.Default.Save();

                drawChallengeTemplate(headerColor, File.Exists(Properties.Settings.Default.challengesBannerFileName));

                trackBar3.Enabled = File.Exists(Properties.Settings.Default.challengesBannerFileName);
                button5.Enabled = File.Exists(Properties.Settings.Default.challengesBannerFileName);
            }
        }

        private void TrackBar3_ValueChanged(object sender, EventArgs e)
        {
            drawChallengeTemplate(headerColor, File.Exists(Properties.Settings.Default.challengesBannerFileName));
            pictureBox1.Refresh();
        }

        private void TextBox6_TextChanged(object sender, EventArgs e)
        {
            drawChallengeTemplate(headerColor, File.Exists(Properties.Settings.Default.challengesBannerFileName));
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            bmp.Dispose();
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.challengesBannerFileName = "";
            Properties.Settings.Default.Save();

            drawChallengeTemplate(headerColor, File.Exists(Properties.Settings.Default.challengesBannerFileName));

            trackBar3.Enabled = File.Exists(Properties.Settings.Default.challengesBannerFileName);
            button5.Enabled = File.Exists(Properties.Settings.Default.challengesBannerFileName);
        }
    }
}
