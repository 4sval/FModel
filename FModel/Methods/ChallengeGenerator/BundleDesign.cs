using FModel.Parser.Challenges;
using FModel.Parser.Items;
using FModel.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FModel
{
    static class BundleDesign
    {
        public static string BundlePath { get; set; }
        public static int theY { get; set; }
        public static Graphics toDrawOn { get; set; }
        public static ItemsIdParser myItem { get; set; }

        /// <summary>
        /// draw the pretty header if DisplayStyle exist, else the shitty background
        /// </summary>
        /// <param name="myBitmap"></param>
        /// <param name="myBundle"></param>
        public static void drawBackground(Bitmap myBitmap, ChallengeBundleIdParser myBundle)
        {
            if (Settings.Default.createIconForChallenges && myBundle.DisplayStyle != null)
            {
                //main header
                toDrawOn.FillRectangle(new SolidBrush(BundleInfos.getSecondaryColor(myBundle)), new Rectangle(0, 0, myBitmap.Width, 271));

                //gradient at left and right main header
                LinearGradientBrush linGrBrush_left = new LinearGradientBrush(new Point(0, 282 / 2), new Point(282, 282 / 2),
                    ControlPaint.Light(BundleInfos.getSecondaryColor(myBundle), (float)0.3), BundleInfos.getSecondaryColor(myBundle));
                toDrawOn.FillRectangle(linGrBrush_left, new Rectangle(0, 0, 282, 282));
                LinearGradientBrush linGrBrush_right = new LinearGradientBrush(new Point(2500, 282 / 2), new Point(1500, 282 / 2),
                    ControlPaint.Light(BundleInfos.getSecondaryColor(myBundle), (float)0.3), BundleInfos.getSecondaryColor(myBundle));
                toDrawOn.FillRectangle(linGrBrush_right, new Rectangle(1500, 0, 1000, 282));

                //last folder with border
                GraphicsPath p = new GraphicsPath();
                Pen myPen = new Pen(ControlPaint.Light(BundleInfos.getSecondaryColor(myBundle), (float)0.2), 3);
                myPen.LineJoin = LineJoin.Round; //needed to avoid spikes
                p.AddString(BundleInfos.getLastFolder(BundlePath), FontUtilities.pfc.Families[1], (int)FontStyle.Regular, 55, new Point(342, 40), FontUtilities.leftString);
                toDrawOn.DrawPath(myPen, p);
                toDrawOn.FillPath(new SolidBrush(ControlPaint.Dark(BundleInfos.getSecondaryColor(myBundle), (float)0.05)), p);

                //name
                toDrawOn.DrawString(BundleInfos.getBundleDisplayName(myItem), new Font(FontUtilities.pfc.Families[1], 115), new SolidBrush(Color.White), new Point(325, 70));

                //image
                string textureFile = Path.GetFileName(myBundle.DisplayStyle.DisplayImage.AssetPathName).Substring(0, Path.GetFileName(myBundle.DisplayStyle.DisplayImage.AssetPathName).LastIndexOf('.'));
                Image challengeIcon = new Bitmap(JohnWick.AssetToTexture2D(textureFile));
                toDrawOn.DrawImage(ImageUtilities.ResizeImage(challengeIcon, 282, 282), new Point(40, 0));

                //fill the rest
                toDrawOn.FillRectangle(new SolidBrush(ControlPaint.Dark(BundleInfos.getSecondaryColor(myBundle), (float)0.1)), new Rectangle(0, 271, myBitmap.Width, myBitmap.Height));
            }
            else
            {
                toDrawOn.DrawImage(new Bitmap(Resources.Quest), new Point(0, 0));

                //last folder
                toDrawOn.DrawString(BundleInfos.getLastFolder(BundlePath), new Font(FontUtilities.pfc.Families[1], 42), new SolidBrush(Color.FromArgb(255, 149, 213, 255)), new Point(340, 40));

                //name
                toDrawOn.DrawString(BundleInfos.getBundleDisplayName(myItem), new Font(FontUtilities.pfc.Families[1], 115), new SolidBrush(Color.White), new Point(325, 70));
            }
        }
    }
}
