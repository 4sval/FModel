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

            textBox1.Text = Properties.Settings.Default.tConsKey;
            textBox2.Text = Properties.Settings.Default.tConsSecret;
            textBox4.Text = Properties.Settings.Default.tToken;
            textBox3.Text = Properties.Settings.Default.tTokenSecret;
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

            Properties.Settings.Default.tConsKey = textBox1.Text;
            Properties.Settings.Default.tConsSecret = textBox2.Text;
            Properties.Settings.Default.tToken = textBox4.Text;
            Properties.Settings.Default.tTokenSecret = textBox3.Text;
            Properties.Settings.Default.Save();

            if (service == null)
            {
                service = new TwitterService(Properties.Settings.Default.tConsKey, Properties.Settings.Default.tConsSecret);
                service.AuthenticateWith(Properties.Settings.Default.tToken, Properties.Settings.Default.tTokenSecret);
                UpdateStatus("Authentication to Twitter");
            }

            Dictionary<string, Stream> myDict = new Dictionary<string, Stream>();
            if (pictureBox1.Image != null)
            {
                myDict.Add(Path.GetFileNameWithoutExtension(ImagePath), new FileStream(ImagePath, FileMode.Open, FileAccess.Read, FileShare.Read));
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
    }
}
