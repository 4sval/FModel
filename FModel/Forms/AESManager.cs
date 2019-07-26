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
        public static bool isClosed { get; set; }

        public AESManager()
        {
            InitializeComponent();
            isClosed = false;

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

                if (DynamicKeysManager.AESEntries != null)
                {
                    foreach (AESEntry s in DynamicKeysManager.AESEntries)
                    {
                        if (s.thePak == dCurrentUsedPak) { txtBox.Text = @"0x" + s.theKey; }
                    }
                }
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox2.Text) && textBox2.Text.StartsWith("0x"))
            {
                if (textBox2.Text.Contains(" ")) { textBox2.Text = textBox2.Text.Replace(" ", string.Empty); }
                Properties.Settings.Default.AESKey = textBox2.Text.Substring(2).ToUpper();
            }
            else { Properties.Settings.Default.AESKey = ""; }

            DynamicKeysManager.AESEntries = new List<AESEntry>();
            for (int i = 0; i < ThePak.dynamicPaksList.Count; i++)
            {
                string dCurrentUsedPak = ThePak.dynamicPaksList[i].thePak; //SET CURRENT DYNAMIC PAK
                int pFrom = dCurrentUsedPak.IndexOf("pakchunk") + "pakchunk".Length;
                int pTo = dCurrentUsedPak.LastIndexOf("WindowsClient.pak");

                Control[] controls = this.Controls.Find("txtBox_" + dCurrentUsedPak.Substring(pFrom, pTo - pFrom - 1), true);
                if (controls.Length > 0)
                {
                    TextBox txtBox = controls[0] as TextBox;
                    if (txtBox != null)
                    {
                        if (!string.IsNullOrWhiteSpace(txtBox.Text))
                        {
                            DynamicKeysManager.serialize(txtBox.Text.Substring(2).ToUpper(), dCurrentUsedPak);
                        }
                    }
                }
            }

            Properties.Settings.Default.Save();
            isClosed = true;
            Close();
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://benbotfn.tk:8080/api/aes");
        }
    }
}
