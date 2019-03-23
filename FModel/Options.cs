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
            imgsPerRow.Value = Properties.Settings.Default.mergerImagesRow;
            checkBox2.Checked = Properties.Settings.Default.createIconForCosmetics;
            checkBox5.Checked = Properties.Settings.Default.createIconForVariants;
            checkBox3.Checked = Properties.Settings.Default.createIconForConsumablesWeapons;
            checkBox4.Checked = Properties.Settings.Default.createIconForTraps;
            checkBox6.Checked = Properties.Settings.Default.createIconForChallenges;
            comboBox1.SelectedItem = Properties.Settings.Default.IconName;

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
            if (checkBox2.Checked == true)
            {
                Properties.Settings.Default.createIconForCosmetics = true;
            }
            if (checkBox2.Checked == false)
            {
                Properties.Settings.Default.createIconForCosmetics = false;
            }
            if (checkBox5.Checked == true)
            {
                Properties.Settings.Default.createIconForVariants = true;
            }
            if (checkBox5.Checked == false)
            {
                Properties.Settings.Default.createIconForVariants = false;
            }
            if (checkBox3.Checked == true)
            {
                Properties.Settings.Default.createIconForConsumablesWeapons = true;
            }
            if (checkBox3.Checked == false)
            {
                Properties.Settings.Default.createIconForConsumablesWeapons = false;
            }
            if (checkBox4.Checked == true)
            {
                Properties.Settings.Default.createIconForTraps = true;
            }
            if (checkBox4.Checked == false)
            {
                Properties.Settings.Default.createIconForTraps = false;
            }
            if (checkBox6.Checked == true)
            {
                Properties.Settings.Default.createIconForChallenges = true;
            }
            if (checkBox6.Checked == false)
            {
                Properties.Settings.Default.createIconForChallenges = false;
            }
            if (comboBox1.SelectedItem == null)
            {
                Properties.Settings.Default.IconName = "Selected Item Name (i.e. CID_001_Athena_Commando_F_Default)";
            }
            else
            {
                Properties.Settings.Default.IconName = comboBox1.SelectedItem.ToString();
            }

            Properties.Settings.Default.ExtractOutput = textBox1.Text;
            Properties.Settings.Default.FortnitePAKs = textBox2.Text;
            Properties.Settings.Default.mergerFileName = textBox3.Text;
            Properties.Settings.Default.mergerImagesRow = Decimal.ToInt32(imgsPerRow.Value);

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
