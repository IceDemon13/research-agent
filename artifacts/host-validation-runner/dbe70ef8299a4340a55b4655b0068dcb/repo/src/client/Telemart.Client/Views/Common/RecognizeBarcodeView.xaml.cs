using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Telemart.Client.Views.Common
{
    /// <summary>
    /// Interaction logic for RecognizeBarcodeView.xaml
    /// </summary>
    public partial class RecognizeBarcodeView
    {
        private readonly CultureInfo inputLanguage;

        public RecognizeBarcodeView()
        {
            InitializeComponent();

            inputLanguage = new CultureInfo("en-US");
        }

        private void BarcodeTextEditOnGotFocus(object sender, RoutedEventArgs e)
        {
            InputLanguageManager.Current.CurrentInputLanguage = inputLanguage;
        }

        private void FocusBarcodeTextEdit()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { BarcodeTextEdit.Focus(); }));
        }

        private void RecognizeBarcodeViewOnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                FocusBarcodeTextEdit();
            }
        }

        private void RecognizeBarcodeViewOnLoaded(object sender, RoutedEventArgs e)
        {
            FocusBarcodeTextEdit();
        }
    }
}
