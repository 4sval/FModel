using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FModel.Forms
{
    public partial class AESManager : Form
    {
        public AESManager()
        {
            InitializeComponent();

            textBox2.Text = @"0x" + Properties.Settings.Default.AESKey;

            for (int i = 0; i < ThePak.dynamicPaksList.Count; i++)
            {
                string dCurrentUsedPak = ThePak.dynamicPaksList[i].thePak; //SET CURRENT DYNAMIC PAK
                int pFrom = dCurrentUsedPak.IndexOf("pakchunk") + "pakchunk".Length;
                int pTo = dCurrentUsedPak.LastIndexOf("WindowsClient.pak");

                TextBox txtBox = new TextBox();
                txtBox.Location = new Point(164, 3 + (26 * i));
                txtBox.Size = new Size(372, 20);
                txtBox.Name = "txtBox_" + dCurrentUsedPak.Substring(pFrom, pTo - pFrom - 1);
                txtBox.Parent = panel1;

                Label lbl = new Label();
                lbl.AutoSize = true;
                lbl.Location = new Point(3, 6 + (26 * i));
                lbl.Size = new Size(155, 13);
                lbl.Text = dCurrentUsedPak.Substring(0, dCurrentUsedPak.LastIndexOf("."));
                lbl.Parent = panel1;
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.AESKey = textBox2.Text.Substring(2).ToUpper();

            Properties.Settings.Default.Save();
            Close();
        }
    }
}
