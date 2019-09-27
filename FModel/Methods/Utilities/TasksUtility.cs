using System;
using System.Threading;

namespace FModel.Methods.Utilities
{
    static class TasksUtility
    {
        public static CancellationTokenSource CancellableTaskTokenSource { get; set; }

        public static void TaskCompleted(Exception ex)
        {
            if (ex != null)
            {
                Exception innerEx = ex.InnerException;
                if (innerEx is ArgumentOutOfRangeException) //aes key is too short
                {
                    new UpdateMyProcessEvents((innerEx as ArgumentOutOfRangeException).ParamName, "Error").Update();
                }
                else
                {
                    new UpdateMyProcessEvents(innerEx.Message, "Error").Update();
                }
            }
        }
    }
}
