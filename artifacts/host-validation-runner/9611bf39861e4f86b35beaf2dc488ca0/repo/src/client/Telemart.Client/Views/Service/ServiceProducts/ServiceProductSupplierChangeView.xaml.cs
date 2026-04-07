using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Telemart.Client.Views.Service.ServiceProducts
{
    /// <summary>
    /// Interaction logic for ServiceProductSupplierChangeView.xaml
    /// </summary>
    public partial class ServiceProductSupplierChangeView
    {
        private readonly CultureInfo inputLanguage = new CultureInfo("en-US");

        public ServiceProductSupplierChangeView()
        {
            InitializeComponent();
        }

        private void SnTextEditOnGotFocus(object sender, RoutedEventArgs e)
        {
            InputLanguageManager.Current.CurrentInputLanguage = inputLanguage;
        }
    }
}
