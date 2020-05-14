using FModel.Windows.SoundPlayer.Visualization;
using System.Windows.Controls;

namespace FModel.Windows.SoundPlayer.UserControls
{
    /// <summary>
    /// Logique d'interaction pour Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl, ISample
    {
        public Timeline(OutputSource output)
        {
            InitializeComponent();
            NotTimeline.Source = output;
        }
    }
}
