using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Telemart.Client.Views.Common
{
    public partial class PrintSnView : UserControl
    {
        public PrintSnView()
        {
            InitializeComponent();
        }

        private void SnFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => SerialNumberTextBox.Focus()));
        }
    }
}