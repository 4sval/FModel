using FModel.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Globalization;
using System.Windows.Forms;

namespace FModel
{
    static class ImagesMerger
    {
        /// <summary>
        /// open a FileDialog to choose our images to merge, add them to the list of images
        /// use this list to merge, with MergeSelected()
        /// </summary>
        public static void AskMergeImages()
        {
            if (string.IsNullOrEmpty(Settings.Default.mergerFileName))
            {
                Settings.Default.mergerFileName = "Merger";
                Settings.Default.Save();
            }

            OpenFileDialog theDialog = new OpenFileDialog();
            theDialog.Multiselect = true;
            theDialog.InitialDirectory = App.DefaultOutputPath + "\\Icons\\";
            theDialog.Title = @"Choose your images";
            theDialog.Filter = @"PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg|BMP Files (*.bmp)|*.bmp|All Files (*.*)|*.*";

            if (theDialog.ShowDialog() == DialogResult.OK)
            {
                List<Image> selectedImages = new List<Image>();
                foreach (var files in theDialog.FileNames)
                {
                    selectedImages.Add(Image.FromFile(files));
                }

                MergeSelected(selectedImages);
            }
        }

        /// <summary>
        /// take all the selected images and draw them one after the other depending on mergerImagesRow
        /// at the end, save and open the generated image with OpenMerged()
        /// </summary>
        /// <param name="mySelectedImages"></param>
        private static void MergeSelected(List<Image> mySelectedImages)
        {
            string mergeFileName = Settings.Default.mergerFileName;
            if (Properties.Settings.Default.mergerImagesSaveAs)
            {
                SaveFileDialog saveFileMergerImages = new SaveFileDialog();
                saveFileMergerImages.InitialDirectory = App.DefaultOutputPath;
                saveFileMergerImages.DefaultExt = "png";
                saveFileMergerImages.Filter = "Image PNG (.png)|*.png";
                saveFileMergerImages.FileName = mergeFileName;

                if (saveFileMergerImages.ShowDialog() != DialogResult.OK)
                    return;
                 mergeFileName = Path.GetFileName(saveFileMergerImages.FileName);
            }

            if (Settings.Default.mergerImagesRow == 0)
            {
                Settings.Default.mergerImagesRow = 7;
                Settings.Default.Save();
            }

            int numperrow = Settings.Default.mergerImagesRow;
            var w = 530 * numperrow;
            if (mySelectedImages.Count * 530 < 530 * numperrow)
            {
                w = mySelectedImages.Count * 530;
            }

            int h = int.Parse(Math.Ceiling(double.Parse(mySelectedImages.Count.ToString()) / numperrow).ToString(CultureInfo.InvariantCulture)) * 530;
            Bitmap bmp = new Bitmap(w - 8, h - 8);

            var num = 1;
            var curW = 0;
            var curH = 0;

            for (int i = 0; i < mySelectedImages.Count; i++)
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(ImageUtilities.ResizeImage(mySelectedImages[i], 522, 522), new PointF(curW, curH));
                    if (num % numperrow == 0)
                    {
                        curW = 0;
                        curH += 530;
                        num += 1;
                    }
                    else
                    {
                        curW += 530;
                        num += 1;
                    }
                }
            }

            if (!mergeFileName.Contains(".png"))
                mergeFileName += ".png";

            bmp.Save(App.DefaultOutputPath + "\\" + mergeFileName, ImageFormat.Png);

            OpenMerged(bmp);
        }

        /// <summary>
        /// if bitmap exist, open a new form in fullscreen and display the bitmap in a picturebox
        /// </summary>
        /// <param name="mergedImage"></param>
        private static void OpenMerged(Bitmap mergedImage)
        {
            if (mergedImage != null)
            {
                var newForm = new Form();
                PictureBox pb = new PictureBox();
                pb.Dock = DockStyle.Fill;
                pb.Image = mergedImage;
                pb.SizeMode = PictureBoxSizeMode.Zoom;

                newForm.WindowState = FormWindowState.Maximized;
                newForm.Size = mergedImage.Size;
                newForm.Icon = Resources.FModel;
                newForm.Text = App.DefaultOutputPath + @"\" + Settings.Default.mergerFileName + @".png";
                newForm.StartPosition = FormStartPosition.CenterScreen;
                newForm.Controls.Add(pb);
                newForm.Show();
            }
        }
    }
}
