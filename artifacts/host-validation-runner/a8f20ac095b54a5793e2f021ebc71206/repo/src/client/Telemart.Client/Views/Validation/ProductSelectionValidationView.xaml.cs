using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Validation
{
    /// <summary>
    /// Interaction logic for ValidationResultCountdownView.xaml
    /// </summary>
    public partial class ProductSelectionValidationView
    {
        public ProductSelectionValidationView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { CancelButton.Focus(); }));
        }
    }
}
