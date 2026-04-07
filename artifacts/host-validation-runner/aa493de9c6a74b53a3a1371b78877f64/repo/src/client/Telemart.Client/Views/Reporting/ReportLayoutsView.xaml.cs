using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Reporting
{
    /// <summary>
    /// Interaction logic for ReportLayoutsView.xaml
    /// </summary>
    public partial class ReportLayoutsView
    {
        public ReportLayoutsView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { GridControl.View.SearchControl.Focus(); }));
        }
    }
}
