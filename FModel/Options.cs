using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FModel
{
    public partial class OptionsWindow : Form
    {
        private static string PAKBefore;
        private static string OutputBefore;

        public OptionsWindow()
        {
            InitializeComponent();

            checkBox1.Checked = Properties.Settings.Default.ExtractAndSerialize;
            textBox1.Text = Properties.Settings.Default.ExtractOutput;
            textBox2.Text = Properties.Settings.Default.FortnitePAKs;
            textBox3.Text = Properties.Settings.Default.mergerFileName;

            PAKBefore = Properties.Settings.Default.FortnitePAKs;
            OutputBefore = Properties.Settings.Default.ExtractOutput;
        }

        private void optionsOKButton_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                Properties.Settings.Default.ExtractAndSerialize = true;
            }
            if (checkBox1.Checked == false)
            {
                Properties.Settings.Default.ExtractAndSerialize = false;
            }
            Properties.Settings.Default.ExtractOutput = textBox1.Text;
            Properties.Settings.Default.FortnitePAKs = textBox2.Text;
            Properties.Settings.Default.mergerFileName = textBox3.Text;

            if (!Directory.Exists(Properties.Settings.Default.ExtractOutput))
                Directory.CreateDirectory(Properties.Settings.Default.ExtractOutput);

            string PAKAfter = Properties.Settings.Default.FortnitePAKs;
            if (PAKBefore != PAKAfter)
            {
                MessageBox.Show("Please, restart FModel to apply your new input path", "FModel Path Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            string OutputAfter = Properties.Settings.Default.ExtractOutput;
            if (OutputBefore != OutputAfter)
            {
                MessageBox.Show("Please, restart FModel to apply your new output path", "FModel Path Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Properties.Settings.Default.Save();
            Close();
        }
    }
}
