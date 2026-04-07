using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Common
{
    /// <summary>
    /// Interaction logic for UsageReasonView.xaml
    /// </summary>
    public partial class UsageReasonView
    {
        public UsageReasonView()
        {
            InitializeComponent();
        }

        private void UsageReasonViewLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => UsageReasonMemo.Focus()));
        }
    }
}
