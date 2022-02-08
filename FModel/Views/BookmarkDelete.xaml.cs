using System.Windows;
using System.Windows.Controls;
using FModel.ViewModels;

namespace FModel.Views
{
    /// <summary>
    /// Interaction logic for BookmarkDelete.xaml
    /// </summary>
    public partial class BookmarkDelete
    {
        public BookmarkDelete(BookmarkList bmList)
        {
            DataContext = bmList;
            InitializeComponent();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
