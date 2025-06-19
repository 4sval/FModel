using System;
using System.Collections.Generic;
using System.Windows;
using AdonisUI.Controls;
using CUE4Parse.FileProvider.Objects;

namespace FModel.Views.Resources.Controls.Diff;

public partial class DiffFileSelectionDialog : AdonisWindow
{
    public GameFile SelectedFile { get; private set; }

    public DiffFileSelectionDialog(List<GameFile> matches)
    {
        InitializeComponent();
        FilesListBox.ItemsSource = matches;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        SelectedFile = FilesListBox.SelectedItem as GameFile;
        DialogResult = SelectedFile != null;
        Close();
    }
}
