using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Telemart.Client.Views.Store.Order
{
    /// <summary>
    /// Interaction logic for CreatePresaleOrderView.xaml
    /// </summary>
    public partial class CreatePresaleOrderView
    {
        private readonly CultureInfo inputLanguage = new CultureInfo("en-US");

        public CreatePresaleOrderView()
        {
            InitializeComponent();
        }

        private void SnTextEditOnGotFocus(object sender, RoutedEventArgs e)
        {
            InputLanguageManager.Current.CurrentInputLanguage = inputLanguage;
        }
    }
}
