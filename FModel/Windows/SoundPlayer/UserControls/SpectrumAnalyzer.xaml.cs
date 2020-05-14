using FModel.Windows.SoundPlayer.Visualization;
using System.Windows.Controls;

namespace FModel.Windows.SoundPlayer.UserControls
{
    /// <summary>
    /// Logique d'interaction pour SpectrumAnalyzer.xaml
    /// </summary>
    public partial class SpectrumAnalyzer : UserControl
    {
        public SpectrumAnalyzer()
        {
            InitializeComponent();
        }

        public SpectrumAnalyzer(OutputSource output)
        {
            InitializeComponent();
            NotSpectrumAnalyzer.Source = output;
        }
    }
}
