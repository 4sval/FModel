using System.Windows.Input;

namespace FModel.Commands
{
    class FModel_Commands
    {
        public static RoutedUICommand OpenSettings = new RoutedUICommand("Open Settings Window", "OpenSettings", typeof(MainWindow));
        public static RoutedUICommand OpenOutput = new RoutedUICommand("Open Output Folder", "OpenOutput", typeof(MainWindow));
    }
}
