using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DevExpress.Xpf.Editors;

namespace Telemart.Client.Views.Store
{
    /// <summary>
    ///     Interaction logic for ProductSerialsView.xaml
    /// </summary>
    public partial class ProductScanSerialsView
    {
        private readonly CultureInfo inputLanguage = new CultureInfo("en-US");

        public ProductScanSerialsView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InputLanguageManager.Current.CurrentInputLanguage = inputLanguage;

            Action action;

            if (SnTextEdit.IsEnabled)
            {
                action = () => SnTextEdit.Focus();
            }
            else
            {
                action = () => OkButton.Focus();
            }

            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, action);
        }

        private void ScanModeComboboxEditOnEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => SnTextEdit.Focus()));
        }

        private void SnTextEditOnGotFocus(object sender, RoutedEventArgs e)
        {
            InputLanguageManager.Current.CurrentInputLanguage = inputLanguage;
        }
    }
}