using System.Windows;
using System.Windows.Media;

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
        }
    }
}
