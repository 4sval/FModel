using System.Windows;
using FModel.ViewModels;

namespace FModel.Views
{
    /// <summary>
    /// Interaction logic for BookmarkAdd.xaml
    /// </summary>
    public partial class BookmarkAdd
    {
        public BookmarkAdd(Bookmark bmark)
        {
            DataContext = bmark;
            InitializeComponent();
            
            Activate();
            Header.Focus();
            Header.SelectAll();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
