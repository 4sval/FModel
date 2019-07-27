using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace FModel
{
    static class SchematicIconDesign
    {
        public static Graphics toDrawOn { get; set; }
        public static Bitmap schematicBitmap { get; set; }

        public static Graphics createGraphic(int x, int y)
        {
            schematicBitmap = new Bitmap(x, y);
            Graphics g = Graphics.FromImage(schematicBitmap);
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.SmoothingMode = SmoothingMode.HighQuality;
            return g;
        }
    }
}
