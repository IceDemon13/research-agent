using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Store.Call
{
    /// <summary>
    /// Interaction logic for CreateCallView.xaml
    /// </summary>
    public partial class CreateCallView
    {
        public CreateCallView()
        {
            InitializeComponent();
        }

        private void CreateCallViewOnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => SubdivisionComboBox.Focus()));
        }
    }
}
