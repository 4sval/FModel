using System.Windows.Input;

namespace FModel.Utils
{
    static class Commands
    {
        public static readonly RoutedUICommand OpenGeneralSettings = new RoutedUICommand(string.Empty, "OpenGeneralSettings", typeof(MainWindow));
        public static readonly RoutedUICommand OpenSearchWindow = new RoutedUICommand(string.Empty, "OpenSearchWindow", typeof(MainWindow));
        public static readonly RoutedUICommand OpenOutputFolder = new RoutedUICommand(string.Empty, "OpenOutputFolder", typeof(MainWindow));
        public static readonly RoutedUICommand AutoExport = new RoutedUICommand(string.Empty, "AutoExport", typeof(MainWindow));
        public static readonly RoutedUICommand AutoSave = new RoutedUICommand(string.Empty, "AutoSave", typeof(MainWindow));
        public static readonly RoutedUICommand AutoSaveImage = new RoutedUICommand(string.Empty, "AutoSaveImage", typeof(MainWindow));
        public static readonly RoutedUICommand OpenImageDoubleClick = new RoutedUICommand(string.Empty, "OpenImageDoubleClick", typeof(MainWindow));
    }
}
