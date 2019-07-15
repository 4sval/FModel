using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TweetSharp;

namespace FModel.Forms
{
    public partial class TweetForm : Form
    {
        private static string ImagePath { get; set; }
        private static TwitterService service { get; set; }

        public TweetForm()
        {
            InitializeComponent();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            ImagePath = null;
            pictureBox1.Image = null;

            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.InitialDirectory = App.DefaultOutputPath + "\\Icons\\";
            theDialog.Title = @"Choose your image";
            theDialog.Filter = @"PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg|BMP Files (*.bmp)|*.bmp|All Files (*.*)|*.*";

            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                ImagePath = theDialog.FileName;
                pictureBox1.Image = Image.FromFile(ImagePath);
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy != true && !string.IsNullOrEmpty(richTextBox1.Text))
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            label5.ForeColor = Color.FromArgb(255, 0, 0, 0);
            string tweetText = "";
            Invoke(new Action(() =>
            {
                tweetText = richTextBox1.Text;
            }));

            if (service == null)
            {
                service = new TwitterService(Properties.Settings.Default.tConsKey, Properties.Settings.Default.tConsSecret);
                service.AuthenticateWith(Properties.Settings.Default.tToken, Properties.Settings.Default.tTokenSecret);
                UpdateStatus("Authentication to Twitter");
            }

            Dictionary<string, Stream> myDict = new Dictionary<string, Stream>();
            if (pictureBox1.Image != null)
            {
                if (new System.IO.FileInfo(ImagePath).Length < 5000000)
                {
                    myDict.Add(Path.GetFileNameWithoutExtension(ImagePath), new FileStream(ImagePath, FileMode.Open, FileAccess.Read, FileShare.Read));
                }
                else
                {
                    throw new ArgumentException("File size can't be larger than 5mb.");
                }
            }

#pragma warning disable CS0618
            if (pictureBox1.Image != null)
            {
                UpdateStatus("Tweeting with " + Path.GetFileNameWithoutExtension(ImagePath));
                TwitterStatus response = service.SendTweetWithMedia(new SendTweetWithMediaOptions
                {
                    Status = tweetText,
                    Images = myDict
                });
            }
            else
            {
                UpdateStatus("Tweeting without image");
                TwitterStatus response = service.SendTweet(new SendTweetOptions
                {
                    Status = tweetText
                });
            }
#pragma warning restore CS0618
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                label5.ForeColor = Color.FromArgb(255, 244, 66, 66);
                label5.Text = "Status: " + e.Error.Message;
            }
            else
            {
                label5.ForeColor = Color.FromArgb(255, 43, 135, 28);
                label5.Text = "Status: Tweeted";
            }
        }

        private void UpdateStatus(string text)
        {
            if (label5.InvokeRequired)
            {
                BeginInvoke(new Action<string>(UpdateStatus), new object[] { text });
                return;
            }

            label5.Text = "Status: " + text;
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0)
            {
                textBox5.Text = Properties.Settings.Default.tConsKey;
            }
            if (comboBox1.SelectedIndex == 1)
            {
                textBox5.Text = Properties.Settings.Default.tConsSecret;
            }
            if (comboBox1.SelectedIndex == 2)
            {
                textBox5.Text = Properties.Settings.Default.tToken;
            }
            if (comboBox1.SelectedIndex == 3)
            {
                textBox5.Text = Properties.Settings.Default.tTokenSecret;
            }
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox5.UseSystemPasswordChar = !checkBox1.Checked;
        }

        private void TextBox5_TextChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0)
            {
                Properties.Settings.Default.tConsKey = textBox5.Text;
            }
            if (comboBox1.SelectedIndex == 1)
            {
                Properties.Settings.Default.tConsSecret = textBox5.Text;
            }
            if (comboBox1.SelectedIndex == 2)
            {
                Properties.Settings.Default.tToken = textBox5.Text;
            }
            if (comboBox1.SelectedIndex == 3)
            {
                Properties.Settings.Default.tTokenSecret = textBox5.Text;
            }

            Properties.Settings.Default.Save();
        }
    }
}
