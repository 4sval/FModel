using FModel.ViewModels.Notifier;
using ToastNotifications.Core;

namespace FModel.Windows.CustomNotifier
{
    /// <summary>
    /// Logique d'interaction pour CustomNotifier.xaml
    /// </summary>
    public partial class CustomNotifier : NotificationDisplayPart
    {
        public CustomNotifier(NotifierViewModel notifier)
        {
            InitializeComponent();
            Bind(notifier);
        }
    }
}
