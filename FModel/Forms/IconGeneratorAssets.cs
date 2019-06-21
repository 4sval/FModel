using System;
using System.Windows.Forms;

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
            checkedAssets.SetItemChecked(6, Properties.Settings.Default.createIconForSTWHeroes);
            checkedAssets.SetItemChecked(7, Properties.Settings.Default.createIconForSTWDefenders);
            checkedAssets.SetItemChecked(8, Properties.Settings.Default.createIconForSTWCardPacks);
            checkedAssets.SetItemChecked(9, Properties.Settings.Default.createIconForCreativeGalleries);
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
            Properties.Settings.Default.createIconForSTWHeroes = checkedAssets.GetItemChecked(6);
            Properties.Settings.Default.createIconForSTWDefenders = checkedAssets.GetItemChecked(7);
            Properties.Settings.Default.createIconForSTWCardPacks = checkedAssets.GetItemChecked(8);
            Properties.Settings.Default.createIconForCreativeGalleries = checkedAssets.GetItemChecked(9);

            Properties.Settings.Default.Save(); //SAVE
            Close();
        }
    }
}
