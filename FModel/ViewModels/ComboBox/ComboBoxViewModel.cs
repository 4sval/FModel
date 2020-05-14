using System.Collections.ObjectModel;

namespace FModel.ViewModels.ComboBox
{
    static class ComboBoxVm
    {
        public static ObservableCollection<ComboBoxViewModel> languageCbViewModel = new ObservableCollection<ComboBoxViewModel>
        {
            new ComboBoxViewModel { Id = 0, Content = Properties.Resources.English, Property = ELanguage.English },
            new ComboBoxViewModel { Id = 1, Content = Properties.Resources.French, Property = ELanguage.French },
            new ComboBoxViewModel { Id = 2, Content = Properties.Resources.German, Property = ELanguage.German },
            new ComboBoxViewModel { Id = 3, Content = Properties.Resources.Italian, Property = ELanguage.Italian },
            new ComboBoxViewModel { Id = 4, Content = Properties.Resources.Spanish, Property = ELanguage.Spanish },
            new ComboBoxViewModel { Id = 5, Content = Properties.Resources.SpanishLatin, Property = ELanguage.SpanishLatin },
            new ComboBoxViewModel { Id = 6, Content = Properties.Resources.Arabic, Property = ELanguage.Arabic },
            new ComboBoxViewModel { Id = 7, Content = Properties.Resources.Japanese, Property = ELanguage.Japanese },
            new ComboBoxViewModel { Id = 8, Content = Properties.Resources.Korean, Property = ELanguage.Korean },
            new ComboBoxViewModel { Id = 9, Content = Properties.Resources.Polish, Property = ELanguage.Polish },
            new ComboBoxViewModel { Id = 10, Content = Properties.Resources.PortugueseBrazil, Property = ELanguage.PortugueseBrazil },
            new ComboBoxViewModel { Id = 11, Content = Properties.Resources.Russian, Property = ELanguage.Russian },
            new ComboBoxViewModel { Id = 12, Content = Properties.Resources.Turkish, Property = ELanguage.Turkish },
            new ComboBoxViewModel { Id = 13, Content = Properties.Resources.Chinese, Property = ELanguage.Chinese },
            new ComboBoxViewModel { Id = 14, Content = Properties.Resources.TraditionalChinese, Property = ELanguage.TraditionalChinese }
        };

        public static ObservableCollection<ComboBoxViewModel> designCbViewModel = new ObservableCollection<ComboBoxViewModel>
        {
            new ComboBoxViewModel { Id = 0, Content = Properties.Resources.Default, Property = EIconDesign.Default },
            new ComboBoxViewModel { Id = 1, Content = Properties.Resources.NoText, Property = EIconDesign.NoText },
            new ComboBoxViewModel { Id = 2, Content = Properties.Resources.Minimalist, Property = EIconDesign.Mini },
            new ComboBoxViewModel { Id = 3, Content = Properties.Resources.Flat, Property = EIconDesign.Flat }
        };

        public static ObservableCollection<ComboBoxViewModel> gamesCbViewModel = new ObservableCollection<ComboBoxViewModel>();
    }

    public class ComboBoxViewModel : PropertyChangedBase
    {
        private string _content;
        public string Content
        {
            get { return _content; }

            set { this.SetProperty(ref this._content, value); }
        }

        private int _id;
        public int Id
        {
            get { return _id; }

            set { this.SetProperty(ref this._id, value); }
        }

        private object _property;
        public object Property
        {
            get { return _property; }

            set { this.SetProperty(ref this._property, value); }
        }
    }
}
