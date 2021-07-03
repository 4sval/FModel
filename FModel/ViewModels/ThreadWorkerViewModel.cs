using FModel.Framework;
using FModel.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using FModel.Views.Resources.Controls;
using Serilog;

namespace FModel.ViewModels
{
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

        public ThreadWorkerViewModel()
        {
            _jobs = new AsyncQueue<Action<CancellationToken>>();
        }

        public async Task Begin(Action<CancellationToken> action)
        {
            if (!_applicationView.IsReady)
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
                _applicationView.Status = EStatusKind.Loading;
                await foreach (var job in _jobs)
                {
                    try
                    {
                        // will end in "catch" if canceled
                        await Task.Run(() => job(CurrentCancellationTokenSource.Token));
                    }
                    catch (OperationCanceledException)
                    {
                        _applicationView.Status = EStatusKind.Stopped;
                        CurrentCancellationTokenSource = null; // kill token
                        OperationCancelled = true;
                        OperationCancelled = false;
                        return;
                    }
                    catch (Exception e)
                    {
                        _applicationView.Status = EStatusKind.Failed;
                        CurrentCancellationTokenSource = null; // kill token

                        Log.Error("{Exception}", e);
                        
                        FLogger.AppendError();
                        FLogger.AppendText(e.Message, Constants.WHITE, true);
                        return;
                    }
                }

                _applicationView.Status = EStatusKind.Completed;
                CurrentCancellationTokenSource = null; // kill token
            }
        }

        public void SignalOperationInProgress()
        {
            StatusChangeAttempted = true;
            StatusChangeAttempted = false;
        }
    }
}