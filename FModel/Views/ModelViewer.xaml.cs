using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AdonisUI.Controls;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using FModel.Services;
using FModel.ViewModels;
using HelixToolkit.Wpf.SharpDX;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace FModel.Views;

public partial class ModelViewer
{
    private bool _messageShown;
    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;

    public ModelViewer()
    {
        DataContext = _applicationView;
        InitializeComponent();
    }

    public void Load(UObject export) => _applicationView.ModelViewer.LoadExport(export);
    public void Overwrite(UMaterialInstance materialInstance)
    {
        if (_applicationView.ModelViewer.TryOverwriteMaterial(materialInstance))
        {
            _applicationView.CUE4Parse.ModelIsOverwritingMaterial = false;
        }
        else
        {
            MessageBox.Show(new MessageBoxModel
            {
                Text = "An attempt to load a material failed.",
                Caption = "Error",
                Icon = MessageBoxImage.Error,
                Buttons = MessageBoxButtons.OkCancel(),
                IsSoundEnabled = false
            });
        }
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        _applicationView.ModelViewer.Clear();
        _applicationView.ModelViewer.AppendMode = false;
        _applicationView.CUE4Parse.ModelIsOverwritingMaterial = false;
        MyAntiCrashGroup.ItemsSource = null; // <3
    }

    private async void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.W:
                _applicationView.ModelViewer.WirefreameToggle();
                break;
            case Key.H:
                _applicationView.ModelViewer.RenderingToggle();
                break;
            case Key.D:
                _applicationView.ModelViewer.DiffuseOnlyToggle();
                break;
            case Key.M:
                _applicationView.ModelViewer.MaterialColorToggle();
                break;
            case Key.Decimal:
                _applicationView.ModelViewer.FocusOnSelectedMesh();
                break;
            case Key.S when Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift):
                _applicationView.ModelViewer.SaveAsScene();
                break;
            case Key.S when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                await _applicationView.ModelViewer.SaveLoadedModels();
                break;
        }
    }

    private void OnMouse3DDown(object sender, MouseDown3DEventArgs e)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || e.HitTestResult.ModelHit is not CustomMeshGeometryModel3D m) return;
        _applicationView.ModelViewer.SelectedModel.SelectedGeometry = m;
        MaterialsListName.ScrollIntoView(m);
    }

    private void OnFocusClick(object sender, RoutedEventArgs e)
        => _applicationView.ModelViewer.FocusOnSelectedMesh();

    private void OnCopyClick(object sender, RoutedEventArgs e)
        => _applicationView.ModelViewer.CopySelectedMaterialName();

    private async void Save(object sender, RoutedEventArgs e)
        => await _applicationView.ModelViewer.SaveLoadedModels();

    private void OnOverwriteMaterialClick(object sender, RoutedEventArgs e)
    {
        _applicationView.CUE4Parse.ModelIsOverwritingMaterial = true;
        if (!_messageShown)
        {
            MessageBox.Show(new MessageBoxModel
            {
                Text = "Simply extract a material once FModel will be brought to the foreground. This message will be shown once per Model Viewer's lifetime, close it to begin.",
                Caption = "How To Overwrite Material?",
                Icon = MessageBoxImage.Information,
                IsSoundEnabled = false
            });
            _messageShown = true;
        }

        MainWindow.YesWeCats.Activate();
    }
}