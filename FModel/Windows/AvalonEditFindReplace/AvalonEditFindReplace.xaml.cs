using System.Windows;
using System.Windows.Input;

namespace FModel.Windows.AvalonEditFindReplace
{
    /// <summary>
    /// Logique d'interaction pour AvalonEditFindReplace.xaml
    /// </summary>
    public partial class AvalonEditFindReplace : Window
    {
        readonly AvalonEditFindReplaceHelper TheVM;

        public AvalonEditFindReplace(AvalonEditFindReplaceHelper theVM)
        {
            InitializeComponent();
            DataContext = TheVM = theVM;
        }

        private void FindNextClick(object sender, RoutedEventArgs e)
        {
            TheVM.FindNext();
        }

        private void ReplaceClick(object sender, RoutedEventArgs e)
        {
            TheVM.Replace();
        }

        private void ReplaceAllClick(object sender, RoutedEventArgs e)
        {
            TheVM.ReplaceAll();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
