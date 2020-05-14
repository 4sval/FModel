using FModel.Windows.SoundPlayer.Visualization;
using System.Windows.Controls;

namespace FModel.Windows.SoundPlayer.UserControls
{
    /// <summary>
    /// Logique d'interaction pour Timeclock.xaml
    /// </summary>
    public partial class Timeclock : UserControl
    {
        public Timeclock(OutputSource output)
        {
            InitializeComponent();
            NotTimeclock.SourceCollection.Add(output);
        }
    }
}
