using System.IO;
using System.Windows.Forms;

namespace FModel.Forms
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();

            label2.Text += @" " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/iAmAsval/FModel");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://discordapp.com/invite/JmWvXKb");
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/iAmAsval/FModel/blob/master/README.md#how-to-use");
        }
    }
}
