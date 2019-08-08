using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using System.Web.UI.WebControls;
using Image = System.Drawing.Image;
using System.Drawing;
using System.Globalization;
using FModel.Properties;
using System.Drawing.Imaging;

namespace FModel.Forms
{
    public partial class MergeImages : Form
    {
        private static List<Image> selectedImages { get; set; }
        private static Bitmap bmp { get; set; }
        private static Timer _scrollingTimer = null;

        public MergeImages()
        {
            InitializeComponent();
        }

        private void MergeImages_Load(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Multiselect = true;
            theDialog.InitialDirectory = App.DefaultOutputPath + "\\Icons\\";
            theDialog.Title = @"Choose your images";
            theDialog.Filter = @"PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg|BMP Files (*.bmp)|*.bmp|All Files (*.*)|*.*";

            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                AddFiles(theDialog.FileNames);

                if (backgroundWorker1.IsBusy != true)
                {
                    backgroundWorker1.RunWorkerAsync();
                }
            }
        }
        private void AddFiles(string[] files)
        {
            if (files.Count() > 0)
            {
                foreach (string file in files)
                {
                    listBox1.Items.Add(new ListItem(Path.GetFileName(file), file));
                }
            }
        }
        private void mergeImages(List<Image> mySelectedImages)
        {
            int numperrow = 7;
            Invoke(new Action(() =>
            {
                numperrow = trackBar1.Value;
            }));
            var w = 527 * numperrow;
            if (mySelectedImages.Count * 527 < 527 * numperrow)
            {
                w = mySelectedImages.Count * 527;
            }

            int h = int.Parse(Math.Ceiling(double.Parse(mySelectedImages.Count.ToString()) / numperrow).ToString(CultureInfo.InvariantCulture)) * 527;
            bmp = new Bitmap(w - 5, h - 5);

            var num = 1;
            var curW = 0;
            var curH = 0;

            for (int i = 0; i < mySelectedImages.Count; i++)
            {
                int percentage = (i + 1) * 100 / mySelectedImages.Count;
                backgroundWorker1.ReportProgress(percentage);

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(ImageUtilities.ResizeImage(mySelectedImages[i], 522, 522), new PointF(curW, curH));
                    if (num % numperrow == 0)
                    {
                        curW = 0;
                        curH += 527;
                        num += 1;
                    }
                    else
                    {
                        curW += 527;
                        num += 1;
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                SaveFileDialog saveTheDialog = new SaveFileDialog();
                saveTheDialog.Title = @"Save Icon";
                saveTheDialog.Filter = @"PNG Files (*.png)|*.png";
                saveTheDialog.InitialDirectory = App.DefaultOutputPath;
                saveTheDialog.FileName = "Merger";
                if (saveTheDialog.ShowDialog() == DialogResult.OK)
                {
                    pictureBox1.Image.Save(saveTheDialog.FileName, ImageFormat.Png);
                    new UpdateMyConsole(Path.GetFileNameWithoutExtension(saveTheDialog.FileName), Color.DarkRed).AppendToConsole();
                    new UpdateMyConsole(" successfully saved", Color.Black, true).AppendToConsole();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count > 0 && listBox1.SelectedIndices.Count > 0)
            {
                for (int i = listBox1.SelectedIndices.Count - 1; i >= 0; --i)
                {
                    listBox1.Items.RemoveAt(listBox1.SelectedIndices[i]);
                }

                if (backgroundWorker1.IsBusy != true)
                {
                    backgroundWorker1.RunWorkerAsync();
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            pictureBox1.Image = null;
            progressBar1.Value = 0;
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                var newForm = new Form();
                PictureBox pb = new PictureBox();
                pb.Dock = DockStyle.Fill;
                pb.Image = pictureBox1.Image;
                pb.SizeMode = PictureBoxSizeMode.Zoom;

                newForm.WindowState = FormWindowState.Maximized;
                newForm.Size = pb.Image.Size;
                newForm.Icon = Resources.FModel;
                newForm.Text = "Temporary Merged Image";
                newForm.StartPosition = FormStartPosition.CenterScreen;
                newForm.Controls.Add(pb);
                newForm.Show();
            }
        }

        private void BackgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if ((selectedImages != null && selectedImages.Count > 0) || listBox1.Items.Count > 0)
            {
                selectedImages = new List<Image>();
                for (int i = 0; i < listBox1.Items.Count; ++i)
                {
                    selectedImages.Add(Image.FromFile(((ListItem)listBox1.Items[i]).Value));
                }
                mergeImages(selectedImages);
            }
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (bmp != null)
            {
                pictureBox1.Image = bmp;
            }

            GC.Collect();
            backgroundWorker1.Dispose();
        }

        private void BackgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void TrackBar1_Scroll(object sender, EventArgs e)
        {
            if (_scrollingTimer == null)
            {
                _scrollingTimer = new Timer()
                {
                    Enabled = false,
                    Interval = 300,
                    Tag = (sender as TrackBar).Value
                };
                _scrollingTimer.Tick += (s, ea) =>
                {
                    // check to see if the value has changed since we last ticked
                    if (trackBar1.Value == (int)_scrollingTimer.Tag)
                    {
                        // scrolling has stopped so we are good to go ahead and do stuff
                        _scrollingTimer.Stop();

                        if (backgroundWorker1.IsBusy != true)
                        {
                            backgroundWorker1.RunWorkerAsync();
                        }

                        _scrollingTimer.Dispose();
                        _scrollingTimer = null;
                    }
                    else
                    {
                        // record the last value seen
                        _scrollingTimer.Tag = trackBar1.Value;
                    }
                };
                _scrollingTimer.Start();
            }
        }
    }
}
