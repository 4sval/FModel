using FModel.Methods.MessageBox;
using FModel.Methods.Utilities;
using System.Windows;

namespace FModel
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string errorMessage = string.Format("An unhandled exception occurred: {0}", e.Exception.Message);
            DebugHelper.WriteException(e.Exception, "thrown in App.xaml.cs by OnDispatcherUnhandledException");
            DarkMessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
