using ScintillaNET_FindReplaceDialog;
using ScintillaNET;
using System.Windows.Forms;
using System.Drawing;

namespace FModel
{
    static class MyScintilla
    {
        private static FindReplace _myFindReplace { get; set; }
        private static GoTo _myGoTo { get; set; }

        /// <summary>
        /// Create instance of FindReplace with reference to a ScintillaNET control.
        /// </summary>
        /// <param name="theBox"></param>
        public static void ScintillaInstance(Scintilla theBox)
        {
            _myFindReplace = new FindReplace(theBox);
            _myFindReplace.Window.StartPosition = FormStartPosition.CenterScreen;
            _myGoTo = new GoTo(theBox);

            theBox.KeyDown += KeyDown;
        }

        /// <summary>
        /// just the allowed keybinds and what they have to do
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                _myFindReplace.ShowFind();
                e.SuppressKeyPress = true;
            }
            else if (e.Shift && e.KeyCode == Keys.F3)
            {
                _myFindReplace.Window.FindPrevious();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                _myFindReplace.Window.FindNext();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.H)
            {
                _myFindReplace.ShowReplace();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.I)
            {
                _myFindReplace.ShowIncrementalSearch();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.G)
            {
                _myGoTo.ShowGoToDialog();
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Set styles to a ScintillaNET control and disable some keybinds
        /// </summary>
        /// <param name="theBox"></param>
        public static void SetScintillaStyle(Scintilla theBox)
        {
            theBox.Styles[Style.Json.Default].ForeColor = Color.Silver;
            theBox.Styles[Style.Json.Number].ForeColor = Color.Goldenrod;
            theBox.Styles[Style.Json.String].ForeColor = Color.RoyalBlue;
            theBox.Styles[Style.Json.StringEol].ForeColor = Color.RoyalBlue;
            theBox.Styles[Style.Json.PropertyName].ForeColor = Color.Crimson;
            theBox.Styles[Style.Json.LineComment].ForeColor = Color.DarkGray;
            theBox.Styles[Style.Json.BlockComment].ForeColor = Color.DarkGray;
            theBox.Styles[Style.Json.Uri].ForeColor = Color.Peru;
            theBox.Styles[Style.Json.CompactIRI].ForeColor = Color.Peru;
            theBox.Styles[Style.LineNumber].ForeColor = Color.DarkGray;
            var nums = theBox.Margins[1];
            nums.Width = 30;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;

            theBox.SetProperty("fold", "1");
            theBox.SetProperty("fold.compact", "1");
            theBox.Margins[2].Type = MarginType.Symbol;
            theBox.Margins[2].Mask = Marker.MaskFolders;
            theBox.Margins[2].Sensitive = true;
            theBox.Margins[2].Width = 20;
            for (int i = 25; i <= 31; i++)
            {
                theBox.Markers[i].SetForeColor(SystemColors.ControlLightLight);
                theBox.Markers[i].SetBackColor(SystemColors.ControlDark);
            }
            theBox.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            theBox.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            theBox.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            theBox.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            theBox.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
            theBox.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            theBox.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            theBox.ClearCmdKey(Keys.Control | Keys.F);
            theBox.ClearCmdKey(Keys.Control | Keys.Z);
        }
    }
}
