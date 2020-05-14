using FModel.Logger;
using FModel.ViewModels.StatusBar;
using System;
using System.Threading;

namespace FModel.Utils
{
    static class Tasks
    {
        public static CancellationTokenSource TokenSource { get; set; }

        public static void TaskCompleted(Exception ex)
        {
            Exception innerEx = ex.InnerException;
            DebugHelper.WriteException(innerEx, "[FModel] [Task] [Exception]");

            if (innerEx is ArgumentOutOfRangeException)
                StatusBarVm.statusBarViewModel.Set((innerEx as ArgumentOutOfRangeException).ParamName, Properties.Resources.Error);
            else
                StatusBarVm.statusBarViewModel.Set(innerEx.Message, Properties.Resources.Error);
        }
    }
}
