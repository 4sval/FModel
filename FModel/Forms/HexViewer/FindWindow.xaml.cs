using System.IO;
using System.Windows;

namespace WpfHexaEditor.Dialog
{
    /// <summary>
    /// Logique d'interaction pour FindWindow.xaml
    /// </summary>
    public partial class FindWindow
    {
        private MemoryStream _findMs = new MemoryStream(1);
        private readonly HexEditor _parent;

        public FindWindow(HexEditor parent, byte[] findData = null)
        {
            InitializeComponent();

            //Parent hexeditor for "binding" search
            _parent = parent;

            InitializeMStream(findData);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
        private void ClearButton_Click(object sender, RoutedEventArgs e) => InitializeMStream();

        private void FindHexEdit_BytesDeleted(object sender, System.EventArgs e) =>
            InitializeMStream(FindHexEdit.GetAllBytes());

        private void FindAllButton_Click(object sender, RoutedEventArgs e) =>
            _parent?.FindAll(FindHexEdit.GetAllBytes(), true);

        private void FindFirstButton_Click(object sender, RoutedEventArgs e) =>
            _parent?.FindFirst(FindHexEdit.GetAllBytes());

        private void FindNextButton_Click(object sender, RoutedEventArgs e) =>
            _parent?.FindNext(FindHexEdit.GetAllBytes());

        private void FindLastButton_Click(object sender, RoutedEventArgs e) =>
            _parent?.FindLast(FindHexEdit.GetAllBytes());

        /// <summary>
        /// Initialize stream and hexeditor
        /// </summary>
        private void InitializeMStream(byte[] findData = null)
        {
            FindHexEdit.CloseProvider();

            _findMs = new MemoryStream(1);

            if (findData != null && findData.Length > 0)
                foreach (byte b in findData)
                    _findMs.WriteByte(b);
            else
                _findMs.WriteByte(0);

            FindHexEdit.Stream = _findMs;
        }
    }
}
