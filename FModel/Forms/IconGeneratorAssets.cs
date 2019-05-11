using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using FModel.Properties;

namespace FModel.Forms
{
    public partial class IconGeneratorAssets : Form
    {
        public IconGeneratorAssets()
        {
            InitializeComponent();

            //ICON CREATION
            checkedAssets.SetItemChecked(0, Properties.Settings.Default.createIconForChallenges);
            checkedAssets.SetItemChecked(1, Properties.Settings.Default.createIconForConsumablesWeapons);
            checkedAssets.SetItemChecked(2, Properties.Settings.Default.createIconForCosmetics);
            checkedAssets.SetItemChecked(3, Properties.Settings.Default.createIconForTraps);
            checkedAssets.SetItemChecked(4, Properties.Settings.Default.createIconForVariants);
            checkedAssets.SetItemChecked(5, Properties.Settings.Default.createIconForAmmo);
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            //ICON CREATION
            Properties.Settings.Default.createIconForChallenges = checkedAssets.GetItemChecked(0);
            Properties.Settings.Default.createIconForConsumablesWeapons = checkedAssets.GetItemChecked(1);
            Properties.Settings.Default.createIconForCosmetics = checkedAssets.GetItemChecked(2);
            Properties.Settings.Default.createIconForTraps = checkedAssets.GetItemChecked(3);
            Properties.Settings.Default.createIconForVariants = checkedAssets.GetItemChecked(4);
            Properties.Settings.Default.createIconForAmmo = checkedAssets.GetItemChecked(5);

            Properties.Settings.Default.Save(); //SAVE
            Close();
        }
    }
}
