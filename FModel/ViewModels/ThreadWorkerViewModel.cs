using System;
using System.Threading;
using System.Threading.Tasks;
using FModel.Framework;
using FModel.Services;
using FModel.Views.Resources.Controls;
using Serilog;

namespace FModel.ViewModels;

public class ThreadWorkerViewModel : ViewModel
{
    private bool _statusChangeAttempted;
    public bool StatusChangeAttempted
    {
        get => _statusChangeAttempted;
        private set => SetProperty(ref _statusChangeAttempted, value);
    }

    private bool _operationCancelled;
    public bool OperationCancelled
    {
        get => _operationCancelled;
        private set => SetProperty(ref _operationCancelled, value);
    }

    private CancellationTokenSource _currentCancellationTokenSource;
    public CancellationTokenSource CurrentCancellationTokenSource
    {
        get => _currentCancellationTokenSource;
        set
        {
            if (_currentCancellationTokenSource == value) return;
            SetProperty(ref _currentCancellationTokenSource, value);
            RaisePropertyChanged("CanBeCanceled");
        }
    }

    public bool CanBeCanceled => CurrentCancellationTokenSource != null;

    private ApplicationViewModel _applicationView => ApplicationService.ApplicationView;
    private readonly AsyncQueue<Action<CancellationToken>> _jobs;
    private const string _at = "   at ";
    private const char _dot = '.';
    private const char _colon = ':';
    private const string _gray = "#999";

    public ThreadWorkerViewModel()
    {
        _jobs = new AsyncQueue<Action<CancellationToken>>();
    }

    public async Task Begin(Action<CancellationToken> action)
    {
        if (_applicationView.CUE4Parse.IsSnooperOpen)
            _applicationView.CUE4Parse.SnooperViewer.Close();
        else if (!_applicationView.Status.IsReady)
        {
            SignalOperationInProgress();
            return;
        }

        CurrentCancellationTokenSource ??= new CancellationTokenSource();
        _jobs.Enqueue(action);
        await ProcessQueues();
    }

    public void Cancel()
    {
        if (!CanBeCanceled)
        {
            SignalOperationInProgress();
            return;
        }

        CurrentCancellationTokenSource.Cancel();
    }

    private async Task ProcessQueues()
    {
        if (_jobs.Count > 0)
        {
            _applicationView.Status.SetStatus(EStatusKind.Loading);
            await foreach (var job in _jobs)
            {
                try
                {
                    // will end in "catch" if canceled
                    await Task.Run(() => job(CurrentCancellationTokenSource.Token));
                }
                catch (OperationCanceledException)
                {
                    _applicationView.Status.SetStatus(EStatusKind.Stopped);
                    if (_applicationView.CUE4Parse.IsSnooperOpen)
                        _applicationView.CUE4Parse.SnooperViewer.Close();
                    CurrentCancellationTokenSource = null; // kill token
                    OperationCancelled = true;
                    OperationCancelled = false;
                    return;
                }
                catch (Exception e)
                {
                    _applicationView.Status.SetStatus(EStatusKind.Failed);
                    CurrentCancellationTokenSource = null; // kill token

                    Log.Error("{Exception}", e);

                    FLogger.AppendError();
                    if ((e.InnerException ?? e) is { TargetSite.DeclaringType: not null } exception)
                    {
                        if (exception.TargetSite.ToString() == "CUE4Parse.FileProvider.GameFile get_Item(System.String)")
                        {
                            FLogger.AppendText(e.Message, Constants.WHITE, true);
                        }
                        else
                        {
                            var t = exception.GetType();
                            FLogger.AppendText(t.Namespace + _dot, Constants.GRAY);
                            FLogger.AppendText(t.Name, Constants.WHITE);
                            FLogger.AppendText(_colon + " ", Constants.GRAY);
                            FLogger.AppendText(exception.Message, Constants.RED, true);

                            FLogger.AppendText(_at, _gray);
                            FLogger.AppendText(exception.TargetSite.DeclaringType.FullName + _dot, Constants.GRAY);
                            FLogger.AppendText(exception.TargetSite.Name, Constants.YELLOW);

                            var p = exception.TargetSite.GetParameters();
                            var parameters = new string[p.Length];
                            for (int i = 0; i < parameters.Length; i++)
                            {
                                parameters[i] = p[i].ParameterType.Name + " " + p[i].Name;
                            }
                            FLogger.AppendText("(" + string.Join(", ", parameters) + ")", Constants.GRAY, true);
                        }
                    }
                    return;
                }
            }

            _applicationView.Status.SetStatus(EStatusKind.Completed);
            CurrentCancellationTokenSource = null; // kill token
        }
    }

    public void SignalOperationInProgress()
    {
        StatusChangeAttempted = true;
        StatusChangeAttempted = false;
    }
}
