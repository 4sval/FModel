using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using System.Web.UI.WebControls;
using Image = System.Drawing.Image;

namespace FModel.Forms
{
    public partial class MergeImages : Form
    {
        public MergeImages()
        {
            InitializeComponent();
        }

        private void MergeImages_Load(object sender, EventArgs e)
        {
            listBox1.Items.Clear();

            numericUpDown1.Value = Properties.Settings.Default.mergerImagesRow;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ImagesMerger.AskMergeImages(this);
        }

        public void AddFiles(string[] files)
        {
            if (files.Count() > 0)
            {
                foreach (string file in files)
                {
                    listBox1.Items.Add(new ListItem(Path.GetFileName(file), file));
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.mergerImagesRow = Decimal.ToInt32(numericUpDown1.Value);
            Properties.Settings.Default.Save();

            if (listBox1.Items.Count > 0)
            {
                List<Image> selectedImages = new List<Image>();
                for (int i = 0; i < listBox1.Items.Count; ++i)
                {
                    selectedImages.Add(Image.FromFile(((ListItem)listBox1.Items[i]).Value));
                }

                ImagesMerger.MergeSelected(selectedImages);
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
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }
    }
}
