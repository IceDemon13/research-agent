using System;
using System.Windows;
using System.Windows.Threading;

namespace Telemart.Client.Views.Directories.Employee
{
    /// <summary>
    /// Interaction logic for AddEmployeeOperationView.xaml
    /// </summary>
    public partial class AddEmployeeOperationView
    {
        public AddEmployeeOperationView()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => OperationLookUpEdit.Focus()));
        }
    }
}
