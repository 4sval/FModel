using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using FModel.Framework;
using FModel.Models;
using FModel.Services;
using System.Windows;
using FModel.Views;
using System.Collections.Generic;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using FModel;

namespace FModel.ViewModels;
public class DifferenceViewModel : ViewModel
{
    private readonly SnapshotService _snapshotService;
    private readonly ApplicationViewModel _applicationView;
    private readonly ThreadWorkerViewModel _threadWorkerView;

    public ObservableCollection<Snapshot> Snapshots { get; }
    public ICollectionView SnapshotsView { get; }

    private Snapshot _selectedSnapshotA;
    public Snapshot SelectedSnapshotA
    {
        get => _selectedSnapshotA;
        set => SetProperty(ref _selectedSnapshotA, value);
    }

    private Snapshot _selectedSnapshotB;
    public Snapshot SelectedSnapshotB
    {
        get => _selectedSnapshotB;
        set => SetProperty(ref _selectedSnapshotB, value);
    }

    public ObservableCollection<SnapshotFile> AddedFiles { get; } = new();
    public ObservableCollection<SnapshotFile> DeletedFiles { get; } = new();
    public ObservableCollection<SnapshotFilePair> ModifiedFiles { get; } = new();
    public ObservableCollection<SnapshotFileRename> RenamedFiles { get; } = new();

    private bool _isCreatingSnapshot;
    public bool IsCreatingSnapshot
    {
        get => _isCreatingSnapshot;
        set => SetProperty(ref _isCreatingSnapshot, value);
    }

    private double _snapshotCreationProgress;
    public double SnapshotCreationProgress
    {
        get => _snapshotCreationProgress;
        set => SetProperty(ref _snapshotCreationProgress, value);
    }

    private string _snapshotCreationProgressText;
    public string SnapshotCreationProgressText
    {
        get => _snapshotCreationProgressText;
        set => SetProperty(ref _snapshotCreationProgressText, value);
    }

    public DifferenceViewModel()
    {
        _applicationView = ApplicationService.ApplicationView;
        _snapshotService = new SnapshotService();
        _threadWorkerView = ApplicationService.ThreadWorkerView;

        Snapshots = new ObservableCollection<Snapshot>();
        SnapshotsView = new ListCollectionView(Snapshots)
        {
            SortDescriptions = { new SortDescription(nameof(Snapshot.CreatedAt), ListSortDirection.Descending) }
        };

        LoadSnapshots();
    }

    public void LoadSnapshots()
    {
        _ = _threadWorkerView.Begin(async _ =>
        {
            await _snapshotService.InitializeSnapshotDirectoryAsync();
            var snapshots = await _snapshotService.GetSnapshotsAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Snapshots.Clear();
                foreach (var snapshot in snapshots)
                {
                    Snapshots.Add(snapshot);
                }
            });
        });
    }

    public bool CanCreateSnapshot()
    {
        return _applicationView.CUE4Parse.Provider?.Files.Count > 0;
    }

    public void CreateSnapshot()
    {
        var dialog = new InputDialog();
        if (dialog.ShowDialog() == true)
        {
            var snapshotName = dialog.ResponseText;
            if (string.IsNullOrWhiteSpace(snapshotName))
            {
                snapshotName = $"Snapshot {DateTime.Now:yyyy-MM-dd HH-mm-ss}";
            }

            _ = _threadWorkerView.Begin(async _ =>
            {
                IsCreatingSnapshot = true;
                _applicationView.Status.SetStatus(EStatusKind.Loading, "Creating Snapshot...");
                try
                {
                    var progress = new Progress<(int processed, int total)>(p =>
                    {
                        SnapshotCreationProgress = (double)p.processed / p.total * 100;
                        SnapshotCreationProgressText = $"{p.processed} / {p.total}";
                    });

                    await _snapshotService.CreateSnapshotAsync(snapshotName, _applicationView.CUE4Parse.Provider.Files.Values, progress);
                    LoadSnapshots();
                    _applicationView.Status.SetStatus(EStatusKind.Completed, "Snapshot Created!");
                }
                catch (Exception ex)
                {
                    _applicationView.Status.SetStatus(EStatusKind.Failed, $"Error creating snapshot: {ex.Message}");
                }
                finally
                {
                    IsCreatingSnapshot = false;
                }
            });
        }
    }

    public bool CanCompareSnapshots()
    {
        return SelectedSnapshotA != null && SelectedSnapshotB != null && SelectedSnapshotA != SelectedSnapshotB;
    }

    public void CompareSnapshots()
    {
        if (!CanCompareSnapshots()) return;

        _ = _threadWorkerView.Begin(async _ =>
        {
            _applicationView.Status.SetStatus(EStatusKind.Loading, "Comparing Snapshots...");
            try
            {
                var result = await _snapshotService.CompareSnapshotsAsync(SelectedSnapshotA.FolderName, SelectedSnapshotB.FolderName);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    AddedFiles.Clear();
                    DeletedFiles.Clear();
                    ModifiedFiles.Clear();
                    RenamedFiles.Clear();

                    foreach (var file in result.AddedFiles) AddedFiles.Add(file);
                    foreach (var file in result.DeletedFiles) DeletedFiles.Add(file);
                    foreach (var pair in result.ModifiedFiles) ModifiedFiles.Add(pair);
                    foreach (var rename in result.RenamedFiles) RenamedFiles.Add(rename);
                });
                _applicationView.Status.SetStatus(EStatusKind.Completed, "Comparison Complete!");
            }
            catch (Exception ex)
            {
                _applicationView.Status.SetStatus(EStatusKind.Failed, $"Error comparing snapshots: {ex.Message}");
            }
        });
    }

    public void ViewFileDifferences(SnapshotFilePair selectedPair)
    {
        var comparisonViewModel = new FileComparisonViewModel(selectedPair.OldFile, selectedPair.NewFile);
        var comparisonWindow = new ComparisonWindow
        {
            DataContext = comparisonViewModel,
            Owner = Application.Current.MainWindow
        };
        comparisonWindow.Show();
    }
}