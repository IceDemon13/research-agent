using System;
using System.Windows.Threading;

namespace Telemart.Client.Views.Directories.Organization
{
    /// <summary>
    /// Interaction logic for OrganizationContactView.xaml
    /// </summary>
    public partial class OrganizationContactView
    {
        public OrganizationContactView()
        {
            InitializeComponent();
        }

        private void OrganizationContactViewLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => EmployeeComboBox.Focus()));
        }
    }
}
