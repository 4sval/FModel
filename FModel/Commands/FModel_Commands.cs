using System.Windows.Input;

namespace FModel.Commands
{
    static class FModel_Commands
    {
        public static readonly RoutedUICommand OpenSettings = new RoutedUICommand("Open Settings Window", "OpenSettings", typeof(MainWindow));
        public static readonly RoutedUICommand OpenSearch = new RoutedUICommand("Open Search Window", "OpenSearch", typeof(MainWindow));
        public static readonly RoutedUICommand OpenOutput = new RoutedUICommand("Open Output Folder", "OpenOutput", typeof(MainWindow));
    }
}
