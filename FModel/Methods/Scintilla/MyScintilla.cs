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
        private static void KeyPressed(object sender, KeyEventArgs e)
        {
            KeyDown(sender, e);
        }

        /// <summary>
        /// Set styles to a ScintillaNET control and disable some keybinds
        /// </summary>
        /// <param name="theBox"></param>
        public static void SetScintillaStyle(Scintilla theBox)
        {
            theBox.Styles[Style.Json.Default].ForeColor = Color.Silver;
            theBox.Styles[Style.Json.BlockComment].ForeColor = Color.FromArgb(0, 128, 0);
            theBox.Styles[Style.Json.LineComment].ForeColor = Color.FromArgb(0, 128, 0);
            theBox.Styles[Style.Json.Number].ForeColor = Color.Green;
            theBox.Styles[Style.Json.PropertyName].ForeColor = Color.SteelBlue;
            theBox.Styles[Style.Json.String].ForeColor = Color.OrangeRed;
            theBox.Styles[Style.Json.StringEol].BackColor = Color.OrangeRed;
            theBox.Styles[Style.Json.Operator].ForeColor = Color.Black;
            theBox.Styles[Style.LineNumber].ForeColor = Color.DarkGray;
            var nums = theBox.Margins[1];
            nums.Width = 30;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;

            theBox.ClearCmdKey(Keys.Control | Keys.F);
            theBox.ClearCmdKey(Keys.Control | Keys.Z);
            theBox.Lexer = Lexer.Json;
        }
    }
}
