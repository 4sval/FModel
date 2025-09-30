
using AdonisUI.Controls;
using FModel.Models;
using FModel.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FModel.Views
{
    public partial class DifferenceView : AdonisWindow
    {
        private readonly DifferenceViewModel _viewModel;

        public DifferenceView()
        {
            InitializeComponent();
            DataContext = _viewModel = new DifferenceViewModel();
        }

        private void CreateSnapshot_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CanCreateSnapshot())
            {
                _viewModel.CreateSnapshot();
            }
        }

        private void CompareSnapshots_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CanCompareSnapshots())
            {
                _viewModel.CompareSnapshots();
            }
        }

        private void ViewDifferences_Click(object sender, RoutedEventArgs e)
        {
            if (ModifiedFilesListView.SelectedItem is SnapshotFilePair selectedPair)
            {
                _viewModel.ViewFileDifferences(selectedPair);
            }
        }

        private void TestLoadSnapshot_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.TestLoadSnapshot();
        }
    }
}
