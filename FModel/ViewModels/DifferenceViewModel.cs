using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using AdonisUI.Controls;
using FModel.Framework;
using FModel.Models;
using FModel.Services;
using FModel.Views;

namespace FModel.ViewModels
{
    public class DifferenceViewModel : ViewModel
    {
        private readonly SnapshotService _snapshotService;
        private readonly ApplicationViewModel _applicationView;
        private readonly ThreadWorkerViewModel _threadWorkerView;

        public string CurrentSessionFileCount { get; }
        public string CurrentSessionVersion { get; }
        public string CurrentSessionDirectory { get; }

        public ObservableCollection<Snapshot> Snapshots { get; }
        public ICollectionView SnapshotsView { get; }

        private Snapshot _selectedSnapshotA;
        public Snapshot SelectedSnapshotA
        {
            get => _selectedSnapshotA;
            set
            {
                SetProperty<Snapshot>(ref _selectedSnapshotA, value);
                RaisePropertyChanged(nameof(CanCompareSnapshots));
            }
        }

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

        public ObservableCollection<SnapshotFile> AddedFiles { get; } = new();
        public ObservableCollection<SnapshotFilePair> ModifiedFiles { get; } = new();
        public ObservableCollection<SnapshotFile> DeletedFiles { get; } = new();
        public ObservableCollection<SnapshotFileRename> RenamedFiles { get; } = new();

        public DifferenceViewModel()
        {
            _applicationView = ApplicationService.ApplicationView;
            _snapshotService = new SnapshotService();
            _threadWorkerView = ApplicationService.ThreadWorkerView;

            // Populate session info safely
            if (_applicationView?.CUE4Parse?.Provider != null)
            {
                CurrentSessionFileCount = _applicationView.CUE4Parse.Provider.Files?.Count.ToString("N0") ?? "N/A";
                CurrentSessionVersion = _applicationView.CUE4Parse.Provider.Versions?.Game.ToString() ?? "N/A";
            }
            else
            {
                CurrentSessionFileCount = "N/A";
                CurrentSessionVersion = "N/A";
            }
            CurrentSessionDirectory = FModel.Settings.UserSettings.Default.CurrentDir?.GameDirectory ?? "N/A";

            Snapshots = new ObservableCollection<Snapshot>();
            SnapshotsView = new ListCollectionView(Snapshots)
            {
                SortDescriptions = { new SortDescription(nameof(Snapshot.CreatedAt), ListSortDirection.Descending) }
            };

            LoadSnapshots();
        }

        public void LoadSnapshots()
        {
            if (_threadWorkerView == null) return;
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
            return SelectedSnapshotA != null;
        }

        public void CompareSnapshots()
        {
            if (!CanCompareSnapshots()) return;

            _ = _threadWorkerView.Begin(async _ =>
            {
                _applicationView.Status.SetStatus(EStatusKind.Loading, "Comparing to current version...");
                try
                {
                    var result = await _snapshotService.CompareProviderToSnapshotAsync(SelectedSnapshotA, _applicationView.CUE4Parse.Provider);

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
            var comparisonViewModel = new FileComparisonViewModel(selectedPair.OldFile, selectedPair.NewFile, SelectedSnapshotA.FolderName, "Live");
            var comparisonWindow = new ComparisonWindow
            {
                DataContext = comparisonViewModel,
                Owner = Application.Current.MainWindow
            };
            comparisonWindow.Show();
        }

        public void TestLoadSnapshot()
        {
            if (SelectedSnapshotA == null) return;

            _ = _threadWorkerView.Begin(_ =>
            {
                try
                {
                    var snapshotDirectory = System.IO.Path.Combine(FModel.Settings.UserSettings.Default.OutputDirectory, "Snapshots", SelectedSnapshotA.FolderName);
                    var mainProvider = _applicationView.CUE4Parse.Provider;

                    var snapshotProvider = new CUE4Parse.FileProvider.DefaultFileProvider(new System.IO.DirectoryInfo(snapshotDirectory), System.IO.SearchOption.AllDirectories, mainProvider?.Versions);
                    snapshotProvider.ReadScriptData = mainProvider.ReadScriptData;
                    snapshotProvider.ReadShaderMaps = mainProvider.ReadShaderMaps;
                    snapshotProvider.ReadNaniteData = mainProvider.ReadNaniteData;
                    snapshotProvider.Initialize();

                    if (mainProvider?.Keys != null) snapshotProvider.SubmitKeys(mainProvider.Keys);
                    if (mainProvider?.MappingsContainer != null) snapshotProvider.MappingsContainer = mainProvider.MappingsContainer;

                    Application.Current.Dispatcher.Invoke(() => AdonisUI.Controls.MessageBox.Show($"Successfully loaded snapshot '{SelectedSnapshotA.Name}'.\nProvider found {snapshotProvider.Files.Count} files.", "Test Load Successful"));
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() => AdonisUI.Controls.MessageBox.Show($"Failed to load snapshot '{SelectedSnapshotA.Name}'.\n\nError: {ex.Message}", "Test Load Failed", AdonisUI.Controls.MessageBoxButton.OK, AdonisUI.Controls.MessageBoxImage.Error));
                }
            });
        }
    }
}