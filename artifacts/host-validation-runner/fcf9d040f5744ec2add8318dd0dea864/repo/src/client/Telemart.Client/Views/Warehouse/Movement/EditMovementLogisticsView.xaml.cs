using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Warehouse.Movement
{
    /// <summary>
    /// Interaction logic for EditMovementLogisticsView.xaml
    /// </summary>
    public partial class EditMovementLogisticsView
    {
        public EditMovementLogisticsView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => CarryComboBox.Focus()));
        }
    }
}
