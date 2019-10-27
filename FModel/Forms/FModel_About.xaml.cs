using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;

namespace FModel.Forms
{
    /// <summary>
    /// Logique d'interaction pour FModel_About.xaml
    /// </summary>
    public partial class FModel_About : Window
    {
        public FModel_About()
        {
            InitializeComponent();
            this.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);

            AboutTitle_Lbl.Content += " " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 5);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }
    }
}
