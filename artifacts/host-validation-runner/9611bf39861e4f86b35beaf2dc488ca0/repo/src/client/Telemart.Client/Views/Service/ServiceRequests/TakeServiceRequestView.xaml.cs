using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Telemart.Client.Views.Service.ServiceRequests
{
    /// <summary>
    /// Interaction logic for TakeServiceRequestView.xaml
    /// </summary>
    public partial class TakeServiceRequestView
    {
        private readonly CultureInfo inputLanguage;

        public TakeServiceRequestView()
        {
            InitializeComponent();

            inputLanguage = new CultureInfo("en-US");
        }

        private void TextEditOnGotFocus(object sender, RoutedEventArgs e)
        {
            InputLanguageManager.Current.CurrentInputLanguage = inputLanguage;
        }
    }
}
