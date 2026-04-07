using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Telemart.Client.Views.Common
{
    public partial class PrintOurBarcodeView : UserControl
    {
        public PrintOurBarcodeView()
        {
            InitializeComponent();
        }

        private void BarcodeFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => SerialNumberTextBox.Focus()));
        }
    }
}