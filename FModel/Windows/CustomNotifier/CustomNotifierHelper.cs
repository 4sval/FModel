using FModel.ViewModels.Notifier;
using ToastNotifications;
using ToastNotifications.Core;

namespace FModel.Windows.CustomNotifier
{
    public static class CustomNotifierHelper
    {
        public static void ShowCustomMessage(this Notifier notifier, string title, string message, string icon = null, string path = null, MessageOptions messageOptions = null)
        {
            notifier.Notify(() => new NotifierViewModel(title, message, string.IsNullOrEmpty(icon) ? "/FModel;component/FModel.ico" : icon, path, messageOptions));
        }
    }
}
