using FModel.ViewModels.Notifier;
using System.Diagnostics;
using System.Windows.Input;
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

            if (!string.IsNullOrEmpty(notifier.Path))
                OpenPath_Img.Visibility = System.Windows.Visibility.Visible;

            Bind(notifier);
        }

        private void NotificationDisplayPart_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is CustomNotifier c && c.DataContext is NotifierViewModel n)
            {
                if (!string.IsNullOrEmpty(n.Path))
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select, \"{n.Path.Replace('/', '\\')}\"", UseShellExecute = true });
            }
        }
    }
}
